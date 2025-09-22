using System.Security.Cryptography;
using CmsLite.Database;
using CmsLite.Database.Repositories;
using CmsLite.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CmsLite.Content;

public static class ContentEndpoints
{
    public static void MapContentEndpoints(this WebApplication app)
    {
        var contentGroup = app.MapGroup("/v1").WithTags("Content Management");

        // PUT /v1/{tenant}/{resource} - Create or update content
        contentGroup.MapPut("/{tenant}/{resource}", async (
            string tenant,
            string resource,
            HttpRequest req,
            CmsLiteDbContext db,
            IBlobRepo blobs) =>
        {
            (tenant, resource) = Utilities.ParseTenantResource(tenant, resource);

            if (!req.ContentType?.StartsWith("application/json") ?? true)
                return Results.BadRequest("Only application/json is allowed.");

            using var ms = new MemoryStream();
            await req.Body.CopyToAsync(ms);
            var bytes = ms.ToArray();
            if (bytes.Length == 0) return Results.BadRequest("Empty body.");

            // Calculate content integrity hash
            string sha256;
            using (var sha = SHA256.Create())
                sha256 = Convert.ToHexString(sha.ComputeHash(bytes));

            // Get current item for optimistic concurrency
            var item = await db.ContentItemsTable
                .SingleOrDefaultAsync(x => x.Tenant.Name == tenant && x.Resource == resource);

            // Handle optimistic concurrency control
            var ifMatch = req.Headers["If-Match"].FirstOrDefault();
            if (!string.IsNullOrEmpty(ifMatch) && item != null && item.ETag != ifMatch)
                return Results.StatusCode(StatusCodes.Status412PreconditionFailed);

            var nextVersion = (item?.LatestVersion ?? 0) + 1;
            var blobKey = $"{tenant}/{resource}/v{nextVersion}.json";

            // Upload content to blob storage
            var (etag, size) = await blobs.UploadJsonAsync(blobKey, bytes);

            // Update or create content item metadata
            if (item == null)
            {
                item = new DbSet.ContentItem
                {
                    TenantId = tenant,
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
                db.ContentItemsTable.Add(item);
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

            // Create version history record
            db.ContentVersionsTable.Add(new DbSet.ContentVersion
            {
                TenantId = tenant,
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
        }).WithName("CreateOrUpdateContent")
        .RequireAuthorization()
        .WithSummary("Create or update content")
        .WithDescription("Create new content or update existing content with versioning");

        // GET /v1/{tenant}/{resource} - Retrieve content
        contentGroup.MapGet("/{tenant}/{resource}", async (
            string tenant,
            string resource,
            int? version,
            HttpResponse res,
            CmsLiteDbContext db,
            IBlobRepo blobs) =>
        {
            (tenant, resource) = Utilities.ParseTenantResource(tenant, resource);

            var latest = await db.ContentItemsTable
                .SingleOrDefaultAsync(x => x.TenantId == tenant && x.Resource == resource && x.IsDeleted == 0);
            if (latest == null) return Results.NotFound();

            var v = version ?? latest.LatestVersion;
            var blobKey = $"{tenant}/{resource}/v{v}.json";
            var blob = await blobs.DownloadAsync(blobKey);
            if (blob == null) return Results.NotFound();

            res.ContentType = "application/json";
            res.Headers.ETag = blob.Value.ETag;
            await res.Body.WriteAsync(blob.Value.Bytes);
            return Results.Empty;
        }).RequireAuthorization()
        .WithName("GetContent")
        .WithSummary("Retrieve content")
        .WithDescription("Get content by tenant and resource, optionally specifying version");

        // HEAD /v1/{tenant}/{resource} - Get content metadata
        contentGroup.MapMethods("/{tenant}/{resource}", new[] { HttpMethods.Head }, async (
            string tenant,
            string resource,
            int? version,
            HttpResponse res,
            CmsLiteDbContext db,
            IBlobRepo blobs) =>
        {
            (tenant, resource) = Utilities.ParseTenantResource(tenant, resource);

            var latest = await db.ContentItemsTable
                .SingleOrDefaultAsync(x => x.TenantId == tenant && x.Resource == resource && x.IsDeleted == 0);
            if (latest == null) return Results.NotFound();

            var v = version ?? latest.LatestVersion;
            var blobKey = $"{tenant}/{resource}/v{v}.json";
            var head = await blobs.HeadAsync(blobKey);
            if (head == null) return Results.NotFound();

            res.ContentType = "application/json";
            res.Headers.ETag = head.Value.ETag;
            res.ContentLength = head.Value.Size;
            return Results.Empty;
        })
        .RequireAuthorization()
        .WithName("GetContentMetadata")
        .WithSummary("Get content metadata")
        .WithDescription("Get content metadata without downloading the content body");

        // GET /v1/{tenant} - List tenant resources
        contentGroup.MapGet("/{tenant}", async (
            string tenant,
            string? prefix,
            bool? includeDeleted,
            int? limit,
            string? cursor,
            CmsLiteDbContext db) =>
        {
            var take = Math.Clamp(limit ?? 50, 1, 200);
            var q = db.ContentItemsTable.AsQueryable().Where(x => x.TenantId == tenant);

            if (!(includeDeleted ?? false))
                q = q.Where(x => x.IsDeleted == 0);

            if (!string.IsNullOrEmpty(prefix))
            {
                q = q.Where(x => x.Resource.StartsWith(prefix));
            }

            // Cursor-based pagination using Id
            int afterId = 0;
            if (!string.IsNullOrEmpty(cursor) && int.TryParse(cursor, out var c))
                afterId = c;

            var page = await q.Where(x => x.Id > afterId)
                            .OrderBy(x => x.Id)
                            .Take(take + 1)
                            .ToListAsync();

            string? next = page.Count > take ? page[^1].Id.ToString() : null;
            if (page.Count > take) page.RemoveAt(page.Count - 1);

            var items = page.Select(x => new
            {
                x.Id,
                x.TenantId,
                x.Resource,
                x.LatestVersion,
                x.ETag,
                x.ByteSize,
                x.Sha256,
                x.UpdatedAtUtc
            });

            return Results.Ok(new { items, nextCursor = next });
        }).RequireAuthorization()
        .RequireAuthorization()
        .WithName("ListTenantResources")
        .WithSummary("List tenant resources")
        .WithDescription("List all resources for a tenant with optional filtering and pagination");

        // DELETE /v1/{tenant}/{resource} - Soft delete content
        contentGroup.MapDelete("/{tenant}/{resource}", async (
            string tenant,
            string resource,
            CmsLiteDbContext db) =>
        {
            (tenant, resource) = Utilities.ParseTenantResource(tenant, resource);

            var item = await db.ContentItemsTable
                .SingleOrDefaultAsync(x => x.TenantId == tenant && x.Resource == resource);
            if (item == null) return Results.NotFound();

            item.IsDeleted = 1;
            item.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.NoContent();
        }).RequireAuthorization()
        .WithName("DeleteContent")
        .WithSummary("Delete content")
        .WithDescription("Soft delete content (marks as deleted but preserves data)");

        // GET /v1/{tenant}/{resource}/versions - List content versions
        contentGroup.MapGet("/{tenant}/{resource}/versions", async (
            string tenant,
            string resource,
            CmsLiteDbContext db) =>
        {
            (tenant, resource) = Utilities.ParseTenantResource(tenant, resource);

            var versions = await db.ContentVersionsTable
                .Where(x => x.TenantId == tenant && x.Resource == resource)
                .OrderByDescending(x => x.Version)
                .Select(x => new
                {
                    x.Version,
                    x.ETag,
                    x.Sha256,
                    x.ByteSize,
                    x.CreatedAtUtc
                })
                .ToListAsync();

            if (versions.Count == 0) return Results.NotFound();
            return Results.Ok(versions);
        }).RequireAuthorization()
        .WithName("GetContentVersions")
        .WithSummary("List content versions")
        .WithDescription("Get all versions of a specific content resource");
    }
}