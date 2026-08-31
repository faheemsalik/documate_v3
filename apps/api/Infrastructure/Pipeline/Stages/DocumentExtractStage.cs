namespace Documate.Api.Infrastructure.Pipeline.Stages;

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Extract;
using Documate.Api.Infrastructure.Notifications;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Pipeline;
using Documate.Api.Infrastructure.Storage;
using Documate.Api.Infrastructure.Webhooks;
using Documate.Api.Infrastructure.Work;
using Documate.Api.Infrastructure.PostProcess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

/// <summary>
/// Per-Document extract via live LLM (façade documate_meta on Document), then JSON Schema validate.
/// Untyped / unrouted Documents fail with no_agent. Post-process is DQ-1101.
/// </summary>
public sealed class DocumentExtractStage(
    DocumateDbContext db,
    ICorEnumIdResolver enums,
    IDocumentExtractAdapter extract,
    IObjectStorage storage,
    IDocumentWebhookScheduler webhooks,
    IOpsAlertSender alerts,
    IAgentPostProcessRunner postProcess,
    IOptions<PipelineOptions> options,
    ILogger<DocumentExtractStage> logger) : IDocumentExtractStage
{
    public async Task ExecuteAsync(FilePipelineContext context, CancellationToken cancellationToken = default)
    {
        var delay = Math.Max(0, options.Value.StubStageDelayMs);
        var fileCancelled = enums.Require("file_public_status", "cancelled");
        await db.Entry(context.File).ReloadAsync(cancellationToken);
        if (context.File.PublicStatusEnumId == fileCancelled)
        {
            logger.LogInformation("File {FileId} cancelled; skipping extract", context.File.Id);
            return;
        }

        context.File.InternalStageEnumId = enums.Require("file_internal_stage", "extract");
        context.File.UpdatedByUserId = context.Item.UserId;
        await db.SaveChangesAsync(cancellationToken);
        await AppendFileEventAsync(context, """{"status":"processing","stage":"extract"}""", null, cancellationToken);

        var docProcessing = enums.Require("document_public_status", "processing");
        var docReady = enums.Require("document_public_status", "ready");
        var docFailed = enums.Require("document_public_status", "failed");
        var docCancelled = enums.Require("document_public_status", "cancelled");
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

        var llmCategoryId = enums.Require("provider_category", "llm");
        var agentProviderIds = agents.Values
            .Where(a => a.DefaultProviderId is long)
            .Select(a => a.DefaultProviderId!.Value)
            .Distinct()
            .ToList();
        var llmProviderKeys = agentProviderIds.Count == 0
            ? new Dictionary<long, string>()
            : await db.CorProviders.AsNoTracking()
                .Where(p => agentProviderIds.Contains(p.Id) && p.IsActive && p.CategoryEnumId == llmCategoryId)
                .ToDictionaryAsync(p => p.Id, p => p.ProviderKey, cancellationToken);

        var metaProviderId = await db.CorProviders.AsNoTracking()
            .Where(p => p.ProviderKey == "documate_meta" && p.IsActive)
            .Select(p => (long?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var sourceText = await TryReadNormalizeTextAsync(context, cancellationToken);

        foreach (var doc in context.Documents)
        {
            await db.Entry(context.File).ReloadAsync(cancellationToken);
            if (context.File.PublicStatusEnumId == fileCancelled)
            {
                logger.LogInformation("File {FileId} cancelled mid-extract; stopping", context.File.Id);
                return;
            }

            await db.Entry(doc).ReloadAsync(cancellationToken);
            if (doc.PublicStatusEnumId == docCancelled
                || doc.PublicStatusEnumId == docReady
                || doc.PublicStatusEnumId == docFailed)
            {
                await webhooks.ScheduleIfTerminalAsync(doc, context.File, cancellationToken);
                continue;
            }

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
                string? preferredLlm = null;
                if (agent.DefaultProviderId is long pid
                    && llmProviderKeys.TryGetValue(pid, out var key))
                {
                    preferredLlm = key;
                }

                try
                {
                    await ExtractOneAsync(
                        context,
                        doc,
                        agent,
                        sourceText,
                        preferredLlm,
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
                    await db.Entry(doc).ReloadAsync(cancellationToken);
                    if (doc.PublicStatusEnumId == docCancelled)
                    {
                        await webhooks.ScheduleIfTerminalAsync(doc, context.File, cancellationToken);
                        continue;
                    }

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
                    await alerts.NotifyLlmExtractFailedAsync(
                        new OpsLlmFailureAlert(
                            context.Item.BusinessId,
                            context.File.Id,
                            doc.Id,
                            context.File.QueueId,
                            preferredLlm ?? "unknown",
                            "extract_failed",
                            ex.Message,
                            DateTimeOffset.UtcNow),
                        cancellationToken);
                }
            }

            await webhooks.ScheduleIfTerminalAsync(doc, context.File, cancellationToken);
        }

        await CompleteFileAsync(context, cancellationToken);
    }

    private async Task ExtractOneAsync(
        FilePipelineContext context,
        OpsDocument doc,
        OpsAgent agent,
        string? sourceText,
        string? preferredLlmProviderKey,
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
                sourceText,
                preferredLlmProviderKey),
            cancellationToken);

        await db.Entry(doc).ReloadAsync(cancellationToken);
        if (doc.PublicStatusEnumId == enums.Require("document_public_status", "cancelled"))
        {
            return;
        }

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

        var finalJson = result.ResultJson;
        if (agent.DefaultWorkflowId is long)
        {
            var postStage = enums.Require("document_internal_stage", "post_process");
            doc.InternalStageEnumId = postStage;
            doc.UpdatedByUserId = context.Item.UserId;
            await db.SaveChangesAsync(cancellationToken);
            await AppendDocEventAsync(
                context,
                doc.Id,
                docSubject,
                statusChanged,
                """{"status":"processing","stage":"post_process"}""",
                cancellationToken);
            await DelayAsync(delay, cancellationToken);

            try
            {
                var processed = await postProcess.RunAsync(agent, finalJson, cancellationToken);
                finalJson = processed.ResultJson;
                doc.ResultJson = finalJson;
                await db.SaveChangesAsync(cancellationToken);
                await TryWriteExtractArtifactAsync(context, doc, finalJson, cancellationToken);
                await AppendDocEventAsync(
                    context,
                    doc.Id,
                    docSubject,
                    statusChanged,
                    JsonSerializer.Serialize(new
                    {
                        status = "processing",
                        stage = "post_process",
                        ran = processed.Ran,
                        workflowKey = processed.WorkflowKey,
                    }),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Post-process failed for Document {DocumentId}", doc.Id);
                FailDocument(doc, docFailed, "post_process_failed", ex.Message, "post_process", context.Item.UserId);
                await db.SaveChangesAsync(cancellationToken);
                await AppendDocEventAsync(
                    context,
                    doc.Id,
                    docSubject,
                    statusChanged,
                    JsonSerializer.Serialize(new
                    {
                        status = "failed",
                        stage = "post_process",
                        errorCode = "post_process_failed",
                    }),
                    cancellationToken);
                return;
            }
        }

        doc.ResultJson = finalJson;
        doc.PublicStatusEnumId = docReady;
        doc.InternalStageEnumId = docComplete;
        doc.CompletedAt = DateTimeOffset.UtcNow;
        doc.ErrorCode = null;
        doc.ErrorMessage = null;
        doc.FailedStage = null;
        doc.UpdatedByUserId = context.Item.UserId;
        await db.SaveChangesAsync(cancellationToken);
        await AppendDocEventAsync(context, doc.Id, docSubject, statusChanged, """{"status":"ready","stage":"complete"}""", cancellationToken);
        logger.LogInformation("Document {DocumentId} extract+validate(+post) ready via {Provider}", doc.Id, result.ProviderKey);
    }

    private async Task CompleteFileAsync(
        FilePipelineContext context,
        CancellationToken cancellationToken)
    {
        await db.Entry(context.File).ReloadAsync(cancellationToken);
        var ids = FilePublicStatusRollup.Resolve(enums);
        if (context.File.PublicStatusEnumId == ids.FileCancelled)
        {
            return;
        }

        // Refresh docs from DB so cancel-doc mid-extract is reflected in rollup.
        var docs = await db.OpsDocuments
            .Where(d => d.FileId == context.File.Id && d.BusinessId == context.Item.BusinessId && !d.IsDeleted)
            .ToListAsync(cancellationToken);

        context.File.InternalStageEnumId = enums.Require("file_internal_stage", "complete");
        context.File.UpdatedByUserId = context.Item.UserId;
        FilePublicStatusRollup.Apply(context.File, docs, ids);

        await db.SaveChangesAsync(cancellationToken);
        var payload = JsonSerializer.Serialize(new
        {
            status = context.File.PublicStatusEnumId == ids.FileReady
                ? "ready"
                : context.File.PublicStatusEnumId == ids.FilePartial ? "partial_ready" : "failed",
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
