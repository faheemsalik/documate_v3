namespace Documate.Api.Infrastructure.EmailIntake;

using System.Collections.Concurrent;
using Documate.Api.Infrastructure.Settings;

public interface IEmailIntakeRateLimiter
{
    /// <summary>Returns false when the mailbox has exceeded configured rate limits.</summary>
    bool TryAcquire(Guid mailboxId);
}

/// <summary>In-process sliding-window limiter (per API host). Sufficient for Phase 1 single-host Hangfire.</summary>
public sealed class MemoryEmailIntakeRateLimiter(IEmailIntakeSettings emailIntakeSettings) : IEmailIntakeRateLimiter
{
    private readonly ConcurrentDictionary<Guid, Window> _windows = new();

    public bool TryAcquire(Guid mailboxId)
    {
        var opts = emailIntakeSettings.Current;
        var perMinute = Math.Max(0, opts.RateLimitPerMailboxPerMinute);
        var perHour = Math.Max(0, opts.RateLimitPerMailboxPerHour);
        if (perMinute == 0 && perHour == 0)
        {
            return true;
        }

        var now = DateTimeOffset.UtcNow;
        var window = _windows.GetOrAdd(mailboxId, _ => new Window());
        lock (window.Gate)
        {
            window.Prune(now);
            if (perMinute > 0 && window.CountSince(now.AddMinutes(-1)) >= perMinute)
            {
                return false;
            }

            if (perHour > 0 && window.CountSince(now.AddHours(-1)) >= perHour)
            {
                return false;
            }

            window.Hits.Add(now);
            return true;
        }
    }

    private sealed class Window
    {
        public object Gate { get; } = new();
        public List<DateTimeOffset> Hits { get; } = [];

        public void Prune(DateTimeOffset now)
        {
            var cutoff = now.AddHours(-1);
            Hits.RemoveAll(t => t < cutoff);
        }

        public int CountSince(DateTimeOffset since) => Hits.Count(t => t >= since);
    }
}
