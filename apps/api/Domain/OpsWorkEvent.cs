namespace Documate.Api.Domain;

public sealed class OpsWorkEvent : CatalogEntity
{
    public string BusinessId { get; set; } = "";
    public long SubjectTypeEnumId { get; set; }
    public Guid SubjectId { get; set; }
    public long EventTypeEnumId { get; set; }
    public long? ProviderId { get; set; }
    public string? PayloadJson { get; set; }

    public CorEnum? SubjectType { get; set; }
    public CorEnum? EventType { get; set; }
    public CorProvider? Provider { get; set; }
}
