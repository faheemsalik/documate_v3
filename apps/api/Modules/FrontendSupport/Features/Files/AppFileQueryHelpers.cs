namespace Documate.Api.Modules.FrontendSupport.Features.Files;

public static class AppFileQueryHelpers
{
    public static (int Page, int PageSize) NormalizePaging(int page, int pageSize)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize switch
        {
            < 1 => 50,
            > 200 => 200,
            _ => pageSize,
        };
        return (normalizedPage, normalizedPageSize);
    }
}
