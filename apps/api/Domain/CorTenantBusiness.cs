namespace Documate.Api.Domain;

public sealed class CorTenantBusiness : WireFacingEntity, IHasRowVersion
{
    /// <summary>FK → CorTenant (column name drops Cor prefix).</summary>
    public Guid TenantId { get; set; }
    public string IdenBusinessId { get; set; } = "";
    public string Name { get; set; } = "";
    public string TenantName { get; set; } = "";
    /// <summary>Stable prefix for intake addresses (e.g. mcm). Set on first mailbox create.</summary>
    public string? IntakeEmailSlug { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public CorTenant? Tenant { get; set; }
}
