using System.ComponentModel.DataAnnotations;

namespace CmsLite.Helpers.RequestMappers;

public record DirectoryTreeRequest
{
    [Required]
    public string TenantName { get; init; } = string.Empty;
}

public record DirectoryTreeResponse
{
    public string TenantId { get; init; } = string.Empty;
    public string TenantName { get; init; } = string.Empty;
    public DirectoryNode RootDirectory { get; init; } = new();
    public int TotalDirectories { get; init; }
    public int TotalContentItems { get; init; }
}

public record DirectoryNode
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Level { get; init; }
    public List<DirectoryNode> SubDirectories { get; init; } = new();
    public List<ContentItemSummary> ContentItems { get; init; } = new();
}

public record ContentItemSummary
{
    public string Resource { get; init; } = string.Empty;
    public int LatestVersion { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public bool IsDeleted { get; init; }
}