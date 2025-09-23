using System;

namespace CmsLite.Database.Repositories;

public interface IContentItemRepo
{
    Task<DbSet.ContentItem?> GetContentItemByIdAsync(string contentItemId);
    Task CreateContentItemAsync(DbSet.ContentItem contentItem);
    Task DeleteContentItemAsync(string contentItemId);
    Task<List<DbSet.ContentItem>> GetContentItemsByDirectoryIdAsync(string directoryId);
    Task<List<DbSet.ContentItem>> GetContentItemsByTenantIdAsync(string tenantId);

}
