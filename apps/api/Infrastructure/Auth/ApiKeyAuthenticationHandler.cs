namespace Documate.Api.Infrastructure.Auth;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Documate.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

public static class ApiKeyAuthDefaults
{
    public const string Scheme = "ApiKey";
}

public static class DocumateAuthDefaults
{
    /// <summary>Policy scheme: ApiKey for External (/api/v1 or X-Api-Key), else DevBypass for app.</summary>
    public const string Scheme = "Documate";
}

/// <summary>F2 temporary Business API key auth for External APIs (bridge until Iden M2M).</summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IServiceScopeFactory scopeFactory)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyService.KeyHeaderName, out var headerValues)
            || string.IsNullOrWhiteSpace(headerValues.ToString()))
        {
            var authHeader = Request.Headers.Authorization.ToString();
            if (authHeader.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
            {
                headerValues = authHeader["ApiKey ".Length..].Trim();
            }
            else
            {
                return AuthenticateResult.Fail("Missing API key. Send X-Api-Key header (F2 temporary bridge).");
            }
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var apiKeys = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
        var db = scope.ServiceProvider.GetRequiredService<DocumateDbContext>();

        var key = await apiKeys.ValidateRawKeyAsync(headerValues.ToString(), Context.RequestAborted);
        if (key is null)
        {
            return AuthenticateResult.Fail("Invalid or expired API key.");
        }

        var business = await db.CorTenantBusinesses.AsNoTracking()
            .Include(b => b.Tenant)
            .FirstOrDefaultAsync(b => b.IdenBusinessId == key.BusinessId && !b.IsDeleted, Context.RequestAborted);

        if (business is null)
        {
            return AuthenticateResult.Fail("API key Business is not provisioned.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, $"apikey:{key.Id:N}"),
            new Claim(AuthClaimTypes.UserId, $"apikey:{key.Id:N}"),
            new Claim(AuthClaimTypes.TenantId, business.Tenant?.IdenTenantId ?? business.TenantId.ToString()),
            new Claim(AuthClaimTypes.BusinessId, key.BusinessId),
            new Claim(AuthClaimTypes.TenantName, business.TenantName),
            new Claim(AuthClaimTypes.BusinessName, business.Name),
            new Claim("documate_auth_kind", "api_key"),
        };

        var identity = new ClaimsIdentity(claims, ApiKeyAuthDefaults.Scheme);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, ApiKeyAuthDefaults.Scheme));
    }
}
