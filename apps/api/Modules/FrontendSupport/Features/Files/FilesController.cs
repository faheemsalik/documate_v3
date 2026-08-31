namespace Documate.Api.Modules.FrontendSupport.Features.Files;

using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Pipeline;
using Documate.Api.Infrastructure.Storage;
using Documate.Api.Infrastructure.Work;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// App-side file intake smoke path (Wave 4). Partner External upload is DQ-0601.
/// </summary>
[ApiController]
[Authorize]
[Route("api/app/queues/{queueId:guid}/files")]
public sealed class FilesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedFileListDto>> List(
        Guid queueId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] List<Guid>? ids = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? batchId = null,
        [FromQuery] DateTimeOffset? createdFrom = null,
        [FromQuery] DateTimeOffset? createdTo = null,
        CancellationToken cancellationToken = default)
    {
        var dto = await mediator.Send(
            new ListAppFilesQuery(
                queueId,
                page,
                pageSize,
                ids is { Count: > 0 } ? ids : null,
                status,
                batchId,
                createdFrom,
                createdTo),
            cancellationToken);
        return Ok(dto);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<FileSummaryDto>> Summary(Guid queueId, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetAppFilesSummaryQuery(queueId), cancellationToken);
        return Ok(dto);
    }

    /// <summary>Schema field search (A4 / CUS-16). Top-level scalar JSON fields only; index follow-up for scale.</summary>
    [HttpGet("search")]
    public async Task<ActionResult<PagedFileSchemaSearchDto>> Search(
        Guid queueId,
        [FromQuery] string fieldKey,
        [FromQuery] string value,
        [FromQuery] string? matchMode = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!AppFileSchemaSearch.TryBuildJsonPath(fieldKey, out var jsonPath, out var fieldError))
        {
            return BadRequest(new { error = fieldError });
        }

        if (!AppFileSchemaSearch.TryNormalizeSearchValue(value, out var searchValue, out var valueError))
        {
            return BadRequest(new { error = valueError });
        }

        var dto = await mediator.Send(
            new SearchAppFilesBySchemaQuery(
                queueId,
                fieldKey.Trim(),
                jsonPath,
                searchValue,
                AppFileSchemaSearch.NormalizeMatchMode(matchMode),
                page,
                pageSize),
            cancellationToken);
        return Ok(dto);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(52_428_800)]
    public async Task<ActionResult<FileDto>> Upload(
        Guid queueId,
        IFormFile file,
        [FromForm] string? documentTypeKey,
        [FromForm] int? documentCount,
        [FromForm] string? priority,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "file is required" });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var dto = await mediator.Send(
                new UploadFileCommand(
                    queueId,
                    file.FileName,
                    file.ContentType,
                    stream,
                    file.Length,
                    documentTypeKey,
                    documentCount,
                    priority),
                cancellationToken);
            return CreatedAtAction(nameof(Get), new { queueId, fileId = dto.Id }, dto);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Queue not found", StringComparison.Ordinal))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("documentTypeKey", StringComparison.Ordinal)
            || ex.Message.Contains("priority", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{fileId:guid}")]
    public async Task<ActionResult<FileDto>> Get(Guid queueId, Guid fileId, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetFileQuery(queueId, fileId), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("{fileId:guid}/download-url")]
    public async Task<ActionResult<FileDownloadUrlDto>> DownloadUrl(
        Guid queueId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetFileDownloadUrlQuery(queueId, fileId), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("{fileId:guid}/documents")]
    public async Task<ActionResult<IReadOnlyList<DocumentListItemDto>>> ListDocuments(
        Guid queueId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var items = await mediator.Send(new ListAppFileDocumentsQuery(queueId, fileId), cancellationToken);
        return items is null ? NotFound() : Ok(items);
    }
}

public sealed record FileDto(
    Guid Id,
    Guid QueueId,
    Guid? BatchId,
    string? OriginalFileName,
    string? ContentType,
    long SizeBytes,
    string StorageKey,
    string? StorageBucket,
    long PublicStatusEnumId,
    string? PublicStatusKey,
    long? InternalStageEnumId,
    string? InternalStageKey,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    int DocumentCount);

