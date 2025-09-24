using CmsLite.Database.Repositories;

namespace CmsLite.Authentication;

//TODO: Move this class and code to a separate folder where all the service registrations are done
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
    }

    /// <summary>
    /// Register all the repositories required
    /// <param name="builder">
    /// The WebApplicationBuilder to add repositories to
    /// </param>
    /// </summary>
    public static void AddCmsRepositories(this WebApplicationBuilder builder)
    {
        // Register repositories
        builder.Services.AddScoped<IUserRepo, UserRepo>();
        builder.Services.AddScoped<IUserSessionRepo, UserSessionRepo>();
        builder.Services.AddScoped<ITenantRepo, TenantRepo>();
        builder.Services.AddScoped<IDirectoryRepo, DirectoryRepo>();
        builder.Services.AddScoped<IContentItemRepo, ContentItemRepo>();
    }

    /// <summary>
    /// Registers logging services
    /// </summary>
    /// <param name="builder">
    /// The WebApplicationBuilder to add logging services to
    /// </param>
    public static void AddLoggingServices(this WebApplicationBuilder builder)
    {
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