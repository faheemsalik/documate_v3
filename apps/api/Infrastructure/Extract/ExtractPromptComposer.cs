namespace Documate.Api.Infrastructure.Extract;

using System.Text;

public static class ExtractPromptDefaults
{
    public const string SystemPrompt =
        "You extract structured data from documents. Return ONLY a JSON object matching the schema. No markdown.";

    public const string DocumentTextPlaceholder = "[Document text will be inserted at extract time]";
}

public sealed record ExtractPromptComposeResult(string SystemMessage, string UserMessage);

public interface IExtractPromptComposer
{
    ExtractPromptComposeResult Compose(
        string? systemPrompt,
        string? instructions,
        string outputSchemaJson,
        string? postProcessPrompt,
        string? documentText);

    /// <summary>Draft an admin system prompt from user-facing agent ingredients (no LLM).</summary>
    string SuggestSystemPrompt(string? instructions, string? outputSchemaJson, string? postProcessPrompt);
}

/// <summary>Plan 22 / DQ-1902 — shared extract prompt composer (preview and runtime).</summary>
public sealed class ExtractPromptComposer : IExtractPromptComposer
{
    public ExtractPromptComposeResult Compose(
        string? systemPrompt,
        string? instructions,
        string outputSchemaJson,
        string? postProcessPrompt,
        string? documentText)
    {
        var system = string.IsNullOrWhiteSpace(systemPrompt)
            ? ExtractPromptDefaults.SystemPrompt
            : systemPrompt.Trim();

        var user = new StringBuilder();
        user.AppendLine("Agent instructions:");
        user.AppendLine(instructions ?? "");
        user.AppendLine();
        user.AppendLine("Output JSON Schema:");
        user.AppendLine(string.IsNullOrWhiteSpace(outputSchemaJson) ? "{}" : outputSchemaJson);

        var post = postProcessPrompt?.Trim();
        if (!string.IsNullOrWhiteSpace(post))
        {
            user.AppendLine();
            user.AppendLine("Additional post-process instructions:");
            user.AppendLine(post);
        }

        user.AppendLine();
        user.AppendLine("Document text (full OCR / normalize text):");
        user.Append(string.IsNullOrWhiteSpace(documentText)
            ? ExtractPromptDefaults.DocumentTextPlaceholder
            : documentText);

        return new ExtractPromptComposeResult(system, user.ToString());
    }

    public string SuggestSystemPrompt(
        string? instructions,
        string? outputSchemaJson,
        string? postProcessPrompt)
    {
        var sb = new StringBuilder();
        sb.AppendLine(ExtractPromptDefaults.SystemPrompt);
        sb.AppendLine();
        sb.AppendLine(
            "The user message includes agent instructions, the output JSON schema, optional post-process rules, and the document text. Follow those sections in order.");

        var inst = instructions?.Trim();
        if (!string.IsNullOrWhiteSpace(inst))
        {
            sb.AppendLine();
            sb.AppendLine("Priorities from this agent's instructions:");
            sb.AppendLine(Truncate(inst, 1200));
        }

        var fields = SchemaPropertyHints(outputSchemaJson);
        if (fields.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Key fields to populate when present in the document:");
            foreach (var f in fields.Take(24))
            {
                sb.Append("- ");
                sb.AppendLine(f);
            }
        }

        var post = postProcessPrompt?.Trim();
        if (!string.IsNullOrWhiteSpace(post))
        {
            sb.AppendLine();
            sb.AppendLine("Always honor post-process constraints from the user message, including:");
            sb.AppendLine(Truncate(post, 800));
        }

        return sb.ToString().TrimEnd();
    }

    private static string Truncate(string value, int max)
    {
        if (value.Length <= max)
        {
            return value;
        }

        return value[..max].TrimEnd() + "…";
    }

    private static List<string> SchemaPropertyHints(string? outputSchemaJson)
    {
        var hints = new List<string>();
        if (string.IsNullOrWhiteSpace(outputSchemaJson))
        {
            return hints;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(outputSchemaJson);
            if (!doc.RootElement.TryGetProperty("properties", out var props)
                || props.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                return hints;
            }

            foreach (var prop in props.EnumerateObject())
            {
                var desc = "";
                if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object
                    && prop.Value.TryGetProperty("description", out var d)
                    && d.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    desc = d.GetString()?.Trim() ?? "";
                }

                hints.Add(string.IsNullOrWhiteSpace(desc) ? prop.Name : $"{prop.Name}: {desc}");
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // ignore malformed schema
        }

        return hints;
    }
}
