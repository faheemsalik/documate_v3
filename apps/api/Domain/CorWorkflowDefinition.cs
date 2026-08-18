namespace Documate.Api.Domain;

public sealed class CorWorkflowDefinition : CatalogEntity, IHasRowVersion
{
    public string BusinessId { get; set; } = "";
    public string? WorkflowKey { get; set; }
    public string Name { get; set; } = "";
    public string DefinitionJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
