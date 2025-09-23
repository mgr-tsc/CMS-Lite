using System;
using Microsoft.EntityFrameworkCore;

namespace CmsLite.Database.Repositories;

public class ContentItemRepo : IContentItemRepo
{
    private readonly CmsLiteDbContext dbContext;
    public ContentItemRepo(CmsLiteDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    public async Task CreateContentItemAsync(DbSet.ContentItem contentItem)
    {
        await dbContext.ContentItemsTable.AddAsync(contentItem);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteContentItemAsync(string contentItemId)
    {
        var contentItem = await GetContentItemByIdAsync(contentItemId);
        if (contentItem != null)
        {
            contentItem.IsDeleted = true; // Soft delete
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<DbSet.ContentItem?> GetContentItemByIdAsync(string contentItemId)
    {
        return await dbContext.ContentItemsTable.FindAsync(contentItemId);
    }

    public async Task<List<DbSet.ContentItem>> GetContentItemsByDirectoryIdAsync(string directoryId)
    {
        return await dbContext.ContentItemsTable
            .Where(ci => ci.DirectoryId == directoryId && !ci.IsDeleted)
            .ToListAsync();
    }

    public async Task<List<DbSet.ContentItem>> GetContentItemsByTenantIdAsync(string tenantId)
    {
        return await dbContext.ContentItemsTable.Where(ci => ci.TenantId == tenantId && !ci.IsDeleted).ToListAsync();
    }
}
