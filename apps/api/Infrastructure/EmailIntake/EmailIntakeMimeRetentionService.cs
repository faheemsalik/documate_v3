namespace Documate.Api.Infrastructure.EmailIntake;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Settings;
using Microsoft.Extensions.Options;

public interface IEmailIntakeMimeRetention
{
    Task<EmailIntakeMimeRetentionResult> PurgeExpiredAsync(CancellationToken cancellationToken = default);
}

public sealed record EmailIntakeMimeRetentionResult(int Listed, int Deleted, int RetentionDays);

public sealed class EmailIntakeMimeRetentionService(
    IEmailIntakeSettings emailIntakeSettings,
    IOptions<AwsOptions> awsOptions,
    IEmailIntakeMetrics metrics,
    ILogger<EmailIntakeMimeRetentionService> logger) : IEmailIntakeMimeRetention
{
    public async Task<EmailIntakeMimeRetentionResult> PurgeExpiredAsync(CancellationToken cancellationToken = default)
    {
        var opts = emailIntakeSettings.Current;
        var days = Math.Max(1, opts.MimeRetentionDays);
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var bucket = opts.S3Bucket;
        var prefix = NormalizePrefix(opts.S3Prefix);

        if (string.IsNullOrWhiteSpace(bucket))
        {
            logger.LogWarning("Email MIME retention skipped: S3Bucket not configured");
            return new EmailIntakeMimeRetentionResult(0, 0, days);
        }

        using var s3 = CreateS3Client(opts, awsOptions.Value);
        var listed = 0;
        var deleted = 0;
        string? token = null;
        do
        {
            var response = await s3.ListObjectsV2Async(
                new ListObjectsV2Request
                {
                    BucketName = bucket,
                    Prefix = prefix,
                    ContinuationToken = token,
                    MaxKeys = 1000,
                },
                cancellationToken);

            foreach (var obj in response.S3Objects ?? [])
            {
                listed++;
                var lastMod = obj.LastModified;
                if (lastMod is null || lastMod.Value.ToUniversalTime() > cutoff)
                {
                    continue;
                }

                await s3.DeleteObjectAsync(
                    new DeleteObjectRequest { BucketName = bucket, Key = obj.Key },
                    cancellationToken);
                deleted++;
            }

            token = response.IsTruncated == true ? response.NextContinuationToken : null;
        } while (token is not null);

        if (deleted > 0)
        {
            metrics.MimeDeleted(deleted);
        }

        logger.LogInformation(
            "Email MIME retention bucket={Bucket} prefix={Prefix} days={Days} listed={Listed} deleted={Deleted}",
            bucket,
            prefix,
            days,
            listed,
            deleted);

        return new EmailIntakeMimeRetentionResult(listed, deleted, days);
    }

    public static string NormalizePrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return "";
        }

        var p = prefix.Replace('\\', '/');
        return p.EndsWith('/') ? p : p + "/";
    }

    public static bool IsExpired(DateTimeOffset lastModifiedUtc, DateTimeOffset cutoffUtc) =>
        lastModifiedUtc.ToUniversalTime() <= cutoffUtc.ToUniversalTime();

    private static AmazonS3Client CreateS3Client(EmailIntakeEffectiveSettings opts, AwsOptions aws)
    {
        var regionName = string.IsNullOrWhiteSpace(opts.AwsRegion) ? "us-west-2" : opts.AwsRegion;
        var region = RegionEndpoint.GetBySystemName(regionName);
        var credentials = aws.TryCreateCredentials();
        return credentials is null
            ? new AmazonS3Client(region)
            : new AmazonS3Client(credentials, region);
    }
}
