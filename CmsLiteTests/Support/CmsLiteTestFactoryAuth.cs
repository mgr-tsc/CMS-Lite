using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CmsLite;
using CmsLite.Database;
using CmsLite.Database.Repositories;
using CmsLiteTests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace CmsLiteTests.Support;

/// <summary>
/// Test factory with full authentication support for testing protected endpoints
/// </summary>
public class CmsLiteTestFactoryAuth : WebApplicationFactory<Program>, IDisposable
{
    private readonly SqliteConnection _connection;
    private bool _initialized;
    private const string TestJwtKey = "test-jwt-secret-key-for-testing-32-characters-minimum-length";
    private const string TestTenantId = "acme";
    private const string TestUserId = "test-user-id";

    public CmsLiteTestFactoryAuth()
    {
        // Create unique in-memory database for each test factory instance
        var dbName = $"TestDb_{Guid.NewGuid():N}";
        _connection = new SqliteConnection($"DataSource={dbName};Mode=Memory;Cache=Shared");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["AzureStorage:ConnectionString"] = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;QueueEndpoint=http://azurite:10001/devstoreaccount1;TableEndpoint=http://azurite:10002/devstoreaccount1;",
                ["AzureStorage:Container"] = "cms-test",
                ["Database:Path"] = "test.db",
                // JWT configuration for tests
                ["Jwt:Key"] = TestJwtKey,
                ["Jwt:Issuer"] = "CmsLiteTest",
                ["Jwt:Audience"] = "CmsLiteTestUsers",
                ["Jwt:ValidateIssuer"] = "false",
                ["Jwt:ValidateAudience"] = "false",
                ["Jwt:ValidateLifetime"] = "false", // Disable for easier testing
                ["Jwt:ValidateIssuerSigningKey"] = "true"
            };
            configBuilder.AddInMemoryCollection(overrides!);
        });

        builder.ConfigureServices(services =>
        {
            // Replace database with in-memory SQLite
            services.RemoveAll(typeof(DbContextOptions<CmsLiteDbContext>));
            services.RemoveAll(typeof(CmsLiteDbContext));
            services.AddDbContext<CmsLiteDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Replace blob storage with in-memory fake
            services.RemoveAll(typeof(IBlobRepo));
            services.AddSingleton<IBlobRepo, InMemoryBlobRepo>();
        });
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CmsLiteDbContext>();
        await db.Database.EnsureCreatedAsync();

        // Seed test data with tenant and user
        await SeedTestDataAsync(db);

        _initialized = true;
    }

    private async Task SeedTestDataAsync(CmsLiteDbContext db)
    {
        // Create test tenant
        var testTenant = new DbSet.Tenant
        {
            Id = TestTenantId,
            Name = TestTenantId, // Use tenant ID as name for URL matching
            Description = "Test tenant for authenticated API tests",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        // Create test user
        var testUser = new DbSet.User
        {
            Id = TestUserId,
            TenantId = TestTenantId,
            Email = "test@acme.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = CmsLite.Helpers.Utilities.HashPassword("password123"),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.TenantsTable.Add(testTenant);
        db.UsersTable.Add(testUser);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Generate a valid JWT token for test requests
    /// </summary>
    public string GenerateTestJwtToken(string? userId = null, string? tenantId = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.PrimarySid, userId ?? TestUserId),
            new Claim(ClaimTypes.GroupSid, tenantId ?? TestTenantId),
            new Claim(ClaimTypes.Sid, Guid.NewGuid().ToString()), // Session ID
            new Claim(ClaimTypes.Email, "test@acme.com"),
            new Claim(ClaimTypes.Name, "Test User")
        };

        var token = new JwtSecurityToken(
            issuer: "CmsLiteTest",
            audience: "CmsLiteTestUsers",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Create an HttpClient with pre-configured JWT authentication
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string? userId = null, string? tenantId = null)
    {
        var client = CreateClient();
        var token = GenerateTestJwtToken(userId, tenantId);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Get the test tenant ID used in tests
    /// </summary>
    public string TestTenant => TestTenantId;

    /// <summary>
    /// Get the test user ID used in tests
    /// </summary>
    public string TestUser => TestUserId;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}