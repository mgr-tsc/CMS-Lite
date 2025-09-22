using System;
using Microsoft.EntityFrameworkCore;
namespace CmsLite.Database.Repositories;

public class TenantRepo : ITenantRepo
{
    private readonly CmsLiteDbContext dbContext;

    public TenantRepo(CmsLiteDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    public async Task CreateTenantAsync(DbSet.Tenant tenant)
    {
        await dbContext.TenantsTable.AddAsync(tenant);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteTenantAsync(string tenantId)
    {
        var tenant = await dbContext.TenantsTable.FindAsync(tenantId);
        if (tenant != null)
        {
            dbContext.TenantsTable.Remove(tenant);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<DbSet.Tenant?> GetTenantByIdAsync(string tenantId)
    {
        return await dbContext.TenantsTable.FindAsync(tenantId);
    }

    public async Task<DbSet.Tenant?> GetTenantByNameAsync(string name)
    {
        return await dbContext.TenantsTable.FirstOrDefaultAsync(t => t.Name == name);
    }

    public async Task UpdateTenantAsync(DbSet.Tenant tenant)
    {
        dbContext.TenantsTable.Update(tenant);
        await dbContext.SaveChangesAsync();
    }
}
