using System;
namespace CmsLite.Helpers.RequestMappers;
using System.ComponentModel.DataAnnotations;


public record CreateDirectoryRequest(
    [Required][StringLength(128, MinimumLength = 1)] string Name,
    string? ParentId = null
);

public record UpdateDirectoryRequest(
    [Required][StringLength(128, MinimumLength = 1)] string Name
);