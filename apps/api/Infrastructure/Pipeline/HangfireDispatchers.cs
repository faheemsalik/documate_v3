namespace Documate.Api.Infrastructure.Pipeline;

using Documate.Api.Infrastructure.Webhooks;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;

/// <summary>Hangfire-backed File enqueue (Decision A1 + Hangfire SQL).</summary>
public sealed class HangfireWorkDispatcher(IBackgroundJobClient jobs) : IWorkDispatcher
{
    public ValueTask EnqueueFileAsync(FileWorkItem item, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var queue = string.Equals(item.Priority, "high", StringComparison.OrdinalIgnoreCase)
            ? "priority"
            : "default";
        var job = Job.FromExpression<FilePipelineJobs>(j =>
            j.ProcessFileAsync(item.FileId, item.BusinessId, item.UserId));
        jobs.Create(job, new EnqueuedState(queue));
        return ValueTask.CompletedTask;
    }
}

/// <summary>Hangfire enqueue onto the webhooks queue (DQ-0801 / Band 18).</summary>
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

    public ValueTask EnqueuePublicActionAsync(
        Guid deliveryId,
        string businessId,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        jobs.Enqueue<PublicActionJobs>(j => j.ExecutePublicActionAsync(deliveryId, businessId));
        return ValueTask.CompletedTask;
    }
}

public sealed class FilePipelineJobs(IFilePipelineStub stub)
{
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public Task ProcessFileAsync(Guid fileId, string businessId, string? userId) =>
        stub.ProcessAsync(new FileWorkItem(fileId, businessId, userId));
}

/// <summary>Per-Document HTTPS webhook (legacy path). Prefer PublicActionJobs.</summary>
public sealed class WebhookJobs(DocumentWebhookDelivery delivery)
{
    [Queue("webhooks")]
    [AutomaticRetry(Attempts = 0)]
    public Task DeliverDocumentWebhookAsync(Guid documentId, string businessId) =>
        delivery.DeliverAsync(documentId, businessId);
}

/// <summary>Public action execution (webhook / email / in_app) — Band 18.</summary>
public sealed class PublicActionJobs(Documate.Api.Infrastructure.PublicEvents.PublicActionExecutor executor)
{
    [Queue("webhooks")]
    [AutomaticRetry(Attempts = 0)]
    public Task ExecutePublicActionAsync(Guid deliveryId, string businessId) =>
        executor.ExecuteAsync(deliveryId, businessId);
}
