namespace Documate.Api.Infrastructure.Pipeline;

using System.Text.Json;
using Documate.Api.Infrastructure.Ocr;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Pipeline.Stages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

/// <summary>
/// Hangfire File worker: normalize → split → classify → route → extract stub.
/// Predetermined documentTypeKey skips split and classify (DQ-0702 Phase 1).
/// </summary>
public interface IFilePipelineStub
{
    Task ProcessAsync(FileWorkItem item, CancellationToken cancellationToken = default);
}

public sealed class FilePipelineStub(
    DocumateDbContext db,
    ICorEnumIdResolver enums,
    IOcrNormalizeAdapter ocr,
    IFileSplitStage split,
    IFileClassifyStage classify,
    IDocumentRouteStage route,
    IOptions<PipelineOptions> options,
    ILogger<FilePipelineStub> logger) : IFilePipelineStub
{
    public async Task ProcessAsync(FileWorkItem item, CancellationToken cancellationToken = default)
    {
        var delay = Math.Max(0, options.Value.StubStageDelayMs);
        var file = await db.OpsFiles.FirstOrDefaultAsync(
            f => f.Id == item.FileId && f.BusinessId == item.BusinessId && !f.IsDeleted,
            cancellationToken);

        if (file is null)
        {
            logger.LogWarning("File {FileId} not found for business {BusinessId}", item.FileId, item.BusinessId);
            return;
        }

        var processing = enums.Require("file_public_status", "processing");
        var ready = enums.Require("file_public_status", "ready");
        var failed = enums.Require("file_public_status", "failed");
        var partialReady = enums.Require("file_public_status", "partial_ready");

        if (file.PublicStatusEnumId == ready)
        {
            logger.LogInformation("File {FileId} already ready; skipping pipeline", file.Id);
            return;
        }

        var context = new FilePipelineContext
        {
            Item = item,
            File = file,
            Hints = IntakeHints.Parse(file.IntakeHintsJson),
        };

        var existingDocs = await db.OpsDocuments
            .Where(d => d.FileId == file.Id && d.BusinessId == item.BusinessId && !d.IsDeleted)
            .ToListAsync(cancellationToken);
        context.Documents.AddRange(existingDocs);

        await SetStageAsync(context, processing, "normalize", cancellationToken);

        try
        {
            context.Normalize = await ocr.NormalizeAsync(
                new NormalizeRequest(
                    item.BusinessId,
                    file.Id,
                    file.SequenceId,
                    file.StorageBucket,
                    file.StorageKey,
                    file.ContentType,
                    file.OriginalFileName),
                cancellationToken);

            var providerId = await db.CorProviders.AsNoTracking()
                .Where(p => p.ProviderKey == context.Normalize.ProviderKey && p.IsActive)
                .Select(p => (long?)p.Id)
                .FirstOrDefaultAsync(cancellationToken);

            await AppendFileEventAsync(
                context,
                JsonSerializer.Serialize(new
                {
                    status = "processing",
                    stage = "normalize",
                    providerKey = context.Normalize.ProviderKey,
                    pageCount = context.Normalize.PageCount,
                    textArtifactKey = context.Normalize.TextArtifactKey,
                    layoutArtifactKey = context.Normalize.LayoutArtifactKey,
                }),
                providerId,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Normalize/OCR failed for File {FileId}", file.Id);
            await FailFileAsync(context, failed, "normalize_failed", ex.Message, cancellationToken);
            return;
        }

        await DelayAsync(delay, cancellationToken);

        await split.ExecuteAsync(context, cancellationToken);
        await DelayAsync(delay, cancellationToken);

        await classify.ExecuteAsync(context, cancellationToken);
        await DelayAsync(delay, cancellationToken);

        await route.ExecuteAsync(context, cancellationToken);
        await DelayAsync(delay, cancellationToken);

        if (file.PublicStatusEnumId == failed)
        {
            return;
        }

        var docFailed = enums.Require("document_public_status", "failed");
        if (context.SkipSplitAndClassify
            && context.Documents.Count > 0
            && context.Documents.All(d => d.PublicStatusEnumId == docFailed))
        {
            file.PublicStatusEnumId = failed;
            file.ErrorCode ??= "unroutable_type";
            file.UpdatedByUserId = item.UserId;
            await db.SaveChangesAsync(cancellationToken);
            await AppendFileEventAsync(context, """{"status":"failed","stage":"route","errorCode":"unroutable_type"}""", null, cancellationToken);
            return;
        }

        await StubExtractAndCompleteAsync(context, ready, partialReady, delay, cancellationToken);
        logger.LogInformation(
            "Pipeline completed File {FileId} skipSplit={Skip} provider={Provider}",
            file.Id,
            context.SkipSplitAndClassify,
            context.Normalize?.ProviderKey);
    }

    private async Task StubExtractAndCompleteAsync(
        FilePipelineContext context,
        long ready,
        long partialReady,
        int delay,
        CancellationToken cancellationToken)
    {
        context.File.InternalStageEnumId = enums.Require("file_internal_stage", "extract");
        context.File.UpdatedByUserId = context.Item.UserId;
        await db.SaveChangesAsync(cancellationToken);
        await AppendFileEventAsync(context, """{"status":"processing","stage":"extract","stub":true}""", null, cancellationToken);
        await DelayAsync(delay, cancellationToken);

        var docProcessing = enums.Require("document_public_status", "processing");
        var docReady = enums.Require("document_public_status", "ready");
        var docFailed = enums.Require("document_public_status", "failed");
        var docComplete = enums.Require("document_internal_stage", "complete");
        var docSubject = enums.Require("work_subject_type", "document");
        var statusChanged = enums.Require("work_event_type", "status_changed");

        foreach (var doc in context.Documents)
        {
            if (doc.PublicStatusEnumId == docFailed)
            {
                continue;
            }

            doc.PublicStatusEnumId = docProcessing;
            doc.InternalStageEnumId = enums.Require("document_internal_stage", "extract");
            doc.SliceRefJson ??= context.SliceRefJson;
            doc.UpdatedByUserId = context.Item.UserId;
            await db.SaveChangesAsync(cancellationToken);
            await AppendDocEventAsync(context, doc.Id, docSubject, statusChanged, """{"status":"processing"}""", cancellationToken);
            await DelayAsync(delay, cancellationToken);

            doc.PublicStatusEnumId = docReady;
            doc.InternalStageEnumId = docComplete;
            doc.CompletedAt = DateTimeOffset.UtcNow;
            doc.UpdatedByUserId = context.Item.UserId;
            await db.SaveChangesAsync(cancellationToken);
            await AppendDocEventAsync(context, doc.Id, docSubject, statusChanged, """{"status":"ready"}""", cancellationToken);
        }

        context.File.InternalStageEnumId = enums.Require("file_internal_stage", "complete");
        context.File.CompletedAt = DateTimeOffset.UtcNow;
        context.File.UpdatedByUserId = context.Item.UserId;
        var anyFailed = context.Documents.Any(d => d.PublicStatusEnumId == docFailed);
        var anyReady = context.Documents.Any(d => d.PublicStatusEnumId == docReady);
        context.File.PublicStatusEnumId = anyFailed && anyReady ? partialReady : ready;
        await db.SaveChangesAsync(cancellationToken);
        await AppendFileEventAsync(context, """{"status":"ready"}""", null, cancellationToken);
    }

    private async Task SetStageAsync(
        FilePipelineContext context,
        long processing,
        string stageKey,
        CancellationToken cancellationToken)
    {
        context.File.PublicStatusEnumId = processing;
        context.File.InternalStageEnumId = enums.Require("file_internal_stage", stageKey);
        context.File.UpdatedByUserId = context.Item.UserId;
        await db.SaveChangesAsync(cancellationToken);
        await AppendFileEventAsync(
            context,
            $"{{\"status\":\"processing\",\"stage\":\"{stageKey}\"}}",
            null,
            cancellationToken);
    }

    private async Task FailFileAsync(
        FilePipelineContext context,
        long failed,
        string errorCode,
        string message,
        CancellationToken cancellationToken)
    {
        var docFailed = enums.Require("document_public_status", "failed");
        context.File.PublicStatusEnumId = failed;
        context.File.ErrorCode = errorCode;
        context.File.ErrorMessage = message.Length > 4000 ? message[..4000] : message;
        context.File.UpdatedByUserId = context.Item.UserId;
        foreach (var doc in context.Documents)
        {
            doc.PublicStatusEnumId = docFailed;
            doc.ErrorCode = errorCode;
            doc.FailedStage = "normalize";
            doc.UpdatedByUserId = context.Item.UserId;
        }

        await db.SaveChangesAsync(cancellationToken);
        await AppendFileEventAsync(
            context,
            $"{{\"status\":\"failed\",\"stage\":\"normalize\",\"errorCode\":\"{errorCode}\"}}",
            null,
            cancellationToken);
    }

    private async Task AppendFileEventAsync(
        FilePipelineContext context,
        string payload,
        long? providerId,
        CancellationToken cancellationToken)
    {
        db.OpsWorkEvents.Add(new Domain.OpsWorkEvent
        {
            BusinessId = context.Item.BusinessId,
            SubjectTypeEnumId = enums.Require("work_subject_type", "file"),
            SubjectId = context.File.Id,
            EventTypeEnumId = enums.Require("work_event_type", "status_changed"),
            ProviderId = providerId,
            PayloadJson = payload,
            CreatedByUserId = context.Item.UserId,
            UpdatedByUserId = context.Item.UserId,
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task AppendDocEventAsync(
        FilePipelineContext context,
        Guid documentId,
        long subjectType,
        long eventType,
        string payload,
        CancellationToken cancellationToken)
    {
        db.OpsWorkEvents.Add(new Domain.OpsWorkEvent
        {
            BusinessId = context.Item.BusinessId,
            SubjectTypeEnumId = subjectType,
            SubjectId = documentId,
            EventTypeEnumId = eventType,
            PayloadJson = payload,
            CreatedByUserId = context.Item.UserId,
            UpdatedByUserId = context.Item.UserId,
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static Task DelayAsync(int delayMs, CancellationToken cancellationToken) =>
        delayMs <= 0 ? Task.CompletedTask : Task.Delay(delayMs, cancellationToken);
}
