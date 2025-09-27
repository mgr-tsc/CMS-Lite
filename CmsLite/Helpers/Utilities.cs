namespace CmsLite.Helpers;

public enum FileSizeUnit
{
    Bytes,
    KB,
    MB
}

public enum SupportedContentType
{
    Json,
    Xml
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

    public static bool IsValidXml(byte[] data)
    {
        try
        {
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(System.Text.Encoding.UTF8.GetString(data));
            return true;
        }
        catch (System.Xml.XmlException)
        {
            return false;
        }
    }

    //TODO: Implement XML schema validation
    public static bool IsValidXmlWithSchema(byte[] data, string schemaPath)
    {
        throw new NotImplementedException();
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

    public static SupportedContentType ParseContentType(string contentTypeHeader)
    {
        // Extract just the media type (before any semicolon) and normalize
        var mediaType = contentTypeHeader.Split(';')[0].Trim().ToLower();

        return mediaType switch
        {
            "application/json" => SupportedContentType.Json,
            "application/xml" => SupportedContentType.Xml,
            "text/xml" => SupportedContentType.Xml,
            _ => throw new ArgumentException($"Unsupported content type '{mediaType}'. Only 'application/json', 'application/xml', and 'text/xml' are supported.")
        };
    }

    public static string GenerateBlobKey(string tenant, string resource, int version, SupportedContentType contentType)
    {
        var ext = contentType.ToString().ToLower();
        return $"{tenant}/{resource}_v{version}.{ext}";
    }
}
