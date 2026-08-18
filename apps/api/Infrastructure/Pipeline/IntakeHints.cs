namespace Documate.Api.Infrastructure.Pipeline;

using System.Text.Json;

/// <summary>Optional caller-supplied File hints. A document type skips split/classify (P0/C0).</summary>
public sealed record IntakeHints(string? DocumentTypeKey, int? DocumentCount)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public bool HasPredeterminedType => !string.IsNullOrWhiteSpace(DocumentTypeKey);

    public int EffectiveDocumentCount =>
        HasPredeterminedType ? Math.Max(1, DocumentCount ?? 1) : 1;

    public static IntakeHints Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new IntakeHints(null, null);
        }

        try
        {
            var dto = JsonSerializer.Deserialize<IntakeHintsDto>(json, JsonOptions);
            var key = string.IsNullOrWhiteSpace(dto?.DocumentTypeKey) ? null : dto.DocumentTypeKey.Trim();
            var count = dto?.DocumentCount is int n && n > 0 ? n : (int?)null;
            return new IntakeHints(key, count);
        }
        catch (JsonException)
        {
            return new IntakeHints(null, null);
        }
    }

    public static string? Serialize(string? documentTypeKey, int? documentCount)
    {
        var key = string.IsNullOrWhiteSpace(documentTypeKey) ? null : documentTypeKey.Trim();
        if (key is null && documentCount is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(new IntakeHintsDto(key, documentCount is int n && n > 0 ? n : null));
    }

    private sealed record IntakeHintsDto(string? DocumentTypeKey, int? DocumentCount);
}
