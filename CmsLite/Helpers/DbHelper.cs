using System;
using CmsLite.Database;
using Microsoft.EntityFrameworkCore;

namespace CmsLite.Helpers;

public class DbHelper
{
    public static async Task<(bool Success, string TenantId, IResult? ErrorResult)> GetTenantIdAsync(string tenantName, CmsLiteDbContext db)
    {
        var tenantEntity = await db.TenantsTable.FirstOrDefaultAsync(t => t.Name == tenantName);
        if (tenantEntity == null)
        {
            return (false, string.Empty, Results.BadRequest($"Tenant '{tenantName}' not found"));
        }
        return (true, tenantEntity.Id, null);
    }
}
