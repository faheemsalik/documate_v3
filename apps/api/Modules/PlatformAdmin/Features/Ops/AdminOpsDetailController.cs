namespace Documate.Api.Modules.PlatformAdmin.Features.Ops;

using System.Text.Json.Nodes;
using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Storage;
using Documate.Api.Modules.FrontendSupport.Features.Files;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize(Policy = PlatformAdminAuth.PolicyName)]
[Route("api/admin/ops")]
public sealed class AdminOpsDetailController(IMediator mediator) : ControllerBase
{
    [HttpGet("files/{fileId:guid}")]
    public async Task<ActionResult<AdminFileDetailDto>> GetFile(Guid fileId, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetAdminFileQuery(fileId), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("files/{fileId:guid}/content")]
    public async Task<IActionResult> FileContent(Guid fileId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAdminFileContentQuery(fileId), cancellationToken);
        return result is null ? NotFound() : File(result.Content, result.ContentType);
    }

    [HttpGet("files/{fileId:guid}/documents")]
    public async Task<ActionResult<IReadOnlyList<AdminFileDocumentItemDto>>> ListFileDocuments(
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var items = await mediator.Send(new ListAdminFileDocumentsQuery(fileId), cancellationToken);
        return items is null ? NotFound() : Ok(items);
    }

    [HttpGet("documents/{documentId:guid}")]
    public async Task<ActionResult<AdminDocumentDetailDto>> GetDocument(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetAdminDocumentQuery(documentId), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("documents/{documentId:guid}/content")]
    public async Task<IActionResult> DocumentContent(Guid documentId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAdminDocumentContentQuery(documentId), cancellationToken);
        return result is null ? NotFound() : File(result.Content, result.ContentType);
    }
}

public sealed record AdminFileDetailDto(
    Guid Id,
    string BusinessId,
    string BusinessName,
    Guid TenantId,
    string TenantName,
    Guid QueueId,
    string? QueueName,
    string? OriginalFileName,
    string? ContentType,
    long SizeBytes,
    string? PublicStatusKey,
    string? InternalStageKey,
    string? SourceKey,
    int DocumentCount,
    string? ErrorCode,
    string? ErrorMessage,
    bool IsReprocess,
    bool IsCancelled,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed record AdminFileDocumentItemDto(
    Guid Id,
    Guid FileId,
    string? DocumentTypeKey,
    string? PublicStatusKey,
    string? InternalStageKey,
    int? PageStart,
    int? PageEnd,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed record AdminDocumentDetailDto(
    Guid Id,
    Guid FileId,
    string BusinessId,
    string BusinessName,
    Guid QueueId,
    string? OriginalFileName,
    string? ContentType,
    string? DocumentTypeKey,
    string? PublicStatusKey,
    string? InternalStageKey,
    int? PageStart,
    int? PageEnd,
    string? ErrorCode,
    string? ErrorMessage,
    JsonNode? ResultJson,
    string? WebhookStatusKey,
    int WebhookAttempts,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed record GetAdminFileQuery(Guid FileId) : IRequest<AdminFileDetailDto?>;
public sealed record GetAdminFileContentQuery(Guid FileId) : IRequest<FileContentResultDto?>;
public sealed record ListAdminFileDocumentsQuery(Guid FileId) : IRequest<IReadOnlyList<AdminFileDocumentItemDto>?>;
public sealed record GetAdminDocumentQuery(Guid DocumentId) : IRequest<AdminDocumentDetailDto?>;
public sealed record GetAdminDocumentContentQuery(Guid DocumentId) : IRequest<FileContentResultDto?>;

public sealed class GetAdminFileHandler(DocumateDbContext db) : IRequestHandler<GetAdminFileQuery, AdminFileDetailDto?>
{
    public async Task<AdminFileDetailDto?> Handle(GetAdminFileQuery request, CancellationToken cancellationToken)
    {
        var file = await db.OpsFiles.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == request.FileId && !f.IsDeleted, cancellationToken);
        if (file is null)
        {
            return null;
        }

        return await AdminOpsDetailMapping.ToFileDetailAsync(db, file, cancellationToken);
    }
}

public sealed class GetAdminFileContentHandler(DocumateDbContext db, IObjectStorage storage)
    : IRequestHandler<GetAdminFileContentQuery, FileContentResultDto?>
{
    public async Task<FileContentResultDto?> Handle(
        GetAdminFileContentQuery request,
        CancellationToken cancellationToken)
    {
        var file = await db.OpsFiles.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == request.FileId && !f.IsDeleted, cancellationToken);
        if (file is null || string.IsNullOrWhiteSpace(file.StorageBucket) || string.IsNullOrWhiteSpace(file.StorageKey))
        {
            return null;
        }

        var stream = await storage.DownloadAsync(file.StorageBucket, file.StorageKey, cancellationToken);
        var contentType = GetFileContentHandler.ResolveContentType(file.ContentType, file.OriginalFileName);
        return new FileContentResultDto(stream, contentType, file.OriginalFileName);
    }
}

public sealed class ListAdminFileDocumentsHandler(DocumateDbContext db)
    : IRequestHandler<ListAdminFileDocumentsQuery, IReadOnlyList<AdminFileDocumentItemDto>?>
{
    public async Task<IReadOnlyList<AdminFileDocumentItemDto>?> Handle(
        ListAdminFileDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var exists = await db.OpsFiles.AsNoTracking()
            .AnyAsync(f => f.Id == request.FileId && !f.IsDeleted, cancellationToken);
        if (!exists)
        {
            return null;
        }

        var docs = await db.OpsDocuments.AsNoTracking()
            .Where(d => d.FileId == request.FileId && !d.IsDeleted)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        return await AdminOpsDetailMapping.ToFileDocumentItemsAsync(db, docs, cancellationToken);
    }
}

public sealed class GetAdminDocumentHandler(DocumateDbContext db)
    : IRequestHandler<GetAdminDocumentQuery, AdminDocumentDetailDto?>
{
    public async Task<AdminDocumentDetailDto?> Handle(
        GetAdminDocumentQuery request,
        CancellationToken cancellationToken)
    {
        var doc = await db.OpsDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && !d.IsDeleted, cancellationToken);
        if (doc is null)
        {
            return null;
        }

        return await AdminOpsDetailMapping.ToDocumentDetailAsync(db, doc, cancellationToken);
    }
}

