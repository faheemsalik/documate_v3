namespace Documate.Api.Infrastructure.PublicEvents;

using System.Text.Json;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed record ResolvedActionBinding(
    OpsActionBinding Binding,
    IReadOnlyList<string> EventKeys);

public interface IActionBindingResolver
{
    Task<IReadOnlyList<ResolvedActionBinding>> ResolveAsync(
        string businessId,
        Guid queueId,
        string eventName,
        CancellationToken cancellationToken = default);
}

public sealed class ActionBindingResolver(DocumateDbContext db) : IActionBindingResolver
{
    public async Task<IReadOnlyList<ResolvedActionBinding>> ResolveAsync(
        string businessId,
        Guid queueId,
        string eventName,
        CancellationToken cancellationToken = default)
    {
        var queue = await db.OpsQueues.AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == queueId && q.BusinessId == businessId && !q.IsDeleted, cancellationToken);
        if (queue is null)
        {
            return [];
        }

        List<OpsActionBinding> rows;
        if (queue.PublicActionsInherit)
        {
            rows = await db.OpsActionBindings.AsNoTracking()
                .Where(b => b.BusinessId == businessId && b.QueueId == null && b.Enabled && !b.IsDeleted)
                .ToListAsync(cancellationToken);
        }
        else
        {
            rows = await db.OpsActionBindings.AsNoTracking()
                .Where(b => b.BusinessId == businessId && b.QueueId == queueId && b.Enabled && !b.IsDeleted)
                .ToListAsync(cancellationToken);

            // Fallback: if override mode but no queue rows yet, use legacy queue webhook columns via synthetic binding.
            if (rows.Count == 0 && queue.WebhookEnabled && !string.IsNullOrWhiteSpace(queue.WebhookUrl))
            {
                return
                [
                    new ResolvedActionBinding(
                        new OpsActionBinding
                        {
                            Id = Guid.Empty,
                            BusinessId = businessId,
                            QueueId = queueId,
                            ActionTypeKey = PublicEventCatalog.ActionWebhook,
                            Enabled = true,
                            ConfigJson = JsonSerializer.Serialize(new
                            {
                                url = queue.WebhookUrl,
                                secretProtected = queue.WebhookSecretProtected,
                            }),
                            EventKeysJson = ActionBindingDefaults.DefaultWebhookEventKeysJson(),
                        },
                        PublicEventCatalog.DefaultBusinessWebhookEvents),
                ];
            }
        }

        var matched = new List<ResolvedActionBinding>();
        foreach (var row in rows)
        {
            var keys = ParseEventKeys(row.EventKeysJson);
            if (keys.Any(k => PublicEventCatalog.EventEquals(k, eventName)))
            {
                matched.Add(new ResolvedActionBinding(row, keys));
            }
        }

        // Inherit mode: also honor legacy OpsQueue webhook if no Business webhook binding exists yet.
        if (queue.PublicActionsInherit
            && matched.All(m => m.Binding.ActionTypeKey != PublicEventCatalog.ActionWebhook)
            && queue.WebhookEnabled
            && !string.IsNullOrWhiteSpace(queue.WebhookUrl))
        {
            var legacyKeys = PublicEventCatalog.DefaultBusinessWebhookEvents;
            if (legacyKeys.Any(k => PublicEventCatalog.EventEquals(k, eventName)))
            {
                matched.Add(new ResolvedActionBinding(
                    new OpsActionBinding
                    {
                        Id = Guid.Empty,
                        BusinessId = businessId,
                        QueueId = queueId,
                        ActionTypeKey = PublicEventCatalog.ActionWebhook,
                        Enabled = true,
                        ConfigJson = JsonSerializer.Serialize(new
                        {
                            url = queue.WebhookUrl,
                            secretProtected = queue.WebhookSecretProtected,
                        }),
                        EventKeysJson = ActionBindingDefaults.DefaultWebhookEventKeysJson(),
                    },
                    legacyKeys));
            }
        }

        return matched;
    }

    public static IReadOnlyList<string> ParseEventKeys(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var raw = JsonSerializer.Deserialize<List<string>>(json) ?? [];
            return raw
                .Select(PublicEventCatalog.Canonicalize)
                .Where(k => k.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
