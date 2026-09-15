namespace Documate.Api.Infrastructure.Auth;

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

public static class AdminGateAuthDefaults
{
    public const string Scheme = "AdminGate";
}

/// <summary>Bearer token auth for /api/admin using Auth:AdminGate (separate from customer InterimFeGate).</summary>
public sealed class AdminGateAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<AuthOptions> authOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var gate = authOptions.Value.AdminGate;
        if (!gate.Enabled)
        {
            return Task.FromResult(AuthenticateResult.Fail("AdminGate is disabled."));
        }

        if (string.IsNullOrWhiteSpace(gate.AccessToken))
        {
            return Task.FromResult(AuthenticateResult.Fail("AdminGate AccessToken not configured."));
        }

        var bearer = ReadBearerToken();
        if (bearer is null || !FixedTimeEqualsUtf8(bearer, gate.AccessToken))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing or invalid admin access token."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, gate.Username),
            new Claim(AuthClaimTypes.UserId, gate.Username),
            new Claim(ClaimTypes.Role, PlatformAdminAuth.RoleName),
            new Claim(AuthClaimTypes.PlatformAdmin, "true"),
        };
        var identity = new ClaimsIdentity(claims, AdminGateAuthDefaults.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AdminGateAuthDefaults.Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private string? ReadBearerToken()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            return null;
        }

        const string prefix = "Bearer ";
        if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return header[prefix.Length..].Trim();
    }

    private static bool FixedTimeEqualsUtf8(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}
