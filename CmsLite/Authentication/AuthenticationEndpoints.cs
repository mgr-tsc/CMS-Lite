using System.Security.Claims;
using CmsLite.Database;
using CmsLite.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CmsLite.Authentication;

public static class AuthenticationEndpoints
{
    public static void MapAuthenticationEndpoints(this WebApplication app)
    {
        var authGroup = app.MapGroup("/auth").WithTags("Authentication");
        // POST /auth/login
        authGroup.MapPost("/login", async (
            LoginRequest request,
            ICmsLiteAuthenticationService authService,
            IUserRepo userRepo) =>
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return Results.BadRequest("Email and password are required");
                }
                // Attempt sign-in (validates credentials only, no session created yet)
                var signInResult = await authService.SignInAsync(request.Email, request.Password);
                if (!signInResult.IsSuccess)
                {
                    return Results.Unauthorized();
                }
                var user = signInResult.User!;
                // Check if user already has an active session
                var existingSession = await authService.GetActiveSessionAsync(user.Id);
                DbSet.UserSession session;
                if (existingSession != null)
                {
                    // User is already signed in, refresh the existing session
                    session = await authService.RefreshExistingSessionAsync(existingSession);
                }
                else
                {
                    // Create new session
                    session = await authService.CreateUserSessionAsync(user);
                }
                var response = new LoginResponse
                {
                    Token = session.JwtToken,
                    ExpiresAt = session.ExpiresAtUtc,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Tenant = new TenantInfo
                        {
                            Id = user.TenantId,
                            Name = user.Tenant.Name
                        }
                    }
                };
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                // TODO: Implement proper logging
                Console.WriteLine($"Login error: {ex.Message}");
                return Results.Problem("An error occurred during login");
            }
        }).WithName("Login")
        .WithSummary("User login")
        .WithDescription("Authenticate user with email and password")
        .RequireRateLimiting("auth");

        // POST /auth/logout
        authGroup.MapPost("/logout", async (
            HttpContext context,
            ICmsLiteAuthenticationService authService) =>
        {
            try
            {
                var userId = context.User.FindFirst(ClaimTypes.PrimarySid)?.Value;
                var sessionId = context.User.FindFirst(ClaimTypes.Sid)?.Value;

                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(sessionId))
                {
                    await authService.SignOutAsync(userId, sessionId);
                }

                return Results.Ok(new LogoutResponse());
            }
            catch (Exception ex)
            {
                // TODO: Implement proper logging
                Console.WriteLine($"Logout error: {ex.Message}");
                return Results.Ok(new LogoutResponse()); // Always return success for logout
            }
        }).RequireAuthorization()
        .WithName("Logout")
        .WithSummary("User logout")
        .WithDescription("Revoke current user session")
        .RequireRateLimiting("auth");

        // GET /auth/me
        authGroup.MapGet("/me", async (
            HttpContext context,
            IUserRepo userRepo) =>
        {
            try
            {
                var userId = context.User.FindFirst(ClaimTypes.PrimarySid)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var user = await userRepo.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return Results.Unauthorized();
                }

                var userInfo = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Tenant = new TenantInfo
                    {
                        Id = user.TenantId,
                    }
                };

                return Results.Ok(userInfo);
            }
            catch (Exception ex)
            {
                // TODO: Implement proper logging
                Console.WriteLine($"Get current user error: {ex.Message}");
                return Results.Problem("An error occurred while retrieving user information");
            }
        }).RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithSummary("Get current user")
        .WithDescription("Get information about the currently authenticated user")
        .RequireRateLimiting("auth");

        // POST /auth/refresh
        authGroup.MapPost("/refresh", async (
            RefreshTokenRequest request,
            ICmsLiteAuthenticationService authService) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Token))
                {
                    return Results.BadRequest("Token is required");
                }

                var refreshResult = await authService.RefreshTokenAsync(request.Token);
                if (refreshResult == null)
                {
                    return Results.Unauthorized();
                }

                var response = new RefreshTokenResponse
                {
                    Token = refreshResult.NewToken,
                    ExpiresAt = refreshResult.ExpiresAt
                };

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                // TODO: Implement proper logging
                Console.WriteLine($"Token refresh error: {ex.Message}");
                return Results.Unauthorized();
            }
        }).WithName("RefreshToken")
        .WithSummary("Refresh authentication token")
        .WithDescription("Generate a new token from an existing valid token")
        .RequireRateLimiting("auth");
    }
}