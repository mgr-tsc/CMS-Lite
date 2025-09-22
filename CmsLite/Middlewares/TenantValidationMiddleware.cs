using System.Security.Claims;

namespace CmsLite.Middlewares;

public class TenantValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantValidationMiddleware> _logger;

    public TenantValidationMiddleware(RequestDelegate next, ILogger<TenantValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsContentEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        try
        {
            // Extract tenant from URL path
            var urlTenant = ExtractTenantFromPath(context.Request.Path);
            if (string.IsNullOrEmpty(urlTenant))
            {
                _logger.LogWarning("Could not extract tenant from path: {Path}", context.Request.Path);
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid tenant in URL");
                return;
            }

            // Get user's tenant from JWT claims
            var userTenantId = context.User.FindFirst(ClaimTypes.GroupSid)?.Value;
            if (string.IsNullOrEmpty(userTenantId))
            {
                _logger.LogWarning("User token missing tenant claim");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid authentication token");
                return;
            }

            // Validate tenant access
            // For now, we'll use simple string comparison since we don't have tenant name resolution
            // TODO: Implement proper tenant ID to name mapping
            if (!string.Equals(urlTenant, userTenantId, StringComparison.OrdinalIgnoreCase))
            {
                var userId = context.User.FindFirst(ClaimTypes.PrimarySid)?.Value;
                _logger.LogWarning("Tenant access denied. User: {UserId}, UserTenant: {UserTenant}, RequestedTenant: {RequestedTenant}",
                    userId, userTenantId, urlTenant);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync($"Access denied to tenant '{urlTenant}'");
                return;
            }
            // Add tenant info to HttpContext for easy access in endpoints
            context.Items["TenantId"] = userTenantId;
            context.Items["UrlTenant"] = urlTenant;
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tenant validation middleware");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Internal server error");
        }
    }


    //TODO: Improve path matching and tenant extraction logic
    private static bool IsContentEndpoint(PathString path)
    {
        // Check if path matches content API pattern: /v1/{tenant}/{resource}
        var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments == null || segments.Length < 3)
            return false;

        // Must start with v1 and have at least tenant and resource
        return segments[0].Equals("v1", StringComparison.OrdinalIgnoreCase) && segments.Length >= 3;
    }

    private static string? ExtractTenantFromPath(PathString path)
    {
        // Extract tenant from path like: /v1/{tenant}/{resource}
        var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments == null || segments.Length < 3)
            return null;
        if (!segments[0].Equals("v1", StringComparison.OrdinalIgnoreCase))
            return null;
        return segments[1]; // Return the tenant segment
    }
}