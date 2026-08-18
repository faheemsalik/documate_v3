namespace Documate.Api.Domain;

public sealed class OpsBatch : WireFacingEntity
{
    public string BusinessId { get; set; } = "";
    public Guid QueueId { get; set; }
    public long SourceEnumId { get; set; }
    public string? EmailMessageId { get; set; }
    public int FileCount { get; set; }

    public OpsQueue? Queue { get; set; }
    public CorEnum? Source { get; set; }
}
