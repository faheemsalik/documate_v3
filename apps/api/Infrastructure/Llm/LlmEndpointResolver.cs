namespace Documate.Api.Infrastructure.Llm;

/// <summary>Resolves chat-completions base URL and Anthropic vs OpenAI-compatible routing.</summary>
public static class LlmEndpointResolver
{
    public static bool IsAnthropic(string providerKey, string? baseUrl) =>
        providerKey.Contains("claude", StringComparison.OrdinalIgnoreCase)
        || (baseUrl ?? "").Contains("anthropic", StringComparison.OrdinalIgnoreCase);

    /// <summary>Base URL without trailing slash; caller appends /chat/completions.</summary>
    public static string ResolveOpenAiCompatibleBase(string providerKey, string? configuredBaseUrl)
    {
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            return configuredBaseUrl.Trim().TrimEnd('/');
        }

        var key = providerKey.Trim();
        if (key.Contains("gemini", StringComparison.OrdinalIgnoreCase))
        {
            // Google OpenAI-compatible endpoint
            return "https://generativelanguage.googleapis.com/v1beta/openai";
        }

        if (key.StartsWith("deepseek", StringComparison.OrdinalIgnoreCase))
        {
            return "https://api.deepseek.com/v1";
        }

        if (key.StartsWith("glm", StringComparison.OrdinalIgnoreCase))
        {
            return "https://open.bigmodel.cn/api/paas/v4";
        }

        if (key.Contains("kimi", StringComparison.OrdinalIgnoreCase)
            || key.Contains("moonshot", StringComparison.OrdinalIgnoreCase))
        {
            return "https://api.moonshot.cn/v1";
        }

        return "https://api.openai.com/v1";
    }

    public static string ResolveAnthropicBase(string? configuredBaseUrl) =>
        string.IsNullOrWhiteSpace(configuredBaseUrl)
            ? "https://api.anthropic.com"
            : configuredBaseUrl.Trim().TrimEnd('/');
}
