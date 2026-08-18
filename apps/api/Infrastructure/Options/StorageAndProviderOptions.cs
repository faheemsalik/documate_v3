namespace Documate.Api.Infrastructure.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>local | s3 — Phase 1 continues old_code AWS S3 path when s3.</summary>
    public string Provider { get; set; } = "local";

    public string? BucketOrContainer { get; set; }
    public string? LocalRootPath { get; set; }

    /// <summary>AWS region e.g. us-west-2 (old_code defaulted USWest2 in places).</summary>
    public string? Region { get; set; } = "us-west-2";

    /// <summary>Optional custom endpoint (LocalStack / MinIO).</summary>
    public string? ServiceUrl { get; set; }

    public int SignedUrlMinutes { get; set; } = 30;

    /// <summary>Secret via env/user-secrets — never commit real values. Prefer IAM role in AWS.</summary>
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
}

public sealed class ProviderCredentialsOptions
{
    public const string SectionName = "Providers";

    public string? DocumateMetaApiKey { get; set; }
    public string? DefaultLlmApiKey { get; set; }
    public string? DefaultOcrApiKey { get; set; }
}
