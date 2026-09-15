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
public sealed class TextractOcrEngine(
    IOptions<OcrOptions> options,
    IOptions<AwsOptions> awsOptions,
    ILogger<TextractOcrEngine> logger) : IOcrEngine
{
    public string ProviderKey => "aws_textract";

    public bool IsConfigured
    {
        get
        {
            var t = options.Value.Textract;
            if (!string.IsNullOrWhiteSpace(t.AccessKey) && !string.IsNullOrWhiteSpace(t.SecretKey))
            {
                return true;
            }

            return awsOptions.Value.HasExplicitCredentials;
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
            throw new InvalidOperationException("Textract is not configured (Aws:AccessKey/SecretKey or Ocr:Textract override).");
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

        var pages = OcrPageSplitter.FromTextractBlocks(response.Blocks, request.EstimatedPageCount);
        var text = OcrPageSplitter.JoinDocumentText(pages);
        EnsureQuality(text);

        logger.LogInformation("Textract sync OCR ok; pages={Pages}; chars={Chars}", pages.Count, text.Length);
        return new OcrEngineResult(
            ProviderKey,
            text,
            pages.Count,
            "textract_detect_document_text",
            pages);
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

        var pages = OcrPageSplitter.FromTextractBlocks(blocks, request.EstimatedPageCount);
        var text = OcrPageSplitter.JoinDocumentText(pages);
        EnsureQuality(text);

        logger.LogInformation("Textract async OCR ok; pages={Pages}; chars={Chars}", pages.Count, text.Length);
        return new OcrEngineResult(
            ProviderKey,
            text,
            pages.Count,
            "textract_start_document_text_detection",
            pages);
    }

    private AmazonTextractClient CreateClient()
    {
        var t = options.Value.Textract;
        var region = RegionEndpoint.GetBySystemName(
            string.IsNullOrWhiteSpace(t.Region) ? "us-west-2" : t.Region!);
        var credentials =
            (!string.IsNullOrWhiteSpace(t.AccessKey) && !string.IsNullOrWhiteSpace(t.SecretKey)
                ? new BasicAWSCredentials(t.AccessKey, t.SecretKey)
                : null)
            ?? awsOptions.Value.TryCreateCredentials()
            ?? throw new InvalidOperationException("Textract is not configured (Aws:AccessKey/SecretKey or Ocr:Textract override).");
        return new AmazonTextractClient(credentials, region);
    }

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
