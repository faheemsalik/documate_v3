namespace Documate.Api.Infrastructure.Storage;

using System.Text.RegularExpressions;
using Documate.Api.Infrastructure.Options;
using Microsoft.Extensions.Options;

public abstract class ObjectStorageBase(IOptions<StorageOptions> options) : IObjectStorage
{
    protected StorageOptions Options => options.Value;

    public virtual string ResolveBucket() =>
        string.IsNullOrWhiteSpace(Options.BucketOrContainer)
            ? "documate"
            : Options.BucketOrContainer;

    public string BuildFileKey(
        long tenantSequenceId,
        long businessSequenceId,
        long queueSequenceId,
        long fileSequenceId,
        string originalFileName)
    {
        var safe = SanitizeFileName(originalFileName);
        // Tenant → Business → Queue → File via SequenceIds (short, readable, isolation-friendly).
        return $"tenants/{tenantSequenceId}/businesses/{businessSequenceId}/queues/{queueSequenceId}/files/{fileSequenceId}/{safe}";
    }

    public string BuildArtifactKey(string fileStorageKey, string artifactFileName)
    {
        if (string.IsNullOrWhiteSpace(fileStorageKey))
        {
            throw new ArgumentException("fileStorageKey is required.", nameof(fileStorageKey));
        }

        var safeArtifact = SanitizeFileName(artifactFileName);
        var normalized = fileStorageKey.Replace('\\', '/').TrimEnd('/');
        var slash = normalized.LastIndexOf('/');
        var folder = slash >= 0 ? normalized[..slash] : normalized;
        return $"{folder}/artifacts/{safeArtifact}";
    }

    protected static string SanitizeFileName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "file.bin";
        }

        var fileName = Path.GetFileName(name.Trim());
        fileName = Regex.Replace(fileName, @"[^\w\.\-]+", "_");
        return fileName.Length > 180 ? fileName[^180..] : fileName;
    }

    public abstract Task UploadAsync(ObjectStoragePutRequest request, CancellationToken cancellationToken = default);
    public abstract Task<Stream> DownloadAsync(string bucket, string key, CancellationToken cancellationToken = default);
    public abstract Task<bool> DeleteAsync(string bucket, string key, CancellationToken cancellationToken = default);
    public abstract Task<string> GetSignedUrlAsync(string bucket, string key, CancellationToken cancellationToken = default);
}
