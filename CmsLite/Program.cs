using System;
using System.Reflection.Metadata;
using Microsoft.EntityFrameworkCore;
using Azure.Storage.Blobs;
using CmsLite.Database.Repositories;
using CmsLite.Database;
using CmsLite.Helpers;
using System.Security.Cryptography;
var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var storageConnectionString = configuration["AzureStorage:ConnectionString"] ?? throw new InvalidOperationException("Azure Storage connection string is not configured.");
var containerName = configuration["AzureStorage:Container"] ?? "cms";
var dbPath = configuration["Database:Path"] ?? "cmslite.db";

builder.Services.AddDbContext<CmsLite.Database.CmsLiteDbContext>(options =>
{
    options.UseSqlite($"Data Source={dbPath}");
});
builder.Services.AddSingleton(_ => new BlobServiceClient(storageConnectionString));
builder.Services.AddSingleton<IBlobRepo, BlobRepo>();


var app = builder.Build();

// Health endpoint
app.MapGet("/health", (IHostEnvironment env) =>
{
    var envValue = env.IsDevelopment() ? "dev" : "prod";
    return Results.Ok(new { env = envValue });
});

using (var scope = app.Services.CreateScope())
{
    var scopedServices = scope.ServiceProvider;
    var db = scopedServices.GetRequiredService<CmsLite.Database.CmsLiteDbContext>();
    _ = scopedServices.GetRequiredService<IBlobRepo>();
    db.Database.EnsureCreated();
}

app.MapPut("/v1/{tenant}/{resource}", async (
    string tenant, string resource, HttpRequest req, CmsLiteDbContext db, IBlobRepo blobs) =>
{
    (tenant, resource) = Utilities.ParseTenantResource(tenant, resource);

    if (!req.ContentType?.StartsWith("application/json") ?? true)
        return Results.BadRequest("Only application/json is allowed.");

    using var ms = new MemoryStream();
    await req.Body.CopyToAsync(ms);
    var bytes = ms.ToArray();
    if (bytes.Length == 0) return Results.BadRequest("Empty body.");

    // integrity
    string sha256;
    using (var sha = SHA256.Create())
        sha256 = Convert.ToHexString(sha.ComputeHash(bytes));

    // get current item
    var item = await db.ContentItems
        .SingleOrDefaultAsync(x => x.Tenant == tenant && x.Resource == resource);

    // optimistic concurrency if provided
    var ifMatch = req.Headers["If-Match"].FirstOrDefault();
    if (!string.IsNullOrEmpty(ifMatch) && item != null && item.ETag != ifMatch)
        return Results.StatusCode(StatusCodes.Status412PreconditionFailed);

    var nextVersion = (item?.LatestVersion ?? 0) + 1;
    var blobKey = $"{tenant}/{resource}/v{nextVersion}.json";

    // upload to blob
    var (etag, size) = await blobs.UploadJsonAsync(blobKey, bytes);

    // upsert metadata
    if (item == null)
    {
        item = new DbSet.ContentItem
        {
            Tenant = tenant,
            Resource = resource,
            LatestVersion = nextVersion,
            ContentType = "application/json",
            ByteSize = size,
            Sha256 = sha256,
            ETag = etag,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsDeleted = 0
        };
        db.ContentItems.Add(item);
    }
    else
    {
        item.LatestVersion = nextVersion;
        item.ByteSize = size;
        item.Sha256 = sha256;
        item.ETag = etag;
        item.UpdatedAtUtc = DateTime.UtcNow;
        item.IsDeleted = 0;
    }
    db.ContentVersions.Add(new DbSet.ContentVersion
    {
        Tenant = tenant,
        Resource = resource,
        Version = nextVersion,
        ByteSize = size,
        Sha256 = sha256,
        ETag = etag,
        CreatedAtUtc = DateTime.UtcNow
    });
    await db.SaveChangesAsync();
    return Results.Created($"/v1/{tenant}/{resource}?version={nextVersion}",
        new { tenant, resource, version = nextVersion, etag, sha256, size });
});

