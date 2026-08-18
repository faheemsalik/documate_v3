namespace Documate.Api.Infrastructure.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>DevBypass | Bearer (Iden later / Band 15).</summary>
    public string Mode { get; set; } = "DevBypass";

    public DevBypassOptions DevBypass { get; set; } = new();
}

public sealed class DevBypassOptions
{
    public string UserId { get; set; } = "dev-user";
    public string TenantId { get; set; } = "";
    public string BusinessId { get; set; } = "";
    public string TenantName { get; set; } = "Dev Tenant";
    public string BusinessName { get; set; } = "Dev Business";
}

public static class AuthClaimTypes
{
    public const string UserId = "documate_user_id";
    public const string TenantId = "documate_tenant_id";
    public const string BusinessId = "documate_business_id";
    public const string TenantName = "documate_tenant_name";
    public const string BusinessName = "documate_business_name";
}
