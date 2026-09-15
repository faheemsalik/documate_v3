namespace Documate.Api.Domain;

/// <summary>Purpose inbox: typed (per-Agent) or multi-type on docsintake.com (Plan 14).</summary>
public sealed class OpsIntakeMailbox : WireFacingEntity, IHasRowVersion
{
    public string BusinessId { get; set; } = "";
    public Guid QueueId { get; set; }
    /// <summary>CorEnum <c>intake_mailbox_kind</c>: typed_agent | multi_type.</summary>
    public long KindEnumId { get; set; }
    /// <summary>Required when kind = typed_agent.</summary>
    public Guid? AgentId { get; set; }
    public bool Enabled { get; set; } = true;
    public string EmailLocalPart { get; set; } = "";
    public string EmailDomain { get; set; } = "";
    public int EmailAddressVersion { get; set; } = 1;
    public long AllowlistModeEnumId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public OpsQueue? Queue { get; set; }
    public OpsAgent? Agent { get; set; }
    public CorEnum? Kind { get; set; }
    public CorEnum? AllowlistMode { get; set; }
    public ICollection<OpsIntakeMailboxAllowlistEntry> AllowlistEntries { get; set; } = new List<OpsIntakeMailboxAllowlistEntry>();
}
