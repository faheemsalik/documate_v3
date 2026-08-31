namespace Documate.Api.Infrastructure.Notifications;

public sealed record OpsLlmFailureAlert(
    string BusinessId,
    Guid FileId,
    Guid DocumentId,
    Guid? QueueId,
    string ProviderKey,
    string ErrorCode,
    string ErrorMessage,
    DateTimeOffset OccurredAtUtc);

public interface IOpsAlertSender
{
    /// <summary>Best-effort notify. Must not throw to callers (DR1-B).</summary>
    Task NotifyLlmExtractFailedAsync(OpsLlmFailureAlert alert, CancellationToken cancellationToken = default);
}
