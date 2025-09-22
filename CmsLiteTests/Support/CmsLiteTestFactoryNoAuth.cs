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

namespace CmsLiteTests.Support;

/// <summary>
/// Test factory that disables authorization for content API testing
/// </summary>
public class CmsLiteTestFactoryNoAuth : WebApplicationFactory<Program>, IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private bool _initialized;

    public CmsLiteTestFactoryNoAuth()
    {
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
                // JWT configuration for tests (required even if not used)
                ["Jwt:Key"] = "test-jwt-secret-key-for-testing-32-characters-minimum-length",
                ["Jwt:Issuer"] = "CmsLiteTest",
                ["Jwt:Audience"] = "CmsLiteTestUsers",
                ["Jwt:ValidateIssuer"] = "false",
                ["Jwt:ValidateAudience"] = "false",
                ["Jwt:ValidateLifetime"] = "true",
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

            // No authorization services needed - we only have authentication
        });

        // Content endpoints don't require authorization, only authentication which we skip in tests
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CmsLiteDbContext>();
        await db.Database.EnsureCreatedAsync();

        // Seed test data for content tests
        await SeedTestDataAsync(db);

        _initialized = true;
    }

    private async Task SeedTestDataAsync(CmsLiteDbContext db)
    {
        // Create test tenant
        var testTenant = new DbSet.Tenant
        {
            Id = "acme",
            Name = "ACME Corporation",
            Description = "Test tenant for content API tests",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.TenantsTable.Add(testTenant);
        await db.SaveChangesAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}