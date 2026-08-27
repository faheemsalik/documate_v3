namespace Documate.Api.Infrastructure.Pipeline.Stages;

using System.Text.Json;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Creates one Document from a single-page type hint, then routes typed Documents via QueueRoute.</summary>
public sealed class DocumentRouteStage(
    DocumateDbContext db,
    ICorEnumIdResolver enums,
    ILogger<DocumentRouteStage> logger) : IDocumentRouteStage
{
    public async Task ExecuteAsync(FilePipelineContext context, CancellationToken cancellationToken = default)
    {
        context.File.InternalStageEnumId = enums.Require("file_internal_stage", "route");
        context.File.UpdatedByUserId = context.Item.UserId;

        if (context.SkipSplitAndClassify)
        {
            await MaterializeHintDocumentsAsync(context, cancellationToken);
        }

        var routes = await db.OpsQueueRoutes.AsNoTracking()
            .Where(r => r.QueueId == context.File.QueueId && r.BusinessId == context.Item.BusinessId && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        var received = enums.Require("document_public_status", "received");
        var failed = enums.Require("document_public_status", "failed");
        var routed = 0;
        var unroutable = 0;

        foreach (var doc in context.Documents)
        {
            if (doc.DocumentTypeId is not long typeId)
            {
                continue;
            }

            var route = routes.FirstOrDefault(r => r.DocumentTypeId == typeId);
            if (route is null)
            {
                doc.PublicStatusEnumId = failed;
                doc.ErrorCode = "unroutable_type";
                doc.ErrorMessage = "No QueueRoute for this DocumentType.";
                doc.FailedStage = "route";
                doc.UpdatedByUserId = context.Item.UserId;
                unroutable++;
                continue;
            }

            doc.AgentId = route.AgentId;
            if (doc.PublicStatusEnumId == failed && doc.ErrorCode == "unroutable_type")
            {
                doc.PublicStatusEnumId = received;
                doc.ErrorCode = null;
                doc.ErrorMessage = null;
                doc.FailedStage = null;
            }

            doc.UpdatedByUserId = context.Item.UserId;
            routed++;
        }

        var payload = JsonSerializer.Serialize(new
        {
            status = "processing",
            stage = "route",
            skippedSplit = context.SkipSplitAndClassify,
            documentTypeKey = context.Hints.DocumentTypeKey,
            routed,
            unroutable,
        });
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
        logger.LogInformation(
            "Routed File {FileId}: routed={Routed} unroutable={Unroutable} skipSplit={Skip}",
            context.File.Id,
            routed,
            unroutable,
            context.SkipSplitAndClassify);
    }

    private async Task MaterializeHintDocumentsAsync(FilePipelineContext context, CancellationToken cancellationToken)
    {
        var key = context.Hints.DocumentTypeKey!;
        var type = await db.CorDocumentTypes.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DocumentTypeKey == key && d.IsActive && !d.IsDeleted, cancellationToken);

        var failed = enums.Require("document_public_status", "failed");
        var received = enums.Require("document_public_status", "received");
        var count = 1;

        if (type is null)
        {
            context.File.PublicStatusEnumId = enums.Require("file_public_status", "failed");
            context.File.ErrorCode = "unroutable_type";
            context.File.ErrorMessage = $"Unknown documentTypeKey '{key}'.";
            context.File.UpdatedByUserId = context.Item.UserId;

            foreach (var doc in context.Documents)
            {
                doc.PublicStatusEnumId = failed;
                doc.ErrorCode = "unroutable_type";
                doc.FailedStage = "route";
                doc.UpdatedByUserId = context.Item.UserId;
            }

            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        while (context.Documents.Count < count)
        {
            var doc = new OpsDocument
            {
                BusinessId = context.Item.BusinessId,
                QueueId = context.File.QueueId,
                FileId = context.File.Id,
                BatchId = context.File.BatchId,
                DocumentTypeId = type.Id,
                PublicStatusEnumId = received,
                PageStart = 1,
                PageEnd = context.Normalize?.PageCount ?? 1,
                SliceRefJson = context.SliceRefJson,
                CreatedByUserId = context.Item.UserId,
                UpdatedByUserId = context.Item.UserId,
            };
            db.OpsDocuments.Add(doc);
            context.Documents.Add(doc);
        }

        foreach (var doc in context.Documents.Take(count))
        {
            doc.DocumentTypeId = type.Id;
            doc.SliceRefJson ??= context.SliceRefJson;
            doc.PageStart ??= 1;
            doc.PageEnd ??= context.Normalize?.PageCount ?? 1;
            doc.UpdatedByUserId = context.Item.UserId;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
