namespace Documate.Api.Domain;

/// <summary>Audit + soft-delete columns shared by all persisted entities.</summary>
public abstract class AuditableEntity
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? CreatedByUserId { get; set; }
    public string? UpdatedByUserId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }
}

/// <summary>User-facing resource: UUID PK + maintenance SequenceId.</summary>
public abstract class WireFacingEntity : AuditableEntity
{
    public Guid Id { get; set; }
    public long SequenceId { get; set; }
}

/// <summary>Catalog / internal row with bigint PK.</summary>
public abstract class CatalogEntity : AuditableEntity
{
    public long Id { get; set; }
}

/// <summary>Optimistic concurrency token (rowversion).</summary>
public interface IHasRowVersion
{
    byte[] RowVersion { get; set; }
}
