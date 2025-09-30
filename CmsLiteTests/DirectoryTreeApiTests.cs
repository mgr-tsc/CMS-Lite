using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CmsLite.Helpers.RequestMappers;
using CmsLiteTests.Support;
using Microsoft.Extensions.DependencyInjection;
using CmsLite.Database.Repositories;
using CmsLite.Database;
using Xunit;

namespace CmsLiteTests;

public class DirectoryTreeApiTests : IAsyncDisposable
{
    private readonly CmsLiteTestFactoryAuth factory = new();

    public async ValueTask DisposeAsync() => await factory.DisposeAsync();

    private async Task CreateRootDirectoryAsync()
    {
        using var scope = factory.Services.CreateScope();
        var directoryRepo = scope.ServiceProvider.GetRequiredService<IDirectoryRepo>();
        await directoryRepo.GetOrCreateRootDirectoryAsync(CmsLiteTestFactoryAuth.TestTenantId);
    }

    [Fact]
    public async Task GetDirectoryTree_ValidTenant_ReturnsFullTree()
    {
        await factory.InitializeAsync();
        await CreateRootDirectoryAsync();

        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await client.GetAsync($"/v1/{factory.TestTenant}/directories/tree");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var tree = JsonSerializer.Deserialize<DirectoryTreeResponse>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(tree);
        Assert.Equal(factory.TestTenant, tree.TenantName);
        Assert.Equal(CmsLiteTestFactoryAuth.TestTenantId, tree.TenantId);
        Assert.NotNull(tree.RootDirectory);
        Assert.Equal("Root", tree.RootDirectory.Name);
        Assert.True(tree.TotalDirectories >= 1);
    }

    [Fact]
    public async Task GetDirectoryTree_WithContentItems_ReturnsTreeWithContent()
    {
        await factory.InitializeAsync();
        await CreateRootDirectoryAsync();

        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Add some content items via API
        var contentData = JsonSerializer.Serialize(new { message = "Test content", data = new { value = 123 } });
        var putResponse = await client.PutAsync($"/v1/{factory.TestTenant}/test-resource",
            new StringContent(contentData, System.Text.Encoding.UTF8, "application/json"));

        Assert.True(putResponse.StatusCode == HttpStatusCode.OK || putResponse.StatusCode == HttpStatusCode.Created);

        // Get the directory tree
        var response = await client.GetAsync($"/v1/{factory.TestTenant}/directories/tree");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var tree = JsonSerializer.Deserialize<DirectoryTreeResponse>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(tree);
        Assert.True(tree.TotalContentItems >= 1);
        Assert.True(tree.RootDirectory.ContentItems.Count >= 1);

        var contentItem = tree.RootDirectory.ContentItems.FirstOrDefault(c => c.Resource == "test-resource");
        Assert.NotNull(contentItem);
        Assert.Equal("application/json", contentItem.ContentType);
        Assert.Equal(1, contentItem.LatestVersion);
        Assert.False(contentItem.IsDeleted);
    }

    [Fact]
    public async Task GetDirectoryTree_WithSubdirectories_ReturnsHierarchicalTree()
    {
        await factory.InitializeAsync();
        await CreateRootDirectoryAsync();

        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Get root directory ID first
        var rootDirResponse = await client.GetAsync($"/v1/{factory.TestTenant}/directories");
        Assert.Equal(HttpStatusCode.OK, rootDirResponse.StatusCode);

        var rootDirContent = await rootDirResponse.Content.ReadAsStringAsync();
        var dirResult = JsonSerializer.Deserialize<JsonElement>(rootDirContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var directories = dirResult.GetProperty("directories").EnumerateArray().ToArray();
        var rootDir = directories.First(d => d.GetProperty("isRoot").GetBoolean());
        var rootDirId = rootDir.GetProperty("id").GetString();

        // Create a subdirectory
        var createDirRequest = new CreateDirectoryRequest("SubDir1", rootDirId);

        var createResponse = await client.PostAsJsonAsync($"/v1/{factory.TestTenant}/directories", createDirRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        // Get the directory tree
        var response = await client.GetAsync($"/v1/{factory.TestTenant}/directories/tree");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var tree = JsonSerializer.Deserialize<DirectoryTreeResponse>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(tree);
        Assert.True(tree.TotalDirectories >= 2); // Root + subdirectory
        Assert.True(tree.RootDirectory.SubDirectories.Count >= 1);

        var subDir = tree.RootDirectory.SubDirectories.FirstOrDefault(d => d.Name == "SubDir1");
        Assert.NotNull(subDir);
        Assert.Equal(1, subDir.Level);
    }

    [Fact]
    public async Task GetDirectoryTree_EmptyTenant_ReturnsEmptyTree()
    {
        await factory.InitializeAsync();

        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Don't create root directory - should return empty tree

        var response = await client.GetAsync($"/v1/{factory.TestTenant}/directories/tree");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var tree = JsonSerializer.Deserialize<DirectoryTreeResponse>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(tree);
        Assert.Equal(factory.TestTenant, tree.TenantName);
        Assert.Equal(0, tree.TotalDirectories);
        Assert.Equal(0, tree.TotalContentItems);
        Assert.Empty(tree.RootDirectory.ContentItems);
        Assert.Empty(tree.RootDirectory.SubDirectories);
    }

    [Fact]
    public async Task GetDirectoryTree_InvalidTenant_ReturnsNotFound()
    {
        await factory.InitializeAsync();

        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await client.GetAsync("/v1/nonexistent-tenant/directories/tree");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDirectoryTree_WithoutAuth_ReturnsUnauthorized()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/v1/{factory.TestTenant}/directories/tree");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDirectoryTree_ExcludesDeletedContent_OnlyReturnsActiveContent()
    {
        await factory.InitializeAsync();
        await CreateRootDirectoryAsync();

        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Create content
        var contentData = JsonSerializer.Serialize(new { message = "Test content" });
        var putResponse = await client.PutAsync($"/v1/{factory.TestTenant}/test-resource",
            new StringContent(contentData, System.Text.Encoding.UTF8, "application/json"));
        Assert.True(putResponse.StatusCode == HttpStatusCode.OK || putResponse.StatusCode == HttpStatusCode.Created);

        // Delete the content
        var deleteResponse = await client.DeleteAsync($"/v1/{factory.TestTenant}/test-resource");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Get the directory tree
        var response = await client.GetAsync($"/v1/{factory.TestTenant}/directories/tree");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var tree = JsonSerializer.Deserialize<DirectoryTreeResponse>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(tree);

        // Should not include deleted content
        var deletedContent = tree.RootDirectory.ContentItems.FirstOrDefault(c => c.Resource == "test-resource");
        Assert.Null(deletedContent);
    }
}