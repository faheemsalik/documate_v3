namespace Documate.Api.Infrastructure.Settings;

using Documate.Api.Infrastructure.Options;
using Microsoft.Extensions.Options;

/// <summary>
/// Overlays allowlisted non-secret keys from <see cref="ISystemSettings"/> onto options
/// after appsettings/SM bind. Secrets stay on the bound instance (SM/env).
/// </summary>
public sealed class DbBackedOptionsPostConfigure(
    ISystemSettings systemSettings) :
    IPostConfigureOptions<StorageOptions>,
    IPostConfigureOptions<OcrOptions>,
    IPostConfigureOptions<PipelineOptions>,
    IPostConfigureOptions<NotificationOptions>,
    IPostConfigureOptions<AdminOptions>
{
    public void PostConfigure(string? name, StorageOptions options)
    {
        options.Provider = systemSettings.GetOrDefault(SystemSettingKeys.StorageProvider, options.Provider);
        options.BucketOrContainer = systemSettings.GetOrDefault(
            SystemSettingKeys.StorageBucketOrContainer,
            options.BucketOrContainer);
        options.LocalRootPath = systemSettings.GetOrDefault(
            SystemSettingKeys.StorageLocalRootPath,
            options.LocalRootPath);
        options.Region = systemSettings.GetOrDefault(SystemSettingKeys.StorageRegion, options.Region);
        options.ServiceUrl = systemSettings.GetOrDefault(SystemSettingKeys.StorageServiceUrl, options.ServiceUrl);
        options.SignedUrlMinutes = systemSettings.GetOrDefault(
            SystemSettingKeys.StorageSignedUrlMinutes,
            options.SignedUrlMinutes);
    }

    public void PostConfigure(string? name, OcrOptions options)
    {
        options.PrimaryProviderKey = systemSettings.GetOrDefault(
            SystemSettingKeys.OcrPrimaryProviderKey,
            options.PrimaryProviderKey);
        options.SecondaryProviderKey = systemSettings.GetOrDefault(
            SystemSettingKeys.OcrSecondaryProviderKey,
            options.SecondaryProviderKey);
        options.SyncMaxPages = systemSettings.GetOrDefault(
            SystemSettingKeys.OcrSyncMaxPages,
            options.SyncMaxPages);
        options.Textract.Region = systemSettings.GetOrDefault(
            SystemSettingKeys.OcrTextractRegion,
            options.Textract.Region);
        options.GoogleDocumentAi.Location = systemSettings.GetOrDefault(
            SystemSettingKeys.OcrGoogleLocation,
            options.GoogleDocumentAi.Location);
        options.GoogleDocumentAi.ProjectId = systemSettings.GetOrDefault(
            SystemSettingKeys.OcrGoogleProjectId,
            options.GoogleDocumentAi.ProjectId);
        options.GoogleDocumentAi.ProcessorId = systemSettings.GetOrDefault(
            SystemSettingKeys.OcrGoogleProcessorId,
            options.GoogleDocumentAi.ProcessorId);
    }

    public void PostConfigure(string? name, PipelineOptions options)
    {
        options.MaxConcurrentFiles = systemSettings.GetOrDefault(
            SystemSettingKeys.PipelineMaxConcurrentFiles,
            options.MaxConcurrentFiles);
        options.MaxConcurrentWebhooks = systemSettings.GetOrDefault(
            SystemSettingKeys.PipelineMaxConcurrentWebhooks,
            options.MaxConcurrentWebhooks);
        options.StubStageDelayMs = systemSettings.GetOrDefault(
            SystemSettingKeys.PipelineStubStageDelayMs,
            options.StubStageDelayMs);
        options.SyncWaitTimeoutSeconds = systemSettings.GetOrDefault(
            SystemSettingKeys.PipelineSyncWaitTimeoutSeconds,
            options.SyncWaitTimeoutSeconds);
        options.SyncMaxPages = systemSettings.GetOrDefault(
            SystemSettingKeys.PipelineSyncMaxPages,
            options.SyncMaxPages);
        options.SyncMaxBytes = systemSettings.GetOrDefault(
            SystemSettingKeys.PipelineSyncMaxBytes,
            options.SyncMaxBytes);
    }

    public void PostConfigure(string? name, NotificationOptions options)
    {
        options.Enabled = systemSettings.GetOrDefault(
            SystemSettingKeys.NotificationsEnabled,
            options.Enabled);
        options.ToAddress = systemSettings.GetOrDefault(
            SystemSettingKeys.NotificationsToAddress,
            options.ToAddress);
        options.Smtp.Host = systemSettings.GetOrDefault(
            SystemSettingKeys.NotificationsSmtpHost,
            options.Smtp.Host);
        options.Smtp.Port = systemSettings.GetOrDefault(
            SystemSettingKeys.NotificationsSmtpPort,
            options.Smtp.Port);
        options.Smtp.User = systemSettings.GetOrDefault(
            SystemSettingKeys.NotificationsSmtpUser,
            options.Smtp.User);
        options.Smtp.From = systemSettings.GetOrDefault(
            SystemSettingKeys.NotificationsSmtpFrom,
            options.Smtp.From);
    }

    public void PostConfigure(string? name, AdminOptions options)
    {
        options.HangfireDashboardUrl = systemSettings.GetOrDefault(
            SystemSettingKeys.AdminHangfireDashboardUrl,
            options.HangfireDashboardUrl);
        options.DatadogDashboardUrl = systemSettings.GetOrDefault(
            SystemSettingKeys.AdminDatadogDashboardUrl,
            options.DatadogDashboardUrl);
    }
}
