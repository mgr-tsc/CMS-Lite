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

    // ========== Sanitization Tests ==========

    [Fact]
    public void SanitizeResourceName_HandlesSpaces()
    {
        var result = Utilities.SanitizeResourceName("Clean Code.pdf");
        Assert.Equal("clean-code.pdf", result);
    }

    [Fact]
    public void SanitizeResourceName_HandlesMultipleSpaces()
    {
        var result = Utilities.SanitizeResourceName("My   Document   File.pdf");
        Assert.Equal("my-document-file.pdf", result);
    }

    [Fact]
    public void SanitizeResourceName_HandlesUppercase()
    {
        var result = Utilities.SanitizeResourceName("SHOUTING.PDF");
        Assert.Equal("shouting.pdf", result);
    }

    [Fact]
    public void SanitizeResourceName_HandlesSpecialCharacters()
    {
        var result = Utilities.SanitizeResourceName("file!@#$%^&*().pdf");
        Assert.Equal("file.pdf", result);
    }

    [Fact]
    public void SanitizeResourceName_HandlesUnderscores()
    {
        var result = Utilities.SanitizeResourceName("my_file_name.pdf");
        Assert.Equal("my_file_name.pdf", result);
    }

    [Fact]
    public void SanitizeResourceName_HandlesMixedCase()
    {
        var result = Utilities.SanitizeResourceName("MyFile-2024_v1.pdf");
        Assert.Equal("myfile-2024_v1.pdf", result);
    }

    [Fact]
    public void SanitizeResourceName_HandlesLeadingTrailingHyphens()
    {
        var result = Utilities.SanitizeResourceName("--file-name--.pdf");
        Assert.Equal("file-name-.pdf", result); // Trailing hyphen before extension is preserved
    }

    [Fact]
    public void SanitizeResourceName_HandlesConsecutiveHyphens()
    {
        var result = Utilities.SanitizeResourceName("file---name.pdf");
        Assert.Equal("file-name.pdf", result);
    }

    [Fact]
    public void SanitizeResourceName_HandlesUnicodeCharacters()
    {
        var result = Utilities.SanitizeResourceName("café-résumé.pdf");
        Assert.Equal("caf-rsum.pdf", result); // Non-ASCII chars removed
    }

    [Fact]
    public void SanitizeResourceName_HandlesComplexExample()
    {
        var result = Utilities.SanitizeResourceName("Clean Code!!! - The Best Book (2024).pdf");
        Assert.Equal("clean-code-the-best-book-2024.pdf", result);
    }

    [Fact]
    public void SanitizeResourceName_PreservesValidNames()
    {
        var result = Utilities.SanitizeResourceName("my-valid-file_v1.2.pdf");
        Assert.Equal("my-valid-file_v1.2.pdf", result);
    }

    [Fact]
    public void SanitizeResourceName_HandlesNullOrWhitespace()
    {
        Assert.Null(Utilities.SanitizeResourceName(null));
        Assert.Equal("   ", Utilities.SanitizeResourceName("   "));
    }

    [Fact]
    public void ParseTenantResource_SanitizesResourceName()
    {
        var (tenant, resource) = Utilities.ParseTenantResource("acme", "Clean Code.pdf");
        Assert.Equal("acme", tenant);
        Assert.Equal("clean-code.pdf", resource);
    }

    [Fact]
    public void ParseTenantResource_SanitizesTenantName()
    {
        var (tenant, resource) = Utilities.ParseTenantResource("ACME Corp!", "home.json");
        Assert.Equal("acme-corp", tenant);
        Assert.Equal("home.json", resource);
    }

    [Fact]
    public void ParseTenantResource_HandlesComplexSanitization()
    {
        var (tenant, resource) = Utilities.ParseTenantResource("My Company (2024)", "Annual Report - Q4.pdf");
        Assert.Equal("my-company-2024", tenant);
        Assert.Equal("annual-report-q4.pdf", resource);
    }
}
