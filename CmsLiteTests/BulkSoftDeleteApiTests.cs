using System.Net;
using System.Text.Json;
using CmsLite.Helpers.RequestMappers;
using CmsLiteTests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CmsLiteTests;

public class BulkSoftDeleteApiTests : IAsyncDisposable
{
    private readonly CmsLiteTestFactoryAuth factory = new();

    public async ValueTask DisposeAsync() => await factory.DisposeAsync();

    [Fact]
    public async Task BulkDelete_SingleResource_DeletesSuccessfully()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Create content first
        var contentData = JsonSerializer.Serialize(new { message = "Test content for deletion" });
        var putResponse = await client.PutAsync($"/v1/{factory.TestTenant}/test-resource.json",
            new StringContent(contentData, System.Text.Encoding.UTF8, "application/json"));
        Assert.True(putResponse.StatusCode == HttpStatusCode.OK || putResponse.StatusCode == HttpStatusCode.Created);

        // Bulk delete the single resource
        var deleteRequest = new SoftDeleteRequest
        {
            Resources = new List<string> { "test-resource.json" }
        };

        var deleteRequestJson = JsonSerializer.Serialize(deleteRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var deleteResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/v1/{factory.TestTenant}/bulk-delete")
        {
            Content = new StringContent(deleteRequestJson, System.Text.Encoding.UTF8, "application/json")
        });

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var responseContent = await deleteResponse.Content.ReadAsStringAsync();
        var deleteResult = JsonSerializer.Deserialize<SoftDeleteResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(deleteResult);
        Assert.Equal(1, deleteResult.DeletedCount);
        Assert.Single(deleteResult.DeletedResources);
        Assert.Equal("test-resource.json", deleteResult.DeletedResources[0].Resource);
        Assert.Equal(factory.TestTenant, deleteResult.TenantName);
        Assert.Equal("/", deleteResult.DirectoryPath);
    }

    [Fact]
    public async Task BulkDelete_MultipleResourcesSameDirectory_DeletesAllSuccessfully()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Create multiple content items in same directory
        var resources = new[] { "file1.json", "file2.json", "file3.json" };

        foreach (var resource in resources)
        {
            var contentData = JsonSerializer.Serialize(new { name = resource, data = "test" });
            var putResponse = await client.PutAsync($"/v1/{factory.TestTenant}/{resource}",
                new StringContent(contentData, System.Text.Encoding.UTF8, "application/json"));
            Assert.True(putResponse.StatusCode == HttpStatusCode.OK || putResponse.StatusCode == HttpStatusCode.Created);
        }

        // Bulk delete all resources
        var deleteRequest = new SoftDeleteRequest
        {
            Resources = resources.ToList()
        };

        var deleteRequestJson = JsonSerializer.Serialize(deleteRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var deleteResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/v1/{factory.TestTenant}/bulk-delete")
        {
            Content = new StringContent(deleteRequestJson, System.Text.Encoding.UTF8, "application/json")
        });

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var responseContent = await deleteResponse.Content.ReadAsStringAsync();
        var deleteResult = JsonSerializer.Deserialize<SoftDeleteResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(deleteResult);
        Assert.Equal(3, deleteResult.DeletedCount);
        Assert.Equal(3, deleteResult.DeletedResources.Count);

        var deletedResourceNames = deleteResult.DeletedResources.Select(r => r.Resource).ToHashSet();
        Assert.Contains("file1.json", deletedResourceNames);
        Assert.Contains("file2.json", deletedResourceNames);
        Assert.Contains("file3.json", deletedResourceNames);
    }

    [Fact]
    public async Task BulkDelete_ResourcesDifferentDirectories_ReturnsBadRequest()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Create subdirectory
        using var scope = factory.Services.CreateScope();
        var directoryRepo = scope.ServiceProvider.GetRequiredService<CmsLite.Database.Repositories.IDirectoryRepo>();

        var rootDir = await directoryRepo.GetOrCreateRootDirectoryAsync(CmsLiteTestFactoryAuth.TestTenantId);
        var subDir = new CmsLite.Database.DbSet.Directory
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = CmsLiteTestFactoryAuth.TestTenantId,
            ParentId = rootDir.Id,
            Name = "SubDirectory",
            Level = 1,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };
        await directoryRepo.CreateDirectoryAsync(subDir);

        // Create content in root directory
        var contentData1 = JsonSerializer.Serialize(new { location = "root" });
        var putResponse1 = await client.PutAsync($"/v1/{factory.TestTenant}/root-file.json",
            new StringContent(contentData1, System.Text.Encoding.UTF8, "application/json"));
        Assert.True(putResponse1.StatusCode == HttpStatusCode.OK || putResponse1.StatusCode == HttpStatusCode.Created);

        // Create content in subdirectory
        var contentData2 = JsonSerializer.Serialize(new { location = "subdirectory" });
        var request = new HttpRequestMessage(HttpMethod.Put, $"/v1/{factory.TestTenant}/sub-file.json")
        {
            Content = new StringContent(contentData2, System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Directory-Id", subDir.Id);

        var putResponse2 = await client.SendAsync(request);
        Assert.True(putResponse2.StatusCode == HttpStatusCode.OK || putResponse2.StatusCode == HttpStatusCode.Created);

        // Try to bulk delete resources from different directories
        var deleteRequest = new SoftDeleteRequest
        {
            Resources = new List<string> { "root-file.json", "sub-file.json" }
        };

        var deleteRequestJson = JsonSerializer.Serialize(deleteRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var deleteResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/v1/{factory.TestTenant}/bulk-delete")
        {
            Content = new StringContent(deleteRequestJson, System.Text.Encoding.UTF8, "application/json")
        });

        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);

        var responseContent = await deleteResponse.Content.ReadAsStringAsync();
        var errorResult = JsonSerializer.Deserialize<SoftDeleteErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(errorResult);
        Assert.Equal("BadRequest", errorResult.Error);
        Assert.Contains("same directory", errorResult.Details);
    }

    [Fact]
    public async Task BulkDelete_NonExistentResources_ReturnsBadRequest()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Try to delete non-existent resources
        var deleteRequest = new SoftDeleteRequest
        {
            Resources = new List<string> { "non-existent1.json", "non-existent2.json" }
        };

        var deleteRequestJson = JsonSerializer.Serialize(deleteRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var deleteResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/v1/{factory.TestTenant}/bulk-delete")
        {
            Content = new StringContent(deleteRequestJson, System.Text.Encoding.UTF8, "application/json")
        });

        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);

        var responseContent = await deleteResponse.Content.ReadAsStringAsync();
        var errorResult = JsonSerializer.Deserialize<SoftDeleteErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(errorResult);
        Assert.Equal("NotFound", errorResult.Error);
        Assert.Contains("not found", errorResult.Details);
        Assert.Contains("non-existent1.json", errorResult.FailedResources);
        Assert.Contains("non-existent2.json", errorResult.FailedResources);
    }

    [Fact]
    public async Task BulkDelete_EmptyResourcesList_ReturnsBadRequest()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var deleteRequest = new SoftDeleteRequest
        {
            Resources = new List<string>()
        };

        var deleteRequestJson = JsonSerializer.Serialize(deleteRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var deleteResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/v1/{factory.TestTenant}/bulk-delete")
        {
            Content = new StringContent(deleteRequestJson, System.Text.Encoding.UTF8, "application/json")
        });

        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);

        var responseContent = await deleteResponse.Content.ReadAsStringAsync();
        var errorResult = JsonSerializer.Deserialize<SoftDeleteErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(errorResult);
        Assert.Equal("BadRequest", errorResult.Error);
        Assert.Contains("At least one resource", errorResult.Details);
    }

    [Fact]
    public async Task BulkDelete_AlreadyDeletedResources_ReturnsBadRequest()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Create and then delete a resource
        var contentData = JsonSerializer.Serialize(new { message = "To be deleted twice" });
        var putResponse = await client.PutAsync($"/v1/{factory.TestTenant}/test-deleted.json",
            new StringContent(contentData, System.Text.Encoding.UTF8, "application/json"));
        Assert.True(putResponse.StatusCode == HttpStatusCode.OK || putResponse.StatusCode == HttpStatusCode.Created);

        // First deletion
        var deleteRequest1 = new SoftDeleteRequest
        {
            Resources = new List<string> { "test-deleted.json" }
        };
        var deleteRequestJson1 = JsonSerializer.Serialize(deleteRequest1, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var deleteResponse1 = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/v1/{factory.TestTenant}/bulk-delete")
        {
            Content = new StringContent(deleteRequestJson1, System.Text.Encoding.UTF8, "application/json")
        });
        Assert.Equal(HttpStatusCode.OK, deleteResponse1.StatusCode);

        // Try to delete again
        var deleteRequest2 = new SoftDeleteRequest
        {
            Resources = new List<string> { "test-deleted.json" }
        };
        var deleteRequestJson2 = JsonSerializer.Serialize(deleteRequest2, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var deleteResponse2 = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/v1/{factory.TestTenant}/bulk-delete")
        {
            Content = new StringContent(deleteRequestJson2, System.Text.Encoding.UTF8, "application/json")
        });

        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse2.StatusCode);

        var responseContent = await deleteResponse2.Content.ReadAsStringAsync();
        var errorResult = JsonSerializer.Deserialize<SoftDeleteErrorResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(errorResult);
        Assert.Equal("NotFound", errorResult.Error);
        Assert.Contains("already deleted", errorResult.Details);
    }

    [Fact]
    public async Task BulkDelete_WithoutAuthorization_ReturnsUnauthorized()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();

        var deleteRequest = new SoftDeleteRequest
        {
            Resources = new List<string> { "test.json" }
        };

        var deleteRequestJson = JsonSerializer.Serialize(deleteRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var deleteResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/v1/{factory.TestTenant}/bulk-delete")
        {
            Content = new StringContent(deleteRequestJson, System.Text.Encoding.UTF8, "application/json")
        });

        Assert.Equal(HttpStatusCode.Unauthorized, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task BulkDelete_DuplicateResources_RemovesDuplicatesAndDeletesOnce()
    {
        await factory.InitializeAsync();
        var client = factory.CreateClient();
        var token = factory.GenerateTestJwtToken();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // Create content
        var contentData = JsonSerializer.Serialize(new { message = "Test for duplicates" });
        var putResponse = await client.PutAsync($"/v1/{factory.TestTenant}/duplicate-test.json",
            new StringContent(contentData, System.Text.Encoding.UTF8, "application/json"));
        Assert.True(putResponse.StatusCode == HttpStatusCode.OK || putResponse.StatusCode == HttpStatusCode.Created);

        // Bulk delete with duplicates
        var deleteRequest = new SoftDeleteRequest
        {
            Resources = new List<string> { "duplicate-test.json", "duplicate-test.json", "duplicate-test.json" }
        };

        var deleteRequestJson = JsonSerializer.Serialize(deleteRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var deleteResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/v1/{factory.TestTenant}/bulk-delete")
        {
            Content = new StringContent(deleteRequestJson, System.Text.Encoding.UTF8, "application/json")
        });

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var responseContent = await deleteResponse.Content.ReadAsStringAsync();
        var deleteResult = JsonSerializer.Deserialize<SoftDeleteResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(deleteResult);
        Assert.Equal(1, deleteResult.DeletedCount); // Should only delete once
        Assert.Single(deleteResult.DeletedResources);
        Assert.Equal("duplicate-test.json", deleteResult.DeletedResources[0].Resource);
    }
}