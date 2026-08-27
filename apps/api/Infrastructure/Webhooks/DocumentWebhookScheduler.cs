namespace Documate.Api.Infrastructure.Webhooks;

using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Pipeline;
using Microsoft.EntityFrameworkCore;

public sealed class DocumentWebhookScheduler(
    DocumateDbContext db,
    ICorEnumIdResolver enums,
    IWebhookDispatcher dispatcher,
    ILogger<DocumentWebhookScheduler> logger) : IDocumentWebhookScheduler
{
    public async Task ScheduleIfTerminalAsync(
        OpsDocument document,
        OpsFile file,
        CancellationToken cancellationToken = default)
    {
        var ready = enums.Require("document_public_status", "ready");
        var failed = enums.Require("document_public_status", "failed");
        var rejected = enums.Require("document_public_status", "rejected");
        var cancelled = enums.Require("document_public_status", "cancelled");
        var status = document.PublicStatusEnumId;
        if (status != ready && status != failed && status != rejected && status != cancelled)
        {
            return;
        }

        var succeeded = enums.Require("webhook_delivery_status", "succeeded");
        var pending = enums.Require("webhook_delivery_status", "pending");
        var exhausted = enums.Require("webhook_delivery_status", "exhausted");
        var skipped = enums.Require("webhook_delivery_status", "skipped");
        var notConfigured = enums.Require("webhook_delivery_status", "not_configured");

        if (document.WebhookStatusEnumId is long current
            && (current == succeeded || current == pending || current == exhausted || current == skipped))
        {
            return;
        }

        var queue = await db.OpsQueues.AsNoTracking().FirstOrDefaultAsync(
            q => q.Id == document.QueueId && q.BusinessId == document.BusinessId && !q.IsDeleted,
            cancellationToken);
        if (queue is null)
        {
            logger.LogWarning("Queue {QueueId} missing for Document {DocumentId} webhook", document.QueueId, document.Id);
            return;
        }

        var sourceKey = await SourceKeyAsync(file.SourceEnumId, cancellationToken);
        if (string.Equals(sourceKey, "api_sync", StringComparison.Ordinal))
        {
            document.WebhookStatusEnumId = skipped;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (!queue.WebhookEnabled || string.IsNullOrWhiteSpace(queue.WebhookUrl))
        {
            document.WebhookStatusEnumId = notConfigured;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        document.WebhookStatusEnumId = pending;
        await db.SaveChangesAsync(cancellationToken);
        await dispatcher.EnqueueDocumentWebhookAsync(document.Id, document.BusinessId, cancellationToken);
        logger.LogInformation("Enqueued webhook for Document {DocumentId}", document.Id);
    }

    private async Task<string?> SourceKeyAsync(long sourceEnumId, CancellationToken cancellationToken)
    {
        return await db.CorEnums.AsNoTracking()
            .Where(e => e.Id == sourceEnumId)
            .Select(e => e.EnumKey)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
