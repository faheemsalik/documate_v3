namespace Documate.Api.Domain;

public sealed class OpsQueueEmailAllowlistEntry : CatalogEntity
{
    public string BusinessId { get; set; } = "";
    public Guid QueueId { get; set; }
    public long MatchTypeEnumId { get; set; }
    public string Value { get; set; } = "";

    public OpsQueue? Queue { get; set; }
    public CorEnum? MatchType { get; set; }
}
