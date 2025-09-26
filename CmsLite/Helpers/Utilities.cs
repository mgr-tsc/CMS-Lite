namespace CmsLite.Helpers;

public enum FileSizeUnit
{
    Bytes,
    KB,
    MB
}

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

    public static bool IsValidJson(byte[] data)
    {
        try
        {
            System.Text.Json.JsonDocument.Parse(data);
            return true;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    public static bool IsValidJsonWithComments(byte[] data)
    {
        var options = new System.Text.Json.JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = System.Text.Json.JsonCommentHandling.Skip
        };
        try
        {
            System.Text.Json.JsonDocument.Parse(data, options);
            return true;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    public static (float result, FileSizeUnit unit) CalculateMbFromBytes(long byteCount)
    {
        var result = byteCount / (1024f * 1024f);
        // Round to 2 decimal places
        return ((float)Math.Round(result, 2), FileSizeUnit.MB);
    }

    public static (float result, FileSizeUnit unit) CalculateKbFromBytes(long byteCount)
    {
        var result = byteCount / 1024f;
        // Round to 2 decimal places
        return ((float)Math.Round(result, 2), FileSizeUnit.KB);
    }

    public static string CalculateFileSizeInBestUnit(long byteCount)
    {
        if (byteCount < 0) throw new ArgumentOutOfRangeException(nameof(byteCount), "Byte count must be non-negative.");
        if (byteCount >= 1024 * 1024)
            return $"{CalculateMbFromBytes(byteCount).result} MB";
        else
            return $"{CalculateKbFromBytes(byteCount).result} KB";
    }

}
