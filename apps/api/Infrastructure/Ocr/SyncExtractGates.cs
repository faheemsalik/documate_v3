namespace Documate.Api.Infrastructure.Ocr;

/// <summary>Cheap page/size estimates for sync extract gates (DQ-0704).</summary>
public static class SyncExtractGates
{
    public static void EnsureWithinLimits(
        IFormFile file,
        int syncMaxPages,
        long syncMaxBytes)
    {
        var maxBytes = syncMaxBytes > 0 ? syncMaxBytes : 5_242_880;
        if (file.Length > maxBytes)
        {
            throw new InvalidOperationException(
                $"Sync-wait rejects files larger than {maxBytes} bytes (got {file.Length}). Use async POST /files.");
        }

        var maxPages = syncMaxPages > 0 ? syncMaxPages : 3;
        var pages = EstimatePages(file);
        if (pages > maxPages)
        {
            throw new InvalidOperationException(
                $"Sync-wait rejects files with more than {maxPages} pages (estimated {pages}). Use async POST /files.");
        }
    }

    public static int EstimatePages(IFormFile file)
    {
        var ct = (file.ContentType ?? "").ToLowerInvariant();
        var ext = Path.GetExtension(file.FileName ?? "").ToLowerInvariant();

        if (ct.StartsWith("text/", StringComparison.Ordinal)
            || ext is ".txt" or ".csv" or ".md" or ".json" or ".xml" or ".html" or ".htm")
        {
            return 1;
        }

        if (ct is "image/png" or "image/jpeg" or "image/jpg" || ext is ".png" or ".jpg" or ".jpeg")
        {
            return 1;
        }

        if (ct.Contains("pdf", StringComparison.Ordinal) || ext == ".pdf")
        {
            using var stream = file.OpenReadStream();
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            return PdfPageCounter.TryCount(buffer.ToArray()) ?? 2;
        }

        // Unknown binary: treat as over-limit for sync so partners use async.
        return int.MaxValue;
    }
}
