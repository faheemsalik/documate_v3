namespace Documate.Api.Infrastructure.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>DevBypass | Bearer (Iden later / Band 15).</summary>
    public string Mode { get; set; } = "DevBypass";

    public DevBypassOptions DevBypass { get; set; } = new();

    /// <summary>Temporary static FE login until Iden (DQ-1502). Not a product identity store.</summary>
    public InterimFeGateOptions InterimFeGate { get; set; } = new();

    /// <summary>Separate ops/admin gate for /api/admin (Plan 15 DR-SS1). Not customer portal credentials.</summary>
    public AdminGateOptions AdminGate { get; set; } = new();
}

public sealed class DevBypassOptions
{
    public string UserId { get; set; } = "dev-user";
    public string TenantId { get; set; } = "";
    public string BusinessId { get; set; } = "";
    public string TenantName { get; set; } = "Dev Tenant";
    public string BusinessName { get; set; } = "Dev Business";
}

/// <summary>Opaque static gate — replace with Iden OIDC. Credentials live in config, not the Angular bundle.</summary>
public sealed class InterimFeGateOptions
{
    public bool Enabled { get; set; }

    public string Username { get; set; } = "admin";

    public string Password { get; set; } = "";

    /// <summary>Bearer token returned by login and required on /api/app when Enabled.</summary>
    public string AccessToken { get; set; } = "";
}

/// <summary>Ops admin API gate — separate username/password/token from InterimFeGate.</summary>
public sealed class AdminGateOptions
{
    public bool Enabled { get; set; } = true;

    public string Username { get; set; } = "ops-admin";

    public string Password { get; set; } = "";

    public string AccessToken { get; set; } = "";
}

public static class AuthClaimTypes
{
    public const string UserId = "documate_user_id";
    public const string TenantId = "documate_tenant_id";
    public const string BusinessId = "documate_business_id";
    public const string TenantName = "documate_tenant_name";
    public const string BusinessName = "documate_business_name";
    /// <summary>Present when AdminGate authenticates — required by PlatformAdmin policy.</summary>
    public const string PlatformAdmin = "documate_platform_admin";
}

public static class PlatformAdminAuth
{
    public const string PolicyName = "PlatformAdmin";
    public const string RoleName = "ops_admin";
}
