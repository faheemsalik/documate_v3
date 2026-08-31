namespace Documate.Api.Infrastructure.Extract;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Storage;
using Microsoft.Extensions.Options;

/// <summary>
/// Live LLM extract (DQ-0704). Resolves Agent/template LLM provider from <see cref="LlmOptions"/>.
/// Retries once on failure. Returns concrete provider key for WorkEvents (Document façade stays documate_meta).
/// </summary>
public sealed class LiveLlmDocumentExtractAdapter(
    IObjectStorage storage,
    IHttpClientFactory httpClientFactory,
    IOptions<LlmOptions> llmOptions,
    ILogger<LiveLlmDocumentExtractAdapter> logger) : IDocumentExtractAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public async Task<ExtractAdapterResult> ExtractAsync(
        ExtractAdapterRequest request,
        CancellationToken cancellationToken = default)
    {
        var text = request.SourceText;
        if (string.IsNullOrWhiteSpace(text))
        {
            text = await ReadTextArtifactAsync(request, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("No source text available for extract.");
        }

        var (providerKey, provider) = ResolveProvider(request.PreferredLlmProviderKey);
        Exception? last = null;
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                var payload = await CallLlmAsync(providerKey, provider, request, text, cancellationToken);
                var json = payload.ToJsonString(JsonOptions);
                logger.LogInformation(
                    "Extracted Document {DocumentId} via {ProviderKey} (attempt={Attempt}); fields={FieldCount}",
                    request.DocumentId,
                    providerKey,
                    attempt,
                    payload.Count);
                return new ExtractAdapterResult(providerKey, payload, json);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                last = ex;
                logger.LogWarning(
                    ex,
                    "LLM extract attempt {Attempt} failed for Document {DocumentId} via {ProviderKey}",
                    attempt,
                    request.DocumentId,
                    providerKey);
            }
        }

        throw new InvalidOperationException(
            $"LLM extract failed after retry via {providerKey}: {last?.Message}",
            last);
    }

    private (string Key, LlmProviderOptions Options) ResolveProvider(string? preferredKey)
    {
        var llm = llmOptions.Value;
        var key = !string.IsNullOrWhiteSpace(preferredKey)
            ? preferredKey!.Trim()
            : llm.DefaultProviderKey;

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("No LLM provider key configured (Llm:DefaultProviderKey).");
        }

        if (!llm.Providers.TryGetValue(key, out var provider))
        {
            provider = llm.Providers
                .FirstOrDefault(kv => string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                .Value;
            if (provider is not null)
            {
                key = llm.Providers.Keys.First(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (provider is null
            || string.IsNullOrWhiteSpace(provider.ApiKey))
        {
            throw new InvalidOperationException(
                $"LLM provider '{key}' is missing ApiKey in Llm:Providers (required in all environments).");
        }

        if (string.IsNullOrWhiteSpace(provider.Model))
        {
            throw new InvalidOperationException($"LLM provider '{key}' is missing Model in Llm:Providers.");
        }

        return (key, provider);
    }

    private async Task<JsonObject> CallLlmAsync(
        string providerKey,
        LlmProviderOptions provider,
        ExtractAdapterRequest request,
        string sourceText,
        CancellationToken cancellationToken)
    {
        if (providerKey.Contains("claude", StringComparison.OrdinalIgnoreCase)
            || (provider.BaseUrl ?? "").Contains("anthropic", StringComparison.OrdinalIgnoreCase))
        {
            return await CallAnthropicAsync(provider, request, sourceText, cancellationToken);
        }

        return await CallOpenAiCompatibleAsync(provider, request, sourceText, cancellationToken);
    }

    private async Task<JsonObject> CallOpenAiCompatibleAsync(
        LlmProviderOptions provider,
        ExtractAdapterRequest request,
        string sourceText,
        CancellationToken cancellationToken)
    {
        var baseUrl = string.IsNullOrWhiteSpace(provider.BaseUrl)
            ? "https://api.openai.com/v1"
            : provider.BaseUrl!.TrimEnd('/');
        if (!baseUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            && !baseUrl.Contains("/v1/", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = baseUrl.TrimEnd('/') + "/v1";
        }

        var body = new
        {
            model = provider.Model,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content =
                        "You extract structured data from documents. Return ONLY a JSON object matching the schema. No markdown.",
                },
                new
                {
                    role = "user",
                    content = BuildUserPrompt(request, sourceText),
                },
            },
        };

        using var http = httpClientFactory.CreateClient("documate-llm");
        using var msg = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);
        msg.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await http.SendAsync(msg, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenAI-compatible LLM HTTP {(int)response.StatusCode}: {Trim(raw, 500)}");
        }

        using var doc = JsonDocument.Parse(raw);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return ParseJsonObject(content);
    }

    private async Task<JsonObject> CallAnthropicAsync(
        LlmProviderOptions provider,
        ExtractAdapterRequest request,
        string sourceText,
        CancellationToken cancellationToken)
    {
        var baseUrl = string.IsNullOrWhiteSpace(provider.BaseUrl)
            ? "https://api.anthropic.com"
            : provider.BaseUrl!.TrimEnd('/');

        var body = new
        {
            model = provider.Model,
            max_tokens = 4096,
            temperature = 0,
            system =
                "You extract structured data from documents. Return ONLY a JSON object matching the schema. No markdown.",
            messages = new object[]
            {
                new { role = "user", content = BuildUserPrompt(request, sourceText) },
            },
        };

        using var http = httpClientFactory.CreateClient("documate-llm");
        using var msg = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/messages");
        msg.Headers.TryAddWithoutValidation("x-api-key", provider.ApiKey);
        msg.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
        msg.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await http.SendAsync(msg, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Anthropic LLM HTTP {(int)response.StatusCode}: {Trim(raw, 500)}");
        }

        using var doc = JsonDocument.Parse(raw);
        var contentBlocks = doc.RootElement.GetProperty("content");
        string? text = null;
        foreach (var block in contentBlocks.EnumerateArray())
        {
            if (block.TryGetProperty("type", out var type)
                && type.GetString() == "text"
                && block.TryGetProperty("text", out var t))
            {
                text = t.GetString();
                break;
            }
        }

        return ParseJsonObject(text);
    }

    private static string BuildUserPrompt(ExtractAdapterRequest request, string sourceText) =>
        $"""
        Agent instructions:
        {request.Instructions}

        Output JSON Schema:
        {request.OutputSchemaJson}

        Document text (full OCR / normalize text):
        {sourceText}
        """;

    private static JsonObject ParseJsonObject(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("LLM returned empty content.");
        }

        var trimmed = content.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNl = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNl > 0 && lastFence > firstNl)
            {
                trimmed = trimmed[(firstNl + 1)..lastFence].Trim();
            }
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(trimmed);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"LLM did not return valid JSON: {ex.Message}");
        }

        return node as JsonObject
            ?? throw new InvalidOperationException("LLM JSON root must be an object.");
    }

    private async Task<string> ReadTextArtifactAsync(ExtractAdapterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.StorageBucket) || string.IsNullOrWhiteSpace(request.TextArtifactKey))
        {
            return "";
        }

        await using var stream = await storage.DownloadAsync(
            request.StorageBucket,
            request.TextArtifactKey,
            cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static string Trim(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