app.MapGet("/v1/{tenant}/{resource}", async (
    string tenant, string resource, int? version, HttpResponse res, CmsLiteDbContext db, IBlobRepo blobs) =>
{
    (tenant, resource) = Utilities.ParseTenantResource(tenant, resource);

    var latest = await db.ContentItems.SingleOrDefaultAsync(x => x.Tenant == tenant && x.Resource == resource && x.IsDeleted == 0);
    if (latest == null) return Results.NotFound();

    var v = version ?? latest.LatestVersion;
    var blobKey = $"{tenant}/{resource}/v{v}.json";
    var blob = await blobs.DownloadAsync(blobKey);
    if (blob == null) return Results.NotFound();

    res.ContentType = "application/json";
    res.Headers.ETag = blob.Value.ETag;
    await res.Body.WriteAsync(blob.Value.Bytes);
    return Results.Empty;
});

app.MapMethods("/v1/{tenant}/{resource}", new [] { HttpMethods.Head }, async (
    string tenant, string resource, int? version, HttpResponse res, CmsLiteDbContext db, IBlobRepo blobs) =>
{
    (tenant, resource) = Utilities.ParseTenantResource(tenant, resource);
    var latest = await db.ContentItems.SingleOrDefaultAsync(x => x.Tenant == tenant && x.Resource == resource && x.IsDeleted == 0);
    if (latest == null) return Results.NotFound();

    var v = version ?? latest.LatestVersion;
    var blobKey = $"{tenant}/{resource}/v{v}.json";
    var head = await blobs.HeadAsync(blobKey);
    if (head == null) return Results.NotFound();

    res.ContentType = "application/json";
    res.Headers.ETag = head.Value.ETag;
    res.ContentLength = head.Value.Size;
    return Results.Ok();
});

app.MapGet("/v1/{tenant}", async (
    string tenant, string? prefix, bool? includeDeleted, int? limit, string? cursor, CmsLiteDbContext db) =>
{
    var take = Math.Clamp(limit ?? 50, 1, 200);
    var q = db.ContentItems.AsQueryable().Where(x => x.Tenant == tenant);
    if (!(includeDeleted ?? false)) q = q.Where(x => x.IsDeleted == 0);

    if (!string.IsNullOrEmpty(prefix))
    {
        q = q.Where(x => x.Resource.StartsWith(prefix));
    }

    // naive cursor: use Id
    int afterId = 0;
    if (!string.IsNullOrEmpty(cursor) && int.TryParse(cursor, out var c)) afterId = c;

    var page = await q.Where(x => x.Id > afterId).OrderBy(x => x.Id).Take(take + 1).ToListAsync();
    string? next = page.Count > take ? page[^1].Id.ToString() : null;
    if (page.Count > take) page.RemoveAt(page.Count - 1);
    var items = page.Select(x => new { x.Id, x.Tenant, x.Resource, x.LatestVersion, x.ETag, x.ByteSize, x.Sha256, x.UpdatedAtUtc });
    return Results.Ok(new { items, nextCursor = next });
});

app.MapDelete("/v1/{tenant}/{resource}", async (string tenant, string resource, CmsLiteDbContext db) =>
{
    (tenant, resource) = Utilities.ParseTenantResource(tenant, resource);
    var item = await db.ContentItems.SingleOrDefaultAsync(x => x.Tenant == tenant && x.Resource == resource);
    if (item == null) return Results.NotFound();
    item.IsDeleted = 1;
    item.UpdatedAtUtc = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet("/v1/{tenant}/{resource}/versions", async (string tenant, string resource, CmsLiteDbContext db) =>
{
    (tenant, resource) = Utilities.ParseTenantResource(tenant, resource);
    var versions = await db.ContentVersions
        .Where(x => x.Tenant == tenant && x.Resource == resource)
        .OrderByDescending(x => x.Version)
        .Select(x => new { x.Version, x.ETag, x.Sha256, x.ByteSize, x.CreatedAtUtc })
        .ToListAsync();

    if (versions.Count == 0) return Results.NotFound();
    return Results.Ok(versions);
});

app.UseHttpsRedirection();
app.Run();
public partial class Program { }