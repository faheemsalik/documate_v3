namespace Documate.Api.Domain;

public sealed class CorAgentTemplate : CatalogEntity
{
    public string AgentTemplateKey { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public long DocumentTypeId { get; set; }
    public string DefaultSchemaJson { get; set; } = "{}";
    public string DefaultInstructions { get; set; } = "";
    public long? DefaultProviderId { get; set; }
    public bool IsPublished { get; set; }
    public int Version { get; set; } = 1;

    public CorDocumentType? DocumentType { get; set; }
    public CorProvider? DefaultProvider { get; set; }
}
