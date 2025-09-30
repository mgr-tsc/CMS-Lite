using System.ComponentModel.DataAnnotations;

namespace CmsLite.Helpers.RequestMappers;

public record ContentDetailsResponse
{
    public string Resource { get; init; } = string.Empty;
    public int LatestVersion { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public string Size { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public bool IsDeleted { get; init; }

    // Directory information
    public ContentDirectoryInfo Directory { get; init; } = new();

    // Version history summary
    public List<VersionSummary> Versions { get; init; } = new();

    // Metadata
    public ContentMetadata Metadata { get; init; } = new();
}

public record ContentDirectoryInfo
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string FullPath { get; init; } = string.Empty;
    public int Level { get; init; }
}

public record VersionSummary
{
    public int Version { get; init; }
    public string Size { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
}

public record ContentMetadata
{
    public string TenantId { get; init; } = string.Empty;
    public string TenantName { get; init; } = string.Empty;
    public bool HasMultipleVersions { get; init; }
    public int TotalVersions { get; init; }
    public string FileExtension { get; init; } = string.Empty;
    public string ReadableSize { get; init; } = string.Empty;
}