namespace Documate.Api.Infrastructure.Pipeline.Stages;

using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Split stage: skipped only when documentTypeKey is set and the File has one page.
/// Multi-page Files still split (Phase 1: one placeholder until real split).
/// </summary>
public sealed class FileSplitStage(
    DocumateDbContext db,
    ICorEnumIdResolver enums,
    ILogger<FileSplitStage> logger) : IFileSplitStage
{
    public async Task ExecuteAsync(FilePipelineContext context, CancellationToken cancellationToken = default)
    {
        context.File.InternalStageEnumId = enums.Require("file_internal_stage", "split");
        context.File.UpdatedByUserId = context.Item.UserId;

        if (context.SkipSplitAndClassify)
        {
            await AppendEventAsync(
                context,
                """{"status":"processing","stage":"split","skipped":true,"reason":"predetermined_type_single_page"}""",
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Split skipped for File {FileId} (type {Type}, pageCount=1)",
                context.File.Id,
                context.Hints.DocumentTypeKey);
            return;
        }

        await EnsurePlaceholderDocumentsAsync(context, cancellationToken);
        var pageCount = context.Normalize?.PageCount ?? 0;
        await AppendEventAsync(
            context,
            $$"""{"status":"processing","stage":"split","deferred":true,"reason":"real_split_not_implemented","pageCount":{{pageCount}}}""",
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Split deferred for File {FileId}; placeholder Document count={Count} pageCount={PageCount}",
            context.File.Id,
            context.Documents.Count,
            pageCount);
    }

    private async Task EnsurePlaceholderDocumentsAsync(FilePipelineContext context, CancellationToken cancellationToken)
    {
        if (context.Documents.Count > 0)
        {
            return;
        }

        var received = enums.Require("document_public_status", "received");
        var doc = new OpsDocument
        {
            BusinessId = context.Item.BusinessId,
            QueueId = context.File.QueueId,
            FileId = context.File.Id,
            BatchId = context.File.BatchId,
            PublicStatusEnumId = received,
            PageStart = 1,
            PageEnd = context.Normalize?.PageCount ?? 1,
            SliceRefJson = context.SliceRefJson,
            CreatedByUserId = context.Item.UserId,
            UpdatedByUserId = context.Item.UserId,
        };
        db.OpsDocuments.Add(doc);
        await db.SaveChangesAsync(cancellationToken);
        context.Documents.Add(doc);
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
