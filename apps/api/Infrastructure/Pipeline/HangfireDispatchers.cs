namespace Documate.Api.Infrastructure.Pipeline;

using Documate.Api.Infrastructure.Webhooks;
using Hangfire;

/// <summary>Hangfire-backed File enqueue (Decision A1 + Hangfire SQL).</summary>
public sealed class HangfireWorkDispatcher(IBackgroundJobClient jobs) : IWorkDispatcher
{
    public ValueTask EnqueueFileAsync(FileWorkItem item, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        jobs.Enqueue<FilePipelineJobs>(j =>
            j.ProcessFileAsync(item.FileId, item.BusinessId, item.UserId));
        return ValueTask.CompletedTask;
    }
}

/// <summary>Hangfire enqueue onto the webhooks queue (DQ-0801).</summary>
public sealed class HangfireWebhookDispatcher(IBackgroundJobClient jobs) : IWebhookDispatcher
{
    public ValueTask EnqueueDocumentWebhookAsync(
        Guid documentId,
        string businessId,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        jobs.Enqueue<WebhookJobs>(j => j.DeliverDocumentWebhookAsync(documentId, businessId));
        return ValueTask.CompletedTask;
    }
}

public sealed class FilePipelineJobs(IFilePipelineStub stub)
{
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public Task ProcessFileAsync(Guid fileId, string businessId, string? userId) =>
        stub.ProcessAsync(new FileWorkItem(fileId, businessId, userId));
}

/// <summary>Per-Document HTTPS webhook (DQ-0801). Retries are self-scheduled; Hangfire auto-retry is off.</summary>
public sealed class WebhookJobs(DocumentWebhookDelivery delivery)
{
    [Queue("webhooks")]
    [AutomaticRetry(Attempts = 0)]
    public Task DeliverDocumentWebhookAsync(Guid documentId, string businessId) =>
        delivery.DeliverAsync(documentId, businessId);
}
