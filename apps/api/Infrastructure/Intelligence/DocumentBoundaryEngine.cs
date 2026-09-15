namespace Documate.Api.Infrastructure.Intelligence;

public sealed record DocumentPageGroup(
    int PageStart,
    int PageEnd,
    bool Unresolved,
    string? PrimaryNumber,
    string? DocumentType);

public static class DocumentBoundaryEngine
{
    public static IReadOnlyList<DocumentPageGroup> Group(IReadOnlyList<PageIntelligenceProfile> profiles)
    {
        var groups = new List<DocumentPageGroup>();
        var ordered = profiles.OrderBy(x => x.Page).ToArray();
        PageIntelligenceProfile? first = null;
        PageIntelligenceProfile? last = null;
        var unresolved = false;

        void Close()
        {
            if (first is null || last is null)
            {
                return;
            }

            groups.Add(new DocumentPageGroup(
                first.Page,
                last.Page,
                unresolved,
                first.PrimaryDocumentNumber
                    ?? ordered.Where(x => x.Page >= first.Page && x.Page <= last.Page)
                        .Select(x => x.PrimaryDocumentNumber)
                        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                first.DocumentType
                    ?? ordered.Where(x => x.Page >= first.Page && x.Page <= last.Page)
                        .Select(x => x.DocumentType)
                        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))));
            first = null;
            last = null;
            unresolved = false;
        }

        foreach (var page in ordered)
        {
            if (page.IsBlank)
            {
                Close();
                continue;
            }

            if (first is null)
            {
                first = last = page;
                unresolved = page.Page > 1 && !page.StartsNewDocument && !page.ContinuesPrevious;
            }
            else
            {
                var newIdentity = !string.IsNullOrWhiteSpace(page.PrimaryDocumentNumber)
                    && !string.Equals(
                        page.PrimaryDocumentNumber,
                        first.PrimaryDocumentNumber,
                        StringComparison.OrdinalIgnoreCase);
                var sequenceRestart = page.StartsNewDocument;
                if (newIdentity || sequenceRestart)
                {
                    Close();
                    first = last = page;
                }
                else
                {
                    if (page.Page == 2 && !page.ContinuesPrevious)
                    {
                        unresolved = true;
                    }

                    last = page;
                }
            }

            if (page.DocumentComplete)
            {
                Close();
            }
        }

        Close();
        return groups;
    }
}
