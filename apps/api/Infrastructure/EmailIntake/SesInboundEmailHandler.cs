namespace Documate.Api.Infrastructure.EmailIntake;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

public interface ISesInboundEmailHandler
{
    Task HandleS3ObjectAsync(string bucket, string key, CancellationToken cancellationToken = default);
}

public sealed class SesInboundEmailHandler(
    IInboundMimeParser mimeParser,
    DocumateDbContext db,
    IBusinessContextSetter businessSetter,
    IEmailIntakeProcessor processor,
    IEmailIntakeMetrics metrics,
    IEmailIntakeSettings emailIntakeSettings,
    IOptions<AwsOptions> awsOptions,
    ILogger<SesInboundEmailHandler> logger) : ISesInboundEmailHandler
{
    public async Task HandleS3ObjectAsync(string bucket, string key, CancellationToken cancellationToken = default)
    {
        var opts = emailIntakeSettings.Current;
        using var s3 = CreateS3Client(opts, awsOptions.Value);
        using var response = await s3.GetObjectAsync(
            new GetObjectRequest { BucketName = bucket, Key = key },
            cancellationToken);
        var parsed = mimeParser.Parse(response.ResponseStream);

        if (string.IsNullOrWhiteSpace(parsed.ToLocalPart))
        {
            logger.LogWarning("SES inbound {Key}: missing To local-part; dropping", key);
            metrics.UnknownRecipient();
            return;
        }

        var domain = string.IsNullOrWhiteSpace(parsed.ToDomain)
            ? opts.DefaultDomain
            : parsed.ToDomain!;

        var mailbox = await db.OpsIntakeMailboxes.AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.EmailLocalPart == parsed.ToLocalPart
                     && m.EmailDomain == domain
                     && !m.IsDeleted,
                cancellationToken);

        if (mailbox is null)
        {
            metrics.UnknownRecipient();
            logger.LogInformation(
                "SES inbound unknown recipient {Local}@{Domain}; drop+metric",
                parsed.ToLocalPart,
                domain);
            return;
        }

        var biz = await db.CorTenantBusinesses.AsNoTracking()
            .FirstOrDefaultAsync(b => b.IdenBusinessId == mailbox.BusinessId && !b.IsDeleted, cancellationToken);
        if (biz is null)
        {
            logger.LogWarning("SES inbound mailbox {Id} business missing", mailbox.Id);
            return;
        }

        using (businessSetter.Use(new BusinessContext
               {
                   IsAuthenticated = true,
                   BusinessId = mailbox.BusinessId,
                   TenantId = "",
                   UserId = "ses-inbound",
                   BusinessName = biz.Name,
                   TenantName = biz.TenantName,
               }))
        {
            await processor.ProcessAsync(
                new EmailIntakeProcessRequest(
                    mailbox.Id,
                    parsed.From,
                    parsed.Subject,
                    parsed.MessageId,
                    parsed.TextBody,
                    parsed.Attachments,
                    parsed.FromName,
                    parsed.ReplyTo,
                    parsed.ResentFrom,
                    parsed.ToAddress),
                cancellationToken);
        }
    }

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
