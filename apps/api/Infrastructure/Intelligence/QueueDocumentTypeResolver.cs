namespace Documate.Api.Infrastructure.Intelligence;

using System.Text.RegularExpressions;
using Documate.Api.Domain;

/// <summary>QueueRoute-allowed document type (catalog key + display name).</summary>
public sealed record QueueRoutableDocumentType(string DocumentTypeKey, string Name, long Id);

/// <summary>
/// Maps free-form page-intelligence labels onto QueueRoute catalog keys
/// (e.g. "Controlled Waste Transfer Note" → delivery_note).
/// </summary>
public static partial class QueueDocumentTypeResolver
{
    public static CorDocumentType? Resolve(
        string? identified,
        IReadOnlyDictionary<string, CorDocumentType> typesByKey)
    {
        if (string.IsNullOrWhiteSpace(identified) || typesByKey.Count == 0)
        {
            return null;
        }

        var raw = identified.Trim();
        if (typesByKey.TryGetValue(raw, out var exact))
        {
            return exact;
        }

        var needle = Normalize(raw);
        if (needle.Length == 0)
        {
            return null;
        }

        foreach (var type in typesByKey.Values)
        {
            if (Normalize(type.DocumentTypeKey) == needle || Normalize(type.Name) == needle)
            {
                return type;
            }
        }

        foreach (var type in typesByKey.Values)
        {
            var keyN = Normalize(type.DocumentTypeKey);
            var nameN = Normalize(type.Name);
            if (IsLooseMatch(needle, keyN) || IsLooseMatch(needle, nameN))
            {
                return type;
            }
        }

        foreach (var (alias, catalogKey) in Aliases)
        {
            var hit = alias.Length >= 4
                ? needle.Contains(alias, StringComparison.Ordinal)
                : needle == alias;
            if (hit && typesByKey.TryGetValue(catalogKey, out var aliased))
            {
                return aliased;
            }
        }

        return null;
    }

    public static string? ResolveKey(
        string? identified,
        IReadOnlyList<QueueRoutableDocumentType> routable)
    {
        if (routable.Count == 0)
        {
            return null;
        }

        var map = routable.ToDictionary(
            x => x.DocumentTypeKey,
            x => new CorDocumentType
            {
                Id = x.Id,
                DocumentTypeKey = x.DocumentTypeKey,
                Name = x.Name,
            },
            StringComparer.OrdinalIgnoreCase);
        return Resolve(identified, map)?.DocumentTypeKey;
    }

    private static bool IsLooseMatch(string needle, string candidate)
    {
        if (candidate.Length < 3)
        {
            return needle == candidate;
        }

        return needle.Contains(candidate, StringComparison.Ordinal)
               || candidate.Contains(needle, StringComparison.Ordinal);
    }

    private static string Normalize(string value) =>
        NonAlphaNum().Replace(value.Trim().ToLowerInvariant(), "");

    /// <summary>Normalized alias fragment → preferred DocumentTypeKey.</summary>
    private static readonly (string Alias, string CatalogKey)[] Aliases =
    [
        ("controlledwastetransfernote", "delivery_note"),
        ("wastetransfernote", "delivery_note"),
        ("wastetransfer", "delivery_note"),
        ("dutyofcare", "delivery_note"),
        ("deliverynote", "delivery_note"),
        ("goodsreceived", "delivery_note"),
        ("packinglist", "delivery_note"),
        ("salesinvoice", "invoice"),
        ("serviceinvoice", "invoice"),
        ("taxinvoice", "invoice"),
        ("proformainvoice", "invoice"),
        ("commercialinvoice", "invoice"),
        ("creditnote", "credit_note"),
        ("purchaseorder", "purchase_order"),
    ];

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NonAlphaNum();
}
