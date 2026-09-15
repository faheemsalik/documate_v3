namespace Documate.Api.Tests;

using Documate.Api.Infrastructure.Intelligence;

public class DocumentBoundaryEngineTests
{
    [Fact]
    public void New_identity_closes_previous_group()
    {
        var groups = DocumentBoundaryEngine.Group(
        [
            Page(1, number: "INV-1", starts: true),
            Page(2, number: "INV-1", continues: true),
            Page(3, number: "INV-2", starts: true),
        ]);

        Assert.Collection(
            groups,
            x => Assert.Equal((1, 2, "INV-1"), (x.PageStart, x.PageEnd, x.PrimaryNumber)),
            x => Assert.Equal((3, 3, "INV-2"), (x.PageStart, x.PageEnd, x.PrimaryNumber)));
    }

    [Fact]
    public void Blank_page_closes_and_resets_anchor()
    {
        var groups = DocumentBoundaryEngine.Group(
        [
            Page(1, number: "DN-1", starts: true),
            Page(2, blank: true),
            Page(3, number: "DN-2", starts: true),
        ]);

        Assert.Equal(2, groups.Count);
        Assert.Equal((1, 1), (groups[0].PageStart, groups[0].PageEnd));
        Assert.Equal((3, 3), (groups[1].PageStart, groups[1].PageEnd));
    }

    [Fact]
    public void Page_two_without_continuation_is_unresolved()
    {
        var group = Assert.Single(DocumentBoundaryEngine.Group(
        [
            Page(1, starts: true),
            Page(2),
        ]));

        Assert.True(group.Unresolved);
    }

    [Fact]
    public void Explicit_sequence_restart_opens_new_group()
    {
        var groups = DocumentBoundaryEngine.Group(
        [
            Page(1, number: "CN-1", starts: true),
            Page(2, number: "CN-1", continues: true),
            Page(3, number: "CN-1", starts: true),
        ]);

        Assert.Equal(2, groups.Count);
        Assert.Equal((1, 2), (groups[0].PageStart, groups[0].PageEnd));
        Assert.Equal((3, 3), (groups[1].PageStart, groups[1].PageEnd));
    }

    private static PageIntelligenceProfile Page(
        int page,
        string? number = null,
        bool starts = false,
        bool continues = false,
        bool blank = false) =>
        new(page, null, number, starts, continues, false, blank, "test", []);
}
