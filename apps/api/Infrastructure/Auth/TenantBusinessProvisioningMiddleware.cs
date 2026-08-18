namespace Documate.Api.Infrastructure.Auth;

/// <summary>Provisions CorTenant extension rows after authentication (DQ-0102).</summary>
public sealed class TenantBusinessProvisioningMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext httpContext,
        IBusinessContext businessContext,
        ITenantBusinessProvisioner provisioner)
    {
        if (businessContext.IsAuthenticated)
        {
            await provisioner.EnsureAsync(businessContext, httpContext.RequestAborted);
        }

        await next(httpContext);
    }
}
