using System.Text;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Documate.Api.Infrastructure.Configuration;

/// <summary>
/// Loads one JSON secret from AWS Secrets Manager into <see cref="IConfiguration"/> at process startup (Plan 17).
/// Not per HTTP request / Hangfire job. Infisical is the documented backup platform — not wired here.
/// </summary>
public static class SecretsManagerConfigurationExtensions
{
    public static void AddDocumateSecretsManager(
        this ConfigurationManager configuration,
        IHostEnvironment? environment = null,
        ILogger? logger = null)
    {
        if (environment?.IsEnvironment("Testing") == true)
        {
            logger?.LogInformation("Testing environment — skipping AWS Secrets Manager load.");
            return;
        }

        var section = configuration.GetSection(SecretsManagerOptions.SectionName);
        var enabled = section.GetValue("Enabled", false);
        var secretId = section["SecretId"];

        if (!enabled)
        {
            logger?.LogWarning(
                "SecretsManager:Enabled=false — platform secrets will be missing unless provided via env. " +
                "Plan 17 requires AWS Secrets Manager for all hosts including local.");
            return;
        }

        if (string.IsNullOrWhiteSpace(secretId))
        {
            throw new InvalidOperationException(
                "SecretsManager:Enabled is true but SecretsManager:SecretId is missing. " +
                "Set SecretId (e.g. documate/dev/api) or disable SecretsManager.");
        }

        var regionName = section["Region"]
            ?? configuration["Aws:Region"]
            ?? configuration["Storage:Region"]
            ?? "us-east-1";

        logger?.LogInformation(
            "Loading platform secrets from AWS Secrets Manager secret {SecretId} in {Region} (once at startup).",
            secretId,
            regionName);

        try
        {
            using var client = CreateClient(regionName);
            var response = client.GetSecretValueAsync(new GetSecretValueRequest
            {
                SecretId = secretId,
            }).GetAwaiter().GetResult();

            var json = response.SecretString;
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException(
                    $"AWS Secrets Manager secret '{secretId}' has an empty SecretString (binary secrets are not supported).");
            }

            // Strip a leading "_comment" is fine — System.Text.Json / config binder ignore unknown keys.
            var bytes = Encoding.UTF8.GetBytes(json);
            configuration.AddJsonStream(new MemoryStream(bytes));

            logger?.LogInformation("AWS Secrets Manager secret {SecretId} merged into configuration.", secretId);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Failed to load AWS Secrets Manager secret '{secretId}' in region '{regionName}'. " +
                "Ensure IAM allows secretsmanager:GetSecretValue (prefer instance/task role) and the secret JSON matches appsettings shape. " +
                "See docs/plans/17-platform-secrets-store.md.",
                ex);
        }
    }

    private static AmazonSecretsManagerClient CreateClient(string regionName)
    {
        var region = RegionEndpoint.GetBySystemName(regionName);
        // Default credential chain: environment, shared profile, then IAM role on the host/task.
        return new AmazonSecretsManagerClient(region);
    }
}
