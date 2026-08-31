namespace Documate.Api.Infrastructure.Ocr;

using Amazon;
using Amazon.Runtime;
using Amazon.Textract;
using Amazon.Textract.Model;
using Documate.Api.Infrastructure.Options;
using Microsoft.Extensions.Options;

/// <summary>
/// AWS Textract. Sync DetectDocumentText for images / single-page PDF bytes.
/// Multi-page or &gt; SyncMaxPages: S3 StartDocumentTextDetection when storage is S3; else throws for fallback.
/// </summary>
public sealed class TextractOcrEngine(IOptions<OcrOptions> options, ILogger<TextractOcrEngine> logger) : IOcrEngine
{
    public string ProviderKey => "aws_textract";

    public bool IsConfigured
    {
        get
        {
            var t = options.Value.Textract;
            return !string.IsNullOrWhiteSpace(t.AccessKey) && !string.IsNullOrWhiteSpace(t.SecretKey);
        }
    }

    public Task<OcrEngineResult> RecognizeAsync(OcrEngineRequest request, CancellationToken cancellationToken = default) =>
        RecognizeSmartAsync(request, storage: null, cancellationToken);

    public async Task<OcrEngineResult> RecognizeSmartAsync(
        OcrEngineRequest request,
        StorageOptions? storage,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Textract is not configured (Ocr:Textract AccessKey/SecretKey).");
        }

        var syncMax = Math.Max(1, options.Value.SyncMaxPages);
        var isPdf = IsPdf(request.ContentType, request.OriginalFileName);
        var needsAsync = request.EstimatedPageCount > syncMax
            || (isPdf && request.EstimatedPageCount > 1);

        if (needsAsync)
        {
            if (storage is not null
                && string.Equals(storage.Provider, "s3", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(request.StorageBucket)
                && !string.IsNullOrWhiteSpace(request.StorageKey))
            {
                return await RecognizeAsyncS3Async(request, cancellationToken);
            }

            throw new InvalidOperationException(
                "Textract sync bytes API cannot handle this document; use secondary OCR or S3 async Textract.");
        }

        return await RecognizeSyncBytesAsync(request, cancellationToken);
    }

    private async Task<OcrEngineResult> RecognizeSyncBytesAsync(
        OcrEngineRequest request,
        CancellationToken cancellationToken)
    {
        using var client = CreateClient();
        var response = await client.DetectDocumentTextAsync(
            new DetectDocumentTextRequest
            {
                Document = new Document { Bytes = new MemoryStream(request.Bytes) },
            },
            cancellationToken);

        var text = JoinLines(response.Blocks);
        EnsureQuality(text);

        var pageCount = Math.Max(1, request.EstimatedPageCount);
        logger.LogInformation("Textract sync OCR ok; pages={Pages}; chars={Chars}", pageCount, text.Length);
        return new OcrEngineResult(
            ProviderKey,
            text,
            pageCount,
            "textract_detect_document_text",
            [new OcrPageText(1, text)]);
    }

    private async Task<OcrEngineResult> RecognizeAsyncS3Async(
        OcrEngineRequest request,
        CancellationToken cancellationToken)
    {
        using var client = CreateClient();
        var start = await client.StartDocumentTextDetectionAsync(
            new StartDocumentTextDetectionRequest
            {
                DocumentLocation = new DocumentLocation
                {
                    S3Object = new S3Object
                    {
                        Bucket = request.StorageBucket,
                        Name = request.StorageKey,
                    },
                },
            },
            cancellationToken);

        var jobId = start.JobId
            ?? throw new InvalidOperationException("Textract async job returned no JobId.");

        List<Block> blocks = [];
        string? nextToken = null;
        for (var attempt = 0; attempt < 120; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var status = await client.GetDocumentTextDetectionAsync(
                new GetDocumentTextDetectionRequest
                {
                    JobId = jobId,
                    NextToken = nextToken,
                },
                cancellationToken);

            var jobStatus = status.JobStatus?.Value ?? "";
            if (string.Equals(jobStatus, "FAILED", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Textract async job failed: {status.StatusMessage}");
            }

            if (string.Equals(jobStatus, "SUCCEEDED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(jobStatus, "PARTIAL_SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                if (status.Blocks is { Count: > 0 })
                {
                    blocks.AddRange(status.Blocks);
                }

                nextToken = status.NextToken;
                if (string.IsNullOrEmpty(nextToken))
                {
                    break;
                }

                continue;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        if (blocks.Count == 0)
        {
            throw new InvalidOperationException("Textract async job timed out or returned no blocks.");
        }

        var text = JoinLines(blocks);
        EnsureQuality(text);
        var pageCount = Math.Max(
            request.EstimatedPageCount,
            blocks.Where(b => b.Page.HasValue).Select(b => b.Page!.Value).DefaultIfEmpty(1).Max());

        logger.LogInformation("Textract async OCR ok; pages={Pages}; chars={Chars}", pageCount, text.Length);
        return new OcrEngineResult(
            ProviderKey,
            text,
            pageCount,
            "textract_start_document_text_detection",
            [new OcrPageText(1, text)]);
    }

    private AmazonTextractClient CreateClient()
    {
        var t = options.Value.Textract;
        var region = RegionEndpoint.GetBySystemName(
            string.IsNullOrWhiteSpace(t.Region) ? "us-west-2" : t.Region!);
        var credentials = new BasicAWSCredentials(t.AccessKey, t.SecretKey);
        return new AmazonTextractClient(credentials, region);
    }

    private static string JoinLines(IList<Block>? blocks) =>
        string.Join(
            '\n',
            (blocks ?? Array.Empty<Block>())
                .Where(b => b.BlockType == BlockType.LINE && !string.IsNullOrWhiteSpace(b.Text))
                .Select(b => b.Text!.Trim())).Trim();

    private static void EnsureQuality(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length < 8)
        {
            throw new OcrEmptyOrLowQualityException("Textract returned empty or very short text.");
        }
    }

    private static bool IsPdf(string? contentType, string? fileName)
    {
        var ct = (contentType ?? "").ToLowerInvariant();
        var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
        return ct.Contains("pdf", StringComparison.Ordinal) || ext == ".pdf";
    }
}
