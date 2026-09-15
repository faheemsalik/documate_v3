namespace Documate.Api.Infrastructure.Storage;

using Documate.Api.Domain;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

public interface ISignedDownloadUrlService
{
    Task RefreshFileAsync(OpsFile file, CancellationToken cancellationToken = default);
    Task RefreshDocumentAsync(OpsDocument document, CancellationToken cancellationToken = default);
}

public sealed class SignedDownloadUrlService(
    IObjectStorage storage,
    IOptionsMonitor<StorageOptions> options,
    DocumateDbContext db) : ISignedDownloadUrlService
{
    public async Task RefreshFileAsync(OpsFile file, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(file.StorageBucket) || string.IsNullOrWhiteSpace(file.StorageKey))
        {
            return;
        }

        if (!NeedsRefresh(file.DownloadUrl, file.DownloadUrlExpiresAt))
        {
            return;
        }

        file.DownloadUrl = await storage.GetSignedUrlAsync(file.StorageBucket, file.StorageKey, cancellationToken);
        file.DownloadUrlExpiresAt = ExpiresAt();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RefreshDocumentAsync(OpsDocument document, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(document.PdfStorageBucket)
            || string.IsNullOrWhiteSpace(document.PdfStorageKey))
        {
            document.DownloadUrl = null;
            document.DownloadUrlExpiresAt = null;
            return;
        }

        if (!NeedsRefresh(document.DownloadUrl, document.DownloadUrlExpiresAt))
        {
            return;
        }

        document.DownloadUrl = await storage.GetSignedUrlAsync(
            document.PdfStorageBucket,
            document.PdfStorageKey,
            cancellationToken);
        document.DownloadUrlExpiresAt = ExpiresAt();
        await db.SaveChangesAsync(cancellationToken);
    }

    private static bool NeedsRefresh(string? url, DateTimeOffset? expiresAt) =>
        string.IsNullOrWhiteSpace(url)
        || expiresAt is null
        || expiresAt <= DateTimeOffset.UtcNow.AddMinutes(1);

    private DateTimeOffset ExpiresAt() =>
        DateTimeOffset.UtcNow.AddMinutes(options.CurrentValue.SignedUrlMinutes <= 0 ? 30 : options.CurrentValue.SignedUrlMinutes);
}
