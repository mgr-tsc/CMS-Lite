using System;
using CmsLite.Helpers;

namespace CmsLiteTests;

public class UtilitiesTests
{
    [Fact]
    public void ParseTenantResource_TrimsAndReturnsValues()
    {
        var (tenant, resource) = Utilities.ParseTenantResource(" acme ", " home ");
        Assert.Equal("acme", tenant);
        Assert.Equal("home", resource);
    }

    [Fact]
    public void ParseTenantResource_ThrowsWhenMissing()
    {
        Assert.Throws<ArgumentException>(() => Utilities.ParseTenantResource("", "home"));
        Assert.Throws<ArgumentException>(() => Utilities.ParseTenantResource("acme", "   "));
    }

    [Fact]
    public void ParseTenantResource_ThrowsWhenContainsSlash()
    {
        Assert.Throws<ArgumentException>(() => Utilities.ParseTenantResource("ac/id", "home"));
        Assert.Throws<ArgumentException>(() => Utilities.ParseTenantResource("acme", "ho/me"));
    }
}
