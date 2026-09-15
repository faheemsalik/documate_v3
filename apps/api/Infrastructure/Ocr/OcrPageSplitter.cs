namespace Documate.Api.Infrastructure.Ocr;

using System.Text;
using Amazon.Textract;
using Amazon.Textract.Model;
using Google.Cloud.DocumentAI.V1;
using Document = Google.Cloud.DocumentAI.V1.Document;

/// <summary>Builds genuine per-page OCR text for DQ-0705 / P9.</summary>
public static class OcrPageSplitter
{
    public static IReadOnlyList<OcrPageText> FromTextractBlocks(
        IList<Block>? blocks,
        int estimatedPageCount)
    {
        var linesByPage = new SortedDictionary<int, List<string>>();
        foreach (var block in blocks ?? Array.Empty<Block>())
        {
            if (block.BlockType != BlockType.LINE || string.IsNullOrWhiteSpace(block.Text))
            {
                continue;
            }

            var page = block.Page is > 0 ? block.Page.Value : 1;
            if (!linesByPage.TryGetValue(page, out var lines))
            {
                lines = [];
                linesByPage[page] = lines;
            }

            lines.Add(block.Text.Trim());
        }

        var maxFromBlocks = linesByPage.Count == 0 ? 1 : linesByPage.Keys.Max();
        var pageCount = Math.Max(1, Math.Max(estimatedPageCount, maxFromBlocks));
        return BuildRange(pageCount, p =>
            linesByPage.TryGetValue(p, out var lines)
                ? string.Join('\n', lines).Trim()
                : "");
    }

    public static IReadOnlyList<OcrPageText> FromGoogleDocument(Document? doc, int estimatedPageCount)
    {
        var fullText = doc?.Text ?? "";
        if (doc?.Pages is not { Count: > 0 })
        {
            var single = fullText.Trim();
            return BuildRange(Math.Max(1, estimatedPageCount), p => p == 1 ? single : "");
        }

        var byPage = new SortedDictionary<int, string>();
        for (var i = 0; i < doc.Pages.Count; i++)
        {
            var pageNum = i + 1;
            var pageText = ExtractAnchorText(fullText, doc.Pages[i].Layout?.TextAnchor);
            if (string.IsNullOrWhiteSpace(pageText) && doc.Pages[i].Paragraphs is { Count: > 0 })
            {
                var parts = doc.Pages[i].Paragraphs
                    .Select(p => ExtractAnchorText(fullText, p.Layout?.TextAnchor))
                    .Where(t => !string.IsNullOrWhiteSpace(t));
                pageText = string.Join('\n', parts).Trim();
            }

            byPage[pageNum] = pageText;
        }

        var pageCount = Math.Max(1, Math.Max(estimatedPageCount, byPage.Keys.Max()));
        return BuildRange(pageCount, p => byPage.TryGetValue(p, out var t) ? t : "");
    }

    public static string JoinDocumentText(IReadOnlyList<OcrPageText> pages) =>
        string.Join("\n\n", pages.Select(p => p.Text).Where(t => !string.IsNullOrWhiteSpace(t))).Trim();

    public static string ExtractAnchorText(string fullText, Document.Types.TextAnchor? anchor)
    {
        if (string.IsNullOrEmpty(fullText) || anchor?.TextSegments is not { Count: > 0 })
        {
            return "";
        }

        var sb = new StringBuilder();
        foreach (var segment in anchor.TextSegments)
        {
            var start = (int)segment.StartIndex;
            var end = (int)segment.EndIndex;
            if (end <= 0 && start == 0)
            {
                end = fullText.Length;
            }

            if (start < 0 || end > fullText.Length || start >= end)
            {
                continue;
            }

            sb.Append(fullText, start, end - start);
        }

        return sb.ToString().Trim();
    }

    private static IReadOnlyList<OcrPageText> BuildRange(int pageCount, Func<int, string> textForPage)
    {
        var pages = new List<OcrPageText>(pageCount);
        for (var p = 1; p <= pageCount; p++)
        {
            var text = textForPage(p) ?? "";
            pages.Add(new OcrPageText(p, text, string.IsNullOrWhiteSpace(text)));
        }

        return pages;
    }
}
