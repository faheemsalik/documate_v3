namespace Documate.Api.Infrastructure.Extract;

using System.Globalization;
using System.Text.Json.Nodes;

/// <summary>
/// Small JSON Schema checker for Agent output schemas (object/string/number/integer/boolean/array + required).
/// Full draft implementations can replace this later without changing the extract stage.
/// </summary>
public static class JsonSchemaLite
{
    public static SchemaValidationResult Validate(string schemaJson, JsonNode? instance)
    {
        JsonNode? schema;
        try
        {
            schema = JsonNode.Parse(schemaJson);
        }
        catch (System.Text.Json.JsonException ex)
        {
            return new SchemaValidationResult(false, [$"Invalid JSON Schema: {ex.Message}"]);
        }

        if (schema is null)
        {
            return new SchemaValidationResult(false, ["JSON Schema is empty."]);
        }

        var errors = new List<string>();
        ValidateNode(schema, instance, "$", errors);
        return new SchemaValidationResult(errors.Count == 0, errors);
    }

    private static void ValidateNode(JsonNode schema, JsonNode? instance, string path, List<string> errors)
    {
        var typeToken = schema["type"];
        if (typeToken is JsonValue typeValue && typeValue.TryGetValue<string>(out var typeName))
        {
            if (!MatchesType(typeName, instance, path, errors))
            {
                return;
            }
        }

        if (instance is JsonObject obj && schema["properties"] is JsonObject properties)
        {
            if (schema["required"] is JsonArray required)
            {
                foreach (var item in required)
                {
                    var name = item?.GetValue<string>();
                    if (!string.IsNullOrWhiteSpace(name) && !obj.ContainsKey(name))
                    {
                        errors.Add($"{path}: missing required property '{name}'.");
                    }
                }
            }

            foreach (var (name, propSchema) in properties)
            {
                if (propSchema is null || !obj.TryGetPropertyValue(name, out var child) || child is null)
                {
                    continue;
                }

                ValidateNode(propSchema, child, $"{path}.{name}", errors);
            }
        }

        if (instance is JsonArray arr && schema["items"] is JsonNode itemsSchema)
        {
            for (var i = 0; i < arr.Count; i++)
            {
                ValidateNode(itemsSchema, arr[i], $"{path}[{i}]", errors);
            }
        }
    }

    private static bool MatchesType(string typeName, JsonNode? instance, string path, List<string> errors)
    {
        var ok = typeName switch
        {
            "object" => instance is JsonObject,
            "array" => instance is JsonArray,
            "string" => instance is JsonValue v && v.TryGetValue<string>(out _),
            "boolean" => instance is JsonValue v && v.TryGetValue<bool>(out _),
            "integer" => instance is JsonValue v && IsInteger(v),
            "number" => instance is JsonValue v && IsNumber(v),
            "null" => instance is null,
            _ => true,
        };

        if (!ok)
        {
            errors.Add($"{path}: expected type '{typeName}'.");
        }

        return ok;
    }

    private static bool IsNumber(JsonValue value) =>
        value.TryGetValue<double>(out _)
        || value.TryGetValue<decimal>(out _)
        || value.TryGetValue<long>(out _)
        || value.TryGetValue<int>(out _);

    private static bool IsInteger(JsonValue value)
    {
        if (value.TryGetValue<long>(out _) || value.TryGetValue<int>(out _))
        {
            return true;
        }

        if (value.TryGetValue<double>(out var d))
        {
            return Math.Abs(d - Math.Truncate(d)) < double.Epsilon;
        }

        if (value.TryGetValue<string>(out var s)
            && decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var dec))
        {
            return dec == decimal.Truncate(dec);
        }

        return false;
    }
}

public sealed record SchemaValidationResult(bool IsValid, IReadOnlyList<string> Errors);
