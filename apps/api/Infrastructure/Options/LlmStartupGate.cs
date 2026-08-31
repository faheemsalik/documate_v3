namespace Documate.Api.Infrastructure.Options;

/// <summary>Fails host startup when the default LLM provider ApiKey is missing (all envs except Testing).</summary>
public static class LlmStartupGate
{
    public static void EnsureConfigured(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsEnvironment("Testing"))
        {
            return;
        }

        var llm = configuration.GetSection(LlmOptions.SectionName).Get<LlmOptions>() ?? new LlmOptions();
        var key = string.IsNullOrWhiteSpace(llm.DefaultProviderKey) ? "gpt_5_6" : llm.DefaultProviderKey.Trim();
        if (!llm.Providers.TryGetValue(key, out var provider)
            || string.IsNullOrWhiteSpace(provider.ApiKey)
            || string.IsNullOrWhiteSpace(provider.Model))
        {
            throw new InvalidOperationException(
                $"Llm:Providers:{key} must set ApiKey and Model in all environments (user-secrets or env). "
                + "Example: dotnet user-secrets set \"Llm:Providers:gpt_5_6:ApiKey\" \"...\"");
        }
    }
}
