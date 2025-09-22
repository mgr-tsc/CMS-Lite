using System.ComponentModel.DataAnnotations;

namespace CmsLite.Helpers.RequestMappers;

public record CreateTenantRequest
{
    // Tenant details
    [Required]
    [MinLength(3, ErrorMessage = "Tenant ID must be at least 3 characters long.")]
    [RegularExpression("^[a-zA-Z0-9_-]+$", ErrorMessage = "Tenant ID can only contain letters, numbers, underscores, and hyphens.")]
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Owner details
    [Required]
    [EmailAddress]
    public string OwnerEmail { get; set; } = string.Empty;
    [Required]
    public string OwnerFirstName { get; set; } = string.Empty;
    [Required]
    public string OwnerLastName { get; set; } = string.Empty;
    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string OwnerPassword { get; set; } = string.Empty;
}
