using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CmsLite.Database;
using CmsLite.Database.Repositories;
using CmsLiteTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CmsLiteTests;

public class DirectoryApiTests
{
    [Fact]
    public async Task GetDirectoryTree_ReturnsDirectoryHierarchy()
    {
        using var factory = new CmsLiteTestFactoryAuth();
        await factory.InitializeAsync();
        var client = factory.CreateAuthenticatedClient();

        using var scope = factory.Services.CreateScope();
        var directoryRepo = scope.ServiceProvider.GetRequiredService<IDirectoryRepo>();

        // Create some directories
        var rootDir = await directoryRepo.GetOrCreateRootDirectoryAsync(CmsLiteTestFactoryAuth.TestTenantId);

        var docsDir = new DbSet.Directory
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = CmsLiteTestFactoryAuth.TestTenantId,
            ParentId = rootDir.Id,
            Name = "Documents",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };
        await directoryRepo.CreateDirectoryAsync(docsDir);

        var response = await client.GetAsync($"/v1/{factory.TestTenant}/directories");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        var directories = result.GetProperty("directories").EnumerateArray().ToList();
        Assert.True(directories.Count >= 2); // Root + Documents

        // Verify root directory exists
        var rootDirResponse = directories.FirstOrDefault(d => d.GetProperty("name").GetString() == "Root");
        Assert.True(rootDirResponse.ValueKind != JsonValueKind.Undefined);
        Assert.True(rootDirResponse.GetProperty("isRoot").GetBoolean());
        Assert.Equal(0, rootDirResponse.GetProperty("level").GetInt32());

