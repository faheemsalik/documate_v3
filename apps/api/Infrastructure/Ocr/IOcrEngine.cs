namespace Documate.Api.Infrastructure.Ocr;

public sealed record OcrEngineRequest(
    byte[] Bytes,
    string? ContentType,
    string? OriginalFileName,
    int EstimatedPageCount,
    string? StorageBucket,
    string StorageKey);

public sealed record OcrEngineResult(
    string ProviderKey,
    string Text,
    int PageCount,
    string Mode,
    IReadOnlyList<OcrPageText> Pages);

public sealed record OcrPageText(int Page, string Text, bool IsBlank = false);

public interface IOcrEngine
{
    string ProviderKey { get; }
    bool IsConfigured { get; }
    Task<OcrEngineResult> RecognizeAsync(OcrEngineRequest request, CancellationToken cancellationToken = default);
}

public sealed class OcrEmptyOrLowQualityException : Exception
{
    public OcrEmptyOrLowQualityException(string message) : base(message) { }
}
