namespace Documate.Api.Infrastructure.PublicEvents;

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Notifications;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Pipeline;
using Documate.Api.Infrastructure.Webhooks;
using Hangfire;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;

public sealed class PublicActionExecutor(
    DocumateDbContext db,
    ICorEnumIdResolver enums,
    IWebhookSecretProtector secrets,
    IBackgroundJobClient jobs,
    IHttpClientFactory httpFactory,
    IHostEnvironment env,
    IOptionsMonitor<NotificationOptions> notificationOptions,
    ILogger<PublicActionExecutor> logger)
{
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(120),
        TimeSpan.FromSeconds(300),
        TimeSpan.FromSeconds(600),
    ];

    public const int MaxAttempts = 5;

    public async Task ExecuteAsync(Guid deliveryId, string businessId, CancellationToken cancellationToken = default)
    {
        var delivery = await db.OpsOutboundDeliveries
            .FirstOrDefaultAsync(d => d.Id == deliveryId && d.BusinessId == businessId && !d.IsDeleted, cancellationToken);
        if (delivery is null)
        {
            logger.LogWarning("Outbound delivery {DeliveryId} not found", deliveryId);
            return;
        }

        var succeeded = enums.Require("webhook_delivery_status", "succeeded");
        if (delivery.StatusEnumId == succeeded)
        {
            return;
        }

        OpsActionBinding? binding = null;
        if (delivery.ActionBindingId is Guid bindingId)
        {
            binding = await db.OpsActionBindings.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == bindingId && b.BusinessId == businessId && !b.IsDeleted, cancellationToken);
        }

        try
        {
            switch (delivery.ActionTypeKey)
            {
                case PublicEventCatalog.ActionWebhook:
                    await DeliverWebhookAsync(delivery, binding, cancellationToken);
                    break;
                case PublicEventCatalog.ActionEmail:
                    await DeliverEmailAsync(delivery, binding, cancellationToken);
                    break;
                case PublicEventCatalog.ActionInApp:
                    await DeliverInAppAsync(delivery, cancellationToken);
                    break;
                default:
                    await FailAsync(delivery, null, $"Unknown action type {delivery.ActionTypeKey}", scheduleRetry: false, cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Public action execution failed for {DeliveryId}", deliveryId);
            await FailAsync(delivery, null, ex.Message, scheduleRetry: true, cancellationToken);
        }
    }

    private async Task DeliverWebhookAsync(
        OpsOutboundDelivery delivery,
        OpsActionBinding? binding,
        CancellationToken cancellationToken)
    {
        var (url, secretProtected) = await ResolveWebhookConfigAsync(delivery, binding, cancellationToken);
        if (string.IsNullOrWhiteSpace(url))
        {
            delivery.StatusEnumId = enums.Require("webhook_delivery_status", "not_configured");
            await db.SaveChangesAsync(cancellationToken);
            await ProjectDocumentMetaAsync(delivery, cancellationToken);
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && !(env.IsDevelopment() && uri.Scheme == Uri.UriSchemeHttp)))
        {
            await FailAsync(delivery, null, "Webhook URL must be https (http allowed in Development).", scheduleRetry: false, cancellationToken);
            return;
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(delivery.PayloadJson);
        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new ByteArrayContent(bytes)
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" } },
            },
        };
        request.Headers.TryAddWithoutValidation(DocumentWebhookPayload.EventHeader, delivery.EventName);
        request.Headers.TryAddWithoutValidation(DocumentWebhookPayload.DeliveryHeader, delivery.EventId);

        if (!string.IsNullOrWhiteSpace(secretProtected))
        {
            try
            {
                var secret = secrets.Unprotect(secretProtected);
                request.Headers.TryAddWithoutValidation(
                    DocumentWebhookPayload.SignatureHeader,
                    DocumentWebhookPayload.SignBody(secret, bytes));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not unprotect webhook secret for delivery {DeliveryId}", delivery.Id);
            }
        }

        int? httpStatus = null;
        string? error = null;
        try
        {
            using var response = await httpFactory.CreateClient("documate-webhooks")
                .SendAsync(request, cancellationToken);
            httpStatus = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode)
            {
                error = $"HTTP {httpStatus}";
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        if (error is null)
        {
            await SucceedAsync(delivery, httpStatus, cancellationToken);
            return;
        }

        await FailAsync(delivery, httpStatus, error, scheduleRetry: true, cancellationToken);
    }

    private async Task<(string? Url, string? SecretProtected)> ResolveWebhookConfigAsync(
        OpsOutboundDelivery delivery,
        OpsActionBinding? binding,
        CancellationToken cancellationToken)
    {
        if (binding?.ConfigJson is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(binding.ConfigJson);
                var root = doc.RootElement;
                var url = root.TryGetProperty("url", out var u) ? u.GetString() : null;
                var secret = root.TryGetProperty("secretProtected", out var s) ? s.GetString() : null;
                if (!string.IsNullOrWhiteSpace(url))
                {
                    return (url, secret);
                }
            }
            catch (JsonException)
            {
                // fall through to queue columns
            }
        }

        if (delivery.QueueId is Guid queueId)
        {
            var queue = await db.OpsQueues.AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == queueId && q.BusinessId == delivery.BusinessId && !q.IsDeleted, cancellationToken);
            if (queue is not null)
            {
                return (queue.WebhookUrl, queue.WebhookSecretProtected);
            }
        }

        return (null, null);
    }

    private async Task DeliverEmailAsync(
        OpsOutboundDelivery delivery,
        OpsActionBinding? binding,
        CancellationToken cancellationToken)
    {
        var opts = notificationOptions.CurrentValue;
        var recipients = new List<string>();
        var audience = "partner";
        if (binding?.ConfigJson is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(binding.ConfigJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("audience", out var a))
                {
                    audience = a.GetString() ?? audience;
                }

                if (root.TryGetProperty("recipients", out var r) && r.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in r.EnumerateArray())
                    {
                        var email = item.GetString();
                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            recipients.Add(email.Trim());
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // ignore
            }
        }

        if (string.Equals(audience, "platform", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(opts.ToAddress))
        {
            recipients.Add(opts.ToAddress.Trim());
        }

        recipients = recipients.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (recipients.Count == 0)
        {
            await FailAsync(delivery, null, "No email recipients configured.", scheduleRetry: false, cancellationToken);
            return;
        }

        if (!opts.Enabled
            || string.IsNullOrWhiteSpace(opts.Smtp.Host)
            || string.IsNullOrWhiteSpace(opts.Smtp.From))
        {
            logger.LogWarning("Email action skipped — notifications SMTP not configured (delivery {DeliveryId})", delivery.Id);
            await SucceedAsync(delivery, null, cancellationToken); // soft-succeed: logged intent without SMTP
            return;
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(opts.Smtp.From));
        foreach (var to in recipients)
        {
            message.To.Add(MailboxAddress.Parse(to));
        }

        message.Subject = $"[Documate] {delivery.EventName}";
        message.Body = new TextPart("plain")
        {
            Text = $"""
                Public event: {delivery.EventName}
                EventId: {delivery.EventId}
                BusinessId: {delivery.BusinessId}
                Resource: {delivery.ResourceTypeKey}/{delivery.ResourceId}

                Payload:
                {delivery.PayloadJson}
                """,
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(
            opts.Smtp.Host,
            opts.Smtp.Port <= 0 ? 587 : opts.Smtp.Port,
            SecureSocketOptions.StartTlsWhenAvailable,
            cancellationToken);
        if (!string.IsNullOrWhiteSpace(opts.Smtp.User))
        {
            await client.AuthenticateAsync(opts.Smtp.User, opts.Smtp.Password ?? "", cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
        await SucceedAsync(delivery, 200, cancellationToken);
    }

    private async Task DeliverInAppAsync(OpsOutboundDelivery delivery, CancellationToken cancellationToken)
    {
        var existing = await db.OpsInAppNotifications.AsNoTracking()
            .AnyAsync(
                n => n.BusinessId == delivery.BusinessId && n.EventId == delivery.EventId && !n.IsDeleted,
                cancellationToken);
        if (!existing)
        {
            db.OpsInAppNotifications.Add(new OpsInAppNotification
            {
                BusinessId = delivery.BusinessId,
                EventId = delivery.EventId,
                EventName = delivery.EventName,
                Title = delivery.EventName,
                Body = $"{delivery.ResourceTypeKey} {delivery.ResourceId}",
                PayloadJson = delivery.PayloadJson,
            });
        }

        await SucceedAsync(delivery, null, cancellationToken);
    }

    private async Task SucceedAsync(OpsOutboundDelivery delivery, int? httpStatus, CancellationToken cancellationToken)
    {
        delivery.Attempts += 1;
        delivery.LastAt = DateTimeOffset.UtcNow;
        delivery.LastHttpStatus = httpStatus;
        delivery.LastError = null;
        delivery.StatusEnumId = enums.Require("webhook_delivery_status", "succeeded");
        await db.SaveChangesAsync(cancellationToken);
        await ProjectDocumentMetaAsync(delivery, cancellationToken);
        await AppendWorkEventAsync(delivery, "webhook_succeeded", httpStatus, null, cancellationToken);
    }

    private async Task FailAsync(
        OpsOutboundDelivery delivery,
        int? httpStatus,
        string error,
        bool scheduleRetry,
        CancellationToken cancellationToken)
    {
        delivery.Attempts += 1;
        delivery.LastAt = DateTimeOffset.UtcNow;
        delivery.LastHttpStatus = httpStatus;
        delivery.LastError = error.Length > 4000 ? error[..4000] : error;

        var willRetry = scheduleRetry && delivery.Attempts < MaxAttempts;
        delivery.StatusEnumId = enums.Require(
            "webhook_delivery_status",
            willRetry ? "pending" : "exhausted");
        await db.SaveChangesAsync(cancellationToken);
        await ProjectDocumentMetaAsync(delivery, cancellationToken);
        await AppendWorkEventAsync(
            delivery,
            willRetry ? "webhook_attempted" : "webhook_failed",
            httpStatus,
            error,
            cancellationToken);

        if (willRetry)
        {
            var delay = RetryDelays[Math.Min(delivery.Attempts - 1, RetryDelays.Length - 1)];
            jobs.Schedule<PublicActionJobs>(
                j => j.ExecutePublicActionAsync(delivery.Id, delivery.BusinessId),
                delay);
        }
    }

    private async Task ProjectDocumentMetaAsync(OpsOutboundDelivery delivery, CancellationToken cancellationToken)
    {
        if (!string.Equals(delivery.ResourceTypeKey, PublicEventCatalog.ResourceDocument, StringComparison.Ordinal)
            || !string.Equals(delivery.ActionTypeKey, PublicEventCatalog.ActionWebhook, StringComparison.Ordinal))
        {
            return;
        }

        var doc = await db.OpsDocuments.FirstOrDefaultAsync(
            d => d.Id == delivery.ResourceId && d.BusinessId == delivery.BusinessId && !d.IsDeleted,
            cancellationToken);
        if (doc is null)
        {
            return;
        }

        doc.WebhookStatusEnumId = delivery.StatusEnumId;
        doc.WebhookAttempts = delivery.Attempts;
        doc.WebhookLastAt = delivery.LastAt;
        doc.WebhookLastHttpStatus = delivery.LastHttpStatus;
        doc.WebhookLastError = delivery.LastError;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task AppendWorkEventAsync(
        OpsOutboundDelivery delivery,
        string eventTypeKey,
        int? httpStatus,
        string? error,
        CancellationToken cancellationToken)
    {
        var subjectType = string.Equals(delivery.ResourceTypeKey, PublicEventCatalog.ResourceFile, StringComparison.Ordinal)
            ? "file"
            : "document";
        db.OpsWorkEvents.Add(new OpsWorkEvent
        {
            BusinessId = delivery.BusinessId,
            SubjectTypeEnumId = enums.Require("work_subject_type", subjectType),
            SubjectId = delivery.ResourceId,
            EventTypeEnumId = enums.Require("work_event_type", eventTypeKey),
            PayloadJson = JsonSerializer.Serialize(new
            {
                httpStatus,
                error,
                attempts = delivery.Attempts,
                eventName = delivery.EventName,
                deliveryId = delivery.Id,
            }),
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
