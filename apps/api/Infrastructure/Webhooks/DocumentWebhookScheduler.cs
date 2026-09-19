namespace Documate.Api.Infrastructure.Webhooks;

using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.PublicEvents;
using Microsoft.EntityFrameworkCore;

/// <summary>Schedules typed public document events (Band 18) instead of hard-coded document.terminal.</summary>
public sealed class DocumentWebhookScheduler(
    DocumateDbContext db,
    ICorEnumIdResolver enums,
    IPublicEventEmitter emitter,
    IPublicEventPayloadBuilder payloads,
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

        if (document.WebhookStatusEnumId is long current
            && (current == succeeded || current == pending || current == exhausted || current == skipped))
        {
            return;
        }

        var sourceKey = await db.CorEnums.AsNoTracking()
            .Where(e => e.Id == file.SourceEnumId)
            .Select(e => e.EnumKey)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.Equals(sourceKey, "api_sync", StringComparison.Ordinal))
        {
            document.WebhookStatusEnumId = skipped;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var statusKey = await db.CorEnums.AsNoTracking()
            .Where(e => e.Id == document.PublicStatusEnumId)
            .Select(e => e.EnumKey)
            .FirstOrDefaultAsync(cancellationToken) ?? "failed";

        var eventName = PublicEventCatalog.DocumentEventForPublicStatus(statusKey);
        var eventId = PublicEventCatalog.EventIdForDocument(document.Id, eventName);
        var payloadJson = await payloads.BuildDocumentPayloadAsync(document, file, eventName, eventId, cancellationToken);

        await emitter.EmitAsync(
            new PublicEventEmitRequest(
                eventName,
                eventId,
                document.BusinessId,
                document.QueueId,
                PublicEventCatalog.ResourceDocument,
                document.Id,
                sourceKey,
                payloadJson),
            cancellationToken);

        logger.LogInformation("Emitted {Event} for Document {DocumentId}", eventName, document.Id);

        if (status == ready)
        {
            await TryEmitFileCompletedAsync(document, file, sourceKey, cancellationToken);
        }
    }

    private async Task TryEmitFileCompletedAsync(
        OpsDocument document,
        OpsFile file,
        string? sourceKey,
        CancellationToken cancellationToken)
    {
        var ready = enums.Require("document_public_status", "ready");
        var siblings = await db.OpsDocuments.AsNoTracking()
            .Where(d => d.FileId == document.FileId && d.BusinessId == document.BusinessId && !d.IsDeleted)
            .Select(d => d.PublicStatusEnumId)
            .ToListAsync(cancellationToken);
        if (siblings.Count == 0 || siblings.Any(s => s != ready))
        {
            return;
        }

        var eventName = PublicEventCatalog.FileCompleted;
        var eventId = PublicEventCatalog.EventIdForFile(file.Id, eventName);
        var payloadJson = await payloads.BuildFilePayloadAsync(file, eventName, eventId, cancellationToken);
        await emitter.EmitAsync(
            new PublicEventEmitRequest(
                eventName,
                eventId,
                file.BusinessId,
                file.QueueId,
                PublicEventCatalog.ResourceFile,
                file.Id,
                sourceKey,
                payloadJson),
            cancellationToken);
    }
}
