namespace Documate.Api.Infrastructure.Intelligence;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Settings;
using Documate.Api.Infrastructure.Storage;
using Microsoft.Extensions.Options;

public sealed partial class PageIntelligenceService(
    IObjectStorage storage,
    IHttpClientFactory httpClientFactory,
    IOptions<LlmOptions> llmOptions,
    IPipelineModelSettings modelSettings,
    ILogger<PageIntelligenceService> logger) : IPageIntelligenceService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<IReadOnlyList<PageIntelligenceProfile>> AnalyzeAsync(
        PageIntelligenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var settings = modelSettings.Current;
        var calls = 0;
        var profiles = new List<PageIntelligenceProfile>(request.PageArtifacts.Count);

        foreach (var artifact in request.PageArtifacts.OrderBy(x => x.Page))
        {
            var text = await ReadTextAsync(request.StorageBucket, artifact.TextArtifactKey, cancellationToken);
            PageIntelligenceProfile? profile = null;

            if (calls < settings.IntelligenceMaxCallsPerFile)
            {
                calls++;
                profile = await TryAnalyzeAsync(
                    artifact.Page,
                    text,
                    settings.IntelligenceT1ProviderKey,
                    "t1",
                    cancellationToken);
            }

            if (profile is null && calls < settings.IntelligenceMaxCallsPerFile)
            {
                calls++;
                profile = await TryAnalyzeAsync(
                    artifact.Page,
                    text,
                    settings.IntelligenceFallbackProviderKey,
                    "fallback",
                    cancellationToken);
            }

            profile ??= BuildHeuristic(artifact.Page, text, artifact.IsBlank);
            profiles.Add(profile);
            await PersistAsync(request, profile, cancellationToken);
        }

        return profiles;
    }

    private async Task<PageIntelligenceProfile?> TryAnalyzeAsync(
        int page,
        string text,
        string providerKey,
        string tier,
        CancellationToken cancellationToken)
    {
        try
        {
            var provider = ResolveProvider(providerKey);
            var json = providerKey.Contains("claude", StringComparison.OrdinalIgnoreCase)
                || (provider.BaseUrl ?? "").Contains("anthropic", StringComparison.OrdinalIgnoreCase)
                ? await CallAnthropicAsync(provider, page, text, cancellationToken)
                : await CallOpenAiAsync(provider, page, text, cancellationToken);
            return ParseProfile(page, tier, json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Page intelligence failed page={Page} provider={Provider}", page, providerKey);
            return null;
        }
    }

    private LlmProviderOptions ResolveProvider(string key)
    {
        var providers = llmOptions.Value.Providers;
        var pair = providers.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
        var provider = pair.Value;
        if (provider is null || string.IsNullOrWhiteSpace(provider.ApiKey) || string.IsNullOrWhiteSpace(provider.Model))
        {
            throw new InvalidOperationException($"LLM provider '{key}' is not fully configured.");
        }

        return provider;
    }

    private async Task<JsonObject> CallOpenAiAsync(
        LlmProviderOptions provider,
        int page,
        string text,
        CancellationToken cancellationToken)
    {
        var baseUrl = string.IsNullOrWhiteSpace(provider.BaseUrl)
            ? "https://api.openai.com/v1"
            : provider.BaseUrl.TrimEnd('/');
        if (!baseUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            && !baseUrl.Contains("/v1/", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl += "/v1";
        }

        var body = new
        {
            model = provider.Model,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = $"Page number: {page}\n\nOCR text:\n{text}" },
            },
        };
        using var msg = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);
        msg.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var response = await httpClientFactory.CreateClient("documate-llm").SendAsync(msg, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();
        using var parsed = JsonDocument.Parse(raw);
        return ParseObject(parsed.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString());
    }

    private async Task<JsonObject> CallAnthropicAsync(
        LlmProviderOptions provider,
        int page,
        string text,
        CancellationToken cancellationToken)
    {
        var baseUrl = string.IsNullOrWhiteSpace(provider.BaseUrl)
            ? "https://api.anthropic.com"
            : provider.BaseUrl.TrimEnd('/');
        var body = new
        {
            model = provider.Model,
            max_tokens = 1024,
            temperature = 0,
            system = SystemPrompt,
            messages = new[] { new { role = "user", content = $"Page number: {page}\n\nOCR text:\n{text}" } },
        };
        using var msg = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/messages");
        msg.Headers.TryAddWithoutValidation("x-api-key", provider.ApiKey);
        msg.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
        msg.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var response = await httpClientFactory.CreateClient("documate-llm").SendAsync(msg, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();
        using var parsed = JsonDocument.Parse(raw);
        return ParseObject(parsed.RootElement.GetProperty("content")[0].GetProperty("text").GetString());
    }

    private static PageIntelligenceProfile ParseProfile(int page, string tier, JsonObject json)
    {
        var evidence = json["evidence"] is JsonArray array
            ? array.Select(x => x?.GetValue<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToArray()
            : [];
        return new PageIntelligenceProfile(
            page,
            json["documentType"]?.GetValue<string?>(),
            json["primaryDocumentNumber"]?.GetValue<string?>(),
            json["startsNewDocument"]?.GetValue<bool>() ?? false,
            json["continuesPrevious"]?.GetValue<bool>() ?? false,
            json["documentComplete"]?.GetValue<bool>() ?? false,
            json["isBlank"]?.GetValue<bool>() ?? false,
            tier,
            evidence);
    }

    private static PageIntelligenceProfile BuildHeuristic(int page, string text, bool normalizeBlank)
    {
        var blank = normalizeBlank || string.IsNullOrWhiteSpace(text);
        var number = DocumentNumberRegex().Match(text).Value;
        return new PageIntelligenceProfile(
            page,
            null,
            string.IsNullOrWhiteSpace(number) ? null : number.ToUpperInvariant(),
            !blank && page == 1,
            !blank && page > 1,
            false,
            blank,
            "heuristic",
            blank ? ["blank_page"] : string.IsNullOrWhiteSpace(number) ? ["continuation_default"] : [$"number:{number}"]);
    }

    private async Task<string> ReadTextAsync(string bucket, string key, CancellationToken cancellationToken)
    {
        await using var stream = await storage.DownloadAsync(bucket, key, cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8, true, leaveOpen: false);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private async Task PersistAsync(
        PageIntelligenceRequest request,
        PageIntelligenceProfile profile,
        CancellationToken cancellationToken)
    {
        var key = storage.BuildArtifactKey(request.FileStorageKey, $"intelligence.page.{profile.Page}.json");
        await using var stream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(profile, JsonOptions));
        await storage.UploadAsync(
            new ObjectStoragePutRequest(
                request.StorageBucket,
                key,
                stream,
                "application/json",
                new Dictionary<string, string>
                {
                    ["FileId"] = request.FileId.ToString(),
                    ["Artifact"] = "page.intelligence",
                    ["Page"] = profile.Page.ToString(),
                }),
            cancellationToken);
    }

    private static JsonObject ParseObject(string? value)
    {
        var text = value?.Trim() ?? "";
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var first = text.IndexOf('\n');
            var last = text.LastIndexOf("```", StringComparison.Ordinal);
            if (first >= 0 && last > first)
            {
                text = text[(first + 1)..last].Trim();
            }
        }

        return JsonNode.Parse(text) as JsonObject
            ?? throw new InvalidOperationException("Page intelligence response must be a JSON object.");
    }

    private const string SystemPrompt =
        """
        Identify page-level document boundaries. Return only JSON with:
        documentType (string|null), primaryDocumentNumber (string|null),
        startsNewDocument, continuesPrevious, documentComplete, isBlank (booleans),
        evidence (short string array). Identification only: never return extracted document data.
        """;

    [GeneratedRegex(@"\b(?:INV|DN|CN)-[A-Z0-9][A-Z0-9\-]*\b", RegexOptions.IgnoreCase)]
    private static partial Regex DocumentNumberRegex();
}
