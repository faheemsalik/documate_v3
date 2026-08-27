namespace Documate.Api.Infrastructure.Ocr;

using UglyToad.PdfPig;

/// <summary>Counts PDF pages from bytes. Returns null when the file is not a readable PDF.</summary>
public static class PdfPageCounter
{
    public static int? TryCount(byte[] bytes)
    {
        if (bytes is not { Length: > 4 })
        {
            return null;
        }

        try
        {
            using var document = PdfDocument.Open(bytes);
            var count = document.NumberOfPages;
            return count > 0 ? count : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
