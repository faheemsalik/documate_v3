namespace Documate.Api.Infrastructure.Options;

public sealed class PipelineOptions
{
    public const string SectionName = "Pipeline";

    /// <summary>Max concurrent stub File pipelines (A1 in-process).</summary>
    public int MaxConcurrentFiles { get; set; } = 4;

    /// <summary>
    /// Artificial delay per stub stage (ms). Prefer 0 for realtime; use a small value only for smoke observability.
    /// </summary>
    public int StubStageDelayMs { get; set; }

    /// <summary>Decision G: max seconds the sync-wait HTTP call blocks.</summary>
    public int SyncWaitTimeoutSeconds { get; set; } = 60;
}
