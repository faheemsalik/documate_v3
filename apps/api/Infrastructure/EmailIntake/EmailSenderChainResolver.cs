namespace Documate.Api.Infrastructure.EmailIntake;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

public sealed record EmailParty(string? Email, string? Name);

public sealed record EmailChainHop(string Role, string? Email, string? Name);

public sealed record EmailSenderChainResult(
    EmailParty From,
    EmailParty? Originator,
    IReadOnlyList<EmailChainHop> Chain);

/// <summary>Best-effort Vendor → User → intake chain (Plan 14 F4a=A).</summary>
public static partial class EmailSenderChainResolver
{
    public static EmailSenderChainResult Resolve(
        string? fromRaw,
        string? fromName,
        string? replyTo,
        string? resentFrom,
        string? textBody,
        string? intakeAddress)
    {
        var forwarderEmail = EmailAllowlistMatcher.NormalizeAddress(fromRaw);
        var forwarderName = NormalizeName(fromName) ?? ExtractDisplayName(fromRaw);

        var originator = FindOriginator(forwarderEmail, replyTo, resentFrom, textBody);
        var intakeEmail = EmailAllowlistMatcher.NormalizeAddress(intakeAddress);

        var chain = new List<EmailChainHop>();
        if (originator is not null
            && !string.Equals(originator.Email, forwarderEmail, StringComparison.OrdinalIgnoreCase))
        {
            chain.Add(new EmailChainHop("originator", originator.Email, originator.Name));
        }

        if (!string.IsNullOrWhiteSpace(forwarderEmail) || !string.IsNullOrWhiteSpace(forwarderName))
        {
            chain.Add(new EmailChainHop("forwarder", forwarderEmail, forwarderName));
        }

        if (!string.IsNullOrWhiteSpace(intakeEmail))
        {
            chain.Add(new EmailChainHop("intake", intakeEmail, null));
        }

        return new EmailSenderChainResult(
            new EmailParty(forwarderEmail, forwarderName),
            originator,
            chain);
    }

    private static EmailParty? FindOriginator(
        string? forwarderEmail,
        string? replyTo,
        string? resentFrom,
        string? textBody)
    {
        foreach (var candidate in new[] { replyTo, resentFrom })
        {
            var email = EmailAllowlistMatcher.NormalizeAddress(candidate);
            if (email is null)
            {
                continue;
            }

            if (string.Equals(email, forwarderEmail, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return new EmailParty(email, ExtractDisplayName(candidate));
        }

        var bodyFrom = BodyFromHeaderRegex().Match(textBody ?? "");
        if (bodyFrom.Success)
        {
            var raw = bodyFrom.Groups[1].Value.Trim();
            var email = EmailAllowlistMatcher.NormalizeAddress(raw);
            if (email is not null
                && !string.Equals(email, forwarderEmail, StringComparison.OrdinalIgnoreCase))
            {
                return new EmailParty(email, ExtractDisplayName(raw) ?? NormalizeName(bodyFrom.Groups[1].Value));
            }
        }

        return null;
    }

    public static string? ExtractDisplayName(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var s = raw.Trim();
        var lt = s.IndexOf('<');
        if (lt > 0)
        {
            return NormalizeName(s[..lt]);
        }

        return null;
    }

    private static string? NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var s = name.Trim().Trim('"', '\'');
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    [GeneratedRegex(@"^From:\s*(.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex BodyFromHeaderRegex();
}

public static class EmailBodyExcerpt
{
    public static (string? Excerpt, bool Truncated) Truncate(string? textBody, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(textBody) || maxChars <= 0)
        {
            return (null, false);
        }

        var body = textBody.Trim();
        if (body.Length <= maxChars)
        {
            return (body, false);
        }

        return (body[..maxChars], true);
    }
}

public static class EmailIntakeJsonBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Build(
        EmailSenderChainResult chain,
        string? emailBodyExcerpt,
        bool emailBodyTruncated,
        IReadOnlyList<EmailIntakeSkippedAttachment>? skippedAttachments = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["from"] = new { email = chain.From.Email, name = chain.From.Name },
            ["originator"] = chain.Originator is null
                ? null
                : new { email = chain.Originator.Email, name = chain.Originator.Name },
            ["chain"] = chain.Chain.Select(h => new { role = h.Role, email = h.Email, name = h.Name }).ToList(),
        };

        if (!string.IsNullOrEmpty(emailBodyExcerpt))
        {
            payload["emailBodyExcerpt"] = emailBodyExcerpt;
            payload["emailBodyTruncated"] = emailBodyTruncated;
        }

        if (skippedAttachments is { Count: > 0 })
        {
            payload["skippedAttachments"] = skippedAttachments
                .Select(s => new { fileName = s.FileName, reason = s.Reason })
                .ToList();
        }

        return JsonSerializer.Serialize(payload, JsonOptions);
    }
}
