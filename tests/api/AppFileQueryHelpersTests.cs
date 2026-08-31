namespace Documate.Api.Tests;

using Documate.Api.Modules.FrontendSupport.Features.Files;

public class AppFileQueryHelpersTests
{
    [Theory]
    [InlineData(0, 50, 1, 50)]
    [InlineData(-1, 25, 1, 25)]
    [InlineData(2, 0, 2, 50)]
    [InlineData(1, 500, 1, 200)]
    [InlineData(3, 100, 3, 100)]
    public void NormalizePaging_clamps_inputs(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var (p, ps) = AppFileQueryHelpers.NormalizePaging(page, pageSize);
        Assert.Equal(expectedPage, p);
        Assert.Equal(expectedPageSize, ps);
    }
}
