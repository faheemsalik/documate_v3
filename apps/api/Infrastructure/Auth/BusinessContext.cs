namespace Documate.Api.Infrastructure.Auth;

using System.Security.Claims;

public interface IBusinessContext
{
    string UserId { get; }
    string TenantId { get; }
    string BusinessId { get; }
    string? TenantName { get; }
    string? BusinessName { get; }
    bool IsAuthenticated { get; }
}

public sealed class BusinessContext : IBusinessContext
{
    public string UserId { get; init; } = "";
    public string TenantId { get; init; } = "";
    public string BusinessId { get; init; } = "";
    public string? TenantName { get; init; }
    public string? BusinessName { get; init; }
    public bool IsAuthenticated { get; init; }

    public static BusinessContext FromPrincipal(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return new BusinessContext { IsAuthenticated = false };
        }

        return new BusinessContext
        {
            IsAuthenticated = true,
            UserId = principal.FindFirstValue(AuthClaimTypes.UserId)
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "",
            TenantId = principal.FindFirstValue(AuthClaimTypes.TenantId) ?? "",
            BusinessId = principal.FindFirstValue(AuthClaimTypes.BusinessId) ?? "",
            TenantName = principal.FindFirstValue(AuthClaimTypes.TenantName),
            BusinessName = principal.FindFirstValue(AuthClaimTypes.BusinessName),
        };
    }
}

public sealed class BusinessContextAccessor(IHttpContextAccessor httpContextAccessor) : IBusinessContext
{
    private BusinessContext Current => BusinessContext.FromPrincipal(httpContextAccessor.HttpContext?.User);

    public string UserId => Current.UserId;
    public string TenantId => Current.TenantId;
    public string BusinessId => Current.BusinessId;
    public string? TenantName => Current.TenantName;
    public string? BusinessName => Current.BusinessName;
    public bool IsAuthenticated => Current.IsAuthenticated;
}
