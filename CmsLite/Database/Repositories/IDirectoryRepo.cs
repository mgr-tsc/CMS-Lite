using System;

namespace CmsLite.Database.Repositories;

public interface IDirectoryRepo
{
    Task<DbSet.Directory?> GetDirectoryByIdAsync(string directoryId);
    Task CreateDirectoryAsync(DbSet.Directory directory);
    Task DeleteDirectoryAsync(string directoryId);
    Task<List<DbSet.Directory>> GetDirectoryTreePerTenant (string tenantId);
    Task<int> GetDirectoryDepthAsync(string directoryId);
    Task<bool> CanCreateSubdirectoryAsync(string parentId);
    Task<bool> IsRootDirectoryAsync(string directoryId);
    Task<DbSet.Directory> GetOrCreateRootDirectoryAsync(string tenantId);
}
