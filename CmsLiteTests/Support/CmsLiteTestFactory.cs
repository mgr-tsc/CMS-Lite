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

public class CmsLiteTestFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private bool _initialized;

    public CmsLiteTestFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["AzureStorage:ConnectionString"] = "UseDevelopmentStorage=true",
                ["AzureStorage:Container"] = "cms-test",
                ["Database:Path"] = "test.db"
            };
            configBuilder.AddInMemoryCollection(overrides!);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<CmsLiteDbContext>));
            services.RemoveAll(typeof(CmsLiteDbContext));
            services.AddDbContext<CmsLiteDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

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
        _initialized = true;
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
