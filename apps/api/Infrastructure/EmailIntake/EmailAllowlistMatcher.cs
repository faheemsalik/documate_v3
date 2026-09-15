namespace Documate.Api.Infrastructure.EmailIntake;

public sealed record AllowlistDecision(bool Accept, bool OnList, string ModeKey);

public static class EmailAllowlistMatcher
{
    public static bool Matches(
        string? fromAddress,
        string allowlistModeKey,
        IReadOnlyList<(string MatchTypeKey, string Value)> entries) =>
        Evaluate(fromAddress, allowlistModeKey, entries).Accept;

    public static AllowlistDecision Evaluate(
        string? fromAddress,
        string allowlistModeKey,
        IReadOnlyList<(string MatchTypeKey, string Value)> entries)
    {
        var mode = string.IsNullOrWhiteSpace(allowlistModeKey) ? "open" : allowlistModeKey.Trim();
        var onList = IsOnList(fromAddress, entries);

        if (string.Equals(mode, "open", StringComparison.OrdinalIgnoreCase))
        {
            return new AllowlistDecision(true, onList, "open");
        }

        if (string.Equals(mode, "allowlist_preferred", StringComparison.OrdinalIgnoreCase))
        {
            // Accept either way; caller may metric/flag misses (Phase 1).
            return new AllowlistDecision(true, onList, "allowlist_preferred");
        }

        // allowlist_enforced (and unknown modes treated as enforced)
        return new AllowlistDecision(onList, onList, "allowlist_enforced");
    }

    public static bool IsOnList(
        string? fromAddress,
        IReadOnlyList<(string MatchTypeKey, string Value)> entries)
    {
        var from = NormalizeAddress(fromAddress);
        if (from is null)
        {
            return false;
        }

        var at = from.LastIndexOf('@');
        var domain = at >= 0 && at < from.Length - 1 ? from[(at + 1)..] : "";

        foreach (var (matchTypeKey, value) in entries)
        {
            var v = NormalizeAddress(value) ?? value.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(v))
            {
                continue;
            }

            if (string.Equals(matchTypeKey, "email", StringComparison.OrdinalIgnoreCase)
                && string.Equals(from, v, StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(matchTypeKey, "domain", StringComparison.OrdinalIgnoreCase))
            {
                var d = v.TrimStart('@');
                // Domain entries are not full addresses — strip accidental user@
                var domainOnly = d.Contains('@') ? d[(d.LastIndexOf('@') + 1)..] : d;
                if (!string.IsNullOrEmpty(domain) && string.Equals(domain, domainOnly, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Extracts bare address from <c>Name &lt;user@host&gt;</c> or plain <c>user@host</c>.</summary>
    public static string? NormalizeAddress(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var s = raw.Trim();
        var lt = s.LastIndexOf('<');
        var gt = s.LastIndexOf('>');
        if (lt >= 0 && gt > lt)
        {
            s = s[(lt + 1)..gt].Trim();
        }

        s = s.Trim().Trim('"').ToLowerInvariant();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
