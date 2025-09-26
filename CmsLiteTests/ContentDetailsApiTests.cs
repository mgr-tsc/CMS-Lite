using System.Net;
using System.Text.Json;
using CmsLite.Helpers.RequestMappers;
using CmsLiteTests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CmsLiteTests;

public class ContentDetailsApiTests : IAsyncDisposable
{
    private readonly CmsLiteTestFactoryAuth factory = new();

    public async ValueTask DisposeAsync() => await factory.DisposeAsync();

    [Fact]
    public async Task GetContentDetails_ValidResource_ReturnsDetailedInformation()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Create content first
        var contentData = JsonSerializer.Serialize(new { message = "Test content for details", timestamp = DateTime.UtcNow });
        var putResponse = await client.PutAsync($"/v1/{factory.TestTenant}/test-resource.json",
            new StringContent(contentData, System.Text.Encoding.UTF8, "application/json"));

        Assert.True(putResponse.StatusCode == HttpStatusCode.OK || putResponse.StatusCode == HttpStatusCode.Created);

        // Get detailed information
        var response = await client.GetAsync($"/v1/{factory.TestTenant}/test-resource.json/details");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var details = JsonSerializer.Deserialize<ContentDetailsResponse>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(details);
        Assert.Equal("test-resource.json", details.Resource);
        Assert.Equal(1, details.LatestVersion);
        Assert.Equal("application/json", details.ContentType);
        Assert.True(details.ByteSize > 0);
        Assert.False(details.IsDeleted);

        // Check directory information
        Assert.NotNull(details.Directory);
        Assert.False(string.IsNullOrEmpty(details.Directory.Id));
        Assert.Equal(0, details.Directory.Level); // Root directory
        Assert.Equal("/", details.Directory.FullPath); // Root path

        // Check version information
        Assert.NotNull(details.Versions);
        Assert.Single(details.Versions); // Should have 1 version
        Assert.Equal(1, details.Versions[0].Version);
        Assert.Equal(details.ByteSize, details.Versions[0].ByteSize);

        // Check metadata
        Assert.NotNull(details.Metadata);
        Assert.Equal(CmsLiteTestFactoryAuth.TestTenantId, details.Metadata.TenantId);
        Assert.Equal(factory.TestTenant, details.Metadata.TenantName);
        Assert.False(details.Metadata.HasMultipleVersions);
        Assert.Equal(1, details.Metadata.TotalVersions);
        Assert.Equal(".json", details.Metadata.FileExtension);
        Assert.False(string.IsNullOrEmpty(details.Metadata.ReadableSize));
    }

    [Fact]
    public async Task GetContentDetails_MultipleVersions_ReturnsAllVersions()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Create initial content
        var contentV1 = JsonSerializer.Serialize(new { version = 1, data = "first version" });
        var putResponse1 = await client.PutAsync($"/v1/{factory.TestTenant}/versioned-resource",
            new StringContent(contentV1, System.Text.Encoding.UTF8, "application/json"));
        Assert.True(putResponse1.StatusCode == HttpStatusCode.OK || putResponse1.StatusCode == HttpStatusCode.Created);

        // Update content to create version 2
        var contentV2 = JsonSerializer.Serialize(new { version = 2, data = "second version with more data" });
        var putResponse2 = await client.PutAsync($"/v1/{factory.TestTenant}/versioned-resource",
            new StringContent(contentV2, System.Text.Encoding.UTF8, "application/json"));
        Assert.True(putResponse2.StatusCode == HttpStatusCode.OK || putResponse2.StatusCode == HttpStatusCode.Created);

        // Get detailed information
        var response = await client.GetAsync($"/v1/{factory.TestTenant}/versioned-resource/details");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var details = JsonSerializer.Deserialize<ContentDetailsResponse>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(details);
        Assert.Equal(2, details.LatestVersion);

        // Should have multiple versions
        Assert.True(details.Metadata.HasMultipleVersions);
        Assert.Equal(2, details.Metadata.TotalVersions);
        Assert.Equal(2, details.Versions.Count);

        // Versions should be ordered by version number (descending)
        Assert.Equal(2, details.Versions[0].Version);
        Assert.Equal(1, details.Versions[1].Version);

        // Version 2 should be larger than version 1
        Assert.True(details.Versions[0].ByteSize > details.Versions[1].ByteSize);
    }

    [Fact]
    public async Task GetContentDetails_NonExistentResource_ReturnsNotFound()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await client.GetAsync($"/v1/{factory.TestTenant}/nonexistent-resource/details");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetContentDetails_InvalidTenant_ReturnsBadRequest()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await client.GetAsync("/v1/invalid-tenant/test-resource/details");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetContentDetails_WithoutAuth_ReturnsUnauthorized()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/v1/{factory.TestTenant}/test-resource/details");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetContentDetails_DeletedContent_ReturnsNotFound()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Create content
        var contentData = JsonSerializer.Serialize(new { message = "Content to be deleted" });
        var putResponse = await client.PutAsync($"/v1/{factory.TestTenant}/deleted-resource",
            new StringContent(contentData, System.Text.Encoding.UTF8, "application/json"));
        Assert.True(putResponse.StatusCode == HttpStatusCode.OK || putResponse.StatusCode == HttpStatusCode.Created);

        // Delete the content
        var deleteResponse = await client.DeleteAsync($"/v1/{factory.TestTenant}/deleted-resource");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Try to get details of deleted content
        var response = await client.GetAsync($"/v1/{factory.TestTenant}/deleted-resource/details");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetContentDetails_WithSubdirectory_ReturnsCorrectPath()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // First create a subdirectory
        using var scope = factory.Services.CreateScope();
        var directoryRepo = scope.ServiceProvider.GetRequiredService<CmsLite.Database.Repositories.IDirectoryRepo>();

        var rootDir = await directoryRepo.GetOrCreateRootDirectoryAsync(CmsLiteTestFactoryAuth.TestTenantId);

        var subDir = new CmsLite.Database.DbSet.Directory
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = CmsLiteTestFactoryAuth.TestTenantId,
            ParentId = rootDir.Id,
            Name = "TestSubDir",
            Level = 1,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };
        await directoryRepo.CreateDirectoryAsync(subDir);

        // Create content in subdirectory
        var contentData = JsonSerializer.Serialize(new { location = "subdirectory" });
        var request = new HttpRequestMessage(HttpMethod.Put, $"/v1/{factory.TestTenant}/subdir-resource")
        {
            Content = new StringContent(contentData, System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Directory-Id", subDir.Id);

        var putResponse = await client.SendAsync(request);
        Assert.True(putResponse.StatusCode == HttpStatusCode.OK || putResponse.StatusCode == HttpStatusCode.Created);

        // Get details
        var response = await client.GetAsync($"/v1/{factory.TestTenant}/subdir-resource/details");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var details = JsonSerializer.Deserialize<ContentDetailsResponse>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(details);
        Assert.Equal("TestSubDir", details.Directory.Name);
        Assert.Equal("/TestSubDir", details.Directory.FullPath);
        Assert.Equal(1, details.Directory.Level);
    }
}