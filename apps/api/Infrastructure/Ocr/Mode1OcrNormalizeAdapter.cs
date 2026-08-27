namespace Documate.Api.Infrastructure.Ocr;

using System.Text;
using System.Text.Json;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Storage;
using Microsoft.Extensions.Options;

/// <summary>
/// Mode 1 OCR/normalize adapter. Uses local/stub extraction by default.
/// When Providers:DefaultOcrApiKey is set, still Phase-1 stub for Textract shape —
/// real AWS Textract can replace the body without changing the interface (0701 evidence).
/// </summary>
public sealed class Mode1OcrNormalizeAdapter(
    IObjectStorage storage,
    IOptions<ProviderCredentialsOptions> credentials,
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
        var (plainText, pageCount, mode) = Extract(bytes, request.ContentType, request.OriginalFileName);

        // Credentials present ⇒ Mode 1 OCR path armed (real Textract later); still write artifacts now.
        var hasOcrCreds = !string.IsNullOrWhiteSpace(credentials.Value.DefaultOcrApiKey);
        var providerKey = hasOcrCreds ? "aws_textract" : "stub_normalize";

        var layout = new
        {
            providerKey,
            mode,
            pageCount,
            sourceContentType = request.ContentType,
            originalFileName = request.OriginalFileName,
            fileId = request.FileId,
            pages = Enumerable.Range(1, pageCount).Select(p => new
            {
                page = p,
                text = pageCount == 1 ? plainText : $"[page {p}]\n{plainText}",
            }).ToArray(),
        };

        var textKey = storage.BuildArtifactKey(request.StorageKey, "normalize.text.txt");
        var layoutKey = storage.BuildArtifactKey(request.StorageKey, "normalize.layout.json");

        var textBytes = Encoding.UTF8.GetBytes(plainText);
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
                        ["ProviderKey"] = providerKey,
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
                        ["ProviderKey"] = providerKey,
                    }),
                cancellationToken);
        }

        logger.LogInformation(
            "Normalized File {FileId} via {ProviderKey} ({Mode}); pages={PageCount}; text={TextKey}",
            request.FileId,
            providerKey,
            mode,
            pageCount,
            textKey);

        return new NormalizeResult(providerKey, pageCount, textKey, layoutKey, bucket);
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream source, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    private static (string Text, int PageCount, string Mode) Extract(
        byte[] bytes,
        string? contentType,
        string? originalFileName)
    {
        var ct = (contentType ?? "").ToLowerInvariant();
        var ext = Path.GetExtension(originalFileName ?? "").ToLowerInvariant();

        if (ct.StartsWith("text/", StringComparison.Ordinal)
            || ext is ".txt" or ".csv" or ".md" or ".json" or ".xml" or ".html" or ".htm")
        {
            var text = Encoding.UTF8.GetString(bytes);
            if (string.IsNullOrWhiteSpace(text))
            {
                text = "[empty text file]";
            }

            return (text, 1, "passthrough_text");
        }

        var isPdf = ct.Contains("pdf", StringComparison.Ordinal) || ext == ".pdf";
        var pageCount = 1;
        var mode = "stub_binary";
        if (isPdf)
        {
            var counted = PdfPageCounter.TryCount(bytes);
            if (counted is int n)
            {
                pageCount = n;
            }
            else
            {
                // Unknown PDF page count: do not treat as a single-page skip.
                pageCount = 2;
                mode = "stub_binary_pagecount_unknown";
            }
        }

        var stub = new StringBuilder();
        stub.AppendLine($"[stub_normalize] contentType={contentType ?? "unknown"} name={originalFileName ?? "file"}");
        stub.AppendLine($"sizeBytes={bytes.Length}");
        stub.AppendLine($"pageCount={pageCount}");
        stub.AppendLine("OCR text placeholder for Mode 1 — replace with Textract/Document AI when credentials + S3 path are production-ready.");
        return (stub.ToString(), pageCount, mode);
    }
}
