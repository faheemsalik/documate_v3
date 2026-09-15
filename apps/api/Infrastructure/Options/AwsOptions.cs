namespace Documate.Api.Infrastructure.Options;

using Amazon.Runtime;

/// <summary>
/// Shared AWS account credentials for Textract, S3 storage, SES intake, etc.
/// Prefer IAM role / env in AWS; set AccessKey/SecretKey for IIS and local hosts.
/// Feature sections keep their own Region / bucket settings.
/// </summary>
public sealed class AwsOptions
{
    public const string SectionName = "Aws";

    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }

    public bool HasExplicitCredentials =>
        !string.IsNullOrWhiteSpace(AccessKey) && !string.IsNullOrWhiteSpace(SecretKey);

    public BasicAWSCredentials? TryCreateCredentials() =>
        HasExplicitCredentials ? new BasicAWSCredentials(AccessKey, SecretKey) : null;
}
