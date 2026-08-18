namespace Documate.Api.Infrastructure.Pipeline.Stages;

using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Classify stage: skipped when documentTypeKey was supplied.
/// Without a type, Phase 1 leaves Documents untyped (real classify later).
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
                """{"status":"processing","stage":"classify","skipped":true,"reason":"predetermined_document_type"}""",
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Classify skipped for File {FileId}", context.File.Id);
            return;
        }

        await AppendEventAsync(
            context,
            """{"status":"processing","stage":"classify","deferred":true,"reason":"real_classify_not_implemented"}""",
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Classify deferred for File {FileId}", context.File.Id);
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
