using CmsLite.Database.Repositories;
using CmsLite.Middlewares;

namespace CmsLite.Authentication;

public static class AuthenticationServiceRegistration
{
    /// <summary>
    /// Register all the services and repositories required
    /// </summary>
    public static void AddCmsLiteAuthentication(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        builder.AddAuthenticationServices(configuration);
        builder.Services.AddAuthorization();
        // Register authentication service and repositories
        builder.Services.AddScoped<ICmsLiteAuthenticationService, CmsLiteAuthenticationService>();
        builder.Services.AddScoped<IUserRepo, UserRepo>();
        builder.Services.AddScoped<IUserSessionRepo, UserSessionRepo>();
        builder.Services.AddScoped<ITenantRepo, TenantRepo>();
        // Add logging
        builder.Services.AddLogging();
    }

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