using System;
using Microsoft.EntityFrameworkCore;

namespace CmsLite.Database.Repositories;

public class DirectoryRepo : IDirectoryRepo
{
    private readonly CmsLiteDbContext dbContext;

    public DirectoryRepo(CmsLiteDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<DbSet.Directory?> GetDirectoryByIdAsync(string directoryId)
    {
        return await dbContext.DirectoriesTable.FindAsync(directoryId);
    }

    public async Task CreateDirectoryAsync(DbSet.Directory directory)
    {
        // Set the level based on parent directory
        if (directory.ParentId != null)
        {
            // Validate nesting level before creating
            if (!await CanCreateSubdirectoryAsync(directory.ParentId))
            {
                throw new InvalidOperationException("Maximum directory nesting level (5) exceeded. Cannot create subdirectory.");
            }

            var parentLevel = await GetDirectoryDepthAsync(directory.ParentId);
            directory.Level = parentLevel + 1;
        }
        else
        {
            // Root directory
            directory.Level = 0;
        }

        await dbContext.DirectoriesTable.AddAsync(directory);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteDirectoryAsync(string directoryId)
    {
        var directory = await GetDirectoryByIdAsync(directoryId);
        if (directory != null)
        {
            dbContext.DirectoriesTable.Remove(directory);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<List<DbSet.Directory>> GetDirectoryTreePerTenant(string tenantId)
    {
        return await GetDirectoryTreeAsync(tenantId);
    }

    private async Task<List<DbSet.Directory>> GetDirectoryTreeAsync(string tenantId)
    {
        var sqlQuery = @"
            WITH RECURSIVE DIRECTORY_TREE AS (
                -- BASE CASE: ROOT DIRECTORIES (ParentId IS NULL)
                SELECT Id, TenantId, ParentId, Name, CreatedAtUtc, UpdatedAtUtc, IsActive, Level, 0 AS QueryLevel
                    FROM Directory
                        WHERE ParentId IS NULL AND IsActive = 1 AND TenantId = {0}
                UNION ALL
                -- RECURSIVE CASE: CHILD DIRECTORIES
                SELECT D.Id, D.TenantId, D.ParentId, D.Name, D.CreatedAtUtc, D.UpdatedAtUtc, D.IsActive, D.Level, DT.QueryLevel + 1
                    FROM Directory D
                        INNER JOIN DIRECTORY_TREE DT ON D.ParentId = DT.Id
                            WHERE D.IsActive = 1 AND D.TenantId = {0}
            )
        SELECT Id, TenantId, ParentId, Name, CreatedAtUtc, UpdatedAtUtc, IsActive, Level FROM DIRECTORY_TREE ORDER BY Level, Name";
        return await dbContext.DirectoriesTable
            .FromSqlRaw(sqlQuery, tenantId)
            .ToListAsync();
    }

    public async Task<int> GetDirectoryDepthAsync(string directoryId)
    {
        var directory = await dbContext.DirectoriesTable.FindAsync(directoryId);
        if (directory == null)
        {
            throw new InvalidOperationException($"Directory with ID {directoryId} not found");
        }
        return directory.Level;
    }

    public async Task<bool> CanCreateSubdirectoryAsync(string parentId)
    {
        var depth = await GetDirectoryDepthAsync(parentId);
        return depth < 4;
    }

    public async Task<bool> IsRootDirectoryAsync(string directoryId)
    {
        var directory = await dbContext.DirectoriesTable.FindAsync(directoryId);
        return directory?.ParentId == null;
    }

    public async Task<DbSet.Directory> GetOrCreateRootDirectoryAsync(string tenantId)
    {
        // Try to find existing root directory for tenant
        var rootDirectory = await dbContext.DirectoriesTable
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.ParentId == null && d.IsActive);

        if (rootDirectory != null)
        {
            return rootDirectory;
        }

        // Create root directory if it doesn't exist
        rootDirectory = new DbSet.Directory
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            ParentId = null,
            Name = "Root",
            Level = 0,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        await dbContext.DirectoriesTable.AddAsync(rootDirectory);
        await dbContext.SaveChangesAsync();
        return rootDirectory;
    }
}