public sealed class GetAdminDocumentContentHandler(DocumateDbContext db, IObjectStorage storage)
    : IRequestHandler<GetAdminDocumentContentQuery, FileContentResultDto?>
{
    public async Task<FileContentResultDto?> Handle(
        GetAdminDocumentContentQuery request,
        CancellationToken cancellationToken)
    {
        var doc = await db.OpsDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && !d.IsDeleted, cancellationToken);
        if (doc is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(doc.PdfStorageBucket) && !string.IsNullOrWhiteSpace(doc.PdfStorageKey))
        {
            var pdfStream = await storage.DownloadAsync(doc.PdfStorageBucket, doc.PdfStorageKey, cancellationToken);
            return new FileContentResultDto(pdfStream, "application/pdf", $"document-{doc.Id}.pdf");
        }

        var file = await db.OpsFiles.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == doc.FileId && !f.IsDeleted, cancellationToken);
        if (file is null || string.IsNullOrWhiteSpace(file.StorageBucket) || string.IsNullOrWhiteSpace(file.StorageKey))
        {
            return null;
        }

        var stream = await storage.DownloadAsync(file.StorageBucket, file.StorageKey, cancellationToken);
        var contentType = GetFileContentHandler.ResolveContentType(file.ContentType, file.OriginalFileName);
        return new FileContentResultDto(stream, contentType, file.OriginalFileName);
    }
}

