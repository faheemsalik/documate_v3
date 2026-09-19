namespace Documate.Api.Infrastructure.PublicEvents;

using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public interface IActionBindingBootstrap
{
    Task EnsureBusinessDefaultsAsync(string businessId, string? userId, CancellationToken cancellationToken = default);
    Task MigrateLegacyQueueWebhooksAsync(CancellationToken cancellationToken = default);
}

public sealed class ActionBindingBootstrap(DocumateDbContext db) : IActionBindingBootstrap
{
    public async Task EnsureBusinessDefaultsAsync(
        string businessId,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var exists = await db.OpsActionBindings.AnyAsync(
            b => b.BusinessId == businessId
                 && b.QueueId == null
                 && b.ActionTypeKey == PublicEventCatalog.ActionWebhook
                 && !b.IsDeleted,
            cancellationToken);
        if (exists)
        {
            return;
        }

        db.OpsActionBindings.Add(new OpsActionBinding
        {
            BusinessId = businessId,
            QueueId = null,
            ActionTypeKey = PublicEventCatalog.ActionWebhook,
            Enabled = true,
            ConfigJson = ActionBindingDefaults.EmptyConfigJson(),
            EventKeysJson = ActionBindingDefaults.DefaultWebhookEventKeysJson(),
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
        });

        // Platform support email on document.failed by default (DR-EA6).
        db.OpsActionBindings.Add(new OpsActionBinding
        {
            BusinessId = businessId,
            QueueId = null,
            ActionTypeKey = PublicEventCatalog.ActionEmail,
            Enabled = true,
            ConfigJson = """{"audience":"platform"}""",
            EventKeysJson = """["Document failed"]""",
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
        });

        db.OpsActionBindings.Add(new OpsActionBinding
        {
            BusinessId = businessId,
            QueueId = null,
            ActionTypeKey = PublicEventCatalog.ActionInApp,
            Enabled = true,
            ConfigJson = ActionBindingDefaults.EmptyConfigJson(),
            EventKeysJson = """["Document failed","File received"]""",
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MigrateLegacyQueueWebhooksAsync(CancellationToken cancellationToken = default)
    {
        var businesses = await db.OpsQueues.AsNoTracking()
            .Where(q => !q.IsDeleted)
            .Select(q => q.BusinessId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var businessId in businesses)
        {
            await EnsureBusinessDefaultsAsync(businessId, "system-migrate", cancellationToken);

            // Rewrite any legacy dotted event keys to human-friendly names.
            var bindings = await db.OpsActionBindings
                .Where(b => b.BusinessId == businessId && !b.IsDeleted)
                .ToListAsync(cancellationToken);
            foreach (var binding in bindings)
            {
                var canonical = ActionBindingResolver.ParseEventKeys(binding.EventKeysJson);
                var rewritten = System.Text.Json.JsonSerializer.Serialize(canonical);
                if (!string.Equals(binding.EventKeysJson, rewritten, StringComparison.Ordinal))
                {
                    binding.EventKeysJson = rewritten;
                    binding.UpdatedByUserId = "system-migrate";
                }
            }

            if (bindings.Count > 0)
            {
                await db.SaveChangesAsync(cancellationToken);
            }

            var webhook = await db.OpsActionBindings
                .FirstOrDefaultAsync(
                    b => b.BusinessId == businessId
                         && b.QueueId == null
                         && b.ActionTypeKey == PublicEventCatalog.ActionWebhook
                         && !b.IsDeleted,
                    cancellationToken);
            if (webhook is null)
            {
                continue;
            }

            // If Business webhook config empty, copy from first queue that has URL.
            if (string.IsNullOrWhiteSpace(webhook.ConfigJson) || webhook.ConfigJson == "{}")
            {
                var donor = await db.OpsQueues.AsNoTracking()
                    .Where(q => q.BusinessId == businessId && !q.IsDeleted && q.WebhookUrl != null)
                    .OrderByDescending(q => q.WebhookEnabled)
                    .ThenBy(q => q.SequenceId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (donor is not null)
                {
                    webhook.Enabled = donor.WebhookEnabled;
                    webhook.ConfigJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        url = donor.WebhookUrl,
                        secretProtected = donor.WebhookSecretProtected,
                    });
                    webhook.UpdatedByUserId = "system-migrate";
                    await db.SaveChangesAsync(cancellationToken);
                }
            }
        }
    }
}
