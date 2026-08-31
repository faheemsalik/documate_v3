namespace Documate.Api.Modules.FrontendSupport.Features.Files;

using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Nodes;

/// <summary>App-side document read (Band 16 / DQ-1602).</summary>
[ApiController]
[Authorize]
[Route("api/app/queues/{queueId:guid}/documents")]
public sealed class DocumentsController(IMediator mediator) : ControllerBase
{
    [HttpGet("{documentId:guid}")]
    public async Task<ActionResult<DocumentDetailDto>> Get(
        Guid queueId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetAppDocumentQuery(queueId, documentId), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }
}

public sealed record DocumentListItemDto(
    Guid Id,
    Guid FileId,
    long? DocumentTypeId,
    string? DocumentTypeKey,
    Guid? AgentId,
    string? PublicStatusKey,
    string? InternalStageKey,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed record DocumentDetailDto(
    Guid Id,
    Guid QueueId,
    Guid FileId,
    Guid? BatchId,
    long? DocumentTypeId,
    string? DocumentTypeKey,
    Guid? AgentId,
    long PublicStatusEnumId,
    string? PublicStatusKey,
    long? InternalStageEnumId,
    string? InternalStageKey,
    string? ErrorCode,
    string? ErrorMessage,
    JsonNode? ResultJson,
    string? WebhookStatusKey,
    int WebhookAttempts,
    int? WebhookLastHttpStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed record GetAppDocumentQuery(Guid QueueId, Guid DocumentId) : IRequest<DocumentDetailDto?>;

public sealed record ListAppFileDocumentsQuery(Guid QueueId, Guid FileId) : IRequest<IReadOnlyList<DocumentListItemDto>?>;

public sealed class GetAppDocumentHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<GetAppDocumentQuery, DocumentDetailDto?>
{
    public async Task<DocumentDetailDto?> Handle(GetAppDocumentQuery request, CancellationToken cancellationToken)
    {
        var doc = await db.OpsDocuments.AsNoTracking().FirstOrDefaultAsync(
            d => d.Id == request.DocumentId
                 && d.QueueId == request.QueueId
                 && d.BusinessId == business.BusinessId
                 && !d.IsDeleted,
            cancellationToken);
        if (doc is null)
        {
            return null;
        }

        var list = await AppDocumentMapping.ToDetailListAsync(db, [doc], cancellationToken);
        return list.FirstOrDefault();
    }
}

public sealed class ListAppFileDocumentsHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<ListAppFileDocumentsQuery, IReadOnlyList<DocumentListItemDto>?>
{
    public async Task<IReadOnlyList<DocumentListItemDto>?> Handle(
        ListAppFileDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var fileExists = await db.OpsFiles.AsNoTracking().AnyAsync(
            f => f.Id == request.FileId
                 && f.QueueId == request.QueueId
                 && f.BusinessId == business.BusinessId
                 && !f.IsDeleted,
            cancellationToken);
        if (!fileExists)
        {
            return null;
        }

        var docs = await db.OpsDocuments.AsNoTracking()
            .Where(d => d.FileId == request.FileId
                        && d.QueueId == request.QueueId
                        && d.BusinessId == business.BusinessId
                        && !d.IsDeleted)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        return await AppDocumentMapping.ToListItemsAsync(db, docs, cancellationToken);
    }
}

file static class AppDocumentMapping
{
    public static async Task<IReadOnlyList<DocumentListItemDto>> ToListItemsAsync(
        DocumateDbContext db,
        IReadOnlyList<Domain.OpsDocument> docs,
        CancellationToken cancellationToken)
    {
        if (docs.Count == 0)
        {
            return [];
        }

        var enumKeys = await LoadEnumKeysAsync(db, docs, cancellationToken);
        var typeKeys = await LoadTypeKeysAsync(db, docs, cancellationToken);

        return docs.Select(d => ToListItem(d, enumKeys, typeKeys)).ToList();
    }

    public static async Task<IReadOnlyList<DocumentDetailDto>> ToDetailListAsync(
        DocumateDbContext db,
        IReadOnlyList<Domain.OpsDocument> docs,
        CancellationToken cancellationToken)
    {
        if (docs.Count == 0)
        {
            return [];
        }

        var enumKeys = await LoadEnumKeysAsync(db, docs, cancellationToken);
        var typeKeys = await LoadTypeKeysAsync(db, docs, cancellationToken);

        return docs.Select(d => ToDetail(d, enumKeys, typeKeys)).ToList();
    }

    private static DocumentListItemDto ToListItem(
        Domain.OpsDocument d,
        IReadOnlyDictionary<long, string> enumKeys,
        IReadOnlyDictionary<long, string> typeKeys)
    {
        enumKeys.TryGetValue(d.PublicStatusEnumId, out var statusKey);
        string? stageKey = null;
        if (d.InternalStageEnumId is long sid)
        {
            enumKeys.TryGetValue(sid, out stageKey);
        }

        string? typeKey = null;
        if (d.DocumentTypeId is long tid)
        {
            typeKeys.TryGetValue(tid, out typeKey);
        }

        return new DocumentListItemDto(
            d.Id,
            d.FileId,
            d.DocumentTypeId,
            typeKey,
            d.AgentId,
            statusKey,
            stageKey,
            d.CreatedAt,
            d.CompletedAt);
    }

    private static DocumentDetailDto ToDetail(
        Domain.OpsDocument d,
        IReadOnlyDictionary<long, string> enumKeys,
        IReadOnlyDictionary<long, string> typeKeys)
    {
        enumKeys.TryGetValue(d.PublicStatusEnumId, out var statusKey);
        string? stageKey = null;
        if (d.InternalStageEnumId is long sid)
        {
            enumKeys.TryGetValue(sid, out stageKey);
        }

        string? typeKey = null;
        if (d.DocumentTypeId is long tid)
        {
            typeKeys.TryGetValue(tid, out typeKey);
        }

        JsonNode? resultJson = null;
        if (!string.IsNullOrWhiteSpace(d.ResultJson))
        {
            try
            {
                resultJson = JsonNode.Parse(d.ResultJson);
            }
            catch (System.Text.Json.JsonException)
            {
                resultJson = null;
            }
        }

        string? webhookStatusKey = null;
        if (d.WebhookStatusEnumId is long wid)
        {
            enumKeys.TryGetValue(wid, out webhookStatusKey);
        }

        return new DocumentDetailDto(
            d.Id,
            d.QueueId,
            d.FileId,
            d.BatchId,
            d.DocumentTypeId,
            typeKey,
            d.AgentId,
            d.PublicStatusEnumId,
            statusKey,
            d.InternalStageEnumId,
            stageKey,
            d.ErrorCode,
            d.ErrorMessage,
            resultJson,
            webhookStatusKey,
            d.WebhookAttempts,
            d.WebhookLastHttpStatus,
            d.CreatedAt,
            d.CompletedAt);
    }

    private static async Task<Dictionary<long, string>> LoadEnumKeysAsync(
        DocumateDbContext db,
        IReadOnlyList<Domain.OpsDocument> docs,
        CancellationToken cancellationToken)
    {
        var enumIds = docs.Select(d => d.PublicStatusEnumId)
            .Concat(docs.Where(d => d.InternalStageEnumId is not null).Select(d => d.InternalStageEnumId!.Value))
            .Concat(docs.Where(d => d.WebhookStatusEnumId is not null).Select(d => d.WebhookStatusEnumId!.Value))
            .Distinct()
            .ToList();

        return await db.CorEnums.AsNoTracking()
            .Where(e => enumIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.EnumKey, cancellationToken);
    }

    private static async Task<Dictionary<long, string>> LoadTypeKeysAsync(
        DocumateDbContext db,
        IReadOnlyList<Domain.OpsDocument> docs,
        CancellationToken cancellationToken)
    {
        var typeIds = docs.Where(d => d.DocumentTypeId is not null).Select(d => d.DocumentTypeId!.Value).Distinct().ToList();
        if (typeIds.Count == 0)
        {
            return [];
        }

        return await db.CorDocumentTypes.AsNoTracking()
            .Where(t => typeIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.DocumentTypeKey, cancellationToken);
    }
}
