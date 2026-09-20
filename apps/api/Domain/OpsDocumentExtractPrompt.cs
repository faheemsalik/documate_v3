namespace Documate.Api.Domain;

/// <summary>Last extract prompt sent for a Document (Plan 22 / DQ-1901). 7-day retention in DQ-1911.</summary>
public sealed class OpsDocumentExtractPrompt : WireFacingEntity
{
    public string BusinessId { get; set; } = "";
    public Guid FileId { get; set; }
    public Guid DocumentId { get; set; }
    public string SystemPromptText { get; set; } = "";
    public string UserPromptText { get; set; } = "";

    public OpsFile? File { get; set; }
    public OpsDocument? Document { get; set; }
}
