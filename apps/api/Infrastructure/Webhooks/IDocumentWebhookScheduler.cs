namespace Documate.Api.Infrastructure.Webhooks;

using Documate.Api.Domain;

/// <summary>Marks webhook status and enqueues Hangfire delivery when a Document becomes terminal.</summary>
public interface IDocumentWebhookScheduler
{
    Task ScheduleIfTerminalAsync(OpsDocument document, OpsFile file, CancellationToken cancellationToken = default);
}
