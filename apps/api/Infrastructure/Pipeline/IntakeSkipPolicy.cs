namespace Documate.Api.Infrastructure.Pipeline;

/// <summary>
/// Skip split+classify only when the caller named a type and normalize found exactly one page.
/// A type hint alone is not enough: a multi-page file may contain several documents of that type.
/// </summary>
public static class IntakeSkipPolicy
{
    public static bool SkipSplitAndClassify(bool hasPredeterminedType, int pageCount) =>
        hasPredeterminedType && pageCount == 1;
}
