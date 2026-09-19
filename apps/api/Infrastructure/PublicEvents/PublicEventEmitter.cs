namespace Documate.Api.Infrastructure.PublicEvents;

using System.Text.Json;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Pipeline;
using Microsoft.EntityFrameworkCore;

public sealed record PublicEventEmitRequest(
    string EventName,
    string EventId,
    string BusinessId,
    Guid QueueId,
    string ResourceTypeKey,
    Guid ResourceId,
    string? SourceKey,
    string PayloadJson);

public interface IPublicEventEmitter
{
    Task EmitAsync(PublicEventEmitRequest request, CancellationToken cancellationToken = default);
}

public sealed class PublicEventEmitter(
    DocumateDbContext db,
    ICorEnumIdResolver enums,
    IActionBindingResolver resolver,
    IWebhookDispatcher dispatcher,
    ILogger<PublicEventEmitter> logger) : IPublicEventEmitter
{
    public async Task EmitAsync(PublicEventEmitRequest request, CancellationToken cancellationToken = default)
    {
        if (string.Equals(request.SourceKey, "api_sync", StringComparison.Ordinal))
        {
            logger.LogDebug("Skip public event {Event} for api_sync resource {ResourceId}", request.EventName, request.ResourceId);
            return;
        }

        var bindings = await resolver.ResolveAsync(request.BusinessId, request.QueueId, request.EventName, cancellationToken);
        if (bindings.Count == 0)
        {
            if (string.Equals(request.ResourceTypeKey, PublicEventCatalog.ResourceDocument, StringComparison.Ordinal))
            {
                await MarkDocumentNotConfiguredAsync(request, cancellationToken);
            }

            return;
        }

        var pending = enums.Require("webhook_delivery_status", "pending");
        var created = new List<Guid>();

        foreach (var resolved in bindings)
        {
            var binding = resolved.Binding;
            var existing = await db.OpsOutboundDeliveries.AsNoTracking()
                .FirstOrDefaultAsync(
                    d => d.BusinessId == request.BusinessId
                         && d.EventId == request.EventId
                         && d.ActionTypeKey == binding.ActionTypeKey
                         && (binding.Id == Guid.Empty
                             ? d.ActionBindingId == null
                             : d.ActionBindingId == binding.Id)
                         && !d.IsDeleted,
                    cancellationToken);
            if (existing is not null)
            {
                var succeeded = enums.Require("webhook_delivery_status", "succeeded");
                if (existing.StatusEnumId == succeeded)
                {
                    continue;
                }

                created.Add(existing.Id);
                continue;
            }

            var delivery = new OpsOutboundDelivery
            {
                BusinessId = request.BusinessId,
                ActionBindingId = binding.Id == Guid.Empty ? null : binding.Id,
                ActionTypeKey = binding.ActionTypeKey,
                EventName = request.EventName,
                EventId = request.EventId,
                ResourceTypeKey = request.ResourceTypeKey,
                ResourceId = request.ResourceId,
                QueueId = request.QueueId,
                StatusEnumId = pending,
                Attempts = 0,
                PayloadJson = request.PayloadJson,
            };
            db.OpsOutboundDeliveries.Add(delivery);
            await db.SaveChangesAsync(cancellationToken);
            created.Add(delivery.Id);
        }

        if (string.Equals(request.ResourceTypeKey, PublicEventCatalog.ResourceDocument, StringComparison.Ordinal)
            && created.Count > 0
            && bindings.Any(b => b.Binding.ActionTypeKey == PublicEventCatalog.ActionWebhook))
        {
            var doc = await db.OpsDocuments.FirstOrDefaultAsync(
                d => d.Id == request.ResourceId && d.BusinessId == request.BusinessId && !d.IsDeleted,
                cancellationToken);
            if (doc is not null)
            {
                doc.WebhookStatusEnumId = pending;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        foreach (var id in created.Distinct())
        {
            await dispatcher.EnqueuePublicActionAsync(id, request.BusinessId, cancellationToken);
        }

        logger.LogInformation(
            "Emitted public event {Event} eventId={EventId} actions={Count}",
            request.EventName,
            request.EventId,
            created.Count);
    }

    private async Task MarkDocumentNotConfiguredAsync(PublicEventEmitRequest request, CancellationToken cancellationToken)
    {
        var doc = await db.OpsDocuments.FirstOrDefaultAsync(
            d => d.Id == request.ResourceId && d.BusinessId == request.BusinessId && !d.IsDeleted,
            cancellationToken);
        if (doc is null)
        {
            return;
        }

        var notConfigured = enums.Require("webhook_delivery_status", "not_configured");
        if (doc.WebhookStatusEnumId == notConfigured)
        {
            return;
        }

        doc.WebhookStatusEnumId = notConfigured;
        await db.SaveChangesAsync(cancellationToken);
    }
}
