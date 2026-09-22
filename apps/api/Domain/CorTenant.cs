namespace Documate.Api.Domain;

public sealed class CorTenant : WireFacingEntity, IHasRowVersion
{
    public string IdenTenantId { get; set; } = "";
    public string Name { get; set; } = "";
    public long ProviderModeEnumId { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Band 20 mirror sync: ok | pending | divergent.</summary>
    public string SyncStatus { get; set; } = "ok";
    public DateTimeOffset? LastSyncedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public CorEnum? ProviderMode { get; set; }
    public ICollection<CorTenantBusiness> Businesses { get; set; } = new List<CorTenantBusiness>();
}
