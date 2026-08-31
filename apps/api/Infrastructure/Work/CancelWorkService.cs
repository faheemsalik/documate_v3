namespace Documate.Api.Infrastructure.Work;

using System.Text.Json;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Webhooks;
using Microsoft.EntityFrameworkCore;

public sealed record CancelFileResult(
    bool Found,
    OpsFile File,
    IReadOnlyList<OpsDocument> NewlyCancelledDocuments);

public sealed record CancelDocumentResult(
    bool Found,
    bool Conflict,
    string? ConflictReason,
    OpsDocument? Document,
    OpsFile? File);

public interface ICancelWorkService
{
    Task<CancelFileResult?> CancelFileAsync(
        Guid fileId,
        string businessId,
        string? cancelledByUserId,
        CancellationToken cancellationToken = default);

    Task<CancelDocumentResult> CancelDocumentAsync(
        Guid documentId,
        string businessId,
        string? cancelledByUserId,
        CancellationToken cancellationToken = default);
}

/// <summary>Plan 02 §11 cancel file / cancel document (DQ-1001).</summary>
public sealed class CancelWorkService(
    DocumateDbContext db,
    ICorEnumIdResolver enums,
    IDocumentWebhookScheduler webhooks,
    ILogger<CancelWorkService> logger) : ICancelWorkService
{
    public async Task<CancelFileResult?> CancelFileAsync(
        Guid fileId,
        string businessId,
        string? cancelledByUserId,
        CancellationToken cancellationToken = default)
    {
        var file = await db.OpsFiles.FirstOrDefaultAsync(
            f => f.Id == fileId && f.BusinessId == businessId && !f.IsDeleted,
            cancellationToken);
        if (file is null)
        {
            return null;
        }

        var ids = FilePublicStatusRollup.Resolve(enums);
        var docs = await db.OpsDocuments
            .Where(d => d.FileId == file.Id && d.BusinessId == businessId && !d.IsDeleted)
            .ToListAsync(cancellationToken);

        if (file.PublicStatusEnumId == ids.FileCancelled)
        {
            return new CancelFileResult(true, file, []);
        }

        var now = DateTimeOffset.UtcNow;
        var newlyCancelled = new List<OpsDocument>();

        foreach (var doc in docs)
        {
            if (IsDocumentTerminal(doc, ids))
            {
                continue;
            }

            MarkDocumentCancelled(doc, ids, cancelledByUserId, now);
            newlyCancelled.Add(doc);
        }

        file.PublicStatusEnumId = ids.FileCancelled;
        file.ErrorCode = "cancelled";
        file.ErrorMessage = "File cancelled.";
        file.CancelledAt = now;
        file.CancelledByUserId = cancelledByUserId;
        file.CompletedAt = now;
        file.UpdatedByUserId = cancelledByUserId;

        await db.SaveChangesAsync(cancellationToken);

        await AppendEventAsync(
            businessId,
            enums.Require("work_subject_type", "file"),
            file.Id,
            enums.Require("work_event_type", "cancelled"),
            JsonSerializer.Serialize(new
            {
                status = "cancelled",
                scope = "file",
                newlyCancelledDocumentCount = newlyCancelled.Count,
            }),
            cancelledByUserId,
            cancellationToken);

        foreach (var doc in newlyCancelled)
        {
            await AppendEventAsync(
                businessId,
                enums.Require("work_subject_type", "document"),
                doc.Id,
                enums.Require("work_event_type", "cancelled"),
                """{"status":"cancelled","scope":"file"}""",
                cancelledByUserId,
                cancellationToken);
            await webhooks.ScheduleIfTerminalAsync(doc, file, cancellationToken);
        }

        logger.LogInformation(
            "Cancelled File {FileId}; newlyCancelledDocs={Count}",
            file.Id,
            newlyCancelled.Count);

        return new CancelFileResult(true, file, newlyCancelled);
    }

    public async Task<CancelDocumentResult> CancelDocumentAsync(
        Guid documentId,
        string businessId,
        string? cancelledByUserId,
        CancellationToken cancellationToken = default)
    {
        var doc = await db.OpsDocuments.FirstOrDefaultAsync(
            d => d.Id == documentId && d.BusinessId == businessId && !d.IsDeleted,
            cancellationToken);
        if (doc is null)
        {
            return new CancelDocumentResult(false, false, null, null, null);
        }

        var file = await db.OpsFiles.FirstOrDefaultAsync(
            f => f.Id == doc.FileId && f.BusinessId == businessId && !f.IsDeleted,
            cancellationToken);
        if (file is null)
        {
            return new CancelDocumentResult(false, false, null, null, null);
        }

        var ids = FilePublicStatusRollup.Resolve(enums);

        if (doc.PublicStatusEnumId == ids.DocCancelled)
        {
            return new CancelDocumentResult(true, false, null, doc, file);
        }

        if (doc.PublicStatusEnumId == ids.DocReady
            || doc.PublicStatusEnumId == ids.DocFailed
            || doc.PublicStatusEnumId == ids.DocRejected)
        {
            return new CancelDocumentResult(
                true,
                true,
                "Document is already terminal; cannot cancel.",
                doc,
                file);
        }

        if (file.PublicStatusEnumId == ids.FileCancelled)
        {
            return new CancelDocumentResult(
                true,
                true,
                "File is already cancelled.",
                doc,
                file);
        }

        var now = DateTimeOffset.UtcNow;
        MarkDocumentCancelled(doc, ids, cancelledByUserId, now);
        await db.SaveChangesAsync(cancellationToken);

        await AppendEventAsync(
            businessId,
            enums.Require("work_subject_type", "document"),
            doc.Id,
            enums.Require("work_event_type", "cancelled"),
            """{"status":"cancelled","scope":"document"}""",
            cancelledByUserId,
            cancellationToken);

        var siblings = await db.OpsDocuments
            .Where(d => d.FileId == file.Id && d.BusinessId == businessId && !d.IsDeleted)
            .ToListAsync(cancellationToken);
        FilePublicStatusRollup.Apply(file, siblings, ids);
        file.UpdatedByUserId = cancelledByUserId;
        await db.SaveChangesAsync(cancellationToken);

        await AppendEventAsync(
            businessId,
            enums.Require("work_subject_type", "file"),
            file.Id,
            enums.Require("work_event_type", "status_changed"),
            JsonSerializer.Serialize(new
            {
                status = "rollup",
                after = "cancel.document",
                documentId = doc.Id,
            }),
            cancelledByUserId,
            cancellationToken);

        await webhooks.ScheduleIfTerminalAsync(doc, file, cancellationToken);

        logger.LogInformation(
            "Cancelled Document {DocumentId}; File {FileId} rollup applied",
            doc.Id,
            file.Id);

        return new CancelDocumentResult(true, false, null, doc, file);
    }

    private static bool IsDocumentTerminal(OpsDocument doc, FilePublicStatusRollup.StatusIds ids) =>
        doc.PublicStatusEnumId == ids.DocReady
        || doc.PublicStatusEnumId == ids.DocFailed
        || doc.PublicStatusEnumId == ids.DocRejected
        || doc.PublicStatusEnumId == ids.DocCancelled;

    private static void MarkDocumentCancelled(
        OpsDocument doc,
        FilePublicStatusRollup.StatusIds ids,
        string? userId,
        DateTimeOffset now)
    {
        doc.PublicStatusEnumId = ids.DocCancelled;
        doc.ErrorCode = "cancelled";
        doc.ErrorMessage = "Document cancelled.";
        doc.FailedStage = null;
        doc.CancelledAt = now;
        doc.CancelledByUserId = userId;
        doc.CompletedAt = now;
        doc.UpdatedByUserId = userId;
    }

    private async Task AppendEventAsync(
        string businessId,
        long subjectType,
        Guid subjectId,
        long eventType,
        string payload,
        string? userId,
        CancellationToken cancellationToken)
    {
        db.OpsWorkEvents.Add(new OpsWorkEvent
        {
            BusinessId = businessId,
            SubjectTypeEnumId = subjectType,
            SubjectId = subjectId,
            EventTypeEnumId = eventType,
            PayloadJson = payload,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
