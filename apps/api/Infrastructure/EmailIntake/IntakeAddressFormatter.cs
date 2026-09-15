using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Documate.Api.Infrastructure.EmailIntake;

public static class IntakeAddressFormatter
{
    private static readonly Regex NonSlug = new("[^a-z0-9]+", RegexOptions.Compiled);

    public static string Slugify(string? raw, string fallback, int maxLen = 24)
    {
        var s = (raw ?? "").Trim().ToLowerInvariant();
        s = NonSlug.Replace(s, "-").Trim('-');
        if (string.IsNullOrEmpty(s))
        {
            s = fallback;
        }

        if (s.Length > maxLen)
        {
            s = s[..maxLen].TrimEnd('-');
        }

        return string.IsNullOrEmpty(s) ? fallback : s;
    }

    public static string RandomToken(int byteCount = 16)
    {
        Span<byte> bytes = stackalloc byte[byteCount];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>Builds {biz}-{purpose}-{random} truncated to 64 chars.</summary>
    public static string BuildLocalPart(string businessSlug, string purposeSlug, string? random = null)
    {
        var biz = Slugify(businessSlug, "biz", 20);
        var purpose = Slugify(purposeSlug, "mail", 20);
        var token = random ?? RandomToken();
        var local = $"{biz}-{purpose}-{token}";
        if (local.Length <= 64)
        {
            return local;
        }

        var overhead = biz.Length + purpose.Length + 2;
        var tokenLen = Math.Max(16, 64 - overhead);
        if (token.Length > tokenLen)
        {
            token = token[..tokenLen];
        }

        return $"{biz}-{purpose}-{token}";
    }

    public static string FormatAddress(string localPart, string domain) =>
        $"{localPart}@{domain}";
}
