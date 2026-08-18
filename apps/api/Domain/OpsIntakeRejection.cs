namespace Documate.Api.Domain;

public sealed class OpsIntakeRejection : WireFacingEntity
{
    public string BusinessId { get; set; } = "";
    public Guid QueueId { get; set; }
    public long SourceEnumId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? EmailMessageId { get; set; }
    public string? EmailFrom { get; set; }
    public string? EmailSubject { get; set; }

    public OpsQueue? Queue { get; set; }
    public CorEnum? Source { get; set; }
}
