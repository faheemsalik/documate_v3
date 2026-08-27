namespace Documate.Api.Modules.External.Features.Extract;

using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Pipeline;
using Documate.Api.Infrastructure.Work;
using Documate.Api.Modules.External.Features.Documents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

/// <summary>
/// External sync-wait extract (DQ-0901). Single file / single Document; wait terminal or 60s; no webhook (C2).
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = ApiKeyAuthDefaults.Scheme)]
[Route("api/v1/queues/{queueId:guid}/extract")]
public sealed class ExternalSyncExtractController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(52_428_800)]
    public async Task<ActionResult<SyncExtractResponse>> Extract(
        Guid queueId,
        IFormFile file,
        [FromForm] string? documentTypeKey,
        [FromForm] int? documentCount,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "file is required (single file). Use async POST /files for multiple files." });
        }

        try
        {
            var outcome = await mediator.Send(
                new SyncExtractCommand(queueId, file, documentTypeKey, documentCount),
                cancellationToken);
            return StatusCode(outcome.StatusCode, outcome.Body);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Queue not found", StringComparison.Ordinal))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("documentTypeKey", StringComparison.Ordinal)
            || ex.Message.Contains("Sync-wait", StringComparison.Ordinal))
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public sealed record SyncExtractResponse(
    Guid FileId,
    Guid QueueId,
    Guid? BatchId,
    string? FileStatus,
    bool TimedOut,
    IReadOnlyList<Guid> FileIds,
    IReadOnlyList<Guid> DocumentIds,
    IReadOnlyList<ExternalDocumentDto> Documents,
    string? Error);

public sealed record SyncExtractOutcome(int StatusCode, SyncExtractResponse Body);

public sealed record SyncExtractCommand(
    Guid QueueId,
    IFormFile File,
    string? DocumentTypeKey,
    int? DocumentCount) : IRequest<SyncExtractOutcome>;

public sealed class SyncExtractHandler(
    IWorkRecordService work,
    IWorkDispatcher dispatcher,
    IBusinessContext business,
    ICorEnumIdResolver enums,
    DocumateDbContext db,
    IOptions<PipelineOptions> pipeline) : IRequestHandler<SyncExtractCommand, SyncExtractOutcome>
{
    public async Task<SyncExtractOutcome> Handle(SyncExtractCommand request, CancellationToken cancellationToken)
    {
        _ = await db.OpsQueues.AsNoTracking().FirstOrDefaultAsync(
                q => q.Id == request.QueueId && q.BusinessId == business.BusinessId && !q.IsDeleted,
                cancellationToken)
            ?? throw new InvalidOperationException("Queue not found for this Business.");

        if (request.DocumentCount is int count && count > 1)
        {
            throw new InvalidOperationException("Sync-wait supports a single Document. Use async upload for multi-doc files.");
        }

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

        var sourceId = enums.Require("intake_source", "api_sync");
        var hintsJson = IntakeHints.Serialize(request.DocumentTypeKey, request.DocumentCount);

        await using var stream = request.File.OpenReadStream();
        var file = await work.CreateFileWithBlobAsync(
            new CreateFileWithBlobRequest(
                request.QueueId,
                BatchId: null,
                sourceId,
                request.File.FileName,
                request.File.ContentType,
                stream,
                request.File.Length,
                hintsJson),
            cancellationToken);

        await dispatcher.EnqueueFileAsync(
            new FileWorkItem(file.Id, business.BusinessId, business.UserId),
            cancellationToken);

        var timeout = TimeSpan.FromSeconds(Math.Clamp(pipeline.Value.SyncWaitTimeoutSeconds, 1, 120));
        var timedOut = !await WaitForTerminalAsync(file.Id, timeout, cancellationToken);
        var snapshot = await LoadSnapshotAsync(file.Id, cancellationToken);

        var error = snapshot.Documents.Count > 1
            ? "Sync-wait supports a single Document. Use async upload for multi-doc files."
            : null;
        var status = snapshot.Documents.Count > 1 ? 409 : 200;

        return new SyncExtractOutcome(
            status,
            new SyncExtractResponse(
                file.Id,
                request.QueueId,
                file.BatchId,
                snapshot.FileStatus,
                timedOut,
                [file.Id],
                snapshot.Documents.Select(d => d.Id).ToList(),
                snapshot.Documents,
                error));
    }

    private async Task<bool> WaitForTerminalAsync(Guid fileId, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var fileReady = enums.Require("file_public_status", "ready");
        var filePartial = enums.Require("file_public_status", "partial_ready");
        var fileFailed = enums.Require("file_public_status", "failed");
        var fileRejected = enums.Require("file_public_status", "rejected");
        var fileCancelled = enums.Require("file_public_status", "cancelled");
        var docReady = enums.Require("document_public_status", "ready");
        var docFailed = enums.Require("document_public_status", "failed");
        var docRejected = enums.Require("document_public_status", "rejected");
        var docCancelled = enums.Require("document_public_status", "cancelled");

        bool FileDone(long id) =>
            id == fileReady || id == filePartial || id == fileFailed || id == fileRejected || id == fileCancelled;
        bool DocDone(long id) =>
            id == docReady || id == docFailed || id == docRejected || id == docCancelled;

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var file = await db.OpsFiles.AsNoTracking().FirstAsync(f => f.Id == fileId, cancellationToken);
            var docs = await db.OpsDocuments.AsNoTracking()
                .Where(d => d.FileId == fileId && d.BusinessId == business.BusinessId && !d.IsDeleted)
                .Select(d => d.PublicStatusEnumId)
                .ToListAsync(cancellationToken);

            if (FileDone(file.PublicStatusEnumId) && (docs.Count == 0 || docs.All(DocDone)))
            {
                return true;
            }

            await Task.Delay(200, cancellationToken);
        }

        return false;
    }

    private async Task<(string? FileStatus, IReadOnlyList<ExternalDocumentDto> Documents)> LoadSnapshotAsync(
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var file = await db.OpsFiles.AsNoTracking().FirstAsync(f => f.Id == fileId, cancellationToken);
        var fileStatus = await db.CorEnums.AsNoTracking()
            .Where(e => e.Id == file.PublicStatusEnumId)
            .Select(e => e.EnumKey)
            .FirstOrDefaultAsync(cancellationToken);

        var docs = await db.OpsDocuments.AsNoTracking()
            .Where(d => d.FileId == fileId && d.BusinessId == business.BusinessId && !d.IsDeleted)
            .OrderBy(d => d.SequenceId)
            .ToListAsync(cancellationToken);

        var mapped = await ListExternalDocumentsHandler.MapAsync(db, docs, cancellationToken);
        return (fileStatus, mapped);
    }
}
