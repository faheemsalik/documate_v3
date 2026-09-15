namespace Documate.Api.Infrastructure.Ocr;

using Documate.Api.Infrastructure.Options;
using Google.Cloud.DocumentAI.V1;
using Google.Protobuf;
using Microsoft.Extensions.Options;

/// <summary>Google Document AI ProcessDocument with raw bytes (PDF/images).</summary>
public sealed class GoogleDocumentAiOcrEngine(
    IOptions<OcrOptions> options,
    ILogger<GoogleDocumentAiOcrEngine> logger) : IOcrEngine
{
    public string ProviderKey => "google_document_ai";

    public bool IsConfigured
    {
        get
        {
            var g = options.Value.GoogleDocumentAi;
            return !string.IsNullOrWhiteSpace(g.ProjectId)
                && !string.IsNullOrWhiteSpace(g.ProcessorId)
                && !string.IsNullOrWhiteSpace(g.Location);
        }
    }

    public async Task<OcrEngineResult> RecognizeAsync(OcrEngineRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Google Document AI is not configured (Ocr:GoogleDocumentAi ProjectId/Location/ProcessorId).");
        }

        var g = options.Value.GoogleDocumentAi;
        var clientBuilder = new DocumentProcessorServiceClientBuilder();
        if (!string.IsNullOrWhiteSpace(g.CredentialsJson))
        {
            clientBuilder.JsonCredentials = g.CredentialsJson;
        }

        var client = await clientBuilder.BuildAsync(cancellationToken);
        var name = ProcessorName.FromProjectLocationProcessor(g.ProjectId!, g.Location!, g.ProcessorId!);

        var mime = ResolveMime(request.ContentType, request.OriginalFileName);
        var raw = await client.ProcessDocumentAsync(
            new ProcessRequest
            {
                Name = name.ToString(),
                RawDocument = new RawDocument
                {
                    Content = ByteString.CopyFrom(request.Bytes),
                    MimeType = mime,
                },
            },
            cancellationToken: cancellationToken);

        var doc = raw.Document;
        var pages = OcrPageSplitter.FromGoogleDocument(doc, request.EstimatedPageCount);
        var text = OcrPageSplitter.JoinDocumentText(pages);
        if (string.IsNullOrWhiteSpace(text) || text.Length < 8)
        {
            throw new OcrEmptyOrLowQualityException("Google Document AI returned empty or very short text.");
        }

        logger.LogInformation("Document AI OCR ok; pages={Pages}; chars={Chars}", pages.Count, text.Length);
        return new OcrEngineResult(ProviderKey, text, pages.Count, "google_document_ai_process", pages);
    }

    private static string ResolveMime(string? contentType, string? fileName)
    {
        if (!string.IsNullOrWhiteSpace(contentType)
            && !contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return contentType;
        }

        var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/pdf",
        };
    }
}
