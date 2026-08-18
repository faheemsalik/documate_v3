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
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(52_428_800)]
    public async Task<ActionResult<FileDto>> Upload(
        Guid queueId,
        IFormFile file,
        [FromForm] string? documentTypeKey,
        [FromForm] int? documentCount,
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
                    documentCount),
                cancellationToken);
            return CreatedAtAction(nameof(Get), new { queueId, fileId = dto.Id }, dto);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Queue not found", StringComparison.Ordinal))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("documentTypeKey", StringComparison.Ordinal))
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
    string? InternalStageKey);

public sealed record FileDownloadUrlDto(Guid FileId, string Url);

public sealed record UploadFileCommand(
    Guid QueueId,
    string FileName,
    string? ContentType,
    Stream Content,
    long SizeBytes,
    string? DocumentTypeKey,
    int? DocumentCount) : IRequest<FileDto>;

public sealed record GetFileQuery(Guid QueueId, Guid FileId) : IRequest<FileDto?>;
public sealed record GetFileDownloadUrlQuery(Guid QueueId, Guid FileId) : IRequest<FileDownloadUrlDto?>;

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

        await dispatcher.EnqueueFileAsync(
            new FileWorkItem(file.Id, business.BusinessId, business.UserId),
            cancellationToken);

        return await FileDtoMapping.ToDto(db, file, cancellationToken);
    }
}

public sealed class GetFileHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<GetFileQuery, FileDto?>
{
    public async Task<FileDto?> Handle(GetFileQuery request, CancellationToken cancellationToken)
    {
        var file = await db.OpsFiles.AsNoTracking().FirstOrDefaultAsync(
            f => f.Id == request.FileId && f.QueueId == request.QueueId && f.BusinessId == business.BusinessId,
            cancellationToken);
        return file is null ? null : await FileDtoMapping.ToDto(db, file, cancellationToken);
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
            f => f.Id == request.FileId && f.QueueId == request.QueueId && f.BusinessId == business.BusinessId,
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
        var statusIds = new List<long> { file.PublicStatusEnumId };
        if (file.InternalStageEnumId is long stageId)
        {
            statusIds.Add(stageId);
        }

        var enums = await db.CorEnums.AsNoTracking()
            .Where(e => statusIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.EnumKey, cancellationToken);

        enums.TryGetValue(file.PublicStatusEnumId, out var publicKey);
        string? stageKey = null;
        if (file.InternalStageEnumId is long sid)
        {
            enums.TryGetValue(sid, out stageKey);
        }

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
            publicKey,
            file.InternalStageEnumId,
            stageKey);
    }
}
