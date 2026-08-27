namespace Documate.Api.Infrastructure.Pipeline.Stages;

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Extract;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Pipeline;
using Documate.Api.Infrastructure.Storage;
using Documate.Api.Infrastructure.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

/// <summary>
/// Per-Document extract via Documate meta-provider, then JSON Schema validate.
/// Untyped / unrouted Documents fail with no_agent. Post-process is DQ-1101.
/// </summary>
public sealed class DocumentExtractStage(
    DocumateDbContext db,
    ICorEnumIdResolver enums,
    IDocumentExtractAdapter extract,
    IObjectStorage storage,
    IDocumentWebhookScheduler webhooks,
    IOptions<PipelineOptions> options,
    ILogger<DocumentExtractStage> logger) : IDocumentExtractStage
{
    public async Task ExecuteAsync(FilePipelineContext context, CancellationToken cancellationToken = default)
    {
        var delay = Math.Max(0, options.Value.StubStageDelayMs);
        context.File.InternalStageEnumId = enums.Require("file_internal_stage", "extract");
        context.File.UpdatedByUserId = context.Item.UserId;
        await db.SaveChangesAsync(cancellationToken);
        await AppendFileEventAsync(context, """{"status":"processing","stage":"extract"}""", null, cancellationToken);

        var docProcessing = enums.Require("document_public_status", "processing");
        var docReady = enums.Require("document_public_status", "ready");
        var docFailed = enums.Require("document_public_status", "failed");
        var docExtract = enums.Require("document_internal_stage", "extract");
        var docValidate = enums.Require("document_internal_stage", "validate");
        var docComplete = enums.Require("document_internal_stage", "complete");
        var docSubject = enums.Require("work_subject_type", "document");
        var statusChanged = enums.Require("work_event_type", "status_changed");

        var agentIds = context.Documents
            .Where(d => d.AgentId is Guid)
            .Select(d => d.AgentId!.Value)
            .Distinct()
            .ToList();
        var agents = agentIds.Count == 0
            ? new Dictionary<Guid, OpsAgent>()
            : await db.OpsAgents.AsNoTracking()
                .Where(a => agentIds.Contains(a.Id) && a.BusinessId == context.Item.BusinessId && !a.IsDeleted)
                .ToDictionaryAsync(a => a.Id, cancellationToken);

        var metaProviderId = await db.CorProviders.AsNoTracking()
            .Where(p => p.ProviderKey == "documate_meta" && p.IsActive)
            .Select(p => (long?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var sourceText = await TryReadNormalizeTextAsync(context, cancellationToken);

        foreach (var doc in context.Documents)
        {
            if (doc.PublicStatusEnumId != docFailed)
            {
                doc.PublicStatusEnumId = docProcessing;
                doc.InternalStageEnumId = docExtract;
                doc.UpdatedByUserId = context.Item.UserId;
                await db.SaveChangesAsync(cancellationToken);
                await AppendDocEventAsync(context, doc.Id, docSubject, statusChanged, """{"status":"processing","stage":"extract"}""", cancellationToken);
                await DelayAsync(delay, cancellationToken);

                if (doc.AgentId is not Guid agentId || !agents.TryGetValue(agentId, out var agent))
                {
                    FailDocument(doc, docFailed, "no_agent", "Document has no routed Agent; cannot extract.", "extract", context.Item.UserId);
                    await db.SaveChangesAsync(cancellationToken);
                    await AppendDocEventAsync(context, doc.Id, docSubject, statusChanged, """{"status":"failed","stage":"extract","errorCode":"no_agent"}""", cancellationToken);
                }
                else
                {
                    try
                    {
                        await ExtractOneAsync(
                            context,
                            doc,
                            agent,
                            sourceText,
                            metaProviderId,
                            docReady,
                            docFailed,
                            docValidate,
                            docComplete,
                            docSubject,
                            statusChanged,
                            delay,
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Extract failed for Document {DocumentId}", doc.Id);
                        FailDocument(doc, docFailed, "extract_failed", ex.Message, "extract", context.Item.UserId);
                        await db.SaveChangesAsync(cancellationToken);
                        await AppendDocEventAsync(
                            context,
                            doc.Id,
                            docSubject,
                            statusChanged,
                            JsonSerializer.Serialize(new
                            {
                                status = "failed",
                                stage = "extract",
                                errorCode = "extract_failed",
                            }),
                            cancellationToken);
                    }
                }
            }

            await webhooks.ScheduleIfTerminalAsync(doc, context.File, cancellationToken);
        }

        await CompleteFileAsync(context, docReady, docFailed, cancellationToken);
    }

    private async Task ExtractOneAsync(
        FilePipelineContext context,
        OpsDocument doc,
        OpsAgent agent,
        string? sourceText,
        long? metaProviderId,
        long docReady,
        long docFailed,
        long docValidate,
        long docComplete,
        long docSubject,
        long statusChanged,
        int delay,
        CancellationToken cancellationToken)
    {
        var result = await extract.ExtractAsync(
            new ExtractAdapterRequest(
                context.File.Id,
                doc.Id,
                doc.SequenceId,
                context.File.StorageBucket ?? context.Normalize?.StorageBucket,
                context.Normalize?.TextArtifactKey,
                agent.OutputSchemaJson,
                agent.Instructions,
                sourceText),
            cancellationToken);

        doc.ResultJson = result.ResultJson;
        doc.SchemaVersion = agent.SchemaVersion;
        doc.ProviderId = metaProviderId;
        doc.InternalStageEnumId = docValidate;
        doc.UpdatedByUserId = context.Item.UserId;
        await db.SaveChangesAsync(cancellationToken);
        await AppendDocEventAsync(
            context,
            doc.Id,
            docSubject,
            statusChanged,
            JsonSerializer.Serialize(new
            {
                status = "processing",
                stage = "validate",
                providerKey = result.ProviderKey,
            }),
            cancellationToken);
        await DelayAsync(delay, cancellationToken);

        await TryWriteExtractArtifactAsync(context, doc, result.ResultJson, cancellationToken);

        JsonNode? instance;
        try
        {
            instance = JsonNode.Parse(result.ResultJson);
        }
        catch (JsonException ex)
        {
            FailDocument(doc, docFailed, "schema_invalid", $"Extract result is not JSON: {ex.Message}", "validate", context.Item.UserId);
            await db.SaveChangesAsync(cancellationToken);
            await AppendDocEventAsync(context, doc.Id, docSubject, statusChanged, """{"status":"failed","stage":"validate","errorCode":"schema_invalid"}""", cancellationToken);
            return;
        }

        var validation = JsonSchemaLite.Validate(agent.OutputSchemaJson, instance);
        if (!validation.IsValid)
        {
            var message = string.Join(" ", validation.Errors);
            FailDocument(
                doc,
                docFailed,
                "schema_invalid",
                message.Length > 4000 ? message[..4000] : message,
                "validate",
                context.Item.UserId);
            await db.SaveChangesAsync(cancellationToken);
            await AppendDocEventAsync(
                context,
                doc.Id,
                docSubject,
                statusChanged,
                JsonSerializer.Serialize(new
                {
                    status = "failed",
                    stage = "validate",
                    errorCode = "schema_invalid",
                    errors = validation.Errors.Take(20).ToArray(),
                }),
                cancellationToken);
            return;
        }

        doc.PublicStatusEnumId = docReady;
        doc.InternalStageEnumId = docComplete;
        doc.CompletedAt = DateTimeOffset.UtcNow;
        doc.ErrorCode = null;
        doc.ErrorMessage = null;
        doc.FailedStage = null;
        doc.UpdatedByUserId = context.Item.UserId;
        await db.SaveChangesAsync(cancellationToken);
        await AppendDocEventAsync(context, doc.Id, docSubject, statusChanged, """{"status":"ready","stage":"validate"}""", cancellationToken);
        logger.LogInformation("Document {DocumentId} extract+validate ready via {Provider}", doc.Id, result.ProviderKey);
    }

    private async Task CompleteFileAsync(
        FilePipelineContext context,
        long docReady,
        long docFailed,
        CancellationToken cancellationToken)
    {
        var fileFailed = enums.Require("file_public_status", "failed");
        var fileReady = enums.Require("file_public_status", "ready");
        var filePartial = enums.Require("file_public_status", "partial_ready");

        var anyFailed = context.Documents.Any(d => d.PublicStatusEnumId == docFailed);
        var anyReady = context.Documents.Any(d => d.PublicStatusEnumId == docReady);

        context.File.InternalStageEnumId = enums.Require("file_internal_stage", "complete");
        context.File.CompletedAt = DateTimeOffset.UtcNow;
        context.File.UpdatedByUserId = context.Item.UserId;

        if (!anyReady && anyFailed)
        {
            context.File.PublicStatusEnumId = fileFailed;
            var codes = context.Documents
                .Where(d => d.PublicStatusEnumId == docFailed && !string.IsNullOrWhiteSpace(d.ErrorCode))
                .Select(d => d.ErrorCode!)
                .Distinct()
                .ToList();
            context.File.ErrorCode = codes.Count == 1 ? codes[0] : (codes.Count == 0 ? "extract_failed" : "extract_failed");
        }
        else if (anyFailed && anyReady)
        {
            context.File.PublicStatusEnumId = filePartial;
        }
        else
        {
            context.File.PublicStatusEnumId = fileReady;
            context.File.ErrorCode = null;
            context.File.ErrorMessage = null;
        }

        await db.SaveChangesAsync(cancellationToken);
        var payload = JsonSerializer.Serialize(new
        {
            status = context.File.PublicStatusEnumId == fileReady
                ? "ready"
                : context.File.PublicStatusEnumId == filePartial ? "partial_ready" : "failed",
            stage = "extract",
        });
        await AppendFileEventAsync(context, payload, null, cancellationToken);
    }

    private static void FailDocument(
        OpsDocument doc,
        long failed,
        string errorCode,
        string message,
        string stage,
        string? userId)
    {
        doc.PublicStatusEnumId = failed;
        doc.ErrorCode = errorCode;
        doc.ErrorMessage = message.Length > 4000 ? message[..4000] : message;
        doc.FailedStage = stage;
        doc.UpdatedByUserId = userId;
    }

    private async Task<string?> TryReadNormalizeTextAsync(FilePipelineContext context, CancellationToken cancellationToken)
    {
        var bucket = context.File.StorageBucket ?? context.Normalize?.StorageBucket;
        var key = context.Normalize?.TextArtifactKey;
        if (string.IsNullOrWhiteSpace(bucket) || string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        try
        {
            await using var stream = await storage.DownloadAsync(bucket, key, cancellationToken);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
            return await reader.ReadToEndAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not read normalize text for File {FileId}; adapter will retry download", context.File.Id);
            return null;
        }
    }

    private async Task TryWriteExtractArtifactAsync(
        FilePipelineContext context,
        OpsDocument doc,
        string resultJson,
        CancellationToken cancellationToken)
    {
        var bucket = context.File.StorageBucket ?? context.Normalize?.StorageBucket;
        if (string.IsNullOrWhiteSpace(bucket) || string.IsNullOrWhiteSpace(context.File.StorageKey))
        {
            return;
        }

        try
        {
            var artifactKey = storage.BuildArtifactKey(
                context.File.StorageKey,
                $"extract.{doc.SequenceId}.result.json");
            var bytes = Encoding.UTF8.GetBytes(resultJson);
            await using var stream = new MemoryStream(bytes);
            await storage.UploadAsync(
                new ObjectStoragePutRequest(
                    bucket,
                    artifactKey,
                    stream,
                    "application/json",
                    new Dictionary<string, string>
                    {
                        ["FileId"] = context.File.Id.ToString(),
                        ["DocumentId"] = doc.Id.ToString(),
                        ["Artifact"] = "extract.result",
                    }),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not write extract artifact for Document {DocumentId}", doc.Id);
        }
    }

    private async Task AppendFileEventAsync(
        FilePipelineContext context,
        string payload,
        long? providerId,
        CancellationToken cancellationToken)
    {
        db.OpsWorkEvents.Add(new OpsWorkEvent
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
        db.OpsWorkEvents.Add(new OpsWorkEvent
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
