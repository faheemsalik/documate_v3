namespace Documate.Api.Domain;

public sealed class OpsAgent : WireFacingEntity, IHasRowVersion
{
    public string BusinessId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public long DocumentTypeId { get; set; }
    public string OutputSchemaJson { get; set; } = "{}";
    public int SchemaVersion { get; set; } = 1;
    public string Instructions { get; set; } = "";
    public long? SourceTemplateId { get; set; }
    public long? DefaultWorkflowId { get; set; }
    public long? DefaultProviderId { get; set; }
    public string? ProviderStrategyJson { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public CorDocumentType? DocumentType { get; set; }
    public CorAgentTemplate? SourceTemplate { get; set; }
    public CorWorkflowDefinition? DefaultWorkflow { get; set; }
    public CorProvider? DefaultProvider { get; set; }
}
