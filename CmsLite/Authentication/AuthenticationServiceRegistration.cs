using CmsLite.Database.Repositories;

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
    }

}