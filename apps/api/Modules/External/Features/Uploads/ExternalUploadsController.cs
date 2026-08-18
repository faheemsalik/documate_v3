namespace Documate.Api.Modules.External.Features.Uploads;

using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Pipeline;
using Documate.Api.Infrastructure.Work;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/// <summary>Partner External multi-file async upload (DQ-0601). Auth: F2 API key.</summary>
[ApiController]
[Authorize(AuthenticationSchemes = ApiKeyAuthDefaults.Scheme)]
[Route("api/v1/queues/{queueId:guid}/files")]
public sealed class ExternalUploadsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(209_715_200)]
    public async Task<ActionResult<ExternalUploadAcceptedDto>> Upload(
        Guid queueId,
        [FromForm(Name = "files")] List<IFormFile> files,
        [FromForm] string? documentTypeKey,
        [FromForm] int? documentCount,
        CancellationToken cancellationToken)
    {
        var list = files?.Where(f => f is { Length: > 0 }).ToList() ?? [];
        if (list.Count == 0)
        {
            return BadRequest(new { error = "At least one non-empty file is required (form field: files)." });
        }

        try
        {
            var dto = await mediator.Send(
                new ExternalUploadFilesCommand(queueId, list, documentTypeKey, documentCount),
                cancellationToken);
            return Accepted(dto);
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
}

public sealed record ExternalUploadAcceptedDto(
    Guid QueueId,
    Guid? BatchId,
    IReadOnlyList<Guid> FileIds);

public sealed record ExternalUploadFilesCommand(
    Guid QueueId,
    IReadOnlyList<IFormFile> Files,
    string? DocumentTypeKey,
    int? DocumentCount)
    : IRequest<ExternalUploadAcceptedDto>;

public sealed class ExternalUploadFilesHandler(
    IWorkRecordService work,
    IWorkDispatcher dispatcher,
    IBusinessContext business,
    ICorEnumIdResolver enums,
    DocumateDbContext db) : IRequestHandler<ExternalUploadFilesCommand, ExternalUploadAcceptedDto>
{
    public async Task<ExternalUploadAcceptedDto> Handle(
        ExternalUploadFilesCommand request,
        CancellationToken cancellationToken)
    {
        _ = await db.OpsQueues.AsNoTracking().FirstOrDefaultAsync(
                q => q.Id == request.QueueId && q.BusinessId == business.BusinessId && !q.IsDeleted,
                cancellationToken)
            ?? throw new InvalidOperationException("Queue not found for this Business.");

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

        var sourceId = enums.Require("intake_source", "api");
        var n = request.Files.Count;
        var hintsJson = IntakeHints.Serialize(request.DocumentTypeKey, request.DocumentCount);

        var batch = await work.CreateBatchAsync(request.QueueId, sourceId, n, emailMessageId: null, cancellationToken);
        var fileIds = new List<Guid>(n);

        foreach (var formFile in request.Files)
        {
            await using var stream = formFile.OpenReadStream();
            var file = await work.CreateFileWithBlobAsync(
                new CreateFileWithBlobRequest(
                    request.QueueId,
                    batch?.Id,
                    sourceId,
                    formFile.FileName,
                    formFile.ContentType,
                    stream,
                    formFile.Length,
                    hintsJson),
                cancellationToken);

            await dispatcher.EnqueueFileAsync(
                new FileWorkItem(file.Id, business.BusinessId, business.UserId),
                cancellationToken);

            fileIds.Add(file.Id);
        }

        return new ExternalUploadAcceptedDto(request.QueueId, batch?.Id, fileIds);
    }
}
