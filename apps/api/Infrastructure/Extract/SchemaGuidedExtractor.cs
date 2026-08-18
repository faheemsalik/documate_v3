namespace Documate.Api.Infrastructure.Extract;

using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

/// <summary>
/// Fills an Agent JSON Schema from source text: JSON pass-through, then label:value lines.
/// Used by Mode 1 documate_meta until a live LLM body is wired.
/// </summary>
public static class SchemaGuidedExtractor
{
    public static JsonObject Extract(string schemaJson, string text)
    {
        JsonNode? schema;
        try
        {
            schema = JsonNode.Parse(schemaJson);
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new InvalidOperationException($"Agent outputSchemaJson is not valid JSON: {ex.Message}");
        }

        if (schema is not JsonObject schemaObj)
        {
            throw new InvalidOperationException("Agent outputSchemaJson must be a JSON object schema.");
        }

        var result = new JsonObject();
        var properties = schemaObj["properties"] as JsonObject;
        if (properties is null || properties.Count == 0)
        {
            return result;
        }

        MergeFromJsonPayload(result, properties, text);
        MergeFromLabeledLines(result, properties, text);
        return result;
    }

    private static void MergeFromJsonPayload(JsonObject result, JsonObject properties, string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith('{') || !trimmed.EndsWith('}'))
        {
            return;
        }

        JsonNode? parsed;
        try
        {
            parsed = JsonNode.Parse(trimmed);
        }
        catch (System.Text.Json.JsonException)
        {
            return;
        }

        if (parsed is not JsonObject source)
        {
            return;
        }

        foreach (var (name, propSchema) in properties)
        {
            if (result.ContainsKey(name) || !source.TryGetPropertyValue(name, out var raw) || raw is null)
            {
                continue;
            }

            var coerced = Coerce(raw, TypeName(propSchema));
            if (coerced is not null)
            {
                result[name] = coerced;
            }
        }
    }

    private static void MergeFromLabeledLines(JsonObject result, JsonObject properties, string text)
    {
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var valuesByLabel = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            var sep = line.IndexOfAny([':', '=', '\t']);
            if (sep <= 0 || sep >= line.Length - 1)
            {
                continue;
            }

            var label = NormalizeKey(line[..sep]);
            var value = line[(sep + 1)..].Trim().Trim('"', '\'');
            if (label.Length > 0 && value.Length > 0)
            {
                valuesByLabel.TryAdd(label, value);
            }
        }

        foreach (var (name, propSchema) in properties)
        {
            if (result.ContainsKey(name))
            {
                continue;
            }

            var typeName = TypeName(propSchema);
            if (typeName == "array")
            {
                result[name] = new JsonArray();
                continue;
            }

            if (typeName == "object")
            {
                result[name] = new JsonObject();
                continue;
            }

            string? found = null;
            foreach (var alias in Aliases(name))
            {
                if (valuesByLabel.TryGetValue(alias, out var v))
                {
                    found = v;
                    break;
                }
            }

            if (found is null)
            {
                continue;
            }

            var coerced = Coerce(JsonValue.Create(found), typeName);
            if (coerced is not null)
            {
                result[name] = coerced;
            }
        }
    }

    private static string? TypeName(JsonNode? propSchema) =>
        propSchema is JsonObject obj && obj["type"] is JsonValue v && v.TryGetValue<string>(out var t)
            ? t
            : null;

    private static JsonNode? Coerce(JsonNode raw, string? typeName)
    {
        if (typeName is null)
        {
            return raw.DeepClone();
        }

        return typeName switch
        {
            "string" => JsonValue.Create(raw.ToString()),
            "boolean" => TryBool(raw, out var b) ? JsonValue.Create(b) : null,
            "integer" => TryDecimal(raw, out var i) && i == decimal.Truncate(i)
                ? JsonValue.Create((long)i)
                : null,
            "number" => TryDecimal(raw, out var n) ? JsonValue.Create(n) : null,
            "array" => raw is JsonArray arr ? arr.DeepClone() : new JsonArray(),
            "object" => raw is JsonObject obj ? obj.DeepClone() : new JsonObject(),
            _ => raw.DeepClone(),
        };
    }

    private static bool TryDecimal(JsonNode raw, out decimal value)
    {
        if (raw is JsonValue jv)
        {
            if (jv.TryGetValue<decimal>(out value))
            {
                return true;
            }

            if (jv.TryGetValue<double>(out var d))
            {
                value = (decimal)d;
                return true;
            }

            if (jv.TryGetValue<long>(out var l))
            {
                value = l;
                return true;
            }

            if (jv.TryGetValue<string>(out var s))
            {
                var cleaned = Regex.Replace(s, @"[^\d.\-]", "");
                return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
            }
        }

        value = 0;
        return decimal.TryParse(raw.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryBool(JsonNode raw, out bool value)
    {
        if (raw is JsonValue jv)
        {
            if (jv.TryGetValue<bool>(out value))
            {
                return true;
            }

            if (jv.TryGetValue<string>(out var s)
                && bool.TryParse(s, out value))
            {
                return true;
            }
        }

        value = false;
        return false;
    }

    private static IEnumerable<string> Aliases(string propertyName)
    {
        yield return NormalizeKey(propertyName);
        yield return NormalizeKey(propertyName.Replace("_", " "));
        yield return NormalizeKey(propertyName.Replace("_", ""));
        if (propertyName.EndsWith("_number", StringComparison.OrdinalIgnoreCase))
        {
            yield return NormalizeKey(propertyName[..^7] + " no");
            yield return NormalizeKey(propertyName[..^7] + " #");
            yield return NormalizeKey(propertyName[..^7] + " num");
        }
    }

    private static string NormalizeKey(string value) =>
        Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "");
}
