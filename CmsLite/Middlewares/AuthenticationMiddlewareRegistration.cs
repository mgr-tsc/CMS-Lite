using System;

namespace CmsLite.Middlewares;

public static class AuthenticationMiddlewareRegistration
{
    /// <summary>
    /// Configures the authentication middleware pipeline
    /// </summary>
    public static void UseCmsLiteAuthentication(this WebApplication app)
    {
        // Add authentication middleware (required for JWT validation)
        app.UseAuthentication();
        // Add authorization middleware (required to enforce [Authorize] attributes)
        app.UseAuthorization();
    }
}
