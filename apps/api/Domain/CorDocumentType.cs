namespace Documate.Api.Domain;

public sealed class CorDocumentType : CatalogEntity
{
    public string DocumentTypeKey { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