public sealed record FileListItemDto(
    Guid Id,
    Guid QueueId,
    Guid? BatchId,
    string? OriginalFileName,
    string? ContentType,
    long SizeBytes,
    string? PublicStatusKey,
    string? InternalStageKey,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    int DocumentCount);

public sealed record PagedFileListDto(
    IReadOnlyList<FileListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record FileSummaryDto(int Total, IReadOnlyDictionary<string, int> ByPublicStatus);

public sealed record FileSchemaSearchItemDto(
    Guid FileId,
    Guid DocumentId,
    string? OriginalFileName,
    string? PublicStatusKey,
    string? DocumentTypeKey,
    string FieldKey,
    string? MatchedValue,
    DateTimeOffset FileCreatedAt);

public sealed record PagedFileSchemaSearchDto(
    IReadOnlyList<FileSchemaSearchItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    string FieldKey,
    string SearchValue,
    string MatchMode);

public sealed record FileDownloadUrlDto(Guid FileId, string Url);

public sealed record UploadFileCommand(
    Guid QueueId,
    string FileName,
    string? ContentType,
    Stream Content,
    long SizeBytes,
    string? DocumentTypeKey,
    int? DocumentCount,
    string? Priority = null) : IRequest<FileDto>;

public sealed record GetFileQuery(Guid QueueId, Guid FileId) : IRequest<FileDto?>;
public sealed record GetFileDownloadUrlQuery(Guid QueueId, Guid FileId) : IRequest<FileDownloadUrlDto?>;

public sealed record ListAppFilesQuery(
    Guid QueueId,
    int Page,
    int PageSize,
    IReadOnlyList<Guid>? Ids,
    string? StatusKey,
    Guid? BatchId,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo) : IRequest<PagedFileListDto>;

public sealed record GetAppFilesSummaryQuery(Guid QueueId) : IRequest<FileSummaryDto>;

public sealed record SearchAppFilesBySchemaQuery(
    Guid QueueId,
    string FieldKey,
    string JsonPath,
    string SearchValue,
    string MatchMode,
    int Page,
    int PageSize) : IRequest<PagedFileSchemaSearchDto>;

public sealed class UploadFileHandler(
    IWorkRecordService work,
    IWorkDispatcher dispatcher,
    IBusinessContext business,
    ICorEnumIdResolver enums,
    DocumateDbContext db) : IRequestHandler<UploadFileCommand, FileDto>
{
    public async Task<FileDto> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        var sourceId = enums.Require("intake_source", "api");
        if (!string.IsNullOrWhiteSpace(request.DocumentTypeKey))
        {
            var exists = await db.CorDocumentTypes.AsNoTracking().AnyAsync(
                d => d.DocumentTypeKey == request.DocumentTypeKey.Trim() && d.IsActive && !d.IsDeleted,
                cancellationToken);
            if (!exists)
            {
                throw new InvalidOperationException($"Unknown documentTypeKey '{request.DocumentTypeKey}'.");
            }
        }

        var file = await work.CreateFileWithBlobAsync(
            new CreateFileWithBlobRequest(
                request.QueueId,
                BatchId: null,
                sourceId,
                request.FileName,
                request.ContentType,
                request.Content,
                request.SizeBytes,
                IntakeHints.Serialize(request.DocumentTypeKey, request.DocumentCount)),
            cancellationToken);

        var priority = NormalizePriority(request.Priority);
        await dispatcher.EnqueueFileAsync(
            new FileWorkItem(file.Id, business.BusinessId, business.UserId, priority),
            cancellationToken);

        return await FileDtoMapping.ToDto(db, file, cancellationToken);
    }

    private static string NormalizePriority(string? priority)
    {
        if (string.IsNullOrWhiteSpace(priority) || priority.Equals("normal", StringComparison.OrdinalIgnoreCase))
        {
            return "normal";
        }

        if (priority.Equals("high", StringComparison.OrdinalIgnoreCase))
        {
            return "high";
        }

        throw new InvalidOperationException("priority must be 'normal' or 'high'.");
    }
}

