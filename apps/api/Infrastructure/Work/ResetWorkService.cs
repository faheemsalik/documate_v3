namespace Documate.Api.Infrastructure.Work;

using System.Text.Json;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Pipeline;
using Microsoft.EntityFrameworkCore;

public sealed record ResetFileResult(OpsFile File, int SoftDeletedDocumentCount);

public interface IResetWorkService
{
    /// <summary>
    /// In-place reset: soft-delete Documents, clear File errors/cancel, set status to received,
    /// re-enqueue the same FileId. Webhooks will fire again when new Documents become terminal.
    /// </summary>
    Task<ResetFileResult?> ResetFileAsync(
        Guid fileId,
        string businessId,
        string? userId,
        CancellationToken cancellationToken = default);
}

public sealed class ResetWorkService(
    DocumateDbContext db,
    ICorEnumIdResolver enums,
    IWorkDispatcher dispatcher,
    ILogger<ResetWorkService> logger) : IResetWorkService
{
    public async Task<ResetFileResult?> ResetFileAsync(
        Guid fileId,
        string businessId,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var file = await db.OpsFiles.FirstOrDefaultAsync(
            f => f.Id == fileId && f.BusinessId == businessId && !f.IsDeleted,
            cancellationToken);
        if (file is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(file.StorageBucket) || string.IsNullOrWhiteSpace(file.StorageKey))
        {
            throw new InvalidOperationException("File has no storage location; cannot reset.");
        }

        var now = DateTimeOffset.UtcNow;
        var docs = await db.OpsDocuments
            .Where(d => d.FileId == file.Id && d.BusinessId == businessId && !d.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var doc in docs)
        {
            doc.IsDeleted = true;
            doc.DeletedAt = now;
            doc.DeletedByUserId = userId;
            doc.UpdatedByUserId = userId;
        }

        var receivedStatus = enums.Require("file_public_status", "received");
        var receivedStage = enums.Require("file_internal_stage", "received");

        file.PublicStatusEnumId = receivedStatus;
        file.InternalStageEnumId = receivedStage;
        file.ErrorCode = null;
        file.ErrorMessage = null;
        file.CancelledAt = null;
        file.CancelledByUserId = null;
        file.CompletedAt = null;
        file.DownloadUrl = null;
        file.DownloadUrlExpiresAt = null;
        file.UpdatedByUserId = userId;

        db.OpsWorkEvents.Add(new OpsWorkEvent
        {
            BusinessId = businessId,
            SubjectTypeEnumId = enums.Require("work_subject_type", "file"),
            SubjectId = file.Id,
            EventTypeEnumId = enums.Require("work_event_type", "status_changed"),
            PayloadJson = JsonSerializer.Serialize(new
            {
                status = "received",
                reset = true,
                softDeletedDocumentCount = docs.Count,
            }),
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
        });

        await db.SaveChangesAsync(cancellationToken);

        await dispatcher.EnqueueFileAsync(
            new FileWorkItem(file.Id, businessId, userId),
            cancellationToken);

        logger.LogInformation(
            "Reset File {FileId} in place; softDeletedDocs={Count}",
            file.Id,
            docs.Count);

        return new ResetFileResult(file, docs.Count);
    }
}
