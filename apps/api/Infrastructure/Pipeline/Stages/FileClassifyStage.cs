namespace Documate.Api.Infrastructure.Pipeline.Stages;

using System.Text.Json;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Classify stage: skipped only when documentTypeKey is set and the File has one page.
/// Typed multi-page Files still run this stage so split can produce multiple same-type Documents;
/// Phase 1 stamps the caller type onto split outputs (real classify later).
/// </summary>
public sealed class FileClassifyStage(
    DocumateDbContext db,
    ICorEnumIdResolver enums,
    ILogger<FileClassifyStage> logger) : IFileClassifyStage
{
    public async Task ExecuteAsync(FilePipelineContext context, CancellationToken cancellationToken = default)
    {
        context.File.InternalStageEnumId = enums.Require("file_internal_stage", "classify");
        context.File.UpdatedByUserId = context.Item.UserId;

        if (context.SkipSplitAndClassify)
        {
            await AppendEventAsync(
                context,
                """{"status":"processing","stage":"classify","skipped":true,"reason":"predetermined_type_single_page"}""",
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Classify skipped for File {FileId} (type + one page)", context.File.Id);
            return;
        }

        if (context.Hints.HasPredeterminedType)
        {
            await ApplyTypeHintAsync(context, cancellationToken);
            return;
        }

        await AppendEventAsync(
            context,
            """{"status":"processing","stage":"classify","deferred":true,"reason":"real_classify_not_implemented"}""",
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Classify deferred for File {FileId}", context.File.Id);
    }

    private async Task ApplyTypeHintAsync(FilePipelineContext context, CancellationToken cancellationToken)
    {
        var key = context.Hints.DocumentTypeKey!;
        var type = await db.CorDocumentTypes.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DocumentTypeKey == key && d.IsActive && !d.IsDeleted, cancellationToken);

        if (type is null)
        {
            var failed = enums.Require("document_public_status", "failed");
            context.File.PublicStatusEnumId = enums.Require("file_public_status", "failed");
            context.File.ErrorCode = "unroutable_type";
            context.File.ErrorMessage = $"Unknown documentTypeKey '{key}'.";
            context.File.UpdatedByUserId = context.Item.UserId;
            foreach (var doc in context.Documents)
            {
                doc.PublicStatusEnumId = failed;
                doc.ErrorCode = "unroutable_type";
                doc.FailedStage = "classify";
                doc.UpdatedByUserId = context.Item.UserId;
            }

            await AppendEventAsync(
                context,
                JsonSerializer.Serialize(new
                {
                    status = "failed",
                    stage = "classify",
                    errorCode = "unroutable_type",
                    documentTypeKey = key,
                }),
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        foreach (var doc in context.Documents)
        {
            doc.DocumentTypeId = type.Id;
            doc.SliceRefJson ??= context.SliceRefJson;
            doc.UpdatedByUserId = context.Item.UserId;
        }

        await AppendEventAsync(
            context,
            JsonSerializer.Serialize(new
            {
                status = "processing",
                stage = "classify",
                skipped = false,
                reason = "type_hint_after_split",
                documentTypeKey = key,
                pageCount = context.Normalize?.PageCount ?? 0,
            }),
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Classify applied type hint {Type} to File {FileId} after split (pageCount={PageCount})",
            key,
            context.File.Id,
            context.Normalize?.PageCount);
    }

    private async Task AppendEventAsync(FilePipelineContext context, string payload, CancellationToken cancellationToken)
    {
        db.OpsWorkEvents.Add(new OpsWorkEvent
        {
            BusinessId = context.Item.BusinessId,
            SubjectTypeEnumId = enums.Require("work_subject_type", "file"),
            SubjectId = context.File.Id,
            EventTypeEnumId = enums.Require("work_event_type", "status_changed"),
            PayloadJson = payload,
            CreatedByUserId = context.Item.UserId,
            UpdatedByUserId = context.Item.UserId,
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
