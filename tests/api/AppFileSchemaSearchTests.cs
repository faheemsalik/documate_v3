namespace Documate.Api.Tests;

using Documate.Api.Modules.FrontendSupport.Features.Files;

public class AppFileSchemaSearchTests
{
    [Theory]
    [InlineData("invoice_number", true)]
    [InlineData("_private", true)]
    [InlineData("line_items", true)]
    [InlineData("bad-key", false)]
    [InlineData("1startsWithDigit", false)]
    [InlineData("", false)]
    public void TryBuildJsonPath_validates_field_key(string fieldKey, bool expected)
    {
        var ok = AppFileSchemaSearch.TryBuildJsonPath(fieldKey, out var jsonPath, out var error);
        Assert.Equal(expected, ok);
        if (expected)
        {
            Assert.Equal("$." + fieldKey, jsonPath);
            Assert.Null(error);
        }
        else
        {
            Assert.False(string.IsNullOrWhiteSpace(error));
        }
    }

    [Fact]
    public void TryNormalizeSearchValue_rejects_blank()
    {
        var ok = AppFileSchemaSearch.TryNormalizeSearchValue("  ", out _, out var error);
        Assert.False(ok);
        Assert.Contains("required", error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null, "exact")]
    [InlineData("exact", "exact")]
    [InlineData("contains", "contains")]
    [InlineData("CONTAINS", "contains")]
    [InlineData("other", "exact")]
    public void NormalizeMatchMode(string? input, string expected)
    {
        Assert.Equal(expected, AppFileSchemaSearch.NormalizeMatchMode(input));
    }

    [Fact]
    public void EscapeLike_escapes_wildcards()
    {
        Assert.Equal("[%]100", AppFileSchemaSearch.EscapeLike("%100"));
    }
}
