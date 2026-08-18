namespace Documate.Api.Domain;

public sealed class CorProvider : CatalogEntity
{
    public string ProviderKey { get; set; } = "";
    public string Name { get; set; } = "";
    public long CategoryEnumId { get; set; }
    public string? VendorHint { get; set; }
    public bool IsPlatformManaged { get; set; }
    public bool IsActive { get; set; } = true;

    public CorEnum? Category { get; set; }
}
