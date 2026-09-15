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

/// <summary>Allows Hangfire/SES workers to run under a Business without HTTP claims.</summary>
public interface IBusinessContextSetter
{
    IDisposable Use(BusinessContext context);
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

public sealed class BusinessContextAccessor(IHttpContextAccessor httpContextAccessor)
    : IBusinessContext, IBusinessContextSetter
{
    private static readonly AsyncLocal<BusinessContext?> Override = new();

    private BusinessContext Current =>
        Override.Value ?? BusinessContext.FromPrincipal(httpContextAccessor.HttpContext?.User);

    public string UserId => Current.UserId;
    public string TenantId => Current.TenantId;
    public string BusinessId => Current.BusinessId;
    public string? TenantName => Current.TenantName;
    public string? BusinessName => Current.BusinessName;
    public bool IsAuthenticated => Current.IsAuthenticated || Override.Value is not null;

    public IDisposable Use(BusinessContext context)
    {
        var previous = Override.Value;
        Override.Value = context;
        return new Restore(() => Override.Value = previous);
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        private Action? _restore = restore;

        public void Dispose()
        {
            var action = Interlocked.Exchange(ref _restore, null);
            action?.Invoke();
        }
    }
}
