namespace Documate.Api.Domain;

public sealed class CorEnumType : CatalogEntity
{
    public string EnumTypeKey { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>Bootstrap only: system | business (not a CorEnum FK).</summary>
    public string Scope { get; set; } = "system";
    public bool IsActive { get; set; } = true;

    public ICollection<CorEnum> Values { get; set; } = new List<CorEnum>();
}
