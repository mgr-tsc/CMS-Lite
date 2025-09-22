using System.ComponentModel.DataAnnotations;

namespace CmsLite.Helpers.RequestMappers;

public record SignUpRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public string TenantId { get; set; } = string.Empty;
}

public record SignUpResponse
{
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}