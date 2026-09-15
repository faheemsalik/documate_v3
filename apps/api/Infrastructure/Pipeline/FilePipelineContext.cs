namespace Documate.Api.Infrastructure.Pipeline;

using Documate.Api.Domain;
using Documate.Api.Infrastructure.Intelligence;
using Documate.Api.Infrastructure.Ocr;

/// <summary>Shared state for File pipeline stages (normalize → split → classify → route → extract).</summary>
public sealed class FilePipelineContext
{
    public required FileWorkItem Item { get; init; }
    public required OpsFile File { get; init; }
    public required IntakeHints Hints { get; init; }
    public NormalizeResult? Normalize { get; set; }
    public IReadOnlyList<PageIntelligenceProfile> IntelligenceProfiles { get; set; } = [];
    public List<OpsDocument> Documents { get; } = [];

    public bool SkipSplitAndClassify =>
        IntakeSkipPolicy.SkipSplitAndClassify(Hints.HasPredeterminedType, Normalize?.PageCount ?? 0);

    public string? SliceRefJson =>
        Normalize is null
            ? null
            : System.Text.Json.JsonSerializer.Serialize(new
            {
                layoutArtifactKey = Normalize.LayoutArtifactKey,
                textArtifactKey = Normalize.TextArtifactKey,
                pageCount = Normalize.PageCount,
                providerKey = Normalize.ProviderKey,
                skippedSplit = SkipSplitAndClassify,
                pageArtifacts = Normalize.PageArtifacts.Select(p => new
                {
                    page = p.Page,
                    textArtifactKey = p.TextArtifactKey,
                    layoutArtifactKey = p.LayoutArtifactKey,
                    isBlank = p.IsBlank,
                }).ToArray(),
            });
}