        // Verify documents directory exists
        var docsDirResponse = directories.FirstOrDefault(d => d.GetProperty("name").GetString() == "Documents");
        Assert.True(docsDirResponse.ValueKind != JsonValueKind.Undefined);
        Assert.False(docsDirResponse.GetProperty("isRoot").GetBoolean());
        Assert.Equal(1, docsDirResponse.GetProperty("level").GetInt32());
    }

    [Fact]
    public async Task GetDirectoryById_ReturnsDirectoryDetails()
    {
        using var factory = new CmsLiteTestFactoryAuth();
        await factory.InitializeAsync();
        var client = factory.CreateAuthenticatedClient();

        using var scope = factory.Services.CreateScope();
        var directoryRepo = scope.ServiceProvider.GetRequiredService<IDirectoryRepo>();

        var rootDir = await directoryRepo.GetOrCreateRootDirectoryAsync(CmsLiteTestFactoryAuth.TestTenantId);

        var response = await client.GetAsync($"/v1/{factory.TestTenant}/directories/{rootDir.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.Equal(rootDir.Id, result.GetProperty("id").GetString());
        Assert.Equal("Root", result.GetProperty("name").GetString());
        Assert.Equal(0, result.GetProperty("level").GetInt32());
        Assert.True(result.GetProperty("isRoot").GetBoolean());
        Assert.True(result.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task GetDirectoryById_NonExistent_ReturnsNotFound()
    {
        using var factory = new CmsLiteTestFactoryAuth();
        await factory.InitializeAsync();
        var client = factory.CreateAuthenticatedClient();

        var nonExistentId = Guid.NewGuid().ToString();
        var response = await client.GetAsync($"/v1/{factory.TestTenant}/directories/{nonExistentId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateDirectory_Success()
    {
        using var factory = new CmsLiteTestFactoryAuth();
        await factory.InitializeAsync();
        var client = factory.CreateAuthenticatedClient();

        using var scope = factory.Services.CreateScope();
        var directoryRepo = scope.ServiceProvider.GetRequiredService<IDirectoryRepo>();

        var rootDir = await directoryRepo.GetOrCreateRootDirectoryAsync(CmsLiteTestFactoryAuth.TestTenantId);

        var createRequest = new
        {
            name = "Projects",
            parentId = rootDir.Id
        };

        var response = await client.PostAsJsonAsync($"/v1/{factory.TestTenant}/directories", createRequest);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.Equal("Projects", result.GetProperty("name").GetString());
        Assert.Equal(1, result.GetProperty("level").GetInt32());
        Assert.Equal(rootDir.Id, result.GetProperty("parentId").GetString());
        Assert.False(result.GetProperty("isRoot").GetBoolean());

        // Verify location header
        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains($"/v1/{factory.TestTenant}/directories/", location);
    }

    [Fact]
    public async Task CreateDirectory_RootDirectory_Success()
    {
        using var factory = new CmsLiteTestFactoryAuth();
        await factory.InitializeAsync();
        var client = factory.CreateAuthenticatedClient();

        var createRequest = new
        {
            name = "TopLevel"
            // No parentId - should create root-level directory
        };

        var response = await client.PostAsJsonAsync($"/v1/{factory.TestTenant}/directories", createRequest);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.Equal("TopLevel", result.GetProperty("name").GetString());
        Assert.Equal(0, result.GetProperty("level").GetInt32());
        Assert.True(result.GetProperty("isRoot").GetBoolean());
    }

    [Fact]
    public async Task CreateDirectory_InvalidParent_ReturnsBadRequest()
    {
        using var factory = new CmsLiteTestFactoryAuth();
        await factory.InitializeAsync();
        var client = factory.CreateAuthenticatedClient();

        var createRequest = new
        {
            name = "Invalid",
            parentId = "non-existent-parent-id"
        };

        var response = await client.PostAsJsonAsync($"/v1/{factory.TestTenant}/directories", createRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid parent directory ID", errorContent);
    }

    [Fact]
    public async Task CreateDirectory_ExceedsNestingLimit_ReturnsBadRequest()
    {
        using var factory = new CmsLiteTestFactoryAuth();
        await factory.InitializeAsync();
        var client = factory.CreateAuthenticatedClient();

        using var scope = factory.Services.CreateScope();
        var directoryRepo = scope.ServiceProvider.GetRequiredService<IDirectoryRepo>();

        // Create 5 levels (0,1,2,3,4)
        var currentParentId = (string?)null;
        var lastDirectoryId = string.Empty;

        for (int level = 0; level < 5; level++)
        {
            var dir = new DbSet.Directory
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = CmsLiteTestFactoryAuth.TestTenantId,
                ParentId = currentParentId,
                Name = $"Level{level}",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                IsActive = true
            };
            await directoryRepo.CreateDirectoryAsync(dir);
            currentParentId = dir.Id;
            lastDirectoryId = dir.Id;
        }

        // Try to create 6th level - should fail
        var createRequest = new
        {
            name = "Level5_ShouldFail",
            parentId = lastDirectoryId
        };

        var response = await client.PostAsJsonAsync($"/v1/{factory.TestTenant}/directories", createRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("Maximum directory nesting level (5) exceeded", errorContent);
    }


    [Fact]
    public async Task GetDirectoryContents_WithContent_ReturnsContentItems()
    {
        using var factory = new CmsLiteTestFactoryAuth();
        await factory.InitializeAsync();
        var client = factory.CreateAuthenticatedClient();

        using var scope = factory.Services.CreateScope();
        var directoryRepo = scope.ServiceProvider.GetRequiredService<IDirectoryRepo>();

        var rootDir = await directoryRepo.GetOrCreateRootDirectoryAsync(CmsLiteTestFactoryAuth.TestTenantId);

        // Create content in root directory
        var contentPayload = new { title = "Test Content" };
        var contentRequest = JsonSerializer.Serialize(contentPayload);
        var createContentResponse = await client.PutAsync($"/v1/{factory.TestTenant}/test-content",
            new StringContent(contentRequest, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, createContentResponse.StatusCode);

        // Get directory contents
        var response = await client.GetAsync($"/v1/{factory.TestTenant}/directories/{rootDir.Id}/contents");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        var directory = result.GetProperty("directory");
        Assert.Equal(rootDir.Id, directory.GetProperty("id").GetString());
        Assert.Equal("Root", directory.GetProperty("name").GetString());

        var contentItems = result.GetProperty("contentItems").EnumerateArray().ToList();
        Assert.Single(contentItems);

        var contentItem = contentItems[0];
        Assert.Equal("test-content", contentItem.GetProperty("resource").GetString());
        Assert.Equal(1, contentItem.GetProperty("latestVersion").GetInt32());
    }

    [Fact]
    public async Task GetDirectoryTree_WithContent_ReturnsCorrectContentCount()
    {
        using var factory = new CmsLiteTestFactoryAuth();
        await factory.InitializeAsync();
        var client = factory.CreateAuthenticatedClient();

        using var scope = factory.Services.CreateScope();
        var directoryRepo = scope.ServiceProvider.GetRequiredService<IDirectoryRepo>();

        var rootDir = await directoryRepo.GetOrCreateRootDirectoryAsync(CmsLiteTestFactoryAuth.TestTenantId);

        // Create a custom directory
        var docsDir = new DbSet.Directory
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = CmsLiteTestFactoryAuth.TestTenantId,
            ParentId = rootDir.Id,
            Name = "Documents",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };
        await directoryRepo.CreateDirectoryAsync(docsDir);

        // Create content in root directory
        var rootContentPayload = new { title = "Root Content" };
        var rootContentRequest = JsonSerializer.Serialize(rootContentPayload);
        var rootContentResponse = await client.PutAsync($"/v1/{factory.TestTenant}/root-content",
            new StringContent(rootContentRequest, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, rootContentResponse.StatusCode);

        // Create content in Documents directory
        var docsContentPayload = new { title = "Doc Content" };
        var docsContentRequest = JsonSerializer.Serialize(docsContentPayload);
        var docsRequest = new HttpRequestMessage(HttpMethod.Put, $"/v1/{factory.TestTenant}/doc-content")
        {
            Content = new StringContent(docsContentRequest, Encoding.UTF8, "application/json")
        };
        docsRequest.Headers.Add("X-Directory-Id", docsDir.Id);
        var docsContentResponse = await client.SendAsync(docsRequest);
        Assert.Equal(HttpStatusCode.Created, docsContentResponse.StatusCode);

        // Get directory tree and verify content counts
        var treeResponse = await client.GetAsync($"/v1/{factory.TestTenant}/directories");
        Assert.Equal(HttpStatusCode.OK, treeResponse.StatusCode);

        var treeContent = await treeResponse.Content.ReadAsStringAsync();
        var treeResult = JsonSerializer.Deserialize<JsonElement>(treeContent);

        var directories = treeResult.GetProperty("directories").EnumerateArray().ToList();

        // Find root directory and verify content count
        var rootDirResponse = directories.FirstOrDefault(d => d.GetProperty("name").GetString() == "Root");
        Assert.True(rootDirResponse.ValueKind != JsonValueKind.Undefined);
        Assert.Equal(1, rootDirResponse.GetProperty("contentCount").GetInt32());

        // Find documents directory and verify content count
        var docsDirResponse = directories.FirstOrDefault(d => d.GetProperty("name").GetString() == "Documents");
        Assert.True(docsDirResponse.ValueKind != JsonValueKind.Undefined);
        Assert.Equal(1, docsDirResponse.GetProperty("contentCount").GetInt32());

        // Also test specific directory endpoint for root
        var rootDetailsResponse = await client.GetAsync($"/v1/{factory.TestTenant}/directories/{rootDir.Id}");
        Assert.Equal(HttpStatusCode.OK, rootDetailsResponse.StatusCode);

        var rootDetailsContent = await rootDetailsResponse.Content.ReadAsStringAsync();
        var rootDetailsResult = JsonSerializer.Deserialize<JsonElement>(rootDetailsContent);
        Assert.Equal(1, rootDetailsResult.GetProperty("contentCount").GetInt32());

        // Also test specific directory endpoint for Documents
        var docsDetailsResponse = await client.GetAsync($"/v1/{factory.TestTenant}/directories/{docsDir.Id}");
        Assert.Equal(HttpStatusCode.OK, docsDetailsResponse.StatusCode);

        var docsDetailsContent = await docsDetailsResponse.Content.ReadAsStringAsync();
        var docsDetailsResult = JsonSerializer.Deserialize<JsonElement>(docsDetailsContent);
        Assert.Equal(1, docsDetailsResult.GetProperty("contentCount").GetInt32());
    }

    [Fact]
    public async Task DirectoryEndpoints_RequireAuthentication()
    {
        using var factory = new CmsLiteTestFactoryAuth();
        await factory.InitializeAsync();
        var client = factory.CreateClient(); // No authentication

        // Test all endpoints require authentication
        var responses = await Task.WhenAll(
            client.GetAsync($"/v1/{factory.TestTenant}/directories"),
            client.GetAsync($"/v1/{factory.TestTenant}/directories/{Guid.NewGuid()}"),
            client.PostAsJsonAsync($"/v1/{factory.TestTenant}/directories", new { name = "Test" }),
            client.GetAsync($"/v1/{factory.TestTenant}/directories/{Guid.NewGuid()}/contents")
        );

        // All should return 401 Unauthorized
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task DirectoryEndpoints_InvalidTenant_ReturnsNotFound()
    {
        using var factory = new CmsLiteTestFactoryAuth();
        await factory.InitializeAsync();
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/v1/nonexistent-tenant/directories");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}