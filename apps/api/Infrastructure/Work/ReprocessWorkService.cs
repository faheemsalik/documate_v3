namespace Documate.Api.Infrastructure.Work;

using System.Text.Json;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Pipeline;
using Documate.Api.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

public sealed record ReprocessFileResult(OpsFile NewFile, Guid SourceFileId);

public interface IReprocessWorkService
{
    /// <summary>
    /// Plan 02 §11.3: explicit reprocess — new File (same bytes), ReprocessOfFileId link, enqueue pipeline.
    /// Single-file → no Batch. Source File is not cancelled.
    /// </summary>
    Task<ReprocessFileResult?> ReprocessFileAsync(
        Guid sourceFileId,
        string businessId,
        string? userId,
        CancellationToken cancellationToken = default);
}

public sealed class ReprocessWorkService(
    DocumateDbContext db,
    IWorkRecordService work,
    IObjectStorage storage,
    IWorkDispatcher dispatcher,
    ICorEnumIdResolver enums,
    ILogger<ReprocessWorkService> logger) : IReprocessWorkService
{
    public async Task<ReprocessFileResult?> ReprocessFileAsync(
        Guid sourceFileId,
        string businessId,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var source = await db.OpsFiles.AsNoTracking().FirstOrDefaultAsync(
            f => f.Id == sourceFileId && f.BusinessId == businessId && !f.IsDeleted,
            cancellationToken);
        if (source is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(source.StorageBucket) || string.IsNullOrWhiteSpace(source.StorageKey))
        {
            throw new InvalidOperationException("Source file has no storage location; cannot reprocess.");
        }

        await using var blob = await storage.DownloadAsync(source.StorageBucket, source.StorageKey, cancellationToken);

        // Buffer so CreateFileWithBlob can seek/size reliably for non-seekable downloads.
        await using var buffer = new MemoryStream();
        await blob.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var newFile = await work.CreateFileWithBlobAsync(
            new CreateFileWithBlobRequest(
                source.QueueId,
                BatchId: null,
                source.SourceEnumId,
                source.OriginalFileName ?? "reprocess.bin",
                source.ContentType,
                buffer,
                buffer.Length,
                source.IntakeHintsJson,
                ReprocessOfFileId: source.Id,
                ContentHash: source.ContentHash),
            cancellationToken);

        await work.AppendWorkEventAsync(
            new AppendWorkEventRequest(
                enums.Require("work_subject_type", "file"),
                newFile.Id,
                enums.Require("work_event_type", "status_changed"),
                JsonSerializer.Serialize(new
                {
                    status = "received",
                    reprocessOfFileId = source.Id,
                })),
            cancellationToken);

        await dispatcher.EnqueueFileAsync(
            new FileWorkItem(newFile.Id, businessId, userId),
            cancellationToken);

        logger.LogInformation(
            "Reprocessed File {SourceFileId} → new File {NewFileId}",
            source.Id,
            newFile.Id);

        return new ReprocessFileResult(newFile, source.Id);
    }
}
