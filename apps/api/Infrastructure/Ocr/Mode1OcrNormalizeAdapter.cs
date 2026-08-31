namespace Documate.Api.Infrastructure.Ocr;

using System.Text;
using System.Text.Json;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Storage;
using Microsoft.Extensions.Options;

/// <summary>
/// Real OCR/normalize (DQ-0704): text passthrough, then primary→secondary OCR with artifact writes.
/// Default order: Textract → Google Document AI. Fallback on API failure or empty/low-quality text.
/// </summary>
public sealed class Mode1OcrNormalizeAdapter(
    IObjectStorage storage,
    IEnumerable<IOcrEngine> engines,
    IOptions<OcrOptions> ocrOptions,
    IOptions<StorageOptions> storageOptions,
    ILogger<Mode1OcrNormalizeAdapter> logger) : IOcrNormalizeAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public async Task<NormalizeResult> NormalizeAsync(NormalizeRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.StorageBucket) || string.IsNullOrWhiteSpace(request.StorageKey))
        {
            throw new InvalidOperationException("File storage location is missing; cannot normalize.");
        }

        var bucket = request.StorageBucket;
        await using var source = await storage.DownloadAsync(bucket, request.StorageKey, cancellationToken);
        var bytes = await ReadAllBytesAsync(source, cancellationToken);

        var (kind, estimatedPages) = Classify(bytes, request.ContentType, request.OriginalFileName);
        if (kind == FileKind.Unsupported)
        {
            throw new InvalidOperationException(
                "Unsupported file format for OCR. Supported: PDF, PNG, JPG/JPEG, and plain text.");
        }

        OcrEngineResult ocr;
        if (kind == FileKind.Text)
        {
            var text = Encoding.UTF8.GetString(bytes);
            if (string.IsNullOrWhiteSpace(text))
            {
                text = "[empty text file]";
            }

            ocr = new OcrEngineResult(
                "passthrough_text",
                text,
                1,
                "passthrough_text",
                [new OcrPageText(1, text)]);
        }
        else
        {
            ocr = await RecognizeWithFallbackAsync(
                new OcrEngineRequest(
                    bytes,
                    request.ContentType,
                    request.OriginalFileName,
                    estimatedPages,
                    bucket,
                    request.StorageKey),
                cancellationToken);
        }

        var layout = new
        {
            providerKey = ocr.ProviderKey,
            mode = ocr.Mode,
            pageCount = ocr.PageCount,
            sourceContentType = request.ContentType,
            originalFileName = request.OriginalFileName,
            fileId = request.FileId,
            pages = ocr.Pages.Select(p => new { page = p.Page, text = p.Text }).ToArray(),
        };

        var textKey = storage.BuildArtifactKey(request.StorageKey, "normalize.text.txt");
        var layoutKey = storage.BuildArtifactKey(request.StorageKey, "normalize.layout.json");

        var textBytes = Encoding.UTF8.GetBytes(ocr.Text);
        await using (var textStream = new MemoryStream(textBytes))
        {
            await storage.UploadAsync(
                new ObjectStoragePutRequest(
                    bucket,
                    textKey,
                    textStream,
                    "text/plain; charset=utf-8",
                    new Dictionary<string, string>
                    {
                        ["FileId"] = request.FileId.ToString(),
                        ["Artifact"] = "normalize.text",
                        ["ProviderKey"] = ocr.ProviderKey,
                    }),
                cancellationToken);
        }

        var layoutBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(layout, JsonOptions));
        await using (var layoutStream = new MemoryStream(layoutBytes))
        {
            await storage.UploadAsync(
                new ObjectStoragePutRequest(
                    bucket,
                    layoutKey,
                    layoutStream,
                    "application/json",
                    new Dictionary<string, string>
                    {
                        ["FileId"] = request.FileId.ToString(),
                        ["Artifact"] = "normalize.layout",
                        ["ProviderKey"] = ocr.ProviderKey,
                    }),
                cancellationToken);
        }

        logger.LogInformation(
            "Normalized File {FileId} via {ProviderKey} ({Mode}); pages={PageCount}; text={TextKey}",
            request.FileId,
            ocr.ProviderKey,
            ocr.Mode,
            ocr.PageCount,
            textKey);

        return new NormalizeResult(ocr.ProviderKey, ocr.PageCount, textKey, layoutKey, bucket);
    }

    private async Task<OcrEngineResult> RecognizeWithFallbackAsync(
        OcrEngineRequest request,
        CancellationToken cancellationToken)
    {
        var opts = ocrOptions.Value;
        var byKey = engines.ToDictionary(e => e.ProviderKey, StringComparer.OrdinalIgnoreCase);
        var order = new[] { opts.PrimaryProviderKey, opts.SecondaryProviderKey }
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (order.Count == 0)
        {
            order = ["aws_textract", "google_document_ai"];
        }

        Exception? last = null;
        foreach (var key in order)
        {
            if (!byKey.TryGetValue(key!, out var engine) || !engine.IsConfigured)
            {
                logger.LogWarning("OCR provider {ProviderKey} skipped (missing or not configured)", key);
                continue;
            }

            try
            {
                if (string.Equals(key, "aws_textract", StringComparison.OrdinalIgnoreCase)
                    && engine is TextractOcrEngine textract)
                {
                    return await textract.RecognizeSmartAsync(
                        request,
                        storageOptions.Value,
                        cancellationToken);
                }

                return await engine.RecognizeAsync(request, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                last = ex;
                logger.LogWarning(ex, "OCR provider {ProviderKey} failed; trying next", key);
            }
        }

        throw new InvalidOperationException(
            "All configured OCR providers failed or are unavailable.",
            last);
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream source, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    private static (FileKind Kind, int EstimatedPages) Classify(
        byte[] bytes,
        string? contentType,
        string? originalFileName)
    {
        var ct = (contentType ?? "").ToLowerInvariant();
        var ext = Path.GetExtension(originalFileName ?? "").ToLowerInvariant();

        if (ct.StartsWith("text/", StringComparison.Ordinal)
            || ext is ".txt" or ".csv" or ".md" or ".json" or ".xml" or ".html" or ".htm")
        {
            return (FileKind.Text, 1);
        }

        if (ct.Contains("pdf", StringComparison.Ordinal) || ext == ".pdf")
        {
            var pages = PdfPageCounter.TryCount(bytes) ?? 2;
            return (FileKind.Binary, Math.Max(1, pages));
        }

        if (ct is "image/png" or "image/jpeg" or "image/jpg"
            || ext is ".png" or ".jpg" or ".jpeg")
        {
            return (FileKind.Binary, 1);
        }

        return (FileKind.Unsupported, 0);
    }

    private enum FileKind
    {
        Text,
        Binary,
        Unsupported,
    }
}
