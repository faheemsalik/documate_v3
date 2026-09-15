namespace Documate.Api.Modules.PlatformAdmin.Features.Analytics;

using System.Globalization;
using System.Text.Json;
using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize(Policy = PlatformAdminAuth.PolicyName)]
[Route("api/admin/analytics")]
public sealed class AdminAnalyticsController(IMediator mediator) : ControllerBase
{
    [HttpGet("summary")]
    public Task<AdminAnalyticsSummaryDto> Summary(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] List<string>? businessIds = null,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new GetAdminAnalyticsSummaryQuery(from, to, businessIds), cancellationToken);

    [HttpGet("volume")]
    public Task<IReadOnlyList<AdminVolumePointDto>> Volume(
        [FromQuery] string granularity = "day",
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] List<string>? businessIds = null,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new GetAdminVolumeQuery(granularity, from, to, businessIds), cancellationToken);

    [HttpGet("by-business")]
    public Task<IReadOnlyList<AdminByBusinessRowDto>> ByBusiness(
        [FromQuery] string? month = null,
        [FromQuery] string? date = null,
        [FromQuery] List<string>? businessIds = null,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new GetAdminByBusinessQuery(month, date, businessIds), cancellationToken);

    [HttpGet("hourly")]
    public Task<IReadOnlyList<AdminHourlyPointDto>> Hourly(
        [FromQuery] string? date = null,
        [FromQuery] List<string>? businessIds = null,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new GetAdminHourlyQuery(date, businessIds), cancellationToken);

    [HttpGet("stage-timings")]
    public Task<AdminStageTimingsDto> StageTimings(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] List<string>? businessIds = null,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new GetAdminStageTimingsQuery(from, to, businessIds), cancellationToken);
}

public sealed record AdminAnalyticsSummaryDto(
    int Files,
    int Documents,
    double FailedFilesPct,
    double FailedDocumentsPct,
    double WebhookSuccessPct,
    double? AvgFileE2eMs,
    double? AvgDocE2eMs);

public sealed record AdminVolumePointDto(DateTimeOffset BucketStartUtc, int Files, int Documents);

public sealed record AdminByBusinessRowDto(
    string BusinessId,
    string BusinessName,
    string TenantName,
    int Files,
    int Documents,
    double FailedPct,
    int? PeakHourUtc);

public sealed record AdminHourlyPointDto(int HourUtc, int Files, int Documents);

public sealed record AdminStageTimingItemDto(string StageKey, double AvgDurationMs, int SampleCount);

public sealed record AdminStageTimingsDto(
    IReadOnlyList<AdminStageTimingItemDto> FileStages,
    IReadOnlyList<AdminStageTimingItemDto> DocumentStages,
    string Note);

public sealed record GetAdminAnalyticsSummaryQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    IReadOnlyList<string>? BusinessIds) : IRequest<AdminAnalyticsSummaryDto>;

public sealed record GetAdminVolumeQuery(
    string Granularity,
    DateTimeOffset? From,
    DateTimeOffset? To,
    IReadOnlyList<string>? BusinessIds) : IRequest<IReadOnlyList<AdminVolumePointDto>>;

public sealed record GetAdminByBusinessQuery(
    string? Month,
    string? Date,
    IReadOnlyList<string>? BusinessIds) : IRequest<IReadOnlyList<AdminByBusinessRowDto>>;

public sealed record GetAdminHourlyQuery(
    string? Date,
    IReadOnlyList<string>? BusinessIds) : IRequest<IReadOnlyList<AdminHourlyPointDto>>;

public sealed record GetAdminStageTimingsQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    IReadOnlyList<string>? BusinessIds) : IRequest<AdminStageTimingsDto>;

