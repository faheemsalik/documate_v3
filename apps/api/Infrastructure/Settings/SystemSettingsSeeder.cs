namespace Documate.Api.Infrastructure.Settings;

using System.Text.Json;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

public static class SystemSettingsSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    public static async Task SeedMissingAsync(
        DocumateDbContext db,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var existing = await db.CorSystemSettings.AsNoTracking()
            .Select(x => x.SettingKey)
            .ToListAsync(cancellationToken);
        var have = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        var email = configuration.GetSection(EmailIntakeOptions.SectionName).Get<EmailIntakeOptions>()
                    ?? new EmailIntakeOptions();
        var pipeline = configuration.GetSection(PipelineOptions.SectionName).Get<PipelineOptions>()
                       ?? new PipelineOptions();
        var llm = configuration.GetSection(LlmOptions.SectionName).Get<LlmOptions>()
                  ?? new LlmOptions();

        var defaults = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            [SystemSettingKeys.EmailDefaultDomain] = email.DefaultDomain,
            [SystemSettingKeys.EmailMaxAttachmentBytes] = email.MaxAttachmentBytes,
            [SystemSettingKeys.EmailMaxTotalAttachmentBytes] = email.MaxTotalAttachmentBytes,
            [SystemSettingKeys.EmailMaxAttachments] = email.MaxAttachments,
            [SystemSettingKeys.EmailAllowedExtensions] = email.AllowedExtensions,
            [SystemSettingKeys.EmailRateLimitPerMailboxPerMinute] = email.RateLimitPerMailboxPerMinute,
            [SystemSettingKeys.EmailRateLimitPerMailboxPerHour] = email.RateLimitPerMailboxPerHour,
            [SystemSettingKeys.EmailS3Bucket] = email.S3Bucket,
            [SystemSettingKeys.EmailS3Prefix] = email.S3Prefix,
            [SystemSettingKeys.EmailAwsRegion] = email.AwsRegion,
            [SystemSettingKeys.EmailMimeRetentionDays] = email.MimeRetentionDays,
            [SystemSettingKeys.EmailBodyExcerptMaxChars] = email.BodyExcerptMaxChars,
            [SystemSettingKeys.PipelineSyncWaitTimeoutSeconds] = pipeline.SyncWaitTimeoutSeconds,
            [SystemSettingKeys.PipelineSyncMaxPages] = pipeline.SyncMaxPages,
            [SystemSettingKeys.PipelineSyncMaxBytes] = pipeline.SyncMaxBytes,
            [SystemSettingKeys.PipelineIntelligenceT1ProviderKey] = llm.DefaultProviderKey,
            [SystemSettingKeys.PipelineIntelligenceFallbackProviderKey] = llm.DefaultProviderKey,
            [SystemSettingKeys.PipelineExtractProviderKey] = llm.DefaultProviderKey,
            [SystemSettingKeys.PipelineIntelligenceMaxCallsPerFile] = 40,
        };

        foreach (var (key, value) in defaults)
        {
            if (have.Contains(key))
            {
                continue;
            }

            db.CorSystemSettings.Add(new CorSystemSetting
            {
                SettingKey = key,
                ValueJson = JsonSerializer.Serialize(value, JsonOptions),
                CreatedByUserId = "system-seed",
                UpdatedByUserId = "system-seed",
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
