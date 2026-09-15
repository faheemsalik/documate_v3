namespace Documate.Api.Modules.External.Features.EmailIntake;

using System.Text.Json;
using Documate.Api.Infrastructure.EmailIntake;
using Documate.Api.Infrastructure.Settings;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>SNS HTTPS subscription endpoint for SES → S3 receipt notifications (Plan 14 EI-5).</summary>
[ApiController]
[Route("api/internal/email-intake")]
public sealed class EmailIntakeInboundController(
    IEmailIntakeSettings emailIntakeSettings,
    IBackgroundJobClient jobs,
    IHttpClientFactory httpClientFactory,
    ILogger<EmailIntakeInboundController> logger) : ControllerBase
{
    [HttpPost("sns")]
    [AllowAnonymous]
    public async Task<IActionResult> ReceiveSns(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
        {
            return BadRequest();
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var type = root.TryGetProperty("Type", out var t) ? t.GetString() : null;

        if (string.Equals(type, "SubscriptionConfirmation", StringComparison.OrdinalIgnoreCase))
        {
            if (root.TryGetProperty("SubscribeURL", out var urlEl))
            {
                var url = urlEl.GetString();
                if (!string.IsNullOrWhiteSpace(url)
                    && Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    && uri.Scheme == Uri.UriSchemeHttps
                    && uri.Host.EndsWith(".amazonaws.com", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var client = httpClientFactory.CreateClient("documate-sns-confirm");
                        using var resp = await client.GetAsync(uri, cancellationToken);
                        logger.LogInformation(
                            "SNS subscription confirm GET {Status} for {Host}",
                            (int)resp.StatusCode,
                            uri.Host);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "SNS subscription confirm GET failed");
                    }
                }
                else
                {
                    logger.LogWarning("SNS SubscribeURL missing or not an amazonaws.com HTTPS URL");
                }
            }

            return Ok();
        }

        if (!string.Equals(type, "Notification", StringComparison.OrdinalIgnoreCase))
        {
            return Ok();
        }

        var secret = emailIntakeSettings.Current.InboundWebhookSecret;
        if (!string.IsNullOrEmpty(secret))
        {
            if (!Request.Headers.TryGetValue("X-Documate-Inbound-Secret", out var hdr)
                || !string.Equals(hdr.ToString(), secret, StringComparison.Ordinal))
            {
                return Unauthorized();
            }
        }

        var message = root.TryGetProperty("Message", out var msg) ? msg.GetString() : null;
        if (string.IsNullOrWhiteSpace(message))
        {
            return Ok();
        }

        string? bucket = null;
        string? key = null;
        try
        {
            using var inner = JsonDocument.Parse(message);
            if (inner.RootElement.TryGetProperty("receipt", out var receipt)
                && receipt.TryGetProperty("action", out var action))
            {
                var actionType = action.TryGetProperty("type", out var at) ? at.GetString() : null;
                if (string.Equals(actionType, "S3", StringComparison.OrdinalIgnoreCase)
                    || action.TryGetProperty("bucketName", out _))
                {
                    bucket = action.TryGetProperty("bucketName", out var b) ? b.GetString() : null;
                    var objectKey = action.TryGetProperty("objectKey", out var k) ? k.GetString() : null;
                    var prefix = action.TryGetProperty("objectKeyPrefix", out var p) ? p.GetString() : null;
                    key = CombineS3Key(prefix, objectKey);
                }
                else
                {
                    logger.LogWarning(
                        "SES SNS action type={ActionType} has no S3 bucket/key; attach SNS topic on the S3 action, not a separate SNS-only action",
                        actionType ?? "(null)");
                }
            }

            bucket ??= inner.RootElement.TryGetProperty("bucket", out var b2) ? b2.GetString() : null;
            key ??= inner.RootElement.TryGetProperty("key", out var k2) ? k2.GetString() : null;
        }
        catch (JsonException)
        {
            logger.LogWarning("SES SNS message was not JSON");
            return Ok();
        }

        if (string.IsNullOrWhiteSpace(bucket) || string.IsNullOrWhiteSpace(key))
        {
            logger.LogWarning("SES SNS notification missing bucket/key");
            return Ok();
        }

        jobs.Enqueue<EmailIntakeJobs>(j => j.ProcessS3ObjectAsync(bucket, key));
        return Ok();
    }

    private static string? CombineS3Key(string? prefix, string? objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(prefix))
        {
            return objectKey;
        }

        var p = prefix.Replace('\\', '/');
        if (!p.EndsWith('/'))
        {
            p += "/";
        }

        // Notification objectKey is sometimes already prefixed.
        if (objectKey.StartsWith(p, StringComparison.Ordinal)
            || objectKey.StartsWith(prefix, StringComparison.Ordinal))
        {
            return objectKey;
        }

        return p + objectKey.TrimStart('/');
    }
}

public sealed class EmailIntakeJobs(
    ISesInboundEmailHandler handler,
    IEmailIntakeMimeRetention mimeRetention)
{
    [AutomaticRetry(Attempts = 3)]
    public Task ProcessS3ObjectAsync(string bucket, string key) =>
        handler.HandleS3ObjectAsync(bucket, key);

    /// <summary>Daily purge of aged raw MIME under EmailIntake S3 prefix (Plan 14 FO-4).</summary>
    [AutomaticRetry(Attempts = 2)]
    public Task PurgeExpiredMimeAsync() => mimeRetention.PurgeExpiredAsync();
}
