namespace Documate.Api.Modules.External.Features.Files;

using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/// <summary>External File poll/list/get (DQ-0602).</summary>
[ApiController]
[Authorize(AuthenticationSchemes = ApiKeyAuthDefaults.Scheme)]
[Route("api/v1")]
public sealed class ExternalFilesController(IMediator mediator) : ControllerBase
{
    [HttpGet("queues/{queueId:guid}/files")]
    public async Task<ActionResult<IReadOnlyList<ExternalFileDto>>> List(
        Guid queueId,
        [FromQuery] string? status,
        [FromQuery] Guid? batchId,
        [FromQuery] DateTimeOffset? createdFrom,
        [FromQuery] DateTimeOffset? createdTo,
        CancellationToken cancellationToken)
    {
        var items = await mediator.Send(
            new ListExternalFilesQuery(queueId, status, batchId, createdFrom, createdTo),
            cancellationToken);
        return Ok(items);
    }

    [HttpGet("files/{fileId:guid}")]
    public async Task<ActionResult<ExternalFileDto>> Get(Guid fileId, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetExternalFileQuery(fileId), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }
}

public sealed record ExternalFileDto(
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

public sealed record ListExternalFilesQuery(
    Guid QueueId,
    string? StatusKey,
    Guid? BatchId,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo) : IRequest<IReadOnlyList<ExternalFileDto>>;

public sealed record GetExternalFileQuery(Guid FileId) : IRequest<ExternalFileDto?>;

public sealed class ListExternalFilesHandler(DocumateDbContext db, IBusinessContext business, ICorEnumIdResolver enums)
    : IRequestHandler<ListExternalFilesQuery, IReadOnlyList<ExternalFileDto>>
{
    public async Task<IReadOnlyList<ExternalFileDto>> Handle(ListExternalFilesQuery request, CancellationToken cancellationToken)
    {
        var q = db.OpsFiles.AsNoTracking()
            .Where(f => f.QueueId == request.QueueId && f.BusinessId == business.BusinessId && !f.IsDeleted);

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
                return [];
            }
        }

        var rows = await q.OrderByDescending(f => f.CreatedAt).Take(200).ToListAsync(cancellationToken);
        return await ExternalFileDtoMapping.MapManyAsync(db, rows, cancellationToken);
    }
}

public sealed class GetExternalFileHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<GetExternalFileQuery, ExternalFileDto?>
{
    public async Task<ExternalFileDto?> Handle(GetExternalFileQuery request, CancellationToken cancellationToken)
    {
        var file = await db.OpsFiles.AsNoTracking().FirstOrDefaultAsync(
            f => f.Id == request.FileId && f.BusinessId == business.BusinessId && !f.IsDeleted,
            cancellationToken);
        if (file is null)
        {
            return null;
        }

        var list = await ExternalFileDtoMapping.MapManyAsync(db, [file], cancellationToken);
        return list.FirstOrDefault();
    }
}

file static class ExternalFileDtoMapping
{
    public static async Task<IReadOnlyList<ExternalFileDto>> MapManyAsync(
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
            return new ExternalFileDto(
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
