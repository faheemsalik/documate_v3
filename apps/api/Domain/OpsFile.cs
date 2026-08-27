namespace Documate.Api.Domain;

public sealed class OpsFile : WireFacingEntity, IHasRowVersion
{
    public string BusinessId { get; set; } = "";
    public Guid QueueId { get; set; }
    public Guid? BatchId { get; set; }
    public long SourceEnumId { get; set; }
    public long PublicStatusEnumId { get; set; }
    public long? InternalStageEnumId { get; set; }

    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }
    public string StorageKey { get; set; } = "";
    public string? StorageBucket { get; set; }
    public string? ContentHash { get; set; }

    public string? EmailMessageId { get; set; }
    public string? EmailFrom { get; set; }
    public string? EmailSubject { get; set; }

    /// <summary>Optional caller hints (documentTypeKey, documentCount). Type + one page skips split/classify.</summary>
    public string? IntakeHintsJson { get; set; }

    public Guid? ReprocessOfFileId { get; set; }

    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelledByUserId { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public OpsQueue? Queue { get; set; }
    public OpsBatch? Batch { get; set; }
    public OpsFile? ReprocessOfFile { get; set; }
    public CorEnum? Source { get; set; }
    public CorEnum? PublicStatus { get; set; }
    public CorEnum? InternalStage { get; set; }
    public ICollection<OpsDocument> Documents { get; set; } = new List<OpsDocument>();
}