file static class AdminOpsDetailMapping
{
    public static async Task<AdminFileDetailDto> ToFileDetailAsync(
        DocumateDbContext db,
        Domain.OpsFile file,
        CancellationToken cancellationToken)
    {
        var biz = await db.CorTenantBusinesses.AsNoTracking()
            .FirstOrDefaultAsync(b => b.IdenBusinessId == file.BusinessId && !b.IsDeleted, cancellationToken);
        var queue = await db.OpsQueues.AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == file.QueueId && !q.IsDeleted, cancellationToken);

        var enumIds = new List<long> { file.PublicStatusEnumId };
        if (file.InternalStageEnumId is long sid) enumIds.Add(sid);
        if (file.SourceEnumId is long src) enumIds.Add(src);

        var enumKeys = await db.CorEnums.AsNoTracking()
            .Where(e => enumIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.EnumKey, cancellationToken);

        enumKeys.TryGetValue(file.PublicStatusEnumId, out var status);
        string? stage = null;
        if (file.InternalStageEnumId is long stageId)
        {
            enumKeys.TryGetValue(stageId, out stage);
        }

        string? source = null;
        if (file.SourceEnumId is long sourceId)
        {
            enumKeys.TryGetValue(sourceId, out source);
        }

        var docCount = await db.OpsDocuments.AsNoTracking()
            .CountAsync(d => d.FileId == file.Id && !d.IsDeleted, cancellationToken);

        return new AdminFileDetailDto(
            file.Id,
            file.BusinessId,
            biz?.Name ?? file.BusinessId,
            biz?.TenantId ?? Guid.Empty,
            biz?.TenantName ?? "",
            file.QueueId,
            queue?.Name,
            file.OriginalFileName,
            GetFileContentHandler.ResolveContentType(file.ContentType, file.OriginalFileName),
            file.SizeBytes,
            status,
            stage,
            source,
            docCount,
            file.ErrorCode,
            file.ErrorMessage,
            file.ReprocessOfFileId is not null,
            file.CancelledAt is not null,
            file.CreatedAt,
            file.CompletedAt);
    }

    public static async Task<IReadOnlyList<AdminFileDocumentItemDto>> ToFileDocumentItemsAsync(
        DocumateDbContext db,
        IReadOnlyList<Domain.OpsDocument> docs,
        CancellationToken cancellationToken)
    {
        if (docs.Count == 0)
        {
            return [];
        }

        var enumIds = docs.Select(d => d.PublicStatusEnumId)
            .Concat(docs.Where(d => d.InternalStageEnumId is not null).Select(d => d.InternalStageEnumId!.Value))
            .Distinct()
            .ToList();
        var enumKeys = await db.CorEnums.AsNoTracking()
            .Where(e => enumIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.EnumKey, cancellationToken);

        var typeIds = docs.Where(d => d.DocumentTypeId is not null).Select(d => d.DocumentTypeId!.Value).Distinct().ToList();
        var typeKeys = typeIds.Count == 0
            ? new Dictionary<long, string>()
            : await db.CorDocumentTypes.AsNoTracking()
                .Where(t => typeIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.DocumentTypeKey, cancellationToken);

        return docs.Select(d =>
        {
            enumKeys.TryGetValue(d.PublicStatusEnumId, out var status);
            string? stage = null;
            if (d.InternalStageEnumId is long sid)
            {
                enumKeys.TryGetValue(sid, out stage);
            }

            string? typeKey = null;
            if (d.DocumentTypeId is long tid)
            {
                typeKeys.TryGetValue(tid, out typeKey);
            }

            return new AdminFileDocumentItemDto(
                d.Id,
                d.FileId,
                typeKey,
                status,
                stage,
                d.PageStart,
                d.PageEnd,
                d.CreatedAt,
                d.CompletedAt);
        }).ToList();
    }

    public static async Task<AdminDocumentDetailDto> ToDocumentDetailAsync(
        DocumateDbContext db,
        Domain.OpsDocument doc,
        CancellationToken cancellationToken)
    {
        var file = await db.OpsFiles.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == doc.FileId && !f.IsDeleted, cancellationToken);
        var biz = await db.CorTenantBusinesses.AsNoTracking()
            .FirstOrDefaultAsync(b => b.IdenBusinessId == doc.BusinessId && !b.IsDeleted, cancellationToken);

        var enumIds = new List<long> { doc.PublicStatusEnumId };
        if (doc.InternalStageEnumId is long sid) enumIds.Add(sid);
        if (doc.WebhookStatusEnumId is long wid) enumIds.Add(wid);

        var enumKeys = await db.CorEnums.AsNoTracking()
            .Where(e => enumIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.EnumKey, cancellationToken);

        enumKeys.TryGetValue(doc.PublicStatusEnumId, out var status);
        string? stage = null;
        if (doc.InternalStageEnumId is long stageId)
        {
            enumKeys.TryGetValue(stageId, out stage);
        }

        string? webhook = null;
        if (doc.WebhookStatusEnumId is long webhookId)
        {
            enumKeys.TryGetValue(webhookId, out webhook);
        }

        string? typeKey = null;
        if (doc.DocumentTypeId is long tid)
        {
            typeKey = await db.CorDocumentTypes.AsNoTracking()
                .Where(t => t.Id == tid)
                .Select(t => t.DocumentTypeKey)
                .FirstOrDefaultAsync(cancellationToken);
        }

        JsonNode? resultJson = null;
        if (!string.IsNullOrWhiteSpace(doc.ResultJson))
        {
            try
            {
                resultJson = JsonNode.Parse(doc.ResultJson);
            }
            catch (System.Text.Json.JsonException)
            {
                resultJson = null;
            }
        }

        var contentType = !string.IsNullOrWhiteSpace(doc.PdfStorageKey)
            ? "application/pdf"
            : GetFileContentHandler.ResolveContentType(file?.ContentType, file?.OriginalFileName);

        return new AdminDocumentDetailDto(
            doc.Id,
            doc.FileId,
            doc.BusinessId,
            biz?.Name ?? doc.BusinessId,
            doc.QueueId,
            file?.OriginalFileName,
            contentType,
            typeKey,
            status,
            stage,
            doc.PageStart,
            doc.PageEnd,
            doc.ErrorCode,
            doc.ErrorMessage,
            resultJson,
            webhook,
            doc.WebhookAttempts,
            doc.CreatedAt,
            doc.CompletedAt);
    }
}
