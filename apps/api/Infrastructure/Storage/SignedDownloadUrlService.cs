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
    DocumateDbContext db,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
    ILogger<SignedDownloadUrlService> logger) : ISignedDownloadUrlService
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

        try
        {
            var signed = await storage.GetSignedUrlAsync(file.StorageBucket, file.StorageKey, cancellationToken);
            file.DownloadUrl = IsHttpUrl(signed)
                ? signed
                : BuildContentUrl($"api/v1/files/{file.Id}/content");
            file.DownloadUrlExpiresAt = ExpiresAt();
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to mint download URL for file {FileId}", file.Id);
        }
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

        try
        {
            var signed = await storage.GetSignedUrlAsync(
                document.PdfStorageBucket,
                document.PdfStorageKey,
                cancellationToken);
            document.DownloadUrl = IsHttpUrl(signed)
                ? signed
                : BuildContentUrl($"api/v1/documents/{document.Id}/content");
            document.DownloadUrlExpiresAt = ExpiresAt();
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to mint download URL for document {DocumentId}", document.Id);
        }
    }

    private static bool NeedsRefresh(string? url, DateTimeOffset? expiresAt) =>
        string.IsNullOrWhiteSpace(url)
        || !IsHttpUrl(url)
        || expiresAt is null
        || expiresAt <= DateTimeOffset.UtcNow.AddMinutes(1);

    private DateTimeOffset ExpiresAt() =>
        DateTimeOffset.UtcNow.AddMinutes(options.CurrentValue.SignedUrlMinutes <= 0 ? 30 : options.CurrentValue.SignedUrlMinutes);

    private static bool IsHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private string BuildContentUrl(string relativePath)
    {
        var origin = ResolvePublicOrigin().TrimEnd('/');
        return $"{origin}/{relativePath.TrimStart('/')}";
    }

    private string ResolvePublicOrigin()
    {
        var configured = options.CurrentValue.PublicBaseUrl;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim().TrimEnd('/');
        }

        var request = httpContextAccessor.HttpContext?.Request;
        if (request is not null)
        {
            return $"{request.Scheme}://{request.Host}";
        }

        var urls = configuration["ASPNETCORE_URLS"] ?? configuration["urls"];
        if (!string.IsNullOrWhiteSpace(urls))
        {
            var first = urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault(u => u.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || u.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(first))
            {
                return first.TrimEnd('/');
            }
        }

        return "http://localhost:5172";
    }
}
