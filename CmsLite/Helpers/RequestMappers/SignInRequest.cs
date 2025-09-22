using System;

namespace CmsLite.Helpers.RequestMappers;

public class SignInRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
