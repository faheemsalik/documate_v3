namespace Documate.Api.Infrastructure.Settings;

/// <summary>Allowlisted setting keys (Plan 15). Secrets are never stored here.</summary>
public static class SystemSettingKeys
{
    public const string EmailDefaultDomain = "EmailIntake:DefaultDomain";
    public const string EmailMaxAttachmentBytes = "EmailIntake:MaxAttachmentBytes";
    public const string EmailMaxTotalAttachmentBytes = "EmailIntake:MaxTotalAttachmentBytes";
    public const string EmailMaxAttachments = "EmailIntake:MaxAttachments";
    public const string EmailAllowedExtensions = "EmailIntake:AllowedExtensions";
    public const string EmailRateLimitPerMailboxPerMinute = "EmailIntake:RateLimitPerMailboxPerMinute";
    public const string EmailRateLimitPerMailboxPerHour = "EmailIntake:RateLimitPerMailboxPerHour";
    public const string EmailS3Bucket = "EmailIntake:S3Bucket";
    public const string EmailS3Prefix = "EmailIntake:S3Prefix";
    public const string EmailAwsRegion = "EmailIntake:AwsRegion";
    public const string EmailMimeRetentionDays = "EmailIntake:MimeRetentionDays";
    public const string EmailBodyExcerptMaxChars = "EmailIntake:BodyExcerptMaxChars";

    public const string PipelineSyncWaitTimeoutSeconds = "Pipeline:SyncWaitTimeoutSeconds";
    public const string PipelineSyncMaxPages = "Pipeline:SyncMaxPages";
    public const string PipelineSyncMaxBytes = "Pipeline:SyncMaxBytes";
    public const string PipelineIntelligenceT1ProviderKey = "Pipeline:IntelligenceT1ProviderKey";
    public const string PipelineIntelligenceFallbackProviderKey = "Pipeline:IntelligenceFallbackProviderKey";
    public const string PipelineExtractProviderKey = "Pipeline:ExtractProviderKey";
    public const string PipelineIntelligenceMaxCallsPerFile = "Pipeline:IntelligenceMaxCallsPerFile";

    public static IReadOnlyList<string> All { get; } =
    [
        EmailDefaultDomain,
        EmailMaxAttachmentBytes,
        EmailMaxTotalAttachmentBytes,
        EmailMaxAttachments,
        EmailAllowedExtensions,
        EmailRateLimitPerMailboxPerMinute,
        EmailRateLimitPerMailboxPerHour,
        EmailS3Bucket,
        EmailS3Prefix,
        EmailAwsRegion,
        EmailMimeRetentionDays,
        EmailBodyExcerptMaxChars,
        PipelineSyncWaitTimeoutSeconds,
        PipelineSyncMaxPages,
        PipelineSyncMaxBytes,
        PipelineIntelligenceT1ProviderKey,
        PipelineIntelligenceFallbackProviderKey,
        PipelineExtractProviderKey,
        PipelineIntelligenceMaxCallsPerFile,
    ];

    public static bool IsAllowlisted(string key) =>
        All.Contains(key, StringComparer.OrdinalIgnoreCase);
}
