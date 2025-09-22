using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace CmsLite.Authentication;


/// <summary>
/// Provides extension methods to register authentication services with JWT Bearer configuration.
/// </summary>
public static class AuthenticationRegister
{
    /// <summary>
    /// Configures the default authentication and challenge schemes to use JWT Bearer authentication.
    /// This delegate ensures that the authentication pipeline uses JWT tokens for both authentication and challenge processes.
    /// </summary>
    private static readonly Action<AuthenticationOptions> ConfigureAuthenticationOptions = options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    };

    /// <summary>
    /// Configures the JwtBearerOptions based on the provided IConfiguration and IWebHostEnvironment.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="environment">The web host environment.</param>
    /// <returns>The configured JwtBearerOptions.</returns>
    private static Action<JwtBearerOptions> GetJwtBearerOptionsConfiguration(IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Here we return the function that follows the signature of a delegate Action<JwtBearerOptions>
        // This function will be called by the AddJwtBearer method to configure the JwtBearerOptions
        return jwtBearerOptionsArgument =>
        {
            var jwtSection = configuration.GetSection("Jwt");
            var key = GetJwtKey(configuration);
            jwtBearerOptionsArgument.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = jwtSection.GetValue<bool>("ValidateIssuer"),
                ValidateAudience = jwtSection.GetValue<bool>("ValidateAudience"),
                ValidateLifetime = jwtSection.GetValue<bool>("ValidateLifetime"),
                ValidateIssuerSigningKey = jwtSection.GetValue<bool>("ValidateIssuerSigningKey"),
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ClockSkew = environment.IsDevelopment() ? TimeSpan.FromMinutes(5) : TimeSpan.Zero
            };
            // Additional development-specific configurations
            if (environment.IsDevelopment())
            {
                jwtBearerOptionsArgument.RequireHttpsMetadata = false;
                jwtBearerOptionsArgument.SaveToken = true;
            }
            else
            {
                jwtBearerOptionsArgument.RequireHttpsMetadata = true;
                jwtBearerOptionsArgument.SaveToken = false;
            }
            // TODO: Add event handlers for logging and error handling as needed and improve with centralized and structured logging
            jwtBearerOptionsArgument.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    // Log the exception or handle it as needed
                    Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    // Additional validation can be done here if necessary
                    Console.WriteLine("Token validated successfully.");
                    var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;
                    var userId = claimsIdentity?.FindFirst(ClaimTypes.PrimarySid)?.Value;
                    var tenantId = claimsIdentity?.FindFirst(ClaimTypes.GroupSid)?.Value;
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    // Handle the challenge response if needed
                    Console.WriteLine("OnChallenge event triggered.");
                    return Task.CompletedTask;
                }
            };
        };
    }

    /// <summary>
    /// Retrieves the JWT secret key from environment variables, user secrets, or provides a development fallback.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="environment">The web host environment.</param>
    /// <returns>The JWT secret key.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    internal static string GetJwtKey(IConfiguration configuration )
    {
        // 1. Try environment variable first (for production)
        var envKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
        if (!string.IsNullOrEmpty(envKey))
        {
            return envKey;
        }

        // 2. Try user secrets (for development)
        var userSecretKey = configuration["Jwt:Key"];
        if (!string.IsNullOrEmpty(userSecretKey))
        {
            return userSecretKey;
        }

        throw new InvalidOperationException("JWT Key not configured. Please set JWT_SECRET_KEY environment variable for production " + "or configure user secrets for development using: dotnet user-secrets set \"Jwt:Key\" \"your-secret-key\"");
    }

    /// <summary>
    /// Registers authentication services with JWT Bearer configuration in the provided WebApplicationBuilder.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="Exception"></exception>
    /// <remarks> This method sets up JWT Bearer authentication using settings from the configuration.
    /// It ensures that the authentication services are properly configured for both development and production environments.
    /// </remarks>
    /// </summary>
    public static void AddAuthenticationServices(this WebApplicationBuilder builder, IConfiguration configuration)
    {
        var environment = builder.Environment ?? throw new ArgumentNullException(nameof(builder.Environment), "Environment is not configured.");
        try
        {
            builder.Services.AddAuthentication(ConfigureAuthenticationOptions).AddJwtBearer(GetJwtBearerOptionsConfiguration(configuration, environment));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to configure authentication services.", ex);
        }
    }
}