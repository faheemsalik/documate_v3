namespace Documate.Api.Modules.FrontendSupport.Features.Files;

using System.Text.RegularExpressions;

public static partial class AppFileSchemaSearch
{
    public const int MaxFieldKeyLength = 128;
    public const int MaxSearchValueLength = 256;
    public const int MaxPageSize = 200;
    public const int DefaultPageSize = 50;

    [GeneratedRegex("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex FieldKeyPattern();

    public static bool TryBuildJsonPath(string? fieldKey, out string jsonPath, out string? error)
    {
        jsonPath = "";
        if (string.IsNullOrWhiteSpace(fieldKey))
        {
            error = "fieldKey is required.";
            return false;
        }

        var trimmed = fieldKey.Trim();
        if (trimmed.Length > MaxFieldKeyLength)
        {
            error = $"fieldKey must be at most {MaxFieldKeyLength} characters.";
            return false;
        }

        if (!FieldKeyPattern().IsMatch(trimmed))
        {
            error = "fieldKey must start with a letter or underscore and contain only letters, digits, or underscores.";
            return false;
        }

        jsonPath = "$." + trimmed;
        error = null;
        return true;
    }

    public static bool TryNormalizeSearchValue(string? value, out string normalized, out string? error)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(value))
        {
            error = "value is required.";
            return false;
        }

        normalized = value.Trim();
        if (normalized.Length > MaxSearchValueLength)
        {
            error = $"value must be at most {MaxSearchValueLength} characters.";
            return false;
        }

        error = null;
        return true;
    }

    public static string NormalizeMatchMode(string? matchMode)
    {
        if (string.IsNullOrWhiteSpace(matchMode))
        {
            return "exact";
        }

        return matchMode.Trim().Equals("contains", StringComparison.OrdinalIgnoreCase) ? "contains" : "exact";
    }

    public static string EscapeLike(string input) =>
        input.Replace("[", "[[]", StringComparison.Ordinal).Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("_", "[_]", StringComparison.Ordinal);
}
