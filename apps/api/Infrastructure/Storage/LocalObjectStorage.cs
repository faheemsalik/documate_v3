namespace Documate.Api.Infrastructure.Storage;

using Documate.Api.Infrastructure.Options;
using Microsoft.Extensions.Options;

/// <summary>Dev/local stand-in when AWS is not configured.</summary>
public sealed class LocalObjectStorage(
    IOptions<StorageOptions> options,
    ILogger<LocalObjectStorage> logger) : ObjectStorageBase(options)
{
    private string Root =>
        Path.GetFullPath(Options.LocalRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "_data", "blobs"));

    public override async Task UploadAsync(ObjectStoragePutRequest request, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(request.Bucket, request.Key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var fs = File.Create(path);
        if (request.Content.CanSeek)
        {
            request.Content.Position = 0;
        }

        await request.Content.CopyToAsync(fs, cancellationToken);
        logger.LogInformation("Stored local object {Path}", path);
    }

    public override Task<Stream> DownloadAsync(string bucket, string key, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(bucket, key);
        Stream stream = File.OpenRead(path);
        return Task.FromResult(stream);
    }

    public override Task<bool> DeleteAsync(string bucket, string key, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(bucket, key);
        if (!File.Exists(path))
        {
            return Task.FromResult(false);
        }

        File.Delete(path);
        return Task.FromResult(true);
    }

    public override Task<string> GetSignedUrlAsync(string bucket, string key, CancellationToken cancellationToken = default)
    {
        // Local: return a file URI; production uses S3 presign.
        var path = ResolvePath(bucket, key);
        return Task.FromResult(new Uri(path).AbsoluteUri);
    }

    private string ResolvePath(string bucket, string key)
    {
        var safeKey = key.Replace('\\', '/').TrimStart('/');
        return Path.Combine(Root, bucket, safeKey.Replace('/', Path.DirectorySeparatorChar));
    }
}
