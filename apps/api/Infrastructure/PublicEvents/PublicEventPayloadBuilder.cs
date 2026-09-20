namespace Documate.Api.Infrastructure.PublicEvents;

using System.Text.Json;
using System.Text.Json.Nodes;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Storage;
using Documate.Api.Infrastructure.Webhooks;
using Microsoft.EntityFrameworkCore;

public interface IPublicEventPayloadBuilder
{
    Task<string> BuildDocumentPayloadAsync(
        OpsDocument doc,
        OpsFile file,
        string eventName,
        string eventId,
        CancellationToken cancellationToken = default);

    Task<string> BuildFilePayloadAsync(
        OpsFile file,
        string eventName,
        string eventId,
        CancellationToken cancellationToken = default);
}

public sealed class PublicEventPayloadBuilder(
    DocumateDbContext db,
    ISignedDownloadUrlService signedUrls) : IPublicEventPayloadBuilder
{
    public async Task<string> BuildDocumentPayloadAsync(
        OpsDocument doc,
        OpsFile file,
        string eventName,
        string eventId,
        CancellationToken cancellationToken = default)
    {
        var trackedFile = await EnsureTrackedFileAsync(file, cancellationToken);
        await signedUrls.RefreshFileAsync(trackedFile, cancellationToken);
        await signedUrls.RefreshDocumentAsync(doc, cancellationToken);

        var statusKey = await EnumKeyAsync(doc.PublicStatusEnumId, cancellationToken) ?? "unknown";
        var typeKey = doc.DocumentTypeId is long typeId
            ? await db.CorDocumentTypes.AsNoTracking()
                .Where(t => t.Id == typeId)
                .Select(t => t.DocumentTypeKey)
                .FirstOrDefaultAsync(cancellationToken)
            : null;
        var sourceKey = await EnumKeyAsync(trackedFile.SourceEnumId, cancellationToken);
        var ready = string.Equals(statusKey, "ready", StringComparison.Ordinal);

        JsonNode? data = null;
        if (ready && !string.IsNullOrWhiteSpace(doc.ResultJson))
        {
            try
            {
                data = JsonNode.Parse(doc.ResultJson);
            }
            catch (JsonException)
            {
                data = null;
            }
        }

        DocumentWebhookOriginalFile? original = null;
        if (!string.Equals(sourceKey, "api", StringComparison.Ordinal)
            && !string.Equals(sourceKey, "api_sync", StringComparison.Ordinal))
        {
            original = new DocumentWebhookOriginalFile(
                trackedFile.OriginalFileName,
                trackedFile.ContentType,
                trackedFile.SizeBytes,
                trackedFile.DownloadUrl,
                trackedFile.DownloadUrlExpiresAt);
        }
        else if (!string.IsNullOrWhiteSpace(trackedFile.DownloadUrl))
        {
            original = new DocumentWebhookOriginalFile(
                trackedFile.OriginalFileName,
                trackedFile.ContentType,
                trackedFile.SizeBytes,
                trackedFile.DownloadUrl,
                trackedFile.DownloadUrlExpiresAt);
        }

        JsonNode? emailIntake = null;
        if (!string.IsNullOrWhiteSpace(trackedFile.EmailIntakeJson))
        {
            try
            {
                emailIntake = JsonNode.Parse(trackedFile.EmailIntakeJson);
            }
            catch (JsonException)
            {
                emailIntake = null;
            }
        }

        var body = new DocumentWebhookBody(
            eventName,
            eventId,
            doc.QueueId,
            doc.BatchId,
            doc.FileId,
            doc.Id,
            statusKey,
            typeKey,
            doc.AgentId,
            data,
            ready ? null : new DocumentWebhookError(doc.ErrorCode, doc.ErrorMessage),
            sourceKey,
            trackedFile.EmailMessageId,
            original,
            DateTimeOffset.UtcNow,
            trackedFile.EmailFrom,
            trackedFile.EmailSubject,
            emailIntake,
            doc.PdfStorageKey is null ? null : doc.DownloadUrl,
            doc.PdfStorageKey is null ? null : doc.DownloadUrlExpiresAt);

        return JsonSerializer.Serialize(body, DocumentWebhookPayload.JsonOptions);
    }

    public async Task<string> BuildFilePayloadAsync(
        OpsFile file,
        string eventName,
        string eventId,
        CancellationToken cancellationToken = default)
    {
        var trackedFile = await EnsureTrackedFileAsync(file, cancellationToken);
        await signedUrls.RefreshFileAsync(trackedFile, cancellationToken);

        var docs = await db.OpsDocuments
            .Where(d => d.FileId == trackedFile.Id && d.BusinessId == trackedFile.BusinessId && !d.IsDeleted)
            .OrderBy(d => d.SequenceId)
            .ToListAsync(cancellationToken);
        foreach (var doc in docs)
        {
            await signedUrls.RefreshDocumentAsync(doc, cancellationToken);
        }

        var sourceKey = await EnumKeyAsync(trackedFile.SourceEnumId, cancellationToken);
        var documentPayloads = new List<object>(docs.Count);
        foreach (var doc in docs)
        {
            var statusKey = await EnumKeyAsync(doc.PublicStatusEnumId, cancellationToken);
            var typeKey = doc.DocumentTypeId is long typeId
                ? await db.CorDocumentTypes.AsNoTracking()
                    .Where(t => t.Id == typeId)
                    .Select(t => t.DocumentTypeKey)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;
            documentPayloads.Add(new
            {
                DocumentId = doc.Id,
                Status = statusKey,
                DocumentType = typeKey,
                Url = doc.PdfStorageKey is null ? null : doc.DownloadUrl,
                UrlExpiresAt = doc.PdfStorageKey is null ? null : doc.DownloadUrlExpiresAt,
            });
        }

        var payload = new
        {
            Event = eventName,
            EventId = eventId,
            QueueId = trackedFile.QueueId,
            BatchId = trackedFile.BatchId,
            FileId = trackedFile.Id,
            BusinessId = trackedFile.BusinessId,
            Source = sourceKey,
            OriginalFile = new
            {
                trackedFile.OriginalFileName,
                trackedFile.ContentType,
                trackedFile.SizeBytes,
                Url = trackedFile.DownloadUrl,
                UrlExpiresAt = trackedFile.DownloadUrlExpiresAt,
            },
            Documents = documentPayloads,
            trackedFile.EmailMessageId,
            trackedFile.EmailFrom,
            trackedFile.EmailSubject,
            OccurredAt = DateTimeOffset.UtcNow,
            Url = trackedFile.DownloadUrl,
            UrlExpiresAt = trackedFile.DownloadUrlExpiresAt,
        };
        return JsonSerializer.Serialize(payload, DocumentWebhookPayload.JsonOptions);
    }

    private async Task<OpsFile> EnsureTrackedFileAsync(OpsFile file, CancellationToken cancellationToken)
    {
        var entry = db.Entry(file);
        if (entry.State != EntityState.Detached)
        {
            return file;
        }

        var tracked = await db.OpsFiles.FirstOrDefaultAsync(
            f => f.Id == file.Id && f.BusinessId == file.BusinessId && !f.IsDeleted,
            cancellationToken);
        return tracked ?? file;
    }

    private async Task<string?> EnumKeyAsync(long enumId, CancellationToken cancellationToken) =>
        await db.CorEnums.AsNoTracking().Where(e => e.Id == enumId).Select(e => e.EnumKey).FirstOrDefaultAsync(cancellationToken);
}
