namespace Documate.Api.Infrastructure.Settings;

/// <summary>Allowlisted setting keys (Plan 15 / 17). Secrets are never stored here.</summary>
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
    public const string PipelineMaxConcurrentFiles = "Pipeline:MaxConcurrentFiles";
    public const string PipelineMaxConcurrentWebhooks = "Pipeline:MaxConcurrentWebhooks";
    public const string PipelineStubStageDelayMs = "Pipeline:StubStageDelayMs";
    public const string PipelineIntelligenceT1ProviderKey = "Pipeline:IntelligenceT1ProviderKey";
    public const string PipelineIntelligenceFallbackProviderKey = "Pipeline:IntelligenceFallbackProviderKey";
    public const string PipelineExtractProviderKey = "Pipeline:ExtractProviderKey";
    public const string PipelineIntelligenceMaxCallsPerFile = "Pipeline:IntelligenceMaxCallsPerFile";

    public const string StorageProvider = "Storage:Provider";
    public const string StorageBucketOrContainer = "Storage:BucketOrContainer";
    public const string StorageLocalRootPath = "Storage:LocalRootPath";
    public const string StorageRegion = "Storage:Region";
    public const string StorageServiceUrl = "Storage:ServiceUrl";
    public const string StorageSignedUrlMinutes = "Storage:SignedUrlMinutes";

    public const string OcrPrimaryProviderKey = "Ocr:PrimaryProviderKey";
    public const string OcrSecondaryProviderKey = "Ocr:SecondaryProviderKey";
    public const string OcrSyncMaxPages = "Ocr:SyncMaxPages";
    public const string OcrTextractRegion = "Ocr:Textract:Region";
    public const string OcrGoogleLocation = "Ocr:GoogleDocumentAi:Location";
    public const string OcrGoogleProjectId = "Ocr:GoogleDocumentAi:ProjectId";
    public const string OcrGoogleProcessorId = "Ocr:GoogleDocumentAi:ProcessorId";

    public const string NotificationsEnabled = "Notifications:Enabled";
    public const string NotificationsToAddress = "Notifications:ToAddress";
    public const string NotificationsSmtpHost = "Notifications:Smtp:Host";
    public const string NotificationsSmtpPort = "Notifications:Smtp:Port";
    public const string NotificationsSmtpUser = "Notifications:Smtp:User";
    public const string NotificationsSmtpFrom = "Notifications:Smtp:From";

    public const string AdminHangfireDashboardUrl = "Admin:HangfireDashboardUrl";
    public const string AdminDatadogDashboardUrl = "Admin:DatadogDashboardUrl";

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
        PipelineMaxConcurrentFiles,
        PipelineMaxConcurrentWebhooks,
        PipelineStubStageDelayMs,
        PipelineIntelligenceT1ProviderKey,
        PipelineIntelligenceFallbackProviderKey,
        PipelineExtractProviderKey,
        PipelineIntelligenceMaxCallsPerFile,
        StorageProvider,
        StorageBucketOrContainer,
        StorageLocalRootPath,
        StorageRegion,
        StorageServiceUrl,
        StorageSignedUrlMinutes,
        OcrPrimaryProviderKey,
        OcrSecondaryProviderKey,
        OcrSyncMaxPages,
        OcrTextractRegion,
        OcrGoogleLocation,
        OcrGoogleProjectId,
        OcrGoogleProcessorId,
        NotificationsEnabled,
        NotificationsToAddress,
        NotificationsSmtpHost,
        NotificationsSmtpPort,
        NotificationsSmtpUser,
        NotificationsSmtpFrom,
        AdminHangfireDashboardUrl,
        AdminDatadogDashboardUrl,
    ];

    public static bool IsAllowlisted(string key) =>
        All.Contains(key, StringComparer.OrdinalIgnoreCase);
}
