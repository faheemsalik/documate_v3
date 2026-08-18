namespace Documate.Api.Domain;

/// <summary>F2 temporary Business-scoped API key entity (bridge until Iden M2M). Not a column prefix.</summary>
public sealed class CorTenantApiKey : WireFacingEntity, IHasRowVersion
{
    public string BusinessId { get; set; } = "";
    public string Name { get; set; } = "";
    public string KeyPrefix { get; set; } = "";
    public string KeyHash { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
