namespace Documate.Api.Infrastructure.Auth;

using System.Security.Claims;
using Documate.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// DevBypass interim: honor <see cref="BusinessIdHeader"/> so the web app can switch active business
/// without Iden token exchange (Band 15).
/// </summary>
public sealed class BusinessContextOverrideMiddleware(RequestDelegate next)
{
    public const string BusinessIdHeader = "X-Business-Id";

    public async Task InvokeAsync(HttpContext httpContext, DocumateDbContext db)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.Request.Headers.TryGetValue(BusinessIdHeader, out var headerValues))
        {
            var requestedBusinessId = headerValues.ToString();
            if (!string.IsNullOrWhiteSpace(requestedBusinessId))
            {
                var tenantId = httpContext.User.FindFirstValue(AuthClaimTypes.TenantId);
                var business = await db.CorTenantBusinesses.AsNoTracking()
                    .Include(b => b.Tenant)
                    .FirstOrDefaultAsync(
                        b => b.IdenBusinessId == requestedBusinessId && !b.IsDeleted,
                        httpContext.RequestAborted);

                if (business?.Tenant is not null
                    && string.Equals(business.Tenant.IdenTenantId, tenantId, StringComparison.Ordinal))
                {
                    var claims = httpContext.User.Claims
                        .Where(c => c.Type is not AuthClaimTypes.BusinessId and not AuthClaimTypes.BusinessName)
                        .ToList();
                    claims.Add(new Claim(AuthClaimTypes.BusinessId, business.IdenBusinessId));
                    claims.Add(new Claim(AuthClaimTypes.BusinessName, business.Name));

                    var identity = new ClaimsIdentity(
                        claims,
                        httpContext.User.Identity.AuthenticationType,
                        ClaimTypes.Name,
                        ClaimTypes.Role);
                    httpContext.User = new ClaimsPrincipal(identity);
                }
            }
        }

        await next(httpContext);
    }
}
