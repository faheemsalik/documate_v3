namespace Documate.Api.Modules.FrontendSupport.Features.SystemSettings;

using System.Security.Cryptography;
using System.Text;
using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

[ApiController]
[Route("api/admin/auth")]
public sealed class AdminAuthController(IOptions<AuthOptions> authOptions) : ControllerBase
{
    public sealed record AdminLoginRequest(string Username, string Password);
    public sealed record AdminLoginResponse(string AccessToken, string TokenType);

    [HttpPost("login")]
    [AllowAnonymous]
    public ActionResult<AdminLoginResponse> Login([FromBody] AdminLoginRequest request)
    {
        var gate = authOptions.Value.AdminGate;
        if (!gate.Enabled)
        {
            return Unauthorized(new { error = "AdminGate disabled." });
        }

        if (string.IsNullOrWhiteSpace(gate.AccessToken)
            || string.IsNullOrWhiteSpace(gate.Password)
            || !FixedTimeEqualsUtf8(request.Username ?? "", gate.Username)
            || !FixedTimeEqualsUtf8(request.Password ?? "", gate.Password))
        {
            return Unauthorized(new { error = "Invalid admin credentials." });
        }

        return Ok(new AdminLoginResponse(gate.AccessToken, "Bearer"));
    }

    private static bool FixedTimeEqualsUtf8(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}

[ApiController]
[Authorize(Policy = PlatformAdminAuth.PolicyName)]
[Route("api/admin/system-settings")]
public sealed class SystemSettingsController(ISystemSettings settings) : ControllerBase
{
    public sealed record SettingDto(string Key, string ValueJson);
    public sealed record UpsertSettingRequest(string ValueJson);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SettingDto>>> List(CancellationToken cancellationToken)
    {
        var all = await settings.GetAllAsync(cancellationToken);
        var list = SystemSettingKeys.All
            .Select(k => new SettingDto(k, all.TryGetValue(k, out var v) ? v : "null"))
            .ToList();
        return Ok(list);
    }

    [HttpGet("{*key}")]
    public ActionResult<SettingDto> Get(string key)
    {
        key = Uri.UnescapeDataString(key);
        if (!SystemSettingKeys.IsAllowlisted(key))
        {
            return NotFound();
        }

        var raw = settings.GetRawJson(key) ?? "null";
        return Ok(new SettingDto(key, raw));
    }

    [HttpPut("{*key}")]
    public async Task<ActionResult<SettingDto>> Put(string key, [FromBody] UpsertSettingRequest body, CancellationToken cancellationToken)
    {
        key = Uri.UnescapeDataString(key);
        if (!SystemSettingKeys.IsAllowlisted(key))
        {
            return BadRequest(new { error = $"Setting key '{key}' is not allowlisted." });
        }

        if (string.IsNullOrWhiteSpace(body.ValueJson))
        {
            return BadRequest(new { error = "ValueJson is required." });
        }

        var userId = User.FindFirst(AuthClaimTypes.UserId)?.Value ?? "ops-admin";
        await settings.UpsertAsync(key, body.ValueJson, userId, cancellationToken);
        return Ok(new SettingDto(key, body.ValueJson));
    }
}
