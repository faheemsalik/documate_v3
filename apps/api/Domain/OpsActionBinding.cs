namespace Documate.Api.Domain;

/// <summary>Business or Queue-scoped action subscription (webhook / email / in_app).</summary>
public sealed class OpsActionBinding : WireFacingEntity
{
    public string BusinessId { get; set; } = "";
    /// <summary>Null = Business default; set = Queue override row.</summary>
    public Guid? QueueId { get; set; }
    /// <summary>webhook | email | in_app</summary>
    public string ActionTypeKey { get; set; } = "";
    public bool Enabled { get; set; } = true;
    /// <summary>Type-specific config (URL/secretProtected, recipients, audience).</summary>
    public string? ConfigJson { get; set; }
    /// <summary>JSON string array of public event keys.</summary>
    public string EventKeysJson { get; set; } = "[]";

    public OpsQueue? Queue { get; set; }
}

/// <summary>Outbound action attempt ledger (PE9 / DR-EA2).</summary>
public sealed class OpsOutboundDelivery : WireFacingEntity
{
    public string BusinessId { get; set; } = "";
    public Guid? ActionBindingId { get; set; }
    public string ActionTypeKey { get; set; } = "";
    public string EventName { get; set; } = "";
    public string EventId { get; set; } = "";
    /// <summary>file | document</summary>
    public string ResourceTypeKey { get; set; } = "";
    public Guid ResourceId { get; set; }
    public Guid? QueueId { get; set; }
    public long StatusEnumId { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset? LastAt { get; set; }
    public int? LastHttpStatus { get; set; }
    public string? LastError { get; set; }
    /// <summary>Serialized public payload body (snake_case JSON).</summary>
    public string PayloadJson { get; set; } = "{}";

    public OpsActionBinding? ActionBinding { get; set; }
    public CorEnum? Status { get; set; }
}

/// <summary>Partner-visible in-app notification (DR-EA5).</summary>
public sealed class OpsInAppNotification : WireFacingEntity
{
    public string BusinessId { get; set; } = "";
    public string EventId { get; set; } = "";
    public string EventName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string? PayloadJson { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
