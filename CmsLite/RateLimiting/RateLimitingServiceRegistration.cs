using System.Threading.RateLimiting;

namespace CmsLite.RateLimiting;

public static class RateLimitingServiceRegistration
{
    public static void AddCmsRateLimiting(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        builder.Services.AddRateLimiter(options =>
        {
            // Authentication endpoints (login, logout, refresh)
            options.AddPolicy("auth", context =>
                RateLimitPartition.GetFixedWindowLimiter(GetPartitionKey(context), key => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = configuration.GetValue("RateLimiting:Auth:PermitLimit", 10),
                    Window = TimeSpan.FromMinutes(configuration.GetValue("RateLimiting:Auth:WindowMinutes", 1)),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = configuration.GetValue("RateLimiting:Auth:QueueLimit", 2)
                }));

            // Content read operations (GET)
            options.AddPolicy("content-read", context =>
                RateLimitPartition.GetFixedWindowLimiter(GetPartitionKey(context), key => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = configuration.GetValue("RateLimiting:ContentRead:PermitLimit", 100),
                    Window = TimeSpan.FromMinutes(configuration.GetValue("RateLimiting:ContentRead:WindowMinutes", 1)),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = configuration.GetValue("RateLimiting:ContentRead:QueueLimit", 5)
                }));

            // Content write operations (PUT, POST)
            options.AddPolicy("content-write", context =>
                RateLimitPartition.GetFixedWindowLimiter(GetPartitionKey(context), key => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = configuration.GetValue("RateLimiting:ContentWrite:PermitLimit", 50),
                    Window = TimeSpan.FromMinutes(configuration.GetValue("RateLimiting:ContentWrite:WindowMinutes", 1)),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = configuration.GetValue("RateLimiting:ContentWrite:QueueLimit", 3)
                }));

            // Bulk operations (bulk delete, etc.)
            options.AddPolicy("bulk-operations", context =>
                RateLimitPartition.GetFixedWindowLimiter(GetPartitionKey(context), key => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = configuration.GetValue("RateLimiting:BulkOperations:PermitLimit", 10),
                    Window = TimeSpan.FromMinutes(configuration.GetValue("RateLimiting:BulkOperations:WindowMinutes", 1)),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = configuration.GetValue("RateLimiting:BulkOperations:QueueLimit", 1)
                }));

            // Admin operations (tenant management)
            options.AddPolicy("admin", context =>
                RateLimitPartition.GetFixedWindowLimiter(GetPartitionKey(context), key => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = configuration.GetValue("RateLimiting:Admin:PermitLimit", 20),
                    Window = TimeSpan.FromMinutes(configuration.GetValue("RateLimiting:Admin:WindowMinutes", 1)),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = configuration.GetValue("RateLimiting:Admin:QueueLimit", 2)
                }));

            // Global rejection status code
            options.RejectionStatusCode = 429; // Too Many Requests

            // Add helpful response headers
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429;

                // Add rate limit headers (use indexer to avoid duplicate key exception)
                context.HttpContext.Response.Headers["X-RateLimit-Policy"] = GetRateLimitPolicyName(context.HttpContext);
                context.HttpContext.Response.Headers["Retry-After"] = "60";

                await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", token);
            };
        });
    }

    private static string GetPartitionKey(HttpContext context)
    {
        // For authenticated users, use user ID
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst("sub")?.Value ?? "unknown";
            return $"user:{userId}";
        }

        // For unauthenticated requests, use IP address
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{clientIp}";
    }

    private static string GetRateLimitPolicyName(HttpContext context)
    {
        return context.GetEndpoint()?.Metadata
            .GetMetadata<Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute>()?.PolicyName ?? "global";
    }
}