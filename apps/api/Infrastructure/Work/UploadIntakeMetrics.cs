namespace Documate.Api.Infrastructure.Work;

using System.Diagnostics;
using System.Diagnostics.Metrics;

public interface IUploadIntakeMetrics
{
    void RecordAccept(int fileCount, double acceptMs, double blobMs, double dbMs);
}

public sealed class UploadIntakeMetrics : IUploadIntakeMetrics
{
    public const string MeterName = "Documate.UploadIntake";

    private readonly Histogram<double> _acceptMs;
    private readonly Histogram<double> _blobMs;
    private readonly Histogram<double> _dbMs;
    private readonly Counter<long> _filesAccepted;

    public UploadIntakeMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _acceptMs = meter.CreateHistogram<double>("upload.accept_ms");
        _blobMs = meter.CreateHistogram<double>("upload.blob_ms");
        _dbMs = meter.CreateHistogram<double>("upload.db_ms");
        _filesAccepted = meter.CreateCounter<long>("upload.files_accepted");
    }

    public void RecordAccept(int fileCount, double acceptMs, double blobMs, double dbMs)
    {
        var tags = new KeyValuePair<string, object?>("file_count", fileCount);
        _acceptMs.Record(acceptMs, tags);
        _blobMs.Record(blobMs, tags);
        _dbMs.Record(dbMs, tags);
        _filesAccepted.Add(fileCount);
    }
}

/// <summary>Stopwatch helpers for DQ-1402 intake timing.</summary>
public sealed class UploadIntakeTimer
{
    private readonly long _start = Stopwatch.GetTimestamp();
    private long _dbTicks;
    private long _blobTicks;

    public async Task<T> MeasureDbAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        var t0 = Stopwatch.GetTimestamp();
        try
        {
            return await action(cancellationToken);
        }
        finally
        {
            _dbTicks += Stopwatch.GetTimestamp() - t0;
        }
    }

    public async Task MeasureDbAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        var t0 = Stopwatch.GetTimestamp();
        try
        {
            await action(cancellationToken);
        }
        finally
        {
            _dbTicks += Stopwatch.GetTimestamp() - t0;
        }
    }

    public async Task MeasureBlobAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        var t0 = Stopwatch.GetTimestamp();
        try
        {
            await action(cancellationToken);
        }
        finally
        {
            _blobTicks += Stopwatch.GetTimestamp() - t0;
        }
    }

    public (double AcceptMs, double BlobMs, double DbMs) Elapsed()
    {
        var accept = Stopwatch.GetElapsedTime(_start).TotalMilliseconds;
        var blob = TicksToMs(_blobTicks);
        var db = TicksToMs(_dbTicks);
        return (accept, blob, db);
    }

    private static double TicksToMs(long ticks) =>
        ticks <= 0 ? 0 : ticks * 1000.0 / Stopwatch.Frequency;
}
