namespace Documate.Api.Infrastructure.Pipeline.Stages;

using System.Text;
using System.Text.Json;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Intelligence;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Storage;

/// <summary>
/// Page intelligence → anchor grouping → per-Document slices and PDF artifacts.
/// </summary>
public sealed class FileSplitStage(
    DocumateDbContext db,
    ICorEnumIdResolver enums,
    IPageIntelligenceService intelligence,
    IObjectStorage storage,
    IDocumentPdfMaterializer pdfMaterializer,
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

        var normalize = context.Normalize
            ?? throw new InvalidOperationException("Normalize result is required before split.");
        var bucket = context.File.StorageBucket ?? normalize.StorageBucket
            ?? throw new InvalidOperationException("Storage bucket is required for page intelligence.");
        context.IntelligenceProfiles = await intelligence.AnalyzeAsync(
            new PageIntelligenceRequest(
                context.Item.BusinessId,
                context.File.Id,
                bucket,
                context.File.StorageKey,
                normalize.PageArtifacts),
            cancellationToken);
        IReadOnlyList<DocumentPageGroup> groups = DocumentBoundaryEngine.Group(context.IntelligenceProfiles);
        if (groups.Count == 0 && normalize.PageCount > 0)
        {
            groups =
            [
                new DocumentPageGroup(1, normalize.PageCount, true, null, null),
            ];
        }

        if (context.Documents.Count == groups.Count
            && context.Documents.All(d => groups.Any(g => g.PageStart == d.PageStart && g.PageEnd == d.PageEnd))
            && context.Documents.All(d => d.SliceRefJson?.Contains("intelligenceDocumentType", StringComparison.Ordinal) == true))
        {
            logger.LogInformation("Split artifacts already exist for File {FileId}; reusing", context.File.Id);
            return;
        }

        if (context.Documents.Count > 0)
        {
            db.OpsDocuments.RemoveRange(context.Documents);
            context.Documents.Clear();
            await db.SaveChangesAsync(cancellationToken);
        }

        var received = enums.Require("document_public_status", "received");
        var failed = enums.Require("document_public_status", "failed");

        foreach (var group in groups)
        {
            var doc = new OpsDocument
            {
                BusinessId = context.Item.BusinessId,
                QueueId = context.File.QueueId,
                FileId = context.File.Id,
                BatchId = context.File.BatchId,
                PublicStatusEnumId = group.Unresolved ? failed : received,
                PageStart = group.PageStart,
                PageEnd = group.PageEnd,
                ErrorCode = group.Unresolved ? "split_unresolved" : null,
                ErrorMessage = group.Unresolved ? "Page boundary could not be resolved safely." : null,
                FailedStage = group.Unresolved ? "split" : null,
                CreatedByUserId = context.Item.UserId,
                UpdatedByUserId = context.Item.UserId,
            };
            db.OpsDocuments.Add(doc);
            await db.SaveChangesAsync(cancellationToken);

            doc.SliceRefJson = await WriteSlicesAsync(context, doc, group, bucket, cancellationToken);
            if (!group.Unresolved)
            {
                await pdfMaterializer.MaterializeAsync(context.File, doc, cancellationToken);
            }

            context.Documents.Add(doc);
            await db.SaveChangesAsync(cancellationToken);
        }

        await AppendEventAsync(
            context,
            JsonSerializer.Serialize(new
            {
                status = "processing",
                stage = "split",
                pageCount = normalize.PageCount,
                groupCount = groups.Count,
                unresolvedCount = groups.Count(x => x.Unresolved),
                groups,
            }),
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Split File {FileId} into {Count} groups; unresolved={Unresolved}",
            context.File.Id,
            groups.Count,
            groups.Count(x => x.Unresolved));
    }

    private async Task<string> WriteSlicesAsync(
        FilePipelineContext context,
        OpsDocument doc,
        DocumentPageGroup group,
        string bucket,
        CancellationToken cancellationToken)
    {
        var pages = context.Normalize!.PageArtifacts
            .Where(x => x.Page >= group.PageStart && x.Page <= group.PageEnd)
            .OrderBy(x => x.Page)
            .ToArray();
        var text = new StringBuilder();
        foreach (var page in pages)
        {
            await using var stream = await storage.DownloadAsync(bucket, page.TextArtifactKey, cancellationToken);
            using var reader = new StreamReader(stream, Encoding.UTF8, true, leaveOpen: false);
            if (text.Length > 0)
            {
                text.AppendLine().AppendLine();
            }

            text.Append(await reader.ReadToEndAsync(cancellationToken));
        }

        var textKey = storage.BuildArtifactKey(
            context.File.StorageKey,
            $"documents/{doc.SequenceId}/slice.text.txt");
        var layoutKey = storage.BuildArtifactKey(
            context.File.StorageKey,
            $"documents/{doc.SequenceId}/slice.layout.json");
        await UploadAsync(bucket, textKey, Encoding.UTF8.GetBytes(text.ToString()), "text/plain", doc, cancellationToken);
        var layout = JsonSerializer.SerializeToUtf8Bytes(new
        {
            group.PageStart,
            group.PageEnd,
            pages = pages.Select(x => new { x.Page, x.LayoutArtifactKey }).ToArray(),
        });
        await UploadAsync(bucket, layoutKey, layout, "application/json", doc, cancellationToken);
        return JsonSerializer.Serialize(new
        {
            textArtifactKey = textKey,
            layoutArtifactKey = layoutKey,
            group.PageStart,
            group.PageEnd,
            group.PrimaryNumber,
            intelligenceDocumentType = group.DocumentType,
        });
    }

    private async Task UploadAsync(
        string bucket,
        string key,
        byte[] bytes,
        string contentType,
        OpsDocument doc,
        CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream(bytes);
        await storage.UploadAsync(
            new ObjectStoragePutRequest(
                bucket,
                key,
                stream,
                contentType,
                new Dictionary<string, string>
                {
                    ["FileId"] = doc.FileId.ToString(),
                    ["DocumentId"] = doc.Id.ToString(),
                    ["Artifact"] = "document.slice",
                }),
            cancellationToken);
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
