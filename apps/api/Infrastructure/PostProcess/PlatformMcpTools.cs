namespace Documate.Api.Infrastructure.PostProcess;

using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

/// <summary>Normalize string date fields to ISO <c>yyyy-MM-dd</c> when parseable.</summary>
public sealed partial class NormalizeDateTool : IPlatformMcpTool
{
    public string ToolName => "normalize_date";

    public Task ExecuteAsync(JsonObject payload, IReadOnlyList<string> fields, CancellationToken cancellationToken = default)
    {
        foreach (var name in ResolveFieldNames(payload, fields))
        {
            if (payload[name] is not JsonValue value || !value.TryGetValue<string>(out var raw) || string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            if (TryParseDate(raw.Trim(), out var date))
            {
                payload[name] = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
        }

        return Task.CompletedTask;
    }

    private static bool TryParseDate(string raw, out DateOnly date)
    {
        if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        string[] formats =
        [
            "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy",
            "MM/dd/yyyy", "M/d/yyyy", "yyyy/MM/dd",
            "dd MMM yyyy", "d MMM yyyy", "MMM d yyyy", "MMMM d, yyyy",
        ];
        return DateOnly.TryParseExact(raw, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    internal static IEnumerable<string> ResolveFieldNames(JsonObject payload, IReadOnlyList<string> fields)
    {
        if (fields.Count == 0 || fields.Any(f => f == "*"))
        {
            return payload.Select(p => p.Key).Where(k => LooksLikeDateField(k));
        }

        return fields.Where(payload.ContainsKey);
    }

    private static bool LooksLikeDateField(string name) =>
        DateFieldRegex().IsMatch(name);

    [GeneratedRegex(@"date|day|_at$|_on$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DateFieldRegex();
}

/// <summary>Normalize currency strings to ISO 4217 codes (e.g. € → EUR).</summary>
public sealed partial class NormalizeCurrencyTool : IPlatformMcpTool
{
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["€"] = "EUR",
        ["euro"] = "EUR",
        ["euros"] = "EUR",
        ["$"] = "USD",
        ["usd"] = "USD",
        ["dollar"] = "USD",
        ["dollars"] = "USD",
        ["£"] = "GBP",
        ["gbp"] = "GBP",
        ["pound"] = "GBP",
        ["pounds"] = "GBP",
    };

    public string ToolName => "normalize_currency";

    public Task ExecuteAsync(JsonObject payload, IReadOnlyList<string> fields, CancellationToken cancellationToken = default)
    {
        foreach (var name in ResolveFieldNames(payload, fields))
        {
            if (payload[name] is not JsonValue value || !value.TryGetValue<string>(out var raw) || string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var trimmed = raw.Trim();
            if (Aliases.TryGetValue(trimmed, out var code))
            {
                payload[name] = code;
                continue;
            }

            if (trimmed.Length == 3 && trimmed.All(char.IsLetter))
            {
                payload[name] = trimmed.ToUpperInvariant();
            }
        }

        return Task.CompletedTask;
    }

    internal static IEnumerable<string> ResolveFieldNames(JsonObject payload, IReadOnlyList<string> fields)
    {
        if (fields.Count == 0 || fields.Any(f => f == "*"))
        {
            return payload.Select(p => p.Key).Where(k => LooksLikeCurrencyField(k));
        }

        return fields.Where(payload.ContainsKey);
    }

    private static bool LooksLikeCurrencyField(string name) =>
        CurrencyFieldRegex().IsMatch(name);

    [GeneratedRegex(@"currency|curr|ccy", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CurrencyFieldRegex();
}
