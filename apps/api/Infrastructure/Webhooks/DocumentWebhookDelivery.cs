namespace Documate.Api.Infrastructure.Webhooks;

using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Pipeline;
using Hangfire;
using Microsoft.EntityFrameworkCore;

public sealed class DocumentWebhookDelivery(
    DocumateDbContext db,
    ICorEnumIdResolver enums,
    IWebhookSecretProtector secrets,
    IBackgroundJobClient jobs,
    IHttpClientFactory httpFactory,
    IHostEnvironment env,
    ILogger<DocumentWebhookDelivery> logger)
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

    public async Task DeliverAsync(Guid documentId, string businessId, CancellationToken cancellationToken = default)
    {
        var doc = await db.OpsDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.BusinessId == businessId && !d.IsDeleted, cancellationToken);
        if (doc is null)
        {
            logger.LogWarning("Webhook Document {DocumentId} not found", documentId);
            return;
        }

        var succeeded = enums.Require("webhook_delivery_status", "succeeded");
        if (doc.WebhookStatusEnumId == succeeded)
        {
            return;
        }

        var queue = await db.OpsQueues.AsNoTracking().FirstOrDefaultAsync(
            q => q.Id == doc.QueueId && q.BusinessId == businessId && !q.IsDeleted, cancellationToken);
        var file = await db.OpsFiles.AsNoTracking().FirstOrDefaultAsync(
            f => f.Id == doc.FileId && f.BusinessId == businessId && !f.IsDeleted, cancellationToken);
        if (queue is null || file is null || !queue.WebhookEnabled || string.IsNullOrWhiteSpace(queue.WebhookUrl))
        {
            doc.WebhookStatusEnumId = enums.Require("webhook_delivery_status", "not_configured");
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (!Uri.TryCreate(queue.WebhookUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && !(env.IsDevelopment() && uri.Scheme == Uri.UriSchemeHttp)))
        {
            await FailAttemptAsync(doc, httpStatus: null, "Webhook URL must be https (http allowed in Development).", scheduleRetry: false, cancellationToken);
            return;
        }

        var statusKey = await EnumKeyAsync(doc.PublicStatusEnumId, cancellationToken) ?? "unknown";
        var typeKey = doc.DocumentTypeId is long typeId
            ? await db.CorDocumentTypes.AsNoTracking()
                .Where(t => t.Id == typeId)
                .Select(t => t.DocumentTypeKey)
                .FirstOrDefaultAsync(cancellationToken)
            : null;
        var sourceKey = await EnumKeyAsync(file.SourceEnumId, cancellationToken);
        var ready = string.Equals(statusKey, "ready", StringComparison.Ordinal);

        JsonNode? data = null;
        if (ready && !string.IsNullOrWhiteSpace(doc.ResultJson))
        {
            try
            {
                data = JsonNode.Parse(doc.ResultJson);
            }
            catch (System.Text.Json.JsonException)
            {
                data = null;
            }
        }

        DocumentWebhookOriginalFile? original = null;
        if (!string.Equals(sourceKey, "api", StringComparison.Ordinal)
            && !string.Equals(sourceKey, "api_sync", StringComparison.Ordinal))
        {
            original = new DocumentWebhookOriginalFile(file.OriginalFileName, file.ContentType, file.SizeBytes);
        }

        var body = new DocumentWebhookBody(
            DocumentWebhookPayload.EventName,
            doc.Id.ToString("D"),
            doc.QueueId,
            doc.BatchId,
            doc.FileId,
            doc.Id,
            statusKey,
            typeKey,
            doc.AgentId,
            data,
            ready ? null : new DocumentWebhookError(doc.ErrorCode, doc.ErrorMessage),
            sourceKey,
            file.EmailMessageId,
            original,
            DateTimeOffset.UtcNow);

        var bytes = DocumentWebhookPayload.Serialize(body);
        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new ByteArrayContent(bytes)
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" } },
            },
        };
        request.Headers.TryAddWithoutValidation(DocumentWebhookPayload.EventHeader, DocumentWebhookPayload.EventName);
        request.Headers.TryAddWithoutValidation(DocumentWebhookPayload.DeliveryHeader, body.EventId);

        if (!string.IsNullOrWhiteSpace(queue.WebhookSecretProtected))
        {
            try
            {
                var secret = secrets.Unprotect(queue.WebhookSecretProtected);
                request.Headers.TryAddWithoutValidation(
                    DocumentWebhookPayload.SignatureHeader,
                    DocumentWebhookPayload.SignBody(secret, bytes));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not unprotect webhook secret for Queue {QueueId}; posting unsigned", queue.Id);
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
            doc.WebhookAttempts += 1;
            doc.WebhookLastAt = DateTimeOffset.UtcNow;
            doc.WebhookLastHttpStatus = httpStatus;
            doc.WebhookLastError = null;
            doc.WebhookStatusEnumId = succeeded;
            await db.SaveChangesAsync(cancellationToken);
            await AppendEventAsync(doc, enums.Require("work_event_type", "webhook_succeeded"), httpStatus, null, cancellationToken);
            logger.LogInformation("Webhook succeeded for Document {DocumentId} status={Status}", doc.Id, httpStatus);
            return;
        }

        await FailAttemptAsync(doc, httpStatus, error, scheduleRetry: true, cancellationToken);
    }

    private async Task FailAttemptAsync(
        OpsDocument doc,
        int? httpStatus,
        string error,
        bool scheduleRetry,
        CancellationToken cancellationToken)
    {
        doc.WebhookAttempts += 1;
        doc.WebhookLastAt = DateTimeOffset.UtcNow;
        doc.WebhookLastHttpStatus = httpStatus;
        doc.WebhookLastError = error.Length > 4000 ? error[..4000] : error;

        var exhausted = enums.Require("webhook_delivery_status", "exhausted");
        var pending = enums.Require("webhook_delivery_status", "pending");
        var failedEvent = enums.Require("work_event_type", "webhook_failed");
        var attempted = enums.Require("work_event_type", "webhook_attempted");

        var willRetry = scheduleRetry && doc.WebhookAttempts < MaxAttempts;
        doc.WebhookStatusEnumId = willRetry ? pending : exhausted;
        await db.SaveChangesAsync(cancellationToken);
        await AppendEventAsync(doc, willRetry ? attempted : failedEvent, httpStatus, error, cancellationToken);

        if (willRetry)
        {
            var delay = RetryDelays[Math.Min(doc.WebhookAttempts - 1, RetryDelays.Length - 1)];
            jobs.Schedule<WebhookJobs>(
                j => j.DeliverDocumentWebhookAsync(doc.Id, doc.BusinessId),
                delay);
            logger.LogWarning(
                "Webhook attempt {Attempt} failed for Document {DocumentId}: {Error}; retry in {Delay}",
                doc.WebhookAttempts,
                doc.Id,
                error,
                delay);
        }
        else
        {
            logger.LogWarning("Webhook exhausted for Document {DocumentId}: {Error}", doc.Id, error);
        }
    }

    private async Task AppendEventAsync(
        OpsDocument doc,
        long eventTypeId,
        int? httpStatus,
        string? error,
        CancellationToken cancellationToken)
    {
        db.OpsWorkEvents.Add(new OpsWorkEvent
        {
            BusinessId = doc.BusinessId,
            SubjectTypeEnumId = enums.Require("work_subject_type", "document"),
            SubjectId = doc.Id,
            EventTypeEnumId = eventTypeId,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                httpStatus,
                error,
                attempts = doc.WebhookAttempts,
            }),
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string?> EnumKeyAsync(long enumId, CancellationToken cancellationToken) =>
        await db.CorEnums.AsNoTracking().Where(e => e.Id == enumId).Select(e => e.EnumKey).FirstOrDefaultAsync(cancellationToken);
}
