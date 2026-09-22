using System.Text;
using Amazon;
using Amazon.Runtime;
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
            using var client = CreateClient(configuration, regionName, logger);
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
            //
            // Important: do NOT AddJsonStream(MemoryStream) onto ConfigurationManager directly.
            // Host Build / later source changes reload all providers; a consumed MemoryStream then
            // yields an empty provider and Auth:AdminGate:Password (etc.) silently disappear while
            // the process keeps running. Flatten once into in-memory keys (reload-safe).
            var bytes = Encoding.UTF8.GetBytes(json);
            using (var stream = new MemoryStream(bytes))
            {
                var secretConfig = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();

                var flat = secretConfig
                    .AsEnumerable()
                    .Where(pair => pair.Value is not null)
                    .Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value))
                    .ToArray();

                // Stamp Auth:* into process env BEFORE AddInMemoryCollection so the env provider
                // reload (triggered by adding the in-memory source) picks up AdminGate too.
                // IIS often already has InterimFeGate via env (customer app works) but not AdminGate.
                PublishAuthKeysToProcessEnvironment(flat);
                configuration.AddInMemoryCollection(flat);
            }

            logger?.LogInformation(
                "AWS Secrets Manager secret {SecretId} merged into configuration. AdminGate: PasswordSet={PasswordSet}, AccessTokenSet={AccessTokenSet}, Username={Username}.",
                secretId,
                !string.IsNullOrWhiteSpace(configuration["Auth:AdminGate:Password"]),
                !string.IsNullOrWhiteSpace(configuration["Auth:AdminGate:AccessToken"]),
                configuration["Auth:AdminGate:Username"] ?? "(unset)");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Failed to load AWS Secrets Manager secret '{secretId}' in region '{regionName}'. " +
                "On IIS/Windows set bootstrap Aws:AccessKey and Aws:SecretKey in appsettings (or AWS_ACCESS_KEY_ID / AWS_SECRET_ACCESS_KEY env). " +
                "Prefer an IAM instance role when hosted in AWS. See docs/plans/17-platform-secrets-store.md.",
                ex);
        }
    }

    private static void PublishAuthKeysToProcessEnvironment(IReadOnlyList<KeyValuePair<string, string?>> flat)
    {
        foreach (var (key, value) in flat)
        {
            if (value is null
                || !key.StartsWith("Auth:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var envKey = key.Replace(":", "__", StringComparison.Ordinal);
            Environment.SetEnvironmentVariable(envKey, value);
        }
    }

    /// <summary>
    /// Bootstrap credentials for the first SM call. Keys inside the secret cannot be used yet (chicken/egg).
    /// Order: explicit <c>Aws:AccessKey</c>/<c>SecretKey</c> from appsettings/env → default AWS credential chain.
    /// </summary>
    private static AmazonSecretsManagerClient CreateClient(
        IConfiguration configuration,
        string regionName,
        ILogger? logger)
    {
        var region = RegionEndpoint.GetBySystemName(regionName);
        var accessKey = configuration["Aws:AccessKey"];
        var secretKey = configuration["Aws:SecretKey"];
        if (!string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey))
        {
            logger?.LogInformation("Secrets Manager client using explicit Aws:AccessKey/SecretKey bootstrap credentials.");
            return new AmazonSecretsManagerClient(new BasicAWSCredentials(accessKey, secretKey), region);
        }

        logger?.LogInformation("Secrets Manager client using default AWS credential chain (env / profile / IAM role).");
        return new AmazonSecretsManagerClient(region);
    }
}
