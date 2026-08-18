namespace Documate.Api.Infrastructure.Pipeline;

/// <summary>Work item enqueued after File intake (durable via Hangfire).</summary>
public sealed record FileWorkItem(
    Guid FileId,
    string BusinessId,
    string? UserId);

/// <summary>Non-blocking durable enqueue for File pipeline work.</summary>
public interface IWorkDispatcher
{
    /// <summary>Schedules processing after the File row is committed. Survives process restart.</summary>
    ValueTask EnqueueFileAsync(FileWorkItem item, CancellationToken cancellationToken = default);
}

/// <summary>Durable webhook job enqueue (DQ-0801). Same Hangfire backbone as File work.</summary>
public interface IWebhookDispatcher
{
    ValueTask EnqueueDocumentWebhookAsync(
        Guid documentId,
        string businessId,
        CancellationToken cancellationToken = default);
}
