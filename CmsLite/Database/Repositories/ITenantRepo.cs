using System;

namespace CmsLite.Database.Repositories;

public interface ITenantRepo
{
    Task<DbSet.Tenant?> GetTenantByIdAsync(string tenantId);
    Task<DbSet.Tenant?> GetTenantByNameAsync(string name);
    Task CreateTenantAsync(DbSet.Tenant tenant);
    Task UpdateTenantAsync(DbSet.Tenant tenant);
    Task DeleteTenantAsync(string tenantId);

}
