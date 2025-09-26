using System;
using Microsoft.EntityFrameworkCore;
using CmsLite.Helpers.RequestMappers;

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
        return await dbContext.DirectoriesTable
            .Include(d => d.ContentItems.Where(ci => !ci.IsDeleted))
            .Include(d => d.SubDirectories.Where(sd => sd.IsActive))
            .FirstOrDefaultAsync(d => d.Id == directoryId);
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

        var directories = await dbContext.DirectoriesTable
            .FromSqlRaw(sqlQuery, tenantId)
            .ToListAsync();

        // Load ContentItems separately for each directory to get proper counts
        foreach (var directory in directories)
        {
            await dbContext.Entry(directory)
                .Collection(d => d.ContentItems)
                .Query()
                .Where(ci => !ci.IsDeleted)
                .LoadAsync();

            await dbContext.Entry(directory)
                .Collection(d => d.SubDirectories)
                .Query()
                .Where(sd => sd.IsActive)
                .LoadAsync();
        }

        return directories;
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

    public async Task<DirectoryTreeResponse> GetFullDirectoryTreeAsync(string tenantId, string tenantName)
    {
        // Get all directories for the tenant with their content items
        var directories = await GetDirectoryTreeAsync(tenantId);

        // Find the root directory
        var rootDirectory = directories.FirstOrDefault(d => d.ParentId == null);
        if (rootDirectory == null)
        {
            // Create default response if no root directory exists
            return new DirectoryTreeResponse
            {
                TenantId = tenantId,
                TenantName = tenantName,
                RootDirectory = new DirectoryNode
                {
                    Id = string.Empty,
                    Name = "Root",
                    Level = 0,
                    SubDirectories = new List<DirectoryNode>(),
                    ContentItems = new List<ContentItemSummary>()
                },
                TotalDirectories = 0,
                TotalContentItems = 0
            };
        }

        // Build the hierarchical tree structure
        var rootNode = BuildDirectoryNode(rootDirectory, directories);

        // Calculate totals
        var totalDirectories = directories.Count;
        var totalContentItems = directories.Sum(d => d.ContentItems.Count);

        return new DirectoryTreeResponse
        {
            TenantId = tenantId,
            TenantName = tenantName,
            RootDirectory = rootNode,
            TotalDirectories = totalDirectories,
            TotalContentItems = totalContentItems
        };
    }

    private DirectoryNode BuildDirectoryNode(DbSet.Directory directory, List<DbSet.Directory> allDirectories)
    {
        // Convert content items to summaries
        var contentItemSummaries = directory.ContentItems
            .Where(ci => !ci.IsDeleted)
            .Select(ci => new ContentItemSummary
            {
                Resource = ci.Resource,
                LatestVersion = ci.LatestVersion,
                ContentType = ci.ContentType,
                IsDeleted = ci.IsDeleted,
                Size = Helpers.Utilities.CalculateFileSizeInBestUnit(ci.ByteSize)
            }).ToList();

        // Find child directories
        var childDirectories = allDirectories
            .Where(d => d.ParentId == directory.Id)
            .OrderBy(d => d.Name)
            .ToList();

        // Recursively build child nodes
        var childNodes = childDirectories
            .Select(child => BuildDirectoryNode(child, allDirectories))
            .ToList();

        return new DirectoryNode
        {
            Id = directory.Id,
            Name = directory.Name,
            Level = directory.Level,
            SubDirectories = childNodes,
            ContentItems = contentItemSummaries
        };
    }
}
