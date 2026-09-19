namespace Documate.Api.Infrastructure.PublicEvents;

using Documate.Api.Infrastructure.Persistence;

/// <summary>One-shot migrate legacy queue webhooks into Business action bindings after schema deploy.</summary>
public sealed class ActionBindingMigrateHostedService(
    IServiceScopeFactory scopes,
    ILogger<ActionBindingMigrateHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var bootstrap = scope.ServiceProvider.GetRequiredService<IActionBindingBootstrap>();
            await bootstrap.MigrateLegacyQueueWebhooksAsync(cancellationToken);
            logger.LogInformation("Action binding legacy webhook migration completed");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Action binding migration skipped/failed (will retry next start)");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
