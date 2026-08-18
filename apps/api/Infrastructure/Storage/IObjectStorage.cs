namespace Documate.Api.Infrastructure.Storage;

/// <summary>Object storage abstraction (Phase 1: local or AWS S3 like old_code).</summary>
public interface IObjectStorage
{
    Task UploadAsync(ObjectStoragePutRequest request, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string bucket, string key, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string bucket, string key, CancellationToken cancellationToken = default);
    Task<string> GetSignedUrlAsync(string bucket, string key, CancellationToken cancellationToken = default);
    string ResolveBucket();
    /// <summary>Key uses SequenceIds (not UUIDs) for shorter, human-readable paths.</summary>
    string BuildFileKey(
        long tenantSequenceId,
        long businessSequenceId,
        long queueSequenceId,
        long fileSequenceId,
        string originalFileName);

    /// <summary>Sibling artifact under the same file folder (e.g. normalize.layout.json).</summary>
    string BuildArtifactKey(string fileStorageKey, string artifactFileName);
}

public sealed record ObjectStoragePutRequest(
    string Bucket,
    string Key,
    Stream Content,
    string? ContentType,
    IReadOnlyDictionary<string, string>? Metadata);
