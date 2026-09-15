namespace Documate.Api.Infrastructure.Intelligence;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Documate.Api.Infrastructure.Llm;
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
                    request.RoutableTypes,
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
                    request.RoutableTypes,
                    cancellationToken);
            }

            profile ??= BuildHeuristic(artifact.Page, text, artifact.IsBlank);
            profile = NormalizeRoutableType(profile, request.RoutableTypes);
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
        IReadOnlyList<QueueRoutableDocumentType> routableTypes,
        CancellationToken cancellationToken)
    {
        try
        {
            var provider = ResolveProvider(providerKey);
            var systemPrompt = BuildSystemPrompt(routableTypes);
            var json = LlmEndpointResolver.IsAnthropic(providerKey, provider.BaseUrl)
                ? await CallAnthropicAsync(provider, page, text, systemPrompt, cancellationToken)
                : await CallOpenAiAsync(providerKey, provider, page, text, systemPrompt, cancellationToken);
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
        string providerKey,
        LlmProviderOptions provider,
        int page,
        string text,
        string systemPrompt,
        CancellationToken cancellationToken)
    {
        var baseUrl = LlmEndpointResolver.ResolveOpenAiCompatibleBase(providerKey, provider.BaseUrl);

        var body = new
        {
            model = provider.Model,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
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
        string systemPrompt,
        CancellationToken cancellationToken)
    {
        var baseUrl = LlmEndpointResolver.ResolveAnthropicBase(provider.BaseUrl);
        var body = new
        {
            model = provider.Model,
            max_tokens = 1024,
            temperature = 0,
            system = systemPrompt,
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

    private static PageIntelligenceProfile NormalizeRoutableType(
        PageIntelligenceProfile profile,
        IReadOnlyList<QueueRoutableDocumentType> routableTypes)
    {
        if (routableTypes.Count == 0 || string.IsNullOrWhiteSpace(profile.DocumentType))
        {
            return profile;
        }

        var key = QueueDocumentTypeResolver.ResolveKey(profile.DocumentType, routableTypes);
        return key is null || string.Equals(key, profile.DocumentType, StringComparison.OrdinalIgnoreCase)
            ? profile
            : profile with { DocumentType = key };
    }

    private static string BuildSystemPrompt(IReadOnlyList<QueueRoutableDocumentType> routableTypes)
    {
        if (routableTypes.Count == 0)
        {
            return SystemPromptBase;
        }

        var expected = string.Join(
            ", ",
            routableTypes.Select(t => $"{t.Name} (`{t.DocumentTypeKey}`)"));
        return
            $"""
            {SystemPromptBase}
            This queue commonly receives: {expected}.
            Prefer those labels when the page matches one of them, but still identify the real
            document type from the page content if it is something else. Do not invent fields
            beyond the JSON schema above.
            """;
    }

    private const string SystemPromptBase =
        """
        Identify page-level document boundaries and what kind of business document this page is.
        Infer documentType yourself from the page content (natural label or catalog key).
        Use null only when the page is blank or the type cannot be judged.
        Return only JSON with:
        documentType (string|null), primaryDocumentNumber (string|null),
        startsNewDocument, continuesPrevious, documentComplete, isBlank (booleans),
        evidence (short string array). Identification only: never return extracted document data.
        """;

    [GeneratedRegex(@"\b(?:INV|DN|CN)-[A-Z0-9][A-Z0-9\-]*\b", RegexOptions.IgnoreCase)]
    private static partial Regex DocumentNumberRegex();
}