file static class AdminAnalyticsRange
{
    public static (DateTimeOffset From, DateTimeOffset To) Resolve(
        DateTimeOffset? from,
        DateTimeOffset? to,
        TimeSpan defaultWindow)
    {
        var end = to ?? DateTimeOffset.UtcNow;
        var start = from ?? end - defaultWindow;
        return (start, end);
    }

    public static bool TryParseUtcDate(string? date, out DateTimeOffset dayStart)
    {
        dayStart = default;
        if (string.IsNullOrWhiteSpace(date))
        {
            return false;
        }

        if (!DateOnly.TryParse(date.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
        {
            return false;
        }

        dayStart = new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        return true;
    }

    public static bool TryParseUtcMonth(string? month, out DateTimeOffset monthStart, out DateTimeOffset monthEnd)
    {
        monthStart = default;
        monthEnd = default;
        if (string.IsNullOrWhiteSpace(month))
        {
            return false;
        }

        if (!DateTime.TryParseExact(
                month.Trim(),
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var dt))
        {
            return false;
        }

        monthStart = new DateTimeOffset(dt.Year, dt.Month, 1, 0, 0, 0, TimeSpan.Zero);
        monthEnd = monthStart.AddMonths(1);
        return true;
    }
}

public sealed class GetAdminAnalyticsSummaryHandler(DocumateDbContext db, ICorEnumIdResolver enums)
    : IRequestHandler<GetAdminAnalyticsSummaryQuery, AdminAnalyticsSummaryDto>
{
    public async Task<AdminAnalyticsSummaryDto> Handle(
        GetAdminAnalyticsSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var (from, to) = AdminAnalyticsRange.Resolve(request.From, request.To, TimeSpan.FromDays(30));
        var fileFailed = enums.Require("file_public_status", "failed");
        var docFailed = enums.Require("document_public_status", "failed");
        var whSucceeded = enums.Require("webhook_delivery_status", "succeeded");

        var filesQ = db.OpsFiles.AsNoTracking()
            .Where(f => !f.IsDeleted && f.CreatedAt >= from && f.CreatedAt < to);
        var docsQ = db.OpsDocuments.AsNoTracking()
            .Where(d => !d.IsDeleted && d.CreatedAt >= from && d.CreatedAt < to);

        if (request.BusinessIds is { Count: > 0 } bids)
        {
            filesQ = filesQ.Where(f => bids.Contains(f.BusinessId));
            docsQ = docsQ.Where(d => bids.Contains(d.BusinessId));
        }

        var fileCount = await filesQ.CountAsync(cancellationToken);
        var fileFailedCount = await filesQ.CountAsync(f => f.PublicStatusEnumId == fileFailed, cancellationToken);
        var docCount = await docsQ.CountAsync(cancellationToken);
        var docFailedCount = await docsQ.CountAsync(d => d.PublicStatusEnumId == docFailed, cancellationToken);

        var webhookTotal = await docsQ.CountAsync(d => d.WebhookStatusEnumId != null, cancellationToken);
        var webhookOk = await docsQ.CountAsync(d => d.WebhookStatusEnumId == whSucceeded, cancellationToken);

        double? avgFile = null;
        var fileDurations = await filesQ
            .Where(f => f.CompletedAt != null)
            .Select(f => new { f.CreatedAt, CompletedAt = f.CompletedAt!.Value })
            .Take(5000)
            .ToListAsync(cancellationToken);
        if (fileDurations.Count > 0)
        {
            avgFile = fileDurations.Average(x => (x.CompletedAt - x.CreatedAt).TotalMilliseconds);
        }

        double? avgDoc = null;
        var docDurations = await docsQ
            .Where(d => d.CompletedAt != null)
            .Select(d => new { d.CreatedAt, CompletedAt = d.CompletedAt!.Value })
            .Take(5000)
            .ToListAsync(cancellationToken);
        if (docDurations.Count > 0)
        {
            avgDoc = docDurations.Average(x => (x.CompletedAt - x.CreatedAt).TotalMilliseconds);
        }

        return new AdminAnalyticsSummaryDto(
            fileCount,
            docCount,
            Pct(fileFailedCount, fileCount),
            Pct(docFailedCount, docCount),
            Pct(webhookOk, webhookTotal),
            avgFile,
            avgDoc);
    }

    private static double Pct(int part, int whole) =>
        whole <= 0 ? 0 : Math.Round(100.0 * part / whole, 2);
}

public sealed class GetAdminVolumeHandler(DocumateDbContext db)
    : IRequestHandler<GetAdminVolumeQuery, IReadOnlyList<AdminVolumePointDto>>
{
    public async Task<IReadOnlyList<AdminVolumePointDto>> Handle(
        GetAdminVolumeQuery request,
        CancellationToken cancellationToken)
    {
        var (from, to) = AdminAnalyticsRange.Resolve(request.From, request.To, TimeSpan.FromDays(14));
        var granularity = (request.Granularity ?? "day").Trim().ToLowerInvariant();

        var filesQ = db.OpsFiles.AsNoTracking()
            .Where(f => !f.IsDeleted && f.CreatedAt >= from && f.CreatedAt < to);
        var docsQ = db.OpsDocuments.AsNoTracking()
            .Where(d => !d.IsDeleted && d.CreatedAt >= from && d.CreatedAt < to);

        if (request.BusinessIds is { Count: > 0 } bids)
        {
            filesQ = filesQ.Where(f => bids.Contains(f.BusinessId));
            docsQ = docsQ.Where(d => bids.Contains(d.BusinessId));
        }

        var fileTimes = await filesQ.Select(f => f.CreatedAt).ToListAsync(cancellationToken);
        var docTimes = await docsQ.Select(d => d.CreatedAt).ToListAsync(cancellationToken);

        var buckets = new SortedDictionary<DateTimeOffset, (int Files, int Docs)>();

        void Add(DateTimeOffset ts, bool isFile)
        {
            var key = BucketStart(ts, granularity);
            buckets.TryGetValue(key, out var cur);
            buckets[key] = isFile ? (cur.Files + 1, cur.Docs) : (cur.Files, cur.Docs + 1);
        }

        foreach (var t in fileTimes) Add(t, true);
        foreach (var t in docTimes) Add(t, false);

        return buckets
            .Select(kv => new AdminVolumePointDto(kv.Key, kv.Value.Files, kv.Value.Docs))
            .ToList();
    }

    private static DateTimeOffset BucketStart(DateTimeOffset ts, string granularity)
    {
        var utc = ts.ToUniversalTime();
        return granularity switch
        {
            "hour" => new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, TimeSpan.Zero),
            "month" => new DateTimeOffset(utc.Year, utc.Month, 1, 0, 0, 0, TimeSpan.Zero),
            _ => new DateTimeOffset(utc.Year, utc.Month, utc.Day, 0, 0, 0, TimeSpan.Zero),
        };
    }
}

public sealed class GetAdminByBusinessHandler(DocumateDbContext db, ICorEnumIdResolver enums)
    : IRequestHandler<GetAdminByBusinessQuery, IReadOnlyList<AdminByBusinessRowDto>>
{
    public async Task<IReadOnlyList<AdminByBusinessRowDto>> Handle(
        GetAdminByBusinessQuery request,
        CancellationToken cancellationToken)
    {
        DateTimeOffset from;
        DateTimeOffset to;
        var usePeakHour = false;

        if (AdminAnalyticsRange.TryParseUtcDate(request.Date, out var dayStart))
        {
            from = dayStart;
            to = dayStart.AddDays(1);
            usePeakHour = true;
        }
        else if (AdminAnalyticsRange.TryParseUtcMonth(request.Month, out var mStart, out var mEnd))
        {
            from = mStart;
            to = mEnd;
        }
        else
        {
            var now = DateTimeOffset.UtcNow;
            from = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
            to = from.AddMonths(1);
        }

        var fileFailed = enums.Require("file_public_status", "failed");

        var filesQ = db.OpsFiles.AsNoTracking()
            .Where(f => !f.IsDeleted && f.CreatedAt >= from && f.CreatedAt < to);
        var docsQ = db.OpsDocuments.AsNoTracking()
            .Where(d => !d.IsDeleted && d.CreatedAt >= from && d.CreatedAt < to);

        if (request.BusinessIds is { Count: > 0 } bids)
        {
            filesQ = filesQ.Where(f => bids.Contains(f.BusinessId));
            docsQ = docsQ.Where(d => bids.Contains(d.BusinessId));
        }

        var fileStats = await filesQ
            .GroupBy(f => f.BusinessId)
            .Select(g => new
            {
                BusinessId = g.Key,
                Files = g.Count(),
                Failed = g.Count(f => f.PublicStatusEnumId == fileFailed),
            })
            .ToListAsync(cancellationToken);

        var docStats = await docsQ
            .GroupBy(d => d.BusinessId)
            .Select(g => new { BusinessId = g.Key, Documents = g.Count() })
            .ToDictionaryAsync(x => x.BusinessId, x => x.Documents, cancellationToken);

        Dictionary<string, int?> peakHours = new();
        if (usePeakHour)
        {
            var hourRows = await filesQ
                .Select(f => new { f.BusinessId, f.CreatedAt })
                .ToListAsync(cancellationToken);
            peakHours = hourRows
                .GroupBy(x => x.BusinessId)
                .ToDictionary(
                    g => g.Key,
                    g => (int?)g.GroupBy(x => x.CreatedAt.ToUniversalTime().Hour)
                        .OrderByDescending(h => h.Count())
                        .Select(h => h.Key)
                        .FirstOrDefault());
        }

        var businessIds = fileStats.Select(f => f.BusinessId)
            .Union(docStats.Keys)
            .Distinct()
            .ToList();

        var businesses = await db.CorTenantBusinesses.AsNoTracking()
            .Where(b => !b.IsDeleted && businessIds.Contains(b.IdenBusinessId))
            .Select(b => new { b.IdenBusinessId, b.Name, b.TenantName })
            .ToDictionaryAsync(b => b.IdenBusinessId, cancellationToken);

        return businessIds
            .Select(id =>
            {
                var fs = fileStats.FirstOrDefault(f => f.BusinessId == id);
                var files = fs?.Files ?? 0;
                var failed = fs?.Failed ?? 0;
                businesses.TryGetValue(id, out var biz);
                peakHours.TryGetValue(id, out var peak);
                return new AdminByBusinessRowDto(
                    id,
                    biz?.Name ?? id,
                    biz?.TenantName ?? "",
                    files,
                    docStats.GetValueOrDefault(id),
                    files <= 0 ? 0 : Math.Round(100.0 * failed / files, 2),
                    usePeakHour ? peak : null);
            })
            .OrderByDescending(r => r.Files)
            .ThenBy(r => r.BusinessName)
            .ToList();
    }
}

public sealed class GetAdminHourlyHandler(DocumateDbContext db)
    : IRequestHandler<GetAdminHourlyQuery, IReadOnlyList<AdminHourlyPointDto>>
{
    public async Task<IReadOnlyList<AdminHourlyPointDto>> Handle(
        GetAdminHourlyQuery request,
        CancellationToken cancellationToken)
    {
        if (!AdminAnalyticsRange.TryParseUtcDate(request.Date, out var dayStart))
        {
            dayStart = DateTimeOffset.UtcNow.Date;
            dayStart = new DateTimeOffset(dayStart.Date, TimeSpan.Zero);
        }

        var dayEnd = dayStart.AddDays(1);

        var filesQ = db.OpsFiles.AsNoTracking()
            .Where(f => !f.IsDeleted && f.CreatedAt >= dayStart && f.CreatedAt < dayEnd);
        var docsQ = db.OpsDocuments.AsNoTracking()
            .Where(d => !d.IsDeleted && d.CreatedAt >= dayStart && d.CreatedAt < dayEnd);

        if (request.BusinessIds is { Count: > 0 } bids)
        {
            filesQ = filesQ.Where(f => bids.Contains(f.BusinessId));
            docsQ = docsQ.Where(d => bids.Contains(d.BusinessId));
        }

        var fileHours = await filesQ.Select(f => f.CreatedAt).ToListAsync(cancellationToken);
        var docHours = await docsQ.Select(d => d.CreatedAt).ToListAsync(cancellationToken);

        var counts = Enumerable.Range(0, 24).ToDictionary(h => h, _ => (Files: 0, Docs: 0));
        foreach (var t in fileHours)
        {
            var h = t.ToUniversalTime().Hour;
            var cur = counts[h];
            counts[h] = (cur.Files + 1, cur.Docs);
        }

        foreach (var t in docHours)
        {
            var h = t.ToUniversalTime().Hour;
            var cur = counts[h];
            counts[h] = (cur.Files, cur.Docs + 1);
        }

        return counts
            .OrderBy(kv => kv.Key)
            .Select(kv => new AdminHourlyPointDto(kv.Key, kv.Value.Files, kv.Value.Docs))
            .ToList();
    }
}

public sealed class GetAdminStageTimingsHandler(DocumateDbContext db, ICorEnumIdResolver enums)
    : IRequestHandler<GetAdminStageTimingsQuery, AdminStageTimingsDto>
{
    public async Task<AdminStageTimingsDto> Handle(
        GetAdminStageTimingsQuery request,
        CancellationToken cancellationToken)
    {
        var (from, to) = AdminAnalyticsRange.Resolve(request.From, request.To, TimeSpan.FromDays(7));
        var statusChanged = enums.Require("work_event_type", "status_changed");
        var fileSubject = enums.Require("work_subject_type", "file");
        var docSubject = enums.Require("work_subject_type", "document");

        var eventsQ = db.OpsWorkEvents.AsNoTracking()
            .Where(e => !e.IsDeleted
                        && e.EventTypeEnumId == statusChanged
                        && e.CreatedAt >= from
                        && e.CreatedAt < to
                        && e.PayloadJson != null);

        if (request.BusinessIds is { Count: > 0 } bids)
        {
            eventsQ = eventsQ.Where(e => bids.Contains(e.BusinessId));
        }

        var rows = await eventsQ
            .OrderBy(e => e.SubjectId)
            .ThenBy(e => e.CreatedAt)
            .Select(e => new
            {
                e.SubjectId,
                e.SubjectTypeEnumId,
                e.CreatedAt,
                e.PayloadJson,
            })
            .Take(20000)
            .ToListAsync(cancellationToken);

        var fileDurations = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);
        var docDurations = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in rows.GroupBy(r => (r.SubjectId, r.SubjectTypeEnumId)))
        {
            var ordered = group.OrderBy(x => x.CreatedAt).ToList();
            for (var i = 0; i < ordered.Count - 1; i++)
            {
                var stage = ReadStage(ordered[i].PayloadJson);
                if (string.IsNullOrWhiteSpace(stage))
                {
                    continue;
                }

                var ms = (ordered[i + 1].CreatedAt - ordered[i].CreatedAt).TotalMilliseconds;
                if (ms < 0 || ms > TimeSpan.FromHours(12).TotalMilliseconds)
                {
                    continue;
                }

                var bag = group.Key.SubjectTypeEnumId == fileSubject ? fileDurations
                    : group.Key.SubjectTypeEnumId == docSubject ? docDurations
                    : null;
                if (bag is null)
                {
                    continue;
                }

                if (!bag.TryGetValue(stage, out var list))
                {
                    list = [];
                    bag[stage] = list;
                }

                list.Add(ms);
            }
        }

        return new AdminStageTimingsDto(
            ToItems(fileDurations),
            ToItems(docDurations),
            "Approximate — derived from consecutive status_changed work events (compute-on-read).");
    }

    private static string? ReadStage(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.TryGetProperty("stage", out var stage) && stage.ValueKind == JsonValueKind.String)
            {
                return stage.GetString();
            }
        }
        catch (JsonException)
        {
            // ignore malformed payloads
        }

        return null;
    }

    private static IReadOnlyList<AdminStageTimingItemDto> ToItems(Dictionary<string, List<double>> map) =>
        map
            .Select(kv => new AdminStageTimingItemDto(
                kv.Key,
                Math.Round(kv.Value.Average(), 1),
                kv.Value.Count))
            .OrderByDescending(x => x.AvgDurationMs)
            .ToList();
}
