namespace Documate.Api.Infrastructure.Auth;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

public static class DevBypassAuthDefaults
{
    public const string Scheme = "DevBypass";
}

/// <summary>Phase 1 (J3) interim auth — not for shipping. Replaced by live Iden in Band 15.</summary>
public sealed class DevBypassAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<AuthOptions> authOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var auth = authOptions.Value;
        if (!string.Equals(auth.Mode, "DevBypass", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var bypass = auth.DevBypass;
        if (string.IsNullOrWhiteSpace(bypass.BusinessId) || string.IsNullOrWhiteSpace(bypass.TenantId))
        {
            return Task.FromResult(AuthenticateResult.Fail("DevBypass TenantId/BusinessId not configured."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, bypass.UserId),
            new Claim(AuthClaimTypes.UserId, bypass.UserId),
            new Claim(AuthClaimTypes.TenantId, bypass.TenantId),
            new Claim(AuthClaimTypes.BusinessId, bypass.BusinessId),
            new Claim(AuthClaimTypes.TenantName, bypass.TenantName),
            new Claim(AuthClaimTypes.BusinessName, bypass.BusinessName),
        };

        var identity = new ClaimsIdentity(claims, DevBypassAuthDefaults.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, DevBypassAuthDefaults.Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
