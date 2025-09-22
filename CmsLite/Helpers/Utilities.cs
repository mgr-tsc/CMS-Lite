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

    public static string HashPassword(string password)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    public static bool VerifyPassword(string password, string hashedPassword)
    {
        var hashedInput = HashPassword(password);
        return hashedInput == hashedPassword;
    }

}
