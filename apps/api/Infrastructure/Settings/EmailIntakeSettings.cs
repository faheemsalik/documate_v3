namespace Documate.Api.Infrastructure.Settings;

using Documate.Api.Infrastructure.Options;
using Microsoft.Extensions.Options;

/// <summary>Effective EmailIntake operational settings (DB SoT with appsettings bootstrap fallback).</summary>
public sealed class EmailIntakeEffectiveSettings
{
    public string DefaultDomain { get; init; } = "docsintake.com";
    public long MaxAttachmentBytes { get; init; } = 25 * 1024 * 1024;
    public long MaxTotalAttachmentBytes { get; init; } = 50 * 1024 * 1024;
    public int MaxAttachments { get; init; } = 20;
    public string[] AllowedExtensions { get; init; } =
        [".pdf", ".png", ".jpg", ".jpeg", ".tif", ".tiff", ".docx", ".xlsx", ".txt"];
    public int RateLimitPerMailboxPerMinute { get; init; } = 20;
    public int RateLimitPerMailboxPerHour { get; init; } = 200;
    public string? S3Bucket { get; init; }
    public string? S3Prefix { get; init; }
    public string? AwsRegion { get; init; }
    public int MimeRetentionDays { get; init; } = 30;
    public int BodyExcerptMaxChars { get; init; } = 4096;
    /// <summary>Secret — always from AWS Secrets Manager / env, never DB (Plan 17).</summary>
    public string? InboundWebhookSecret { get; init; }
}

public interface IEmailIntakeSettings
{
    EmailIntakeEffectiveSettings Current { get; }
}

public sealed class EmailIntakeSettings(
    ISystemSettings systemSettings,
    IOptions<EmailIntakeOptions> bootstrap) : IEmailIntakeSettings
{
    public EmailIntakeEffectiveSettings Current
    {
        get
        {
            var b = bootstrap.Value;
            return new EmailIntakeEffectiveSettings
            {
                DefaultDomain = systemSettings.GetOrDefault(SystemSettingKeys.EmailDefaultDomain, b.DefaultDomain),
                MaxAttachmentBytes = systemSettings.GetOrDefault(SystemSettingKeys.EmailMaxAttachmentBytes, b.MaxAttachmentBytes),
                MaxTotalAttachmentBytes = systemSettings.GetOrDefault(
                    SystemSettingKeys.EmailMaxTotalAttachmentBytes,
                    b.MaxTotalAttachmentBytes),
                MaxAttachments = systemSettings.GetOrDefault(SystemSettingKeys.EmailMaxAttachments, b.MaxAttachments),
                AllowedExtensions = systemSettings.GetOrDefault(SystemSettingKeys.EmailAllowedExtensions, b.AllowedExtensions)
                    ?? b.AllowedExtensions,
                RateLimitPerMailboxPerMinute = systemSettings.GetOrDefault(
                    SystemSettingKeys.EmailRateLimitPerMailboxPerMinute,
                    b.RateLimitPerMailboxPerMinute),
                RateLimitPerMailboxPerHour = systemSettings.GetOrDefault(
                    SystemSettingKeys.EmailRateLimitPerMailboxPerHour,
                    b.RateLimitPerMailboxPerHour),
                S3Bucket = systemSettings.GetOrDefault(SystemSettingKeys.EmailS3Bucket, b.S3Bucket),
                S3Prefix = systemSettings.GetOrDefault(SystemSettingKeys.EmailS3Prefix, b.S3Prefix),
                AwsRegion = systemSettings.GetOrDefault(SystemSettingKeys.EmailAwsRegion, b.AwsRegion),
                MimeRetentionDays = systemSettings.GetOrDefault(SystemSettingKeys.EmailMimeRetentionDays, b.MimeRetentionDays),
                BodyExcerptMaxChars = systemSettings.GetOrDefault(
                    SystemSettingKeys.EmailBodyExcerptMaxChars,
                    b.BodyExcerptMaxChars),
                InboundWebhookSecret = b.InboundWebhookSecret,
            };
        }
    }
}

public sealed class PipelineSyncEffectiveSettings
{
    public int SyncWaitTimeoutSeconds { get; init; } = 60;
    public int SyncMaxPages { get; init; } = 3;
    public long SyncMaxBytes { get; init; } = 5_242_880;
}

public interface IPipelineSyncSettings
{
    PipelineSyncEffectiveSettings Current { get; }
}

public sealed class PipelineSyncSettings(
    ISystemSettings systemSettings,
    IOptions<PipelineOptions> bootstrap) : IPipelineSyncSettings
{
    public PipelineSyncEffectiveSettings Current
    {
        get
        {
            var b = bootstrap.Value;
            return new PipelineSyncEffectiveSettings
            {
                SyncWaitTimeoutSeconds = systemSettings.GetOrDefault(
                    SystemSettingKeys.PipelineSyncWaitTimeoutSeconds,
                    b.SyncWaitTimeoutSeconds),
                SyncMaxPages = systemSettings.GetOrDefault(SystemSettingKeys.PipelineSyncMaxPages, b.SyncMaxPages),
                SyncMaxBytes = systemSettings.GetOrDefault(SystemSettingKeys.PipelineSyncMaxBytes, b.SyncMaxBytes),
            };
        }
    }
}

public sealed class PipelineModelEffectiveSettings
{
    public string IntelligenceT1ProviderKey { get; init; } = "";
    public string IntelligenceFallbackProviderKey { get; init; } = "";
    public string ExtractProviderKey { get; init; } = "";
    public int IntelligenceMaxCallsPerFile { get; init; } = 40;
}

public interface IPipelineModelSettings
{
    PipelineModelEffectiveSettings Current { get; }
}

public sealed class PipelineModelSettings(
    ISystemSettings systemSettings,
    IOptions<LlmOptions> llmBootstrap) : IPipelineModelSettings
{
    public PipelineModelEffectiveSettings Current
    {
        get
        {
            var defaultProvider = llmBootstrap.Value.DefaultProviderKey;
            return new PipelineModelEffectiveSettings
            {
                IntelligenceT1ProviderKey = systemSettings.GetOrDefault(
                    SystemSettingKeys.PipelineIntelligenceT1ProviderKey,
                    defaultProvider),
                IntelligenceFallbackProviderKey = systemSettings.GetOrDefault(
                    SystemSettingKeys.PipelineIntelligenceFallbackProviderKey,
                    defaultProvider),
                ExtractProviderKey = systemSettings.GetOrDefault(
                    SystemSettingKeys.PipelineExtractProviderKey,
                    defaultProvider),
                IntelligenceMaxCallsPerFile = Math.Max(
                    0,
                    systemSettings.GetOrDefault(SystemSettingKeys.PipelineIntelligenceMaxCallsPerFile, 40)),
            };
        }
    }
}
