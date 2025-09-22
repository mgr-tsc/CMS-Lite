using System;
using CmsLite.Database;

namespace CmsLite.Authentication;

public interface ICmsLiteAuthenticationService
{
    Task<RefreshTokenResult?> RefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> AuthenticateSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default);
    Task<SignInResult> SignInAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<DbSet.UserSession> CreateUserSessionAsync(DbSet.User user, CancellationToken cancellationToken = default);
    Task SignOutAsync(string userId, string sessionId, CancellationToken cancellationToken = default);
    Task<DbSet.UserSession?> GetActiveSessionAsync(string userId, CancellationToken cancellationToken = default);
    Task<DbSet.UserSession> RefreshExistingSessionAsync(DbSet.UserSession session, CancellationToken cancellationToken = default);
}

public record RefreshTokenResult
{
    public string NewToken { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}

public record SignInResult
{
    public bool IsSuccess { get; init; }
    public DbSet.User? User { get; init; }
    public string? ErrorMessage { get; init; }
    
    public static SignInResult Success(DbSet.User user) => new()
    {
        IsSuccess = true,
        User = user,
        ErrorMessage = null
    };
    
    public static SignInResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        User = null,
        ErrorMessage = errorMessage
    };
}
