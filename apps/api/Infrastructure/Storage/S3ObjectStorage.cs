namespace Documate.Api.Infrastructure.Storage;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Documate.Api.Infrastructure.Options;
using Microsoft.Extensions.Options;

/// <summary>
/// Continues old_code S3 pattern: TransferUtility upload, GetObject download,
/// GetPreSignedURL (~30 min), Intelligent-Tiering.
/// </summary>
public sealed class S3ObjectStorage(
    IAmazonS3 s3,
    IOptions<StorageOptions> options,
    ILogger<S3ObjectStorage> logger) : ObjectStorageBase(options)
{
    public override async Task UploadAsync(ObjectStoragePutRequest request, CancellationToken cancellationToken = default)
    {
        var transfer = new TransferUtility(s3);
        var upload = new TransferUtilityUploadRequest
        {
            BucketName = request.Bucket,
            Key = request.Key,
            InputStream = request.Content,
            StorageClass = S3StorageClass.IntelligentTiering,
            AutoCloseStream = false,
        };

        if (!string.IsNullOrWhiteSpace(request.ContentType))
        {
            upload.ContentType = request.ContentType;
        }

        if (request.Metadata is not null)
        {
            foreach (var (k, v) in request.Metadata)
            {
                upload.Metadata.Add(k, v);
            }
        }

        await transfer.UploadAsync(upload, cancellationToken);
        logger.LogInformation("Uploaded s3://{Bucket}/{Key}", request.Bucket, request.Key);
    }

    public override async Task<Stream> DownloadAsync(string bucket, string key, CancellationToken cancellationToken = default)
    {
        var response = await s3.GetObjectAsync(new GetObjectRequest
        {
            BucketName = bucket,
            Key = key,
        }, cancellationToken);

        var ms = new MemoryStream();
        await response.ResponseStream.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;
        return ms;
    }

    public override async Task<bool> DeleteAsync(string bucket, string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await s3.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = bucket,
                Key = key,
            }, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed deleting s3://{Bucket}/{Key}", bucket, key);
            return false;
        }
    }

    public override Task<string> GetSignedUrlAsync(string bucket, string key, CancellationToken cancellationToken = default)
    {
        var minutes = Options.SignedUrlMinutes <= 0 ? 30 : Options.SignedUrlMinutes;
        var url = s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = key,
            Expires = DateTime.UtcNow.AddMinutes(minutes),
            Verb = HttpVerb.GET,
        });
        return Task.FromResult(url);
    }
}
