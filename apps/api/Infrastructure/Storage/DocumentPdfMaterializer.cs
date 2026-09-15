namespace Documate.Api.Infrastructure.Storage;

using Documate.Api.Domain;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;

public interface IDocumentPdfMaterializer
{
    Task MaterializeAsync(OpsFile file, OpsDocument document, CancellationToken cancellationToken = default);
}

public sealed class DocumentPdfMaterializer(
    IObjectStorage storage,
    ILogger<DocumentPdfMaterializer> logger) : IDocumentPdfMaterializer
{
    public async Task MaterializeAsync(
        OpsFile file,
        OpsDocument document,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(file.StorageBucket)
            || string.IsNullOrWhiteSpace(file.StorageKey)
            || document.PageStart is not int pageStart
            || document.PageEnd is not int pageEnd)
        {
            return;
        }

        var isPdf = string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
            || file.OriginalFileName?.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) == true;
        var isImage = file.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true;
        if (!isPdf && !(isImage && pageStart == 1 && pageEnd == 1))
        {
            return;
        }

        await using var original = await storage.DownloadAsync(file.StorageBucket, file.StorageKey, cancellationToken);
        await using var copy = new MemoryStream();
        await original.CopyToAsync(copy, cancellationToken);
        byte[] bytes;
        string contentType;

        if (isPdf)
        {
            using var source = PdfDocument.Open(copy.ToArray());
            var builder = new PdfDocumentBuilder();
            for (var page = pageStart; page <= pageEnd; page++)
            {
                builder.AddPage(source, page);
            }

            bytes = builder.Build();
            contentType = "application/pdf";
        }
        else
        {
            bytes = copy.ToArray();
            contentType = file.ContentType ?? "application/octet-stream";
        }

        var key = storage.BuildArtifactKey(file.StorageKey, $"document.{document.SequenceId}.pdf");
        await using var output = new MemoryStream(bytes);
        await storage.UploadAsync(
            new ObjectStoragePutRequest(
                file.StorageBucket,
                key,
                output,
                contentType,
                new Dictionary<string, string>
                {
                    ["FileId"] = file.Id.ToString(),
                    ["DocumentId"] = document.Id.ToString(),
                    ["Artifact"] = "document.pdf",
                }),
            cancellationToken);
        document.PdfStorageBucket = file.StorageBucket;
        document.PdfStorageKey = key;
        logger.LogInformation("Materialized Document {DocumentId} pages {Start}-{End}", document.Id, pageStart, pageEnd);
    }
}
