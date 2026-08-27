namespace Documate.Api.Domain;

public sealed class OpsQueue : WireFacingEntity, IHasRowVersion
{
    public string BusinessId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool RoutingLocked { get; set; }
    public DateTimeOffset? RoutingLockedAt { get; set; }
    public string? WebhookUrl { get; set; }
    public string? WebhookSecretHash { get; set; }
    /// <summary>Data-Protection payload of the HMAC secret (hash alone cannot sign).</summary>
    public string? WebhookSecretProtected { get; set; }
    public bool WebhookEnabled { get; set; }
    public bool EmailIntakeEnabled { get; set; }
    public string? EmailLocalPart { get; set; }
    public string? EmailDomain { get; set; }
    public int EmailAddressVersion { get; set; }
    public long AllowlistModeEnumId { get; set; }
    public long WorkflowModeEnumId { get; set; }
    public long? WorkflowId { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public CorEnum? AllowlistMode { get; set; }
    public CorEnum? WorkflowMode { get; set; }
    public CorWorkflowDefinition? Workflow { get; set; }
    public ICollection<OpsQueueRoute> Routes { get; set; } = new List<OpsQueueRoute>();
    public ICollection<OpsQueueEmailAllowlistEntry> AllowlistEntries { get; set; } = new List<OpsQueueEmailAllowlistEntry>();
}
