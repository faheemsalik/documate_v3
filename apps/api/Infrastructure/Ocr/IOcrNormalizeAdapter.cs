namespace Documate.Api.Infrastructure.Ocr;

/// <summary>Mode 1 normalize/OCR — produces text/layout for split/classify (DQ-0701).</summary>
public interface IOcrNormalizeAdapter
{
    Task<NormalizeResult> NormalizeAsync(NormalizeRequest request, CancellationToken cancellationToken = default);
}

public sealed record NormalizeRequest(
    string BusinessId,
    Guid FileId,
    long FileSequenceId,
    string? StorageBucket,
    string StorageKey,
    string? ContentType,
    string? OriginalFileName);

public sealed record NormalizeResult(
    string ProviderKey,
    int PageCount,
    string TextArtifactKey,
    string LayoutArtifactKey,
    string? StorageBucket,
    IReadOnlyList<NormalizePageArtifactRef> PageArtifacts);

/// <summary>Per-page normalize artifacts for P9 (DQ-0705).</summary>
public sealed record NormalizePageArtifactRef(
    int Page,
    string TextArtifactKey,
    string LayoutArtifactKey,
    bool IsBlank);
