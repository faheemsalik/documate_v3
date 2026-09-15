namespace Documate.Api.Infrastructure.EmailIntake;

using System.Diagnostics;
using System.Diagnostics.Metrics;

public interface IEmailIntakeMetrics
{
    void UnknownRecipient();
    void RateLimited();
    void Rejected(string code);
    void Accepted(int fileCount);
    void AllowlistPreferredMiss();
    void MimeDeleted(int count);
    IDisposable MeasureProcess();
}

public sealed class EmailIntakeMetrics : IEmailIntakeMetrics
{
    public const string MeterName = "Documate.EmailIntake";

    private readonly Counter<long> _unknownRecipient;
    private readonly Counter<long> _rateLimited;
    private readonly Counter<long> _rejected;
    private readonly Counter<long> _accepted;
    private readonly Counter<long> _filesAccepted;
    private readonly Counter<long> _allowlistPreferredMiss;
    private readonly Counter<long> _mimeDeleted;
    private readonly Histogram<double> _processDurationMs;

    public EmailIntakeMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _unknownRecipient = meter.CreateCounter<long>("email_intake.unknown_recipient");
        _rateLimited = meter.CreateCounter<long>("email_intake.rate_limited");
        _rejected = meter.CreateCounter<long>("email_intake.rejected");
        _accepted = meter.CreateCounter<long>("email_intake.accepted");
        _filesAccepted = meter.CreateCounter<long>("email_intake.files_accepted");
        _allowlistPreferredMiss = meter.CreateCounter<long>("email_intake.allowlist_preferred_miss");
        _mimeDeleted = meter.CreateCounter<long>("email_intake.mime_deleted");
        _processDurationMs = meter.CreateHistogram<double>("email_intake.process_duration_ms");
    }

    public void UnknownRecipient() => _unknownRecipient.Add(1);
    public void RateLimited() => _rateLimited.Add(1);
    public void Rejected(string code) => _rejected.Add(1, new KeyValuePair<string, object?>("code", code));
    public void Accepted(int fileCount)
    {
        _accepted.Add(1);
        _filesAccepted.Add(fileCount);
    }

    public void AllowlistPreferredMiss() => _allowlistPreferredMiss.Add(1);
    public void MimeDeleted(int count) => _mimeDeleted.Add(count);

    public IDisposable MeasureProcess() => new ProcessTimer(_processDurationMs);

    private sealed class ProcessTimer(Histogram<double> histogram) : IDisposable
    {
        private readonly long _start = Stopwatch.GetTimestamp();

        public void Dispose()
        {
            var elapsedMs = Stopwatch.GetElapsedTime(_start).TotalMilliseconds;
            histogram.Record(elapsedMs);
        }
    }
}
