using System;

namespace CmsLite.Authentication;

public interface ICmsLiteAuthenticationService
{
    Task<string?> GenerateTokenAsync(string userId, string tenantId, CancellationToken cancellationToken = default);
    Task<RefreshTokenResult?> RefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> AuthenticateSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default);
    Task<bool> SignInAsync(string email, string password, CancellationToken cancellationToken = default);
    Task SignOutAsync(string userId, string sessionId, CancellationToken cancellationToken = default);
}

public record RefreshTokenResult
{
    public string NewToken { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}
