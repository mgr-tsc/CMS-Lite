using System;
using System.ComponentModel.DataAnnotations;

namespace CmsLite.Helpers.RequestMappers;

public record CreateDirectoryRequest(
    [Required][StringLength(128, MinimumLength = 1)] string Name,
    string? ParentId = null
);

public record UpdateDirectoryRequest(
    [Required][StringLength(128, MinimumLength = 1)] string Name
);