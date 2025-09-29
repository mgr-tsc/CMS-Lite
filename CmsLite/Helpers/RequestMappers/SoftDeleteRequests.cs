using System.ComponentModel.DataAnnotations;

namespace CmsLite.Helpers.RequestMappers;

public record SoftDeleteRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one resource is required")]
    [MaxLength(100, ErrorMessage = "Maximum 100 resources allowed per request")]
    public List<string> Resources { get; init; } = new();
}

public record SoftDeleteResponse
{
    public string TenantId { get; init; } = string.Empty;
    public string TenantName { get; init; } = string.Empty;
    public string DirectoryId { get; init; } = string.Empty;
    public string DirectoryPath { get; init; } = string.Empty;
    public int DeletedCount { get; init; }
    public List<DeletedResourceInfo> DeletedResources { get; init; } = new();
    public DateTime DeletedAtUtc { get; init; }
}

public record DeletedResourceInfo
{
    public string Resource { get; init; } = string.Empty;
    public int LatestVersion { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public string Size { get; init; } = string.Empty;
    public DateTime OriginalCreatedAtUtc { get; init; }
}

public record SoftDeleteErrorResponse
{
    public string Error { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public List<string> FailedResources { get; init; } = new();
    public string ValidationFailure { get; init; } = string.Empty;
}