public sealed class GetFileHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<GetFileQuery, FileDto?>
{
    public async Task<FileDto?> Handle(GetFileQuery request, CancellationToken cancellationToken)
    {
        var file = await db.OpsFiles.AsNoTracking().FirstOrDefaultAsync(
            f => f.Id == request.FileId
                 && f.QueueId == request.QueueId
                 && f.BusinessId == business.BusinessId
                 && !f.IsDeleted,
            cancellationToken);
        return file is null ? null : await FileDtoMapping.ToDto(db, file, cancellationToken);
    }
}

public sealed class ListAppFilesHandler(DocumateDbContext db, IBusinessContext business, ICorEnumIdResolver enums)
    : IRequestHandler<ListAppFilesQuery, PagedFileListDto>
{
    public async Task<PagedFileListDto> Handle(ListAppFilesQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = AppFileQueryHelpers.NormalizePaging(request.Page, request.PageSize);

        var q = db.OpsFiles.AsNoTracking()
            .Where(f => f.QueueId == request.QueueId && f.BusinessId == business.BusinessId && !f.IsDeleted);

        if (request.Ids is { Count: > 0 } ids)
        {
            q = q.Where(f => ids.Contains(f.Id));
        }

        if (request.BatchId is Guid batchId)
        {
            q = q.Where(f => f.BatchId == batchId);
        }

        if (request.CreatedFrom is DateTimeOffset from)
        {
            q = q.Where(f => f.CreatedAt >= from);
        }

        if (request.CreatedTo is DateTimeOffset to)
        {
            q = q.Where(f => f.CreatedAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(request.StatusKey))
        {
            try
            {
                var statusId = enums.Require("file_public_status", request.StatusKey);
                q = q.Where(f => f.PublicStatusEnumId == statusId);
            }
            catch (InvalidOperationException)
            {
                return new PagedFileListDto([], 0, page, pageSize);
            }
        }

        var total = await q.CountAsync(cancellationToken);
        var rows = await q.OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = await FileDtoMapping.ToListItemsAsync(db, rows, cancellationToken);
        return new PagedFileListDto(items, total, page, pageSize);
    }
}

public sealed class GetAppFilesSummaryHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<GetAppFilesSummaryQuery, FileSummaryDto>
{
    public async Task<FileSummaryDto> Handle(GetAppFilesSummaryQuery request, CancellationToken cancellationToken)
    {
        var counts = await db.OpsFiles.AsNoTracking()
            .Where(f => f.QueueId == request.QueueId && f.BusinessId == business.BusinessId && !f.IsDeleted)
            .Join(
                db.CorEnums.AsNoTracking(),
                f => f.PublicStatusEnumId,
                e => e.Id,
                (f, e) => e.EnumKey)
            .GroupBy(k => k)
            .Select(g => new { StatusKey = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var byStatus = counts.ToDictionary(x => x.StatusKey, x => x.Count, StringComparer.OrdinalIgnoreCase);
        var total = counts.Sum(x => x.Count);
        return new FileSummaryDto(total, byStatus);
    }
}

public sealed class SearchAppFilesBySchemaHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<SearchAppFilesBySchemaQuery, PagedFileSchemaSearchDto>
{
    public async Task<PagedFileSchemaSearchDto> Handle(
        SearchAppFilesBySchemaQuery request,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = AppFileQueryHelpers.NormalizePaging(request.Page, request.PageSize);
        var skip = (page - 1) * pageSize;

        var total = await CountMatchesAsync(request, cancellationToken);
        var rows = total == 0
            ? Array.Empty<SchemaSearchSqlRow>()
            : await FetchMatchesAsync(request, skip, pageSize, cancellationToken);

        if (rows.Length == 0)
        {
            return new PagedFileSchemaSearchDto(
                [],
                total,
                page,
                pageSize,
                request.FieldKey,
                request.SearchValue,
                request.MatchMode);
        }

        var fileIds = rows.Select(r => r.FileId).Distinct().ToList();
        var docIds = rows.Select(r => r.DocumentId).ToList();

        var files = await db.OpsFiles.AsNoTracking()
            .Where(f => fileIds.Contains(f.Id) && f.BusinessId == business.BusinessId && !f.IsDeleted)
            .ToDictionaryAsync(f => f.Id, cancellationToken);

        var docs = await db.OpsDocuments.AsNoTracking()
            .Where(d => docIds.Contains(d.Id) && d.BusinessId == business.BusinessId && !d.IsDeleted)
            .ToListAsync(cancellationToken);

        var enumIds = docs.Select(d => d.PublicStatusEnumId).Distinct().ToList();
        var enumKeys = await db.CorEnums.AsNoTracking()
            .Where(e => enumIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.EnumKey, cancellationToken);

        var typeIds = docs.Where(d => d.DocumentTypeId is not null).Select(d => d.DocumentTypeId!.Value).Distinct().ToList();
        var typeKeys = typeIds.Count == 0
            ? new Dictionary<long, string>()
            : await db.CorDocumentTypes.AsNoTracking()
                .Where(t => typeIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.DocumentTypeKey, cancellationToken);

        var docById = docs.ToDictionary(d => d.Id);
        var items = new List<FileSchemaSearchItemDto>(rows.Length);
        foreach (var row in rows)
        {
            if (!files.TryGetValue(row.FileId, out var file) || !docById.TryGetValue(row.DocumentId, out var doc))
            {
                continue;
            }

            enumKeys.TryGetValue(doc.PublicStatusEnumId, out var statusKey);
            string? typeKey = null;
            if (doc.DocumentTypeId is long tid)
            {
                typeKeys.TryGetValue(tid, out typeKey);
            }

            items.Add(new FileSchemaSearchItemDto(
                file.Id,
                doc.Id,
                file.OriginalFileName,
                statusKey,
                typeKey,
                request.FieldKey,
                row.MatchedValue,
                file.CreatedAt));
        }

        return new PagedFileSchemaSearchDto(
            items,
            total,
            page,
            pageSize,
            request.FieldKey,
            request.SearchValue,
            request.MatchMode);
    }

    private async Task<int> CountMatchesAsync(
        SearchAppFilesBySchemaQuery request,
        CancellationToken cancellationToken)
    {
        if (request.MatchMode == "contains")
        {
            var like = $"%{AppFileSchemaSearch.EscapeLike(request.SearchValue)}%";
            return await db.Database.SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*)
                    FROM OpsDocuments d
                    WHERE d.BusinessId = {0}
                      AND d.QueueId = {1}
                      AND d.IsDeleted = 0
                      AND d.ResultJson IS NOT NULL
                      AND JSON_VALUE(d.ResultJson, {2}) LIKE {3}
                    """,
                    business.BusinessId,
                    request.QueueId,
                    request.JsonPath,
                    like)
                .SingleAsync(cancellationToken);
        }

        return await db.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*)
                FROM OpsDocuments d
                WHERE d.BusinessId = {0}
                  AND d.QueueId = {1}
                  AND d.IsDeleted = 0
                  AND d.ResultJson IS NOT NULL
                  AND JSON_VALUE(d.ResultJson, {2}) = {3}
                """,
                business.BusinessId,
                request.QueueId,
                request.JsonPath,
                request.SearchValue)
            .SingleAsync(cancellationToken);
    }

    private async Task<SchemaSearchSqlRow[]> FetchMatchesAsync(
        SearchAppFilesBySchemaQuery request,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        if (request.MatchMode == "contains")
        {
            var like = $"%{AppFileSchemaSearch.EscapeLike(request.SearchValue)}%";
            return await db.Database.SqlQueryRaw<SchemaSearchSqlRow>(
                    """
                    SELECT d.Id AS DocumentId, d.FileId, JSON_VALUE(d.ResultJson, {2}) AS MatchedValue
                    FROM OpsDocuments d
                    WHERE d.BusinessId = {0}
                      AND d.QueueId = {1}
                      AND d.IsDeleted = 0
                      AND d.ResultJson IS NOT NULL
                      AND JSON_VALUE(d.ResultJson, {2}) LIKE {3}
                    ORDER BY d.CreatedAt DESC
                    OFFSET {4} ROWS FETCH NEXT {5} ROWS ONLY
                    """,
                    business.BusinessId,
                    request.QueueId,
                    request.JsonPath,
                    like,
                    skip,
                    take)
                .ToArrayAsync(cancellationToken);
        }

        return await db.Database.SqlQueryRaw<SchemaSearchSqlRow>(
                """
                SELECT d.Id AS DocumentId, d.FileId, JSON_VALUE(d.ResultJson, {2}) AS MatchedValue
                FROM OpsDocuments d
                WHERE d.BusinessId = {0}
                  AND d.QueueId = {1}
                  AND d.IsDeleted = 0
                  AND d.ResultJson IS NOT NULL
                  AND JSON_VALUE(d.ResultJson, {2}) = {3}
                ORDER BY d.CreatedAt DESC
                OFFSET {4} ROWS FETCH NEXT {5} ROWS ONLY
                """,
                business.BusinessId,
                request.QueueId,
                request.JsonPath,
                request.SearchValue,
                skip,
                take)
            .ToArrayAsync(cancellationToken);
    }

    private sealed class SchemaSearchSqlRow
    {
        public Guid DocumentId { get; set; }
        public Guid FileId { get; set; }
        public string? MatchedValue { get; set; }
    }
}

public sealed class GetFileDownloadUrlHandler(
    DocumateDbContext db,
    IObjectStorage storage,
    IBusinessContext business)
    : IRequestHandler<GetFileDownloadUrlQuery, FileDownloadUrlDto?>
{
    public async Task<FileDownloadUrlDto?> Handle(GetFileDownloadUrlQuery request, CancellationToken cancellationToken)
    {
        var file = await db.OpsFiles.AsNoTracking().FirstOrDefaultAsync(
            f => f.Id == request.FileId
                 && f.QueueId == request.QueueId
                 && f.BusinessId == business.BusinessId
                 && !f.IsDeleted,
            cancellationToken);
        if (file is null || string.IsNullOrWhiteSpace(file.StorageBucket) || string.IsNullOrWhiteSpace(file.StorageKey))
        {
            return null;
        }

        var url = await storage.GetSignedUrlAsync(file.StorageBucket, file.StorageKey, cancellationToken);
        return new FileDownloadUrlDto(file.Id, url);
    }
}

file static class FileDtoMapping
{
    public static async Task<FileDto> ToDto(
        DocumateDbContext db,
        Domain.OpsFile file,
        CancellationToken cancellationToken)
    {
        var list = await ToListItemsAsync(db, [file], cancellationToken);
        var item = list[0];
        return new FileDto(
            file.Id,
            file.QueueId,
            file.BatchId,
            file.OriginalFileName,
            file.ContentType,
            file.SizeBytes,
            file.StorageKey,
            file.StorageBucket,
            file.PublicStatusEnumId,
            item.PublicStatusKey,
            file.InternalStageEnumId,
            item.InternalStageKey,
            item.CreatedAt,
            item.CompletedAt,
            item.DocumentCount);
    }

    public static async Task<IReadOnlyList<FileListItemDto>> ToListItemsAsync(
        DocumateDbContext db,
        IReadOnlyList<Domain.OpsFile> files,
        CancellationToken cancellationToken)
    {
        if (files.Count == 0)
        {
            return [];
        }

        var ids = files.Select(f => f.Id).ToList();
        var enumIds = files.Select(f => f.PublicStatusEnumId)
            .Concat(files.Where(f => f.InternalStageEnumId is not null).Select(f => f.InternalStageEnumId!.Value))
            .Distinct()
            .ToList();

        var enumKeys = await db.CorEnums.AsNoTracking()
            .Where(e => enumIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.EnumKey, cancellationToken);

        var docCounts = await db.OpsDocuments.AsNoTracking()
            .Where(d => ids.Contains(d.FileId) && !d.IsDeleted)
            .GroupBy(d => d.FileId)
            .Select(g => new { FileId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.FileId, x => x.Count, cancellationToken);

        return files.Select(f =>
        {
            enumKeys.TryGetValue(f.PublicStatusEnumId, out var statusKey);
            string? stageKey = null;
            if (f.InternalStageEnumId is long sid)
            {
                enumKeys.TryGetValue(sid, out stageKey);
            }

            docCounts.TryGetValue(f.Id, out var count);
            return new FileListItemDto(
                f.Id,
                f.QueueId,
                f.BatchId,
                f.OriginalFileName,
                f.ContentType,
                f.SizeBytes,
                statusKey,
                stageKey,
                f.CreatedAt,
                f.CompletedAt,
                count);
        }).ToList();
    }
}
