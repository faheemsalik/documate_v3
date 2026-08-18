namespace Documate.Api.Domain;

public sealed class CorEnum : CatalogEntity
{
    public long TypeId { get; set; }
    public string Name { get; set; } = "";
    public string EnumKey { get; set; } = "";
    public string? ShortName { get; set; }
    public string? Narration { get; set; }
    public string? DisplayStyle { get; set; }
    public string? BusinessId { get; set; }

    public CorEnumType? Type { get; set; }
}
