using CmsLite.Database.Repositories;
using CmsLite.Middlewares;

namespace CmsLite.Authentication;

public static class AuthenticationServiceRegistration
{
    /// <summary>
    /// Registers all authentication-related services in the DI container
    /// </summary>
    public static void AddCmsLiteAuthentication(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        // Add authentication services
        builder.AddAuthenticationServices(configuration);
        // Register authentication service and repositories
        builder.Services.AddScoped<ICmsLiteAuthenticationService, CmsLiteAuthenticationService>();
        builder.Services.AddScoped<IUserRepo, UserRepo>();
        builder.Services.AddScoped<IUserSessionRepo, UserSessionRepo>();
        builder.Services.AddLogging();
    }

    /// <summary>
    /// Configures the authentication middleware pipeline and endpoints
    /// </summary>
    public static void UseCmsLiteAuthentication(this WebApplication app)
    {
        // Add authentication middleware (required for JWT validation)
        app.UseAuthentication();
        // Map authentication endpoints
        app.MapAuthenticationEndpoints();
    }
}