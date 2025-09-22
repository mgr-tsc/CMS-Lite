using System.ComponentModel.DataAnnotations;

namespace CmsLite.Authentication;

public record LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; init; } = string.Empty;
}

public record LoginResponse
{
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public UserInfo User { get; init; } = new();
}

public record UserInfo
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public TenantInfo Tenant { get; init; } = new();
}

public record TenantInfo
{
    public string Id { get; init; } = string.Empty;
}

public record LogoutResponse
{
    public string Message { get; init; } = "Logged out successfully";
}

public record RefreshTokenRequest
{
    [Required]
    public string Token { get; init; } = string.Empty;
}

public record RefreshTokenResponse
{
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}