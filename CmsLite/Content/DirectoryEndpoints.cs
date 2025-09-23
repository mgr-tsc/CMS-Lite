using CmsLite.Database;
using CmsLite.Database.Repositories;
using CmsLite.Helpers;
using CmsLite.Helpers.RequestMappers;
using Microsoft.EntityFrameworkCore;

namespace CmsLite.Content;

public static class DirectoryEndpoints
{
    public static void MapDirectoryEndpoints(this WebApplication app)
    {
        var directoryGroup = app.MapGroup("/v1/{tenant}/directories").WithTags("Directory Management");

        // GET /v1/{tenant}/directories - List directory tree for tenant
        directoryGroup.MapGet("", async (
            string tenant,
            CmsLiteDbContext db,
            IDirectoryRepo directoryRepo) =>
        {
            // Get tenant ID
            var (tenantSuccess, tenantId, tenantError) = await DbHelper.GetTenantIdAsync(tenant, db);
            if (!tenantSuccess) return tenantError!;

            var directoryTree = await directoryRepo.GetDirectoryTreePerTenant(tenantId);

            var result = directoryTree.Select(d => new
            {
                d.Id,
                d.Name,
                d.Level,
                d.ParentId,
                d.CreatedAtUtc,
                d.UpdatedAtUtc,
                IsRoot = d.ParentId == null,
                ContentCount = d.ContentItems?.Count ?? 0
            }).ToList();

            return Results.Ok(new { directories = result, totalCount = result.Count });
        })
        .RequireAuthorization()
        .WithName("GetDirectoryTree")
        .WithSummary("Get directory tree for tenant")
        .WithDescription("Returns hierarchical directory structure for the authenticated user's tenant");

        // GET /v1/{tenant}/directories/{id} - Get specific directory details
        directoryGroup.MapGet("/{id}", async (
            string tenant,
            string id,
            CmsLiteDbContext db,
            IDirectoryRepo directoryRepo) =>
        {
            // Get tenant ID
            var (tenantSuccess, tenantId, tenantError) = await DbHelper.GetTenantIdAsync(tenant, db);
            if (!tenantSuccess) return tenantError!;

            var directory = await directoryRepo.GetDirectoryByIdAsync(id);
            if (directory == null)
            {
                return Results.NotFound($"Directory with ID '{id}' not found");
            }

            // Verify directory belongs to tenant
            if (directory.TenantId != tenantId)
            {
                return Results.NotFound($"Directory with ID '{id}' not found");
            }

            var result = new
            {
                directory.Id,
                directory.Name,
                directory.Level,
                directory.ParentId,
                directory.TenantId,
                directory.CreatedAtUtc,
                directory.UpdatedAtUtc,
                IsRoot = directory.ParentId == null,
                directory.IsActive,
                ContentCount = directory.ContentItems?.Count ?? 0,
                SubDirectoryCount = directory.SubDirectories?.Count ?? 0
            };

            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithName("GetDirectoryById")
        .WithSummary("Get directory by ID")
        .WithDescription("Returns detailed information about a specific directory");

        // POST /v1/{tenant}/directories - Create new directory
        directoryGroup.MapPost("", async (
            string tenant,
            CreateDirectoryRequest request,
            CmsLiteDbContext db,
            IDirectoryRepo directoryRepo) =>
        {
            // Get tenant ID
            var (tenantSuccess, tenantId, tenantError) = await DbHelper.GetTenantIdAsync(tenant, db);
            if (!tenantSuccess) return tenantError!;

            // Validate parent directory if specified
            if (!string.IsNullOrEmpty(request.ParentId))
            {
                var parentDirectory = await directoryRepo.GetDirectoryByIdAsync(request.ParentId);
                if (parentDirectory == null || parentDirectory.TenantId != tenantId || !parentDirectory.IsActive)
                {
                    return Results.BadRequest($"Invalid parent directory ID: {request.ParentId}");
                }

                // Check if can create subdirectory (5-level limit)
                if (!await directoryRepo.CanCreateSubdirectoryAsync(request.ParentId))
                {
                    return Results.BadRequest("Maximum directory nesting level (5) exceeded. Cannot create subdirectory.");
                }
            }

            // Create directory
            var directory = new DbSet.Directory
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                ParentId = request.ParentId,
                Name = request.Name.Trim(),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                IsActive = true
            };

            try
            {
                await directoryRepo.CreateDirectoryAsync(directory);

                var result = new
                {
                    directory.Id,
                    directory.Name,
                    directory.Level,
                    directory.ParentId,
                    directory.CreatedAtUtc,
                    IsRoot = directory.ParentId == null
                };

                return Results.Created($"/v1/{tenant}/directories/{directory.Id}", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .RequireAuthorization()
        .WithName("CreateDirectory")
        .WithSummary("Create new directory")
        .WithDescription("Creates a new directory with optional parent directory");

        // GET /v1/{tenant}/directories/{id}/contents - Get directory contents (content items)
        directoryGroup.MapGet("/{id}/contents", async (
            string tenant,
            string id,
            int? limit,
            string? cursor,
            CmsLiteDbContext db,
            IDirectoryRepo directoryRepo) =>
        {
            // Get tenant ID
            var (tenantSuccess, tenantId, tenantError) = await DbHelper.GetTenantIdAsync(tenant, db);
            if (!tenantSuccess) return tenantError!;

            var directory = await directoryRepo.GetDirectoryByIdAsync(id);
            if (directory == null)
            {
                return Results.NotFound($"Directory with ID '{id}' not found");
            }

            // Verify directory belongs to tenant
            if (directory.TenantId != tenantId)
            {
                return Results.NotFound($"Directory with ID '{id}' not found");
            }

            var take = Math.Clamp(limit ?? 50, 1, 200);

            // Cursor-based pagination
            int afterId = 0;
            if (!string.IsNullOrEmpty(cursor) && int.TryParse(cursor, out var c))
                afterId = c;

            var contentQuery = db.ContentItemsTable
                .Where(ci => ci.DirectoryId == id && !ci.IsDeleted && ci.Id > afterId)
                .OrderBy(ci => ci.Id)
                .Take(take + 1);

            var contentItems = await contentQuery.ToListAsync();

            string? nextCursor = contentItems.Count > take ? contentItems[^1].Id.ToString() : null;
            if (contentItems.Count > take) contentItems.RemoveAt(contentItems.Count - 1);

            var result = new
            {
                Directory = new
                {
                    directory.Id,
                    directory.Name,
                    directory.Level,
                    directory.ParentId,
                    IsRoot = directory.ParentId == null
                },
                ContentItems = contentItems.Select(ci => new
                {
                    ci.Id,
                    ci.Resource,
                    ci.LatestVersion,
                    ci.ContentType,
                    ci.ByteSize,
                    ci.ETag,
                    ci.UpdatedAtUtc,
                    ci.CreatedAtUtc
                }),
                NextCursor = nextCursor,
                TotalCount = contentItems.Count
            };

            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithName("GetDirectoryContents")
        .WithSummary("Get directory contents")
        .WithDescription("Returns content items within a specific directory with pagination");
    }
}