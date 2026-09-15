namespace Documate.Api.Infrastructure.Configuration;

/// <summary>Optional AWS Secrets Manager bootstrap (Plan 17). Disabled unless Enabled + SecretId.</summary>
public sealed class SecretsManagerOptions
{
    public const string SectionName = "SecretsManager";

    /// <summary>When true and <see cref="SecretId"/> is set, load the JSON secret once at process startup.</summary>
    public bool Enabled { get; set; }

    /// <summary>Secret name or ARN, e.g. documate/dev/api.</summary>
    public string? SecretId { get; set; }

    /// <summary>AWS region for the Secrets Manager API. Falls back to Aws:Region then us-east-1.</summary>
    public string? Region { get; set; }
}
