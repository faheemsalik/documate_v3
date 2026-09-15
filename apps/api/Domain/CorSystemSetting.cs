namespace Documate.Api.Domain;

/// <summary>Platform operational setting (non-secret). SoT after seed — Plan 15.</summary>
public sealed class CorSystemSetting : CatalogEntity
{
    public string SettingKey { get; set; } = "";
    /// <summary>JSON-encoded value (string, number, bool, or array).</summary>
    public string ValueJson { get; set; } = "null";
}
