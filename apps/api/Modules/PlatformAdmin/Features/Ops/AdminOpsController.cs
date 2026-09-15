namespace Documate.Api.Modules.PlatformAdmin.Features.Ops;

using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Work;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize(Policy = PlatformAdminAuth.PolicyName)]
[Route("api/admin/ops")]
public sealed class AdminOpsController(IMediator mediator) : ControllerBase
{
    [HttpGet("files")]
    public async Task<ActionResult<PagedAdminFileListDto>> ListFiles(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sort = null,
        [FromQuery] List<string>? businessIds = null,
        [FromQuery] List<Guid>? tenantIds = null,
        [FromQuery] List<Guid>? queueIds = null,
        [FromQuery] string? status = null,
        [FromQuery] string? stage = null,
        [FromQuery] string? source = null,
        [FromQuery] string? name = null,
        [FromQuery] string? emailFrom = null,
        [FromQuery] bool? hasError = null,
        [FromQuery] bool? isReprocess = null,
        [FromQuery] bool? isCancelled = null,
        [FromQuery] DateTimeOffset? createdFrom = null,
        [FromQuery] DateTimeOffset? createdTo = null,
        CancellationToken cancellationToken = default)
    {
        var dto = await mediator.Send(
            new ListAdminFilesQuery(
                page, pageSize, sort, businessIds, tenantIds, queueIds,
                status, stage, source, name, emailFrom,
                hasError, isReprocess, isCancelled, createdFrom, createdTo),
            cancellationToken);
        return Ok(dto);
    }

