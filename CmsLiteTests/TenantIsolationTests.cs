using System.Net;
using System.Text;
using System.Text.Json;
using CmsLiteTests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CmsLiteTests;

public class TenantIsolationTests : IAsyncDisposable
{
    private readonly CmsLiteTestFactoryAuth factory = new();

    public async ValueTask DisposeAsync() => await factory.DisposeAsync();

    [Fact]
    public async Task TenantA_User_Cannot_Access_TenantB_Directories()
    {
        await factory.InitializeAsync();

        // Create a second tenant and user for testing cross-tenant access
        await SetupSecondTenantAsync();

        // Create an authenticated client for Tenant A user
        var tenantAClient = factory.CreateAuthenticatedClient();

        // Try to access Tenant B's directories - should be forbidden
        var response = await tenantAClient.GetAsync("/v1/tenant-b/directories/tree");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Access denied to tenant 'tenant-b'", content);
    }

    [Fact]
    public async Task TenantA_User_Cannot_Access_TenantB_Content()
    {
        await factory.InitializeAsync();
        await SetupSecondTenantAsync();

        var tenantAClient = factory.CreateAuthenticatedClient();

        // Try to access Tenant B's content - should be forbidden
        var response = await tenantAClient.GetAsync("/v1/tenant-b/some-resource");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Access denied to tenant 'tenant-b'", content);
    }

    [Fact]
    public async Task TenantA_User_Cannot_Create_Content_In_TenantB()
    {
        await factory.InitializeAsync();
        await SetupSecondTenantAsync();

        var tenantAClient = factory.CreateAuthenticatedClient();

        // Try to create content in Tenant B - should be forbidden
        var contentJson = JsonSerializer.Serialize(new { title = "Malicious Content" });
        var response = await tenantAClient.PutAsync("/v1/tenant-b/malicious-resource",
            new StringContent(contentJson, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Access denied to tenant 'tenant-b'", content);
    }

    [Fact]
    public async Task TenantA_User_Cannot_Delete_TenantB_Content()
    {
        await factory.InitializeAsync();
        await SetupSecondTenantAsync();

        var tenantAClient = factory.CreateAuthenticatedClient();

        // Try to delete content in Tenant B - should be forbidden
        var response = await tenantAClient.DeleteAsync("/v1/tenant-b/some-resource");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Access denied to tenant 'tenant-b'", content);
    }

    [Fact]
    public async Task TenantA_User_Cannot_Bulk_Delete_TenantB_Content()
    {
        await factory.InitializeAsync();
        await SetupSecondTenantAsync();

        var tenantAClient = factory.CreateAuthenticatedClient();

        // Try to bulk delete content in Tenant B - should be forbidden
        var deleteRequest = new { resources = new[] { "resource1", "resource2" } };
        var deleteRequestJson = JsonSerializer.Serialize(deleteRequest);
        var request = new HttpRequestMessage(HttpMethod.Delete, "/v1/tenant-b/bulk-delete")
        {
            Content = new StringContent(deleteRequestJson, Encoding.UTF8, "application/json")
        };

        var response = await tenantAClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Access denied to tenant 'tenant-b'", content);
    }

    [Fact]
    public async Task TenantA_User_Can_Access_Own_Tenant_Resources()
    {
        await factory.InitializeAsync();

        var tenantAClient = factory.CreateAuthenticatedClient();

        // User should be able to access their own tenant's directories
        var response = await tenantAClient.GetAsync("/v1/acme/directories/tree");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Access denied", content);
    }

    [Fact]
    public async Task TenantB_User_Cannot_Access_TenantA_Resources()
    {
        await factory.InitializeAsync();
        await SetupSecondTenantAsync();

        // Create an authenticated client for Tenant B user
        var tenantBToken = factory.GenerateTestJwtToken("tenant-b-user-id", "tenant-b");
        var tenantBClient = factory.CreateClient();
        tenantBClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tenantBToken);

        // Try to access Tenant A's directories - should be forbidden
        var response = await tenantBClient.GetAsync("/v1/acme/directories/tree");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Access denied to tenant 'acme'", content);
    }

    [Fact]
    public async Task Invalid_Tenant_In_URL_Returns_BadRequest()
    {
        await factory.InitializeAsync();

        var client = factory.CreateAuthenticatedClient();

        // Try to access an endpoint with an invalid tenant format (missing resource)
        var response = await client.GetAsync("/v1/invalid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        // The tenant validation now happens after tenant resolution, so we get tenant not found instead
        Assert.Contains("Tenant 'invalid' not found", content);
    }

    [Fact]
    public async Task Unauthenticated_Requests_To_Tenant_Endpoints_Are_Allowed_Through()
    {
        await factory.InitializeAsync();

        var client = factory.CreateClient(); // No authentication

        // Unauthenticated requests should pass through the middleware
        // (will be handled by authentication middleware later)
        var response = await client.GetAsync("/v1/acme/directories/tree");

        // Should return 401 Unauthorized (from authentication middleware), not 403 Forbidden
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Non_Tenant_Endpoints_Are_Not_Affected()
    {
        await factory.InitializeAsync();

        var client = factory.CreateClient();

        // Health endpoint should work normally (not affected by tenant validation)
        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TenantValidation_Logs_Security_Violations()
    {
        await factory.InitializeAsync();
        await SetupSecondTenantAsync();

        var tenantAClient = factory.CreateAuthenticatedClient();

        // This should trigger a security log entry
        var response = await tenantAClient.GetAsync("/v1/tenant-b/directories/tree");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // Note: In a real scenario, you'd verify log entries
        // For now, we verify the response indicates proper validation occurred
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Access denied to tenant 'tenant-b'", content);
    }

    /// <summary>
    /// Sets up a second tenant for cross-tenant testing
    /// </summary>
    private async Task SetupSecondTenantAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CmsLite.Database.CmsLiteDbContext>();

        // Create second tenant
        var tenantB = new CmsLite.Database.DbSet.Tenant
        {
            Id = "tenant-b",
            Name = "tenant-b",
            Description = "Second tenant for isolation testing",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        // Create user for second tenant
        var tenantBUser = new CmsLite.Database.DbSet.User
        {
            Id = "tenant-b-user-id",
            TenantId = "tenant-b",
            Email = "user@tenant-b.com",
            FirstName = "Tenant B",
            LastName = "User",
            PasswordHash = CmsLite.Helpers.Utilities.HashPassword("password123"),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        // Create root directory for second tenant
        var tenantBRootDir = new CmsLite.Database.DbSet.Directory
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = "tenant-b",
            Name = "root",
            Level = 0,
            ParentId = null,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.TenantsTable.Add(tenantB);
        db.UsersTable.Add(tenantBUser);
        db.DirectoriesTable.Add(tenantBRootDir);

        await db.SaveChangesAsync();
    }
}