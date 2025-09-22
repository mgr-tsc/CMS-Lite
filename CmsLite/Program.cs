using Microsoft.EntityFrameworkCore;
using Azure.Storage.Blobs;
using CmsLite.Database.Repositories;
using CmsLite.Database;
using CmsLite.Authentication;
using CmsLite.Content;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var storageConnectionString = configuration["AzureStorage:ConnectionString"] ?? throw new InvalidOperationException("Azure Storage connection string is not configured.");
var containerName = configuration["AzureStorage:Container"] ?? "cms";
var dbPath = configuration["Database:Path"] ?? "cmslite.db";
// Add database services
builder.Services.AddDbContext<CmsLiteDbContext>(options =>
{
    options.UseSqlite($"Data Source={dbPath}");
});
// Add blob storage services
builder.Services.AddSingleton(_ => new BlobServiceClient(storageConnectionString));
builder.Services.AddSingleton<IBlobRepo, BlobRepo>();
// Add authentication services (but endpoints remain unprotected for now)
builder.AddCmsLiteAuthentication();

var app = builder.Build();

// Health endpoint
app.MapGet("/health", (IHostEnvironment env) =>
{
    var envValue = env.IsDevelopment() ? "dev" : "prod";
    return Results.Ok(new { env = envValue });
});

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var scopedServices = scope.ServiceProvider;
    var db = scopedServices.GetRequiredService<CmsLiteDbContext>();
    _ = scopedServices.GetRequiredService<IBlobRepo>();
    db.Database.EnsureCreated();
}

// Configure authentication endpoints (without middleware)
app.UseCmsLiteAuthentication();
// Map content endpoints (unprotected for now)
app.MapContentEndpoints();

app.UseHttpsRedirection();
app.Run();
public partial class Program { }