    [HttpGet("documents")]
    public async Task<ActionResult<PagedAdminDocumentListDto>> ListDocuments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sort = null,
        [FromQuery] List<string>? businessIds = null,
        [FromQuery] List<Guid>? tenantIds = null,
        [FromQuery] List<Guid>? queueIds = null,
        [FromQuery] string? status = null,
        [FromQuery] string? stage = null,
        [FromQuery] string? webhookStatus = null,
        [FromQuery] string? documentTypeKey = null,
        [FromQuery] string? fileName = null,
        [FromQuery] bool? hasError = null,
        [FromQuery] bool? isCancelled = null,
        [FromQuery] DateTimeOffset? createdFrom = null,
        [FromQuery] DateTimeOffset? createdTo = null,
        CancellationToken cancellationToken = default)
    {
        var dto = await mediator.Send(
            new ListAdminDocumentsQuery(
                page, pageSize, sort, businessIds, tenantIds, queueIds,
                status, stage, webhookStatus, documentTypeKey, fileName,
                hasError, isCancelled, createdFrom, createdTo),
            cancellationToken);
        return Ok(dto);
    }

    /// <summary>
    /// Ops Reset: same FileId — soft-delete Documents, set status to received, re-enqueue pipeline.
    /// New Documents/webhooks are produced when the run completes.
    /// </summary>
    [HttpPost("files/{fileId:guid}/reset")]
    public async Task<ActionResult<AdminResetResultDto>> ResetFile(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = await mediator.Send(new ResetAdminFileCommand(fileId), cancellationToken);
            if (dto is null)
            {
                return NotFound();
            }

            return Accepted(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Ops Reset for a Document: resets the parent File (same FileId).</summary>
    [HttpPost("documents/{documentId:guid}/reset")]
    public async Task<ActionResult<AdminResetResultDto>> ResetDocument(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = await mediator.Send(new ResetAdminDocumentCommand(documentId), cancellationToken);
            if (dto is null)
            {
                return NotFound();
            }

            return Accepted(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public sealed record AdminResetResultDto(
    Guid FileId,
    string BusinessId,
    string? OriginalFileName,
    int SoftDeletedDocumentCount);

public sealed record ResetAdminFileCommand(Guid FileId) : IRequest<AdminResetResultDto?>;

public sealed record ResetAdminDocumentCommand(Guid DocumentId) : IRequest<AdminResetResultDto?>;

public sealed class ResetAdminFileHandler(
    DocumateDbContext db,
    IResetWorkService reset,
    IBusinessContextSetter businessSetter) : IRequestHandler<ResetAdminFileCommand, AdminResetResultDto?>
{
    public async Task<AdminResetResultDto?> Handle(
        ResetAdminFileCommand request,
        CancellationToken cancellationToken)
    {
        var source = await db.OpsFiles.AsNoTracking()
            .Where(f => f.Id == request.FileId && !f.IsDeleted)
            .Select(f => new { f.Id, f.BusinessId, f.OriginalFileName })
            .FirstOrDefaultAsync(cancellationToken);
        if (source is null)
        {
            return null;
        }

        var biz = await db.CorTenantBusinesses.AsNoTracking()
            .FirstOrDefaultAsync(b => b.IdenBusinessId == source.BusinessId && !b.IsDeleted, cancellationToken);
        if (biz is null)
        {
            throw new InvalidOperationException("Business for this file was not found.");
        }

        using (businessSetter.Use(new BusinessContext
               {
                   IsAuthenticated = true,
                   BusinessId = source.BusinessId,
                   TenantId = biz.TenantId.ToString(),
                   UserId = "ops-admin",
                   BusinessName = biz.Name,
                   TenantName = biz.TenantName,
               }))
        {
            var result = await reset.ResetFileAsync(
                source.Id,
                source.BusinessId,
                userId: "ops-admin",
                cancellationToken);
            if (result is null)
            {
                return null;
            }

            return new AdminResetResultDto(
                result.File.Id,
                source.BusinessId,
                result.File.OriginalFileName ?? source.OriginalFileName,
                result.SoftDeletedDocumentCount);
        }
    }
}

public sealed class ResetAdminDocumentHandler(
    DocumateDbContext db,
    IMediator mediator) : IRequestHandler<ResetAdminDocumentCommand, AdminResetResultDto?>
{
    public async Task<AdminResetResultDto?> Handle(
        ResetAdminDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var fileId = await db.OpsDocuments.AsNoTracking()
            .Where(d => d.Id == request.DocumentId && !d.IsDeleted)
            .Select(d => (Guid?)d.FileId)
            .FirstOrDefaultAsync(cancellationToken);
        if (fileId is null)
        {
            return null;
        }

        return await mediator.Send(new ResetAdminFileCommand(fileId.Value), cancellationToken);
    }
}

public sealed record PagedAdminFileListDto(
    IReadOnlyList<AdminFileListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AdminFileListItemDto(
    Guid Id,
    string BusinessId,
    string BusinessName,
    Guid TenantId,
    string TenantName,
    string IdenTenantId,
    Guid QueueId,
    string? QueueName,
    string? OriginalFileName,
    string? PublicStatusKey,
    string? InternalStageKey,
    string? SourceKey,
    int DocumentCount,
    long SizeBytes,
    string? EmailFrom,
    string? EmailSubject,
    string? ErrorCode,
    string? ErrorMessage,
    bool IsReprocess,
    bool IsCancelled,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed record PagedAdminDocumentListDto(
    IReadOnlyList<AdminDocumentListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AdminDocumentListItemDto(
    Guid Id,
    Guid FileId,
    string? OriginalFileName,
    string BusinessId,
    string BusinessName,
    Guid TenantId,
    string TenantName,
    string IdenTenantId,
    Guid QueueId,
    string? DocumentTypeKey,
    string? AgentName,
    string? PublicStatusKey,
    string? InternalStageKey,
    string? WebhookStatusKey,
    DateTimeOffset? WebhookLastAt,
    int? WebhookLastHttpStatus,
    int WebhookAttempts,
    bool HasResultJson,
    string? ErrorCode,
    string? ErrorMessage,
    bool IsCancelled,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset FileCreatedAt);

public sealed record ListAdminFilesQuery(
    int Page,
    int PageSize,
    string? Sort,
    IReadOnlyList<string>? BusinessIds,
    IReadOnlyList<Guid>? TenantIds,
    IReadOnlyList<Guid>? QueueIds,
    string? StatusKey,
    string? StageKey,
    string? SourceKey,
    string? Name,
    string? EmailFrom,
    bool? HasError,
    bool? IsReprocess,
    bool? IsCancelled,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo) : IRequest<PagedAdminFileListDto>;

public sealed record ListAdminDocumentsQuery(
    int Page,
    int PageSize,
    string? Sort,
    IReadOnlyList<string>? BusinessIds,
    IReadOnlyList<Guid>? TenantIds,
    IReadOnlyList<Guid>? QueueIds,
    string? StatusKey,
    string? StageKey,
    string? WebhookStatusKey,
    string? DocumentTypeKey,
    string? FileName,
    bool? HasError,
    bool? IsCancelled,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo) : IRequest<PagedAdminDocumentListDto>;

file static class AdminOpsPaging
{
    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        var p = page < 1 ? 1 : page;
        var s = pageSize switch
        {
            < 1 => 50,
            > 200 => 200,
            _ => pageSize,
        };
        return (p, s);
    }
}

public sealed class ListAdminFilesHandler(DocumateDbContext db, ICorEnumIdResolver enums)
    : IRequestHandler<ListAdminFilesQuery, PagedAdminFileListDto>
{
    public async Task<PagedAdminFileListDto> Handle(ListAdminFilesQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = AdminOpsPaging.Normalize(request.Page, request.PageSize);

        var q =
            from f in db.OpsFiles.AsNoTracking()
            where !f.IsDeleted
            join b in db.CorTenantBusinesses.AsNoTracking() on f.BusinessId equals b.IdenBusinessId
            where !b.IsDeleted
            join t in db.CorTenants.AsNoTracking() on b.TenantId equals t.Id
            where !t.IsDeleted
            select new { File = f, Business = b, Tenant = t };

        if (request.BusinessIds is { Count: > 0 } bids)
        {
            q = q.Where(x => bids.Contains(x.File.BusinessId));
        }

        if (request.TenantIds is { Count: > 0 } tids)
        {
            q = q.Where(x => tids.Contains(x.Tenant.Id));
        }

        if (request.QueueIds is { Count: > 0 } qids)
        {
            q = q.Where(x => qids.Contains(x.File.QueueId));
        }

        if (request.CreatedFrom is DateTimeOffset from)
        {
            q = q.Where(x => x.File.CreatedAt >= from);
        }

        if (request.CreatedTo is DateTimeOffset to)
        {
            q = q.Where(x => x.File.CreatedAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var term = request.Name.Trim();
            q = q.Where(x => x.File.OriginalFileName != null && x.File.OriginalFileName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.EmailFrom))
        {
            var term = request.EmailFrom.Trim();
            q = q.Where(x => x.File.EmailFrom != null && x.File.EmailFrom.Contains(term));
        }

        if (request.HasError == true)
        {
            q = q.Where(x => x.File.ErrorCode != null || x.File.ErrorMessage != null);
        }
        else if (request.HasError == false)
        {
            q = q.Where(x => x.File.ErrorCode == null && x.File.ErrorMessage == null);
        }

        if (request.IsReprocess == true)
        {
            q = q.Where(x => x.File.ReprocessOfFileId != null);
        }
        else if (request.IsReprocess == false)
        {
            q = q.Where(x => x.File.ReprocessOfFileId == null);
        }

        if (request.IsCancelled == true)
        {
            q = q.Where(x => x.File.CancelledAt != null);
        }
        else if (request.IsCancelled == false)
        {
            q = q.Where(x => x.File.CancelledAt == null);
        }

        if (!TryApplyEnumFilter(enums, "file_public_status", request.StatusKey, out var statusId, out var empty))
        {
            if (empty) return new PagedAdminFileListDto([], 0, page, pageSize);
            q = q.Where(x => x.File.PublicStatusEnumId == statusId);
        }

        if (!TryApplyEnumFilter(enums, "file_internal_stage", request.StageKey, out var stageId, out empty))
        {
            if (empty) return new PagedAdminFileListDto([], 0, page, pageSize);
            q = q.Where(x => x.File.InternalStageEnumId == stageId);
        }

        if (!TryApplyEnumFilter(enums, "intake_source", request.SourceKey, out var sourceId, out empty))
        {
            if (empty) return new PagedAdminFileListDto([], 0, page, pageSize);
            q = q.Where(x => x.File.SourceEnumId == sourceId);
        }

        var total = await q.CountAsync(cancellationToken);

        var ordered = string.Equals(request.Sort, "createdAtAsc", StringComparison.OrdinalIgnoreCase)
            ? q.OrderBy(x => x.File.CreatedAt)
            : q.OrderByDescending(x => x.File.CreatedAt);

        var rows = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.File.Id,
                x.File.BusinessId,
                BusinessName = x.Business.Name,
                TenantId = x.Tenant.Id,
                TenantName = x.Tenant.Name,
                x.Tenant.IdenTenantId,
                x.File.QueueId,
                QueueName = x.File.Queue != null ? x.File.Queue.Name : null,
                x.File.OriginalFileName,
                x.File.PublicStatusEnumId,
                x.File.InternalStageEnumId,
                x.File.SourceEnumId,
                DocumentCount = x.File.Documents.Count(d => !d.IsDeleted),
                x.File.SizeBytes,
                x.File.EmailFrom,
                x.File.EmailSubject,
                x.File.ErrorCode,
                x.File.ErrorMessage,
                IsReprocess = x.File.ReprocessOfFileId != null,
                IsCancelled = x.File.CancelledAt != null,
                x.File.CreatedAt,
                x.File.CompletedAt,
            })
            .ToListAsync(cancellationToken);

        var enumIds = rows
            .SelectMany(r => new long?[] { r.PublicStatusEnumId, r.InternalStageEnumId, r.SourceEnumId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var enumMap = await db.CorEnums.AsNoTracking()
            .Where(e => enumIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.EnumKey, cancellationToken);

        var items = rows.Select(r => new AdminFileListItemDto(
            r.Id,
            r.BusinessId,
            r.BusinessName,
            r.TenantId,
            r.TenantName,
            r.IdenTenantId,
            r.QueueId,
            r.QueueName,
            r.OriginalFileName,
            enumMap.GetValueOrDefault(r.PublicStatusEnumId),
            r.InternalStageEnumId is long sid ? enumMap.GetValueOrDefault(sid) : null,
            enumMap.GetValueOrDefault(r.SourceEnumId),
            r.DocumentCount,
            r.SizeBytes,
            r.EmailFrom,
            r.EmailSubject,
            r.ErrorCode,
            Truncate(r.ErrorMessage, 200),
            r.IsReprocess,
            r.IsCancelled,
            r.CreatedAt,
            r.CompletedAt)).ToList();

        return new PagedAdminFileListDto(items, total, page, pageSize);
    }

    /// <returns>true when no filter; false when applied or empty.</returns>
    private static bool TryApplyEnumFilter(
        ICorEnumIdResolver enums,
        string typeKey,
        string? enumKey,
        out long enumId,
        out bool empty)
    {
        enumId = 0;
        empty = false;
        if (string.IsNullOrWhiteSpace(enumKey))
        {
            return true;
        }

        try
        {
            enumId = enums.Require(typeKey, enumKey.Trim());
            return false;
        }
        catch (InvalidOperationException)
        {
            empty = true;
            return false;
        }
    }

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max] + "…";
}

public sealed class ListAdminDocumentsHandler(DocumateDbContext db, ICorEnumIdResolver enums)
    : IRequestHandler<ListAdminDocumentsQuery, PagedAdminDocumentListDto>
{
    public async Task<PagedAdminDocumentListDto> Handle(
        ListAdminDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = AdminOpsPaging.Normalize(request.Page, request.PageSize);

        var q =
            from d in db.OpsDocuments.AsNoTracking()
            where !d.IsDeleted
            join f in db.OpsFiles.AsNoTracking() on d.FileId equals f.Id
            where !f.IsDeleted
            join b in db.CorTenantBusinesses.AsNoTracking() on d.BusinessId equals b.IdenBusinessId
            where !b.IsDeleted
            join t in db.CorTenants.AsNoTracking() on b.TenantId equals t.Id
            where !t.IsDeleted
            select new { Doc = d, File = f, Business = b, Tenant = t };

        if (request.BusinessIds is { Count: > 0 } bids)
        {
            q = q.Where(x => bids.Contains(x.Doc.BusinessId));
        }

        if (request.TenantIds is { Count: > 0 } tids)
        {
            q = q.Where(x => tids.Contains(x.Tenant.Id));
        }

        if (request.QueueIds is { Count: > 0 } qids)
        {
            q = q.Where(x => qids.Contains(x.Doc.QueueId));
        }

        if (request.CreatedFrom is DateTimeOffset from)
        {
            q = q.Where(x => x.Doc.CreatedAt >= from);
        }

        if (request.CreatedTo is DateTimeOffset to)
        {
            q = q.Where(x => x.Doc.CreatedAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(request.FileName))
        {
            var term = request.FileName.Trim();
            q = q.Where(x => x.File.OriginalFileName != null && x.File.OriginalFileName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.DocumentTypeKey))
        {
            var key = request.DocumentTypeKey.Trim();
            q = q.Where(x => x.Doc.DocumentType != null && x.Doc.DocumentType.DocumentTypeKey == key);
        }

        if (request.HasError == true)
        {
            q = q.Where(x => x.Doc.ErrorCode != null || x.Doc.ErrorMessage != null);
        }
        else if (request.HasError == false)
        {
            q = q.Where(x => x.Doc.ErrorCode == null && x.Doc.ErrorMessage == null);
        }

        if (request.IsCancelled == true)
        {
            q = q.Where(x => x.Doc.CancelledAt != null);
        }
        else if (request.IsCancelled == false)
        {
            q = q.Where(x => x.Doc.CancelledAt == null);
        }

        if (!TryApplyEnum(enums, "document_public_status", request.StatusKey, out var statusId, out var empty))
        {
            if (empty) return new PagedAdminDocumentListDto([], 0, page, pageSize);
            q = q.Where(x => x.Doc.PublicStatusEnumId == statusId);
        }

        if (!TryApplyEnum(enums, "document_internal_stage", request.StageKey, out var stageId, out empty))
        {
            if (empty) return new PagedAdminDocumentListDto([], 0, page, pageSize);
            q = q.Where(x => x.Doc.InternalStageEnumId == stageId);
        }

        if (!TryApplyEnum(enums, "webhook_delivery_status", request.WebhookStatusKey, out var whId, out empty))
        {
            if (empty) return new PagedAdminDocumentListDto([], 0, page, pageSize);
            q = q.Where(x => x.Doc.WebhookStatusEnumId == whId);
        }

        var total = await q.CountAsync(cancellationToken);

        var ordered = string.Equals(request.Sort, "createdAtAsc", StringComparison.OrdinalIgnoreCase)
            ? q.OrderBy(x => x.Doc.CreatedAt)
            : q.OrderByDescending(x => x.Doc.CreatedAt);

        var rows = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Doc.Id,
                x.Doc.FileId,
                x.File.OriginalFileName,
                x.Doc.BusinessId,
                BusinessName = x.Business.Name,
                TenantId = x.Tenant.Id,
                TenantName = x.Tenant.Name,
                x.Tenant.IdenTenantId,
                x.Doc.QueueId,
                DocumentTypeKey = x.Doc.DocumentType != null ? x.Doc.DocumentType.DocumentTypeKey : null,
                AgentName = x.Doc.Agent != null ? x.Doc.Agent.Name : null,
                x.Doc.PublicStatusEnumId,
                x.Doc.InternalStageEnumId,
                x.Doc.WebhookStatusEnumId,
                x.Doc.WebhookLastAt,
                x.Doc.WebhookLastHttpStatus,
                x.Doc.WebhookAttempts,
                HasResultJson = x.Doc.ResultJson != null && x.Doc.ResultJson.Length > 0,
                x.Doc.ErrorCode,
                x.Doc.ErrorMessage,
                IsCancelled = x.Doc.CancelledAt != null,
                x.Doc.CreatedAt,
                x.Doc.CompletedAt,
                FileCreatedAt = x.File.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var enumIds = rows
            .SelectMany(r => new long?[] { r.PublicStatusEnumId, r.InternalStageEnumId, r.WebhookStatusEnumId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var enumMap = await db.CorEnums.AsNoTracking()
            .Where(e => enumIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.EnumKey, cancellationToken);

        var items = rows.Select(r => new AdminDocumentListItemDto(
            r.Id,
            r.FileId,
            r.OriginalFileName,
            r.BusinessId,
            r.BusinessName,
            r.TenantId,
            r.TenantName,
            r.IdenTenantId,
            r.QueueId,
            r.DocumentTypeKey,
            r.AgentName,
            enumMap.GetValueOrDefault(r.PublicStatusEnumId),
            r.InternalStageEnumId is long sid ? enumMap.GetValueOrDefault(sid) : null,
            r.WebhookStatusEnumId is long wid ? enumMap.GetValueOrDefault(wid) : null,
            r.WebhookLastAt,
            r.WebhookLastHttpStatus,
            r.WebhookAttempts,
            r.HasResultJson,
            r.ErrorCode,
            Truncate(r.ErrorMessage, 200),
            r.IsCancelled,
            r.CreatedAt,
            r.CompletedAt,
            r.FileCreatedAt)).ToList();

        return new PagedAdminDocumentListDto(items, total, page, pageSize);
    }

    private static bool TryApplyEnum(
        ICorEnumIdResolver enums,
        string typeKey,
        string? enumKey,
        out long enumId,
        out bool empty)
    {
        enumId = 0;
        empty = false;
        if (string.IsNullOrWhiteSpace(enumKey))
        {
            return true;
        }

        try
        {
            enumId = enums.Require(typeKey, enumKey.Trim());
            return false;
        }
        catch (InvalidOperationException)
        {
            empty = true;
            return false;
        }
    }

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max] + "…";
}
