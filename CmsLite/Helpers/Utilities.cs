using System;

namespace CmsLite.Helpers;

public class Utilities
{
    public static (string tenant, string resource) ParseTenantResource(string tenant, string resource)
    {
        if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("Tenant and resource are required.");
        if (tenant.Contains('/') || resource.Contains('/'))
            throw new ArgumentException("Tenant/resource cannot contain '/'.");
        return (tenant.Trim(), resource.Trim());
    }

}
