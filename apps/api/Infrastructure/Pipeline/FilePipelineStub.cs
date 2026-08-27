namespace Documate.Api.Infrastructure.Pipeline;

using System.Text.Json;
using Documate.Api.Infrastructure.Ocr;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Pipeline.Stages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

/// <summary>
/// Hangfire File worker: normalize → split → classify → route → extract+validate.
/// documentTypeKey + single-page file skips split and classify.
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
    IDocumentExtractStage extract,
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

        if (file.PublicStatusEnumId == failed)
        {
            return;
        }

        await route.ExecuteAsync(context, cancellationToken);
        await DelayAsync(delay, cancellationToken);

        if (file.PublicStatusEnumId == failed)
        {
            return;
        }

        await extract.ExecuteAsync(context, cancellationToken);
        logger.LogInformation(
            "Pipeline completed File {FileId} skipSplit={Skip} status={Status} provider={Provider}",
            file.Id,
            context.SkipSplitAndClassify,
            file.PublicStatusEnumId,
            context.Normalize?.ProviderKey);
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

    private static Task DelayAsync(int delayMs, CancellationToken cancellationToken) =>
        delayMs <= 0 ? Task.CompletedTask : Task.Delay(delayMs, cancellationToken);
}
