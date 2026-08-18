namespace Documate.Api.Domain;

public sealed class OpsDocument : WireFacingEntity, IHasRowVersion
{
    public string BusinessId { get; set; } = "";
    public Guid QueueId { get; set; }
    public Guid FileId { get; set; }
    public Guid? BatchId { get; set; }
    public long? DocumentTypeId { get; set; }
    public Guid? AgentId { get; set; }
    public long? ProviderId { get; set; }
    public int? SchemaVersion { get; set; }

    public long PublicStatusEnumId { get; set; }
    public long? InternalStageEnumId { get; set; }

    public int? PageStart { get; set; }
    public int? PageEnd { get; set; }
    public string? SliceRefJson { get; set; }
    public string? ResultJson { get; set; }

    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? FailedStage { get; set; }
    public int RetryCount { get; set; }
    public DateTimeOffset? NextRetryAt { get; set; }

    public long? WebhookStatusEnumId { get; set; }
    public int WebhookAttempts { get; set; }
    public DateTimeOffset? WebhookLastAt { get; set; }
    public int? WebhookLastHttpStatus { get; set; }
    public string? WebhookLastError { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelledByUserId { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public OpsQueue? Queue { get; set; }
    public OpsFile? File { get; set; }
    public OpsBatch? Batch { get; set; }
    public CorDocumentType? DocumentType { get; set; }
    public OpsAgent? Agent { get; set; }
    public CorProvider? Provider { get; set; }
    public CorEnum? PublicStatus { get; set; }
    public CorEnum? InternalStage { get; set; }
    public CorEnum? WebhookStatus { get; set; }
}
