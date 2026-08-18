namespace Documate.Api.Modules.External.Features.Documents;

using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/// <summary>External Document poll/list/get (DQ-0602).</summary>
[ApiController]
[Authorize(AuthenticationSchemes = ApiKeyAuthDefaults.Scheme)]
[Route("api/v1")]
public sealed class ExternalDocumentsController(IMediator mediator) : ControllerBase
{
    [HttpGet("queues/{queueId:guid}/documents")]
    public async Task<ActionResult<IReadOnlyList<ExternalDocumentDto>>> List(
        Guid queueId,
        [FromQuery] Guid? fileId,
        [FromQuery] Guid? batchId,
        [FromQuery] string? status,
        [FromQuery] DateTimeOffset? createdFrom,
        [FromQuery] DateTimeOffset? createdTo,
        CancellationToken cancellationToken)
    {
        var items = await mediator.Send(
            new ListExternalDocumentsQuery(queueId, fileId, batchId, status, createdFrom, createdTo),
            cancellationToken);
        return Ok(items);
    }

    [HttpGet("documents/{documentId:guid}")]
    public async Task<ActionResult<ExternalDocumentDto>> Get(Guid documentId, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetExternalDocumentQuery(documentId), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }
}

public sealed record ExternalDocumentDto(
    Guid Id,
    Guid QueueId,
    Guid FileId,
    Guid? BatchId,
    long? DocumentTypeId,
    string? DocumentTypeKey,
    Guid? AgentId,
    string? PublicStatusKey,
    string? InternalStageKey,
    string? ErrorCode,
    string? ErrorMessage,
    System.Text.Json.Nodes.JsonNode? ResultJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed record ListExternalDocumentsQuery(
    Guid QueueId,
    Guid? FileId,
    Guid? BatchId,
    string? StatusKey,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo) : IRequest<IReadOnlyList<ExternalDocumentDto>>;

public sealed record GetExternalDocumentQuery(Guid DocumentId) : IRequest<ExternalDocumentDto?>;

public sealed class ListExternalDocumentsHandler(DocumateDbContext db, IBusinessContext business, ICorEnumIdResolver enums)
    : IRequestHandler<ListExternalDocumentsQuery, IReadOnlyList<ExternalDocumentDto>>
{
    public async Task<IReadOnlyList<ExternalDocumentDto>> Handle(
        ListExternalDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var q = db.OpsDocuments.AsNoTracking()
            .Where(d => d.QueueId == request.QueueId && d.BusinessId == business.BusinessId && !d.IsDeleted);

        if (request.FileId is Guid fileId)
        {
            q = q.Where(d => d.FileId == fileId);
        }

        if (request.BatchId is Guid batchId)
        {
            q = q.Where(d => d.BatchId == batchId);
        }

        if (request.CreatedFrom is DateTimeOffset from)
        {
            q = q.Where(d => d.CreatedAt >= from);
        }

        if (request.CreatedTo is DateTimeOffset to)
        {
            q = q.Where(d => d.CreatedAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(request.StatusKey))
        {
            try
            {
                var statusId = enums.Require("document_public_status", request.StatusKey);
                q = q.Where(d => d.PublicStatusEnumId == statusId);
            }
            catch (InvalidOperationException)
            {
                return [];
            }
        }

        var rows = await q.OrderByDescending(d => d.CreatedAt).Take(200).ToListAsync(cancellationToken);
        return await MapAsync(db, rows, cancellationToken);
    }

    internal static async Task<IReadOnlyList<ExternalDocumentDto>> MapAsync(
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

            System.Text.Json.Nodes.JsonNode? resultJson = null;
            if (!string.IsNullOrWhiteSpace(d.ResultJson))
            {
                try
                {
                    resultJson = System.Text.Json.Nodes.JsonNode.Parse(d.ResultJson);
                }
                catch (System.Text.Json.JsonException)
                {
                    resultJson = null;
                }
            }

            return new ExternalDocumentDto(
                d.Id,
                d.QueueId,
                d.FileId,
                d.BatchId,
                d.DocumentTypeId,
                typeKey,
                d.AgentId,
                statusKey,
                stageKey,
                d.ErrorCode,
                d.ErrorMessage,
                resultJson,
                d.CreatedAt,
                d.CompletedAt);
        }).ToList();
    }
}

public sealed class GetExternalDocumentHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<GetExternalDocumentQuery, ExternalDocumentDto?>
{
    public async Task<ExternalDocumentDto?> Handle(GetExternalDocumentQuery request, CancellationToken cancellationToken)
    {
        var doc = await db.OpsDocuments.AsNoTracking().FirstOrDefaultAsync(
            d => d.Id == request.DocumentId && d.BusinessId == business.BusinessId && !d.IsDeleted,
            cancellationToken);
        if (doc is null)
        {
            return null;
        }

        var list = await ListExternalDocumentsHandler.MapAsync(db, [doc], cancellationToken);
        return list.FirstOrDefault();
    }
}
