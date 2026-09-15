using Amazon.Textract;
using Amazon.Textract.Model;
using Documate.Api.Infrastructure.Ocr;
using GoogleDoc = Google.Cloud.DocumentAI.V1.Document;

namespace Documate.Api.Tests;

public sealed class OcrPageSplitterTests
{
    [Fact]
    public void FromTextractBlocks_SplitsLinesByPage()
    {
        var blocks = new List<Block>
        {
            Line(1, "INV-1001"),
            Line(1, "Total due"),
            Line(2, "INV-1002"),
            Line(3, "Page three"),
        };

        var pages = OcrPageSplitter.FromTextractBlocks(blocks, estimatedPageCount: 3);

        Assert.Equal(3, pages.Count);
        Assert.Equal(1, pages[0].Page);
        Assert.Contains("INV-1001", pages[0].Text);
        Assert.False(pages[0].IsBlank);
        Assert.Equal("INV-1002", pages[1].Text);
        Assert.Equal("Page three", pages[2].Text);
        Assert.Contains("INV-1001", OcrPageSplitter.JoinDocumentText(pages));
        Assert.Contains("INV-1002", OcrPageSplitter.JoinDocumentText(pages));
    }

    [Fact]
    public void FromTextractBlocks_PadsMissingPagesAsBlank()
    {
        var blocks = new List<Block> { Line(1, "Only page one has text") };

        var pages = OcrPageSplitter.FromTextractBlocks(blocks, estimatedPageCount: 3);

        Assert.Equal(3, pages.Count);
        Assert.False(pages[0].IsBlank);
        Assert.True(pages[1].IsBlank);
        Assert.True(pages[2].IsBlank);
        Assert.Equal("", pages[1].Text);
    }

    [Fact]
    public void FromTextractBlocks_DefaultsMissingPagePropertyToOne()
    {
        var blocks = new List<Block>
        {
            new()
            {
                BlockType = BlockType.LINE,
                Text = "Sync image line",
            },
        };

        var pages = OcrPageSplitter.FromTextractBlocks(blocks, estimatedPageCount: 1);

        Assert.Single(pages);
        Assert.Equal("Sync image line", pages[0].Text);
    }

    [Fact]
    public void ExtractAnchorText_UsesSegments()
    {
        const string full = "AAAAINV-9BBBB";
        var anchor = new GoogleDoc.Types.TextAnchor();
        anchor.TextSegments.Add(new GoogleDoc.Types.TextAnchor.Types.TextSegment
        {
            StartIndex = 4,
            EndIndex = 9,
        });

        var text = OcrPageSplitter.ExtractAnchorText(full, anchor);

        Assert.Equal("INV-9", text);
    }

    [Fact]
    public void FromGoogleDocument_UsesPerPageAnchors()
    {
        var doc = new GoogleDoc { Text = "PAGE1TEXTPAGE2TEXT" };
        var page1 = new GoogleDoc.Types.Page();
        page1.Layout = new GoogleDoc.Types.Page.Types.Layout
        {
            TextAnchor = new GoogleDoc.Types.TextAnchor(),
        };
        page1.Layout.TextAnchor.TextSegments.Add(new GoogleDoc.Types.TextAnchor.Types.TextSegment
        {
            StartIndex = 0,
            EndIndex = 9,
        });
        var page2 = new GoogleDoc.Types.Page();
        page2.Layout = new GoogleDoc.Types.Page.Types.Layout
        {
            TextAnchor = new GoogleDoc.Types.TextAnchor(),
        };
        page2.Layout.TextAnchor.TextSegments.Add(new GoogleDoc.Types.TextAnchor.Types.TextSegment
        {
            StartIndex = 9,
            EndIndex = 18,
        });
        doc.Pages.Add(page1);
        doc.Pages.Add(page2);

        var pages = OcrPageSplitter.FromGoogleDocument(doc, estimatedPageCount: 2);

        Assert.Equal(2, pages.Count);
        Assert.Equal("PAGE1TEXT", pages[0].Text);
        Assert.Equal("PAGE2TEXT", pages[1].Text);
        Assert.False(pages[0].IsBlank);
        Assert.False(pages[1].IsBlank);
    }

    private static Block Line(int page, string text) => new()
    {
        BlockType = BlockType.LINE,
        Text = text,
        Page = page,
    };
}
