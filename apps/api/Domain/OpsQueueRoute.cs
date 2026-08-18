namespace Documate.Api.Domain;

public sealed class OpsQueueRoute : CatalogEntity
{
    public string BusinessId { get; set; } = "";
    public Guid QueueId { get; set; }
    public long DocumentTypeId { get; set; }
    public Guid AgentId { get; set; }

    public OpsQueue? Queue { get; set; }
    public CorDocumentType? DocumentType { get; set; }
    public OpsAgent? Agent { get; set; }
}
