namespace Documate.Api.Infrastructure.Intelligence;

public sealed record PageIntelligenceProfile(
    int Page,
    string? DocumentType,
    string? PrimaryDocumentNumber,
    bool StartsNewDocument,
    bool ContinuesPrevious,
    bool DocumentComplete,
    bool IsBlank,
    string ModelTier,
    IReadOnlyList<string> Evidence);

public sealed record PageIntelligenceRequest(
    string BusinessId,
    Guid FileId,
    string StorageBucket,
    string FileStorageKey,
    IReadOnlyList<Documate.Api.Infrastructure.Ocr.NormalizePageArtifactRef> PageArtifacts,
    IReadOnlyList<QueueRoutableDocumentType> RoutableTypes);

public interface IPageIntelligenceService
{
    Task<IReadOnlyList<PageIntelligenceProfile>> AnalyzeAsync(
        PageIntelligenceRequest request,
        CancellationToken cancellationToken = default);
}
