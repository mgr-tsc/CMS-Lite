using System;
using System.Security.Claims;
using System.Text;
using CmsLite.Database;
using CmsLite.Database.Repositories;
using Microsoft.IdentityModel.Tokens;

namespace CmsLite.Authentication;

public class CmsLiteAuthenticationService : ICmsLiteAuthenticationService
{
    private IUserSessionRepo userSessionRepo;
    private IUserRepo userRepo;
    private readonly IConfiguration configuration;

    public CmsLiteAuthenticationService(IUserSessionRepo userSessionRepo, IUserRepo userRepo, IConfiguration configuration)
    {
        this.userSessionRepo = userSessionRepo ?? throw new ArgumentNullException(nameof(userSessionRepo));
        this.userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    private string? GenerateToken(string userId, string tenantId, CancellationToken cancellationToken = default)
    {
        return GenerateTokenWithSessionId(userId, tenantId, Guid.NewGuid().ToString());
    }

    private string? GenerateTokenWithSessionId(string userId, string tenantId, string sessionId)
    {
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var key = AuthenticationRegister.GetJwtKey(configuration);
        if (key == null)
            throw new InvalidOperationException("JWT key is not configured.");

        var jwtSection = configuration.GetSection("Jwt");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(ClaimTypes.PrimarySid, userId),
                new System.Security.Claims.Claim(ClaimTypes.GroupSid, tenantId),
                new System.Security.Claims.Claim(ClaimTypes.Sid, sessionId),
                new System.Security.Claims.Claim("LastLoginTime", DateTime.UtcNow.ToString())
            }),
            Expires = DateTime.UtcNow.AddMinutes(30),
            Issuer = jwtSection["Issuer"],
            Audience = jwtSection["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    Task<string?> ICmsLiteAuthenticationService.GenerateTokenAsync(string userId, string tenantId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GenerateToken(userId, tenantId));
    }

    public async Task<RefreshTokenResult?> RefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var key = AuthenticationRegister.GetJwtKey(configuration);

            // Get JWT configuration for proper validation
            var jwtSection = configuration.GetSection("Jwt");

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = jwtSection.GetValue<bool>("ValidateIssuer"),
                ValidateAudience = jwtSection.GetValue<bool>("ValidateAudience"),
                ValidateLifetime = true, // Always validate lifetime for refresh
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ClockSkew = TimeSpan.Zero // No clock skew for refresh tokens
            }, out SecurityToken validatedToken);

            if (validatedToken == null)
            {
                return null;
            }

            // Extract claims from the validated token
            var userId = principal.FindFirst(ClaimTypes.PrimarySid)?.Value;
            var tenantId = principal.FindFirst(ClaimTypes.GroupSid)?.Value;
            var sessionId = principal.FindFirst(ClaimTypes.Sid)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(sessionId))
            {
                return null;
            }

            // Verify the session is still active and valid
            var session = await userSessionRepo.GetSessionByIdAsync(sessionId, cancellationToken);
            if (session == null || session.IsRevoked || session.ExpiresAtUtc <= DateTime.UtcNow || session.User.Id != userId)
            {
                return null;
            }

            // Generate new token with new session ID for token rotation
            var newSessionId = Guid.NewGuid().ToString();
            var newToken = GenerateTokenWithSessionId(userId, tenantId, newSessionId);

            if (string.IsNullOrEmpty(newToken))
            {
                return null;
            }

            // Update the existing session with new token and session ID
            session.JwtToken = newToken;
            session.Id = newSessionId; // Token rotation - invalidate old session ID
            session.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30);

            await userSessionRepo.UpdateSessionAsync(session, cancellationToken);

            return new RefreshTokenResult
            {
                NewToken = newToken,
                SessionId = newSessionId,
                ExpiresAt = session.ExpiresAtUtc
            };
        }
        catch (Exception ex)
        {
            // TODO: Implement proper logging
            Console.WriteLine($"Token refresh error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> SignInAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await userRepo.GetUserByEmailAsync(email);
            if (user == null)
                return false;
            if (await CheckIfAlreadySignedInAsync(user.Id))
                    return false;
            var passwordHashed = user?.PasswordHash;
            if (passwordHashed == null)
                return false;
            if (!Helpers.Utilities.VerifyPassword(password, passwordHashed))
                return false;
            var jwtToken = GenerateToken(user.Id, user.TenantId, cancellationToken);
            if (jwtToken == null)
                return false;
            var session = new DbSet.UserSession
            {
                Id = Guid.NewGuid().ToString(),
                User = user,
                UserId = user.Id,
                JwtToken = jwtToken,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30),
                IsRevoked = false
            };
            await userSessionRepo.CreateSessionAsync(session);
            return true;
        }
        catch (Exception ex)
        {
            //TODO: Implement logging
            Console.WriteLine($"Error during sign-in: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CheckIfAlreadySignedInAsync(string userId, CancellationToken cancellationToken = default)
    {
        var existingSession = await userSessionRepo.GetActiveSessionByUserIdAsync(userId, cancellationToken);
        if (existingSession != null && existingSession.ExpiresAtUtc > DateTime.UtcNow)
        {
            return true;
        }
        return false;
    }

    public Task SignOutAsync(string userId, string sessionId, CancellationToken cancellationToken = default)
    {
        return userSessionRepo.DeleteSessionAsync(sessionId, cancellationToken);
    }

    public async Task<bool> AuthenticateSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await userSessionRepo.GetSessionByIdAsync(sessionId, cancellationToken);
        if (session != null && session.User.Id == userId && !session.IsRevoked && session.ExpiresAtUtc > DateTime.UtcNow)
        {
            return true;
        }
        return false;
    }
}
