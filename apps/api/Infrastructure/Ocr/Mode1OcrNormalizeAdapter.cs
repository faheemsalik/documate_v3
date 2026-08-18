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

        var (plainText, pageCount, mode) = await ExtractAsync(source, request.ContentType, request.OriginalFileName, cancellationToken);

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

    private static async Task<(string Text, int PageCount, string Mode)> ExtractAsync(
        Stream source,
        string? contentType,
        string? originalFileName,
        CancellationToken cancellationToken)
    {
        var ct = (contentType ?? "").ToLowerInvariant();
        var ext = Path.GetExtension(originalFileName ?? "").ToLowerInvariant();

        if (ct.StartsWith("text/", StringComparison.Ordinal)
            || ext is ".txt" or ".csv" or ".md" or ".json" or ".xml" or ".html" or ".htm")
        {
            using var reader = new StreamReader(source, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
            var text = await reader.ReadToEndAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(text))
            {
                text = "[empty text file]";
            }

            return (text, 1, "passthrough_text");
        }

        // Binary / PDF / image: Phase 1 stub layout for split/classify until real Textract.
        // Do not load entire large binaries as UTF-8.
        var sizeHint = source.CanSeek ? source.Length : 0;
        var stub = new StringBuilder();
        stub.AppendLine($"[stub_normalize] contentType={contentType ?? "unknown"} name={originalFileName ?? "file"}");
        stub.AppendLine($"sizeBytes={sizeHint}");
        stub.AppendLine("OCR text placeholder for Mode 1 — replace with Textract/Document AI when credentials + S3 path are production-ready.");
        return (stub.ToString(), pageCount: 1, mode: "stub_binary");
    }
}
