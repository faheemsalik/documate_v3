namespace Documate.Api.Infrastructure.Pipeline.Stages;

using System.Text.Json;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Intelligence;
using Documate.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Classify stage: skipped only when documentTypeKey is set and the File has one page.
/// Typed multi-page Files stamp the caller type after grouping. Untyped Files classify from
/// QueueRoute C1 or each group's page-intelligence type.
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

        var routes = await db.OpsQueueRoutes.AsNoTracking()
            .Where(r => r.QueueId == context.File.QueueId
                && r.BusinessId == context.Item.BusinessId
                && !r.IsDeleted)
            .ToListAsync(cancellationToken);
        var routeTypeIds = routes.Select(x => x.DocumentTypeId).Distinct().ToArray();
        var types = await db.CorDocumentTypes.AsNoTracking()
            .Where(x => routeTypeIds.Contains(x.Id) && x.IsActive && !x.IsDeleted)
            .ToDictionaryAsync(x => x.DocumentTypeKey, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var failed = enums.Require("document_public_status", "failed");
        var classified = 0;
        var unroutable = 0;

        foreach (var doc in context.Documents)
        {
            if (doc.ErrorCode == "split_unresolved")
            {
                continue;
            }

            CorDocumentType? type = null;
            if (routeTypeIds.Length == 1)
            {
                type = types.Values.FirstOrDefault(x => x.Id == routeTypeIds[0]);
            }
            else
            {
                var identifiedType = context.IntelligenceProfiles
                    .Where(x => x.Page >= doc.PageStart && x.Page <= doc.PageEnd)
                    .Select(x => x.DocumentType)
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
                type = QueueDocumentTypeResolver.Resolve(identifiedType, types);
            }

            if (type is null)
            {
                doc.PublicStatusEnumId = failed;
                doc.ErrorCode = "unroutable_type";
                doc.ErrorMessage = "Page intelligence did not identify a type configured in QueueRoute.";
                doc.FailedStage = "classify";
                unroutable++;
            }
            else
            {
                doc.DocumentTypeId = type.Id;
                classified++;
            }

            doc.UpdatedByUserId = context.Item.UserId;
        }

        await AppendEventAsync(
            context,
            JsonSerializer.Serialize(new
            {
                status = "processing",
                stage = "classify",
                strategy = routeTypeIds.Length == 1 ? "single_queue_route" : "group_intelligence",
                classified,
                unroutable,
            }),
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Classified File {FileId}: classified={Classified} unroutable={Unroutable}",
            context.File.Id,
            classified,
            unroutable);
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
