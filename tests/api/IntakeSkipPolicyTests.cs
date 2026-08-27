namespace Documate.Api.Tests;

using Documate.Api.Infrastructure.Ocr;
using Documate.Api.Infrastructure.Pipeline;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Writer;

public class IntakeSkipPolicyTests
{
    [Fact]
    public void Type_and_one_page_skips_split_and_classify()
    {
        Assert.True(IntakeSkipPolicy.SkipSplitAndClassify(hasPredeterminedType: true, pageCount: 1));
    }

    [Fact]
    public void Type_and_multiple_pages_does_not_skip()
    {
        Assert.False(IntakeSkipPolicy.SkipSplitAndClassify(hasPredeterminedType: true, pageCount: 2));
        Assert.False(IntakeSkipPolicy.SkipSplitAndClassify(hasPredeterminedType: true, pageCount: 5));
    }

    [Fact]
    public void No_type_never_skips()
    {
        Assert.False(IntakeSkipPolicy.SkipSplitAndClassify(hasPredeterminedType: false, pageCount: 1));
        Assert.False(IntakeSkipPolicy.SkipSplitAndClassify(hasPredeterminedType: false, pageCount: 3));
    }

    [Fact]
    public void Unknown_page_count_does_not_skip()
    {
        Assert.False(IntakeSkipPolicy.SkipSplitAndClassify(hasPredeterminedType: true, pageCount: 0));
    }
}

public class PdfPageCounterTests
{
    [Fact]
    public void Counts_single_page_pdf()
    {
        Assert.Equal(1, PdfPageCounter.TryCount(BuildPdf(pageCount: 1)));
    }

    [Fact]
    public void Counts_multi_page_pdf()
    {
        Assert.Equal(3, PdfPageCounter.TryCount(BuildPdf(pageCount: 3)));
    }

    [Fact]
    public void Non_pdf_returns_null()
    {
        Assert.Null(PdfPageCounter.TryCount("not a pdf"u8.ToArray()));
    }

    private static byte[] BuildPdf(int pageCount)
    {
        var builder = new PdfDocumentBuilder();
        for (var i = 0; i < pageCount; i++)
        {
            builder.AddPage(PageSize.A4);
        }

        return builder.Build();
    }
}
