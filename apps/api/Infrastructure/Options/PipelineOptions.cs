namespace Documate.Api.Infrastructure.Options;

public sealed class PipelineOptions
{
    public const string SectionName = "Pipeline";

    /// <summary>
    /// Max concurrent File pipeline jobs (Hangfire workers on priority/default queues).
    /// Shared across all businesses on this API process — raise for multi-tenant throughput;
    /// watch OCR/LLM provider rate limits and SQL load.
    /// </summary>
    public int MaxConcurrentFiles { get; set; } = 12;

    /// <summary>
    /// Dedicated Hangfire workers for the webhooks queue (isolated from file OCR workers).
    /// </summary>
    public int MaxConcurrentWebhooks { get; set; } = 4;

    /// <summary>
    /// Artificial delay per stub stage (ms). Prefer 0 for realtime; use a small value only for smoke observability.
    /// </summary>
    public int StubStageDelayMs { get; set; }

    /// <summary>Decision G: max seconds the sync-wait HTTP call blocks.</summary>
    public int SyncWaitTimeoutSeconds { get; set; } = 60;

    /// <summary>Sync extract: reject when estimated pages exceed this (default 3).</summary>
    public int SyncMaxPages { get; set; } = 3;

    /// <summary>Sync extract: reject when file size exceeds this (default 5 MB).</summary>
    public long SyncMaxBytes { get; set; } = 5_242_880;
}
