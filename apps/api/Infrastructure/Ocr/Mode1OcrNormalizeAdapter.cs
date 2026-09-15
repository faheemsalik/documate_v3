namespace Documate.Api.Infrastructure.Ocr;

using System.Text;
using System.Text.Json;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Storage;
using Microsoft.Extensions.Options;

/// <summary>
/// Real OCR/normalize (DQ-0704/0705): text passthrough, primary→secondary OCR,
/// file-level + per-page artifacts (`normalize.page.{n}.*`).
/// </summary>
public sealed class Mode1OcrNormalizeAdapter(
    IObjectStorage storage,
    IEnumerable<IOcrEngine> engines,
    IOptionsMonitor<OcrOptions> ocrOptions,
    IOptionsMonitor<StorageOptions> storageOptions,
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
                [new OcrPageText(1, text, string.IsNullOrWhiteSpace(text))]);
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

        var pages = ocr.Pages.Count > 0
            ? ocr.Pages
            : [new OcrPageText(1, ocr.Text, string.IsNullOrWhiteSpace(ocr.Text))];
        var pageCount = Math.Max(1, pages.Count);

        var layout = new
        {
            providerKey = ocr.ProviderKey,
            mode = ocr.Mode,
            pageCount,
            sourceContentType = request.ContentType,
            originalFileName = request.OriginalFileName,
            fileId = request.FileId,
            pages = pages.Select(p => new { page = p.Page, text = p.Text, isBlank = p.IsBlank }).ToArray(),
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

        var pageArtifacts = new List<NormalizePageArtifactRef>(pages.Count);
        foreach (var page in pages)
        {
            var pageTextName = $"normalize.page.{page.Page}.text.txt";
            var pageLayoutName = $"normalize.page.{page.Page}.layout.json";
            var pageTextKey = storage.BuildArtifactKey(request.StorageKey, pageTextName);
            var pageLayoutKey = storage.BuildArtifactKey(request.StorageKey, pageLayoutName);

            var pageLayout = new
            {
                providerKey = ocr.ProviderKey,
                mode = ocr.Mode,
                page = page.Page,
                pageCount,
                isBlank = page.IsBlank,
                fileId = request.FileId,
                text = page.Text,
            };

            var pageTextBytes = Encoding.UTF8.GetBytes(page.Text ?? "");
            await using (var pageTextStream = new MemoryStream(pageTextBytes))
            {
                await storage.UploadAsync(
                    new ObjectStoragePutRequest(
                        bucket,
                        pageTextKey,
                        pageTextStream,
                        "text/plain; charset=utf-8",
                        new Dictionary<string, string>
                        {
                            ["FileId"] = request.FileId.ToString(),
                            ["Artifact"] = "normalize.page.text",
                            ["Page"] = page.Page.ToString(),
                            ["ProviderKey"] = ocr.ProviderKey,
                        }),
                    cancellationToken);
            }

            var pageLayoutBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pageLayout, JsonOptions));
            await using (var pageLayoutStream = new MemoryStream(pageLayoutBytes))
            {
                await storage.UploadAsync(
                    new ObjectStoragePutRequest(
                        bucket,
                        pageLayoutKey,
                        pageLayoutStream,
                        "application/json",
                        new Dictionary<string, string>
                        {
                            ["FileId"] = request.FileId.ToString(),
                            ["Artifact"] = "normalize.page.layout",
                            ["Page"] = page.Page.ToString(),
                            ["ProviderKey"] = ocr.ProviderKey,
                        }),
                    cancellationToken);
            }

            pageArtifacts.Add(new NormalizePageArtifactRef(page.Page, pageTextKey, pageLayoutKey, page.IsBlank));
        }

        logger.LogInformation(
            "Normalized File {FileId} via {ProviderKey} ({Mode}); pages={PageCount}; text={TextKey}; pageArtifacts={PageArtifactCount}",
            request.FileId,
            ocr.ProviderKey,
            ocr.Mode,
            pageCount,
            textKey,
            pageArtifacts.Count);

        return new NormalizeResult(ocr.ProviderKey, pageCount, textKey, layoutKey, bucket, pageArtifacts);
    }

    private async Task<OcrEngineResult> RecognizeWithFallbackAsync(
        OcrEngineRequest request,
        CancellationToken cancellationToken)
    {
        var opts = ocrOptions.CurrentValue;
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
                        storageOptions.CurrentValue,
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

        var detail = last is null
            ? "All configured OCR providers failed or are unavailable."
            : $"All configured OCR providers failed or are unavailable. Last: {last.GetType().Name}: {last.Message}";
        throw new InvalidOperationException(detail, last);
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
