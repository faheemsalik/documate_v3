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
        await signedUrls.RefreshDocumentAsync(doc, cancellationToken);

        var statusKey = await EnumKeyAsync(doc.PublicStatusEnumId, cancellationToken) ?? "unknown";
        var typeKey = doc.DocumentTypeId is long typeId
            ? await db.CorDocumentTypes.AsNoTracking()
                .Where(t => t.Id == typeId)
                .Select(t => t.DocumentTypeKey)
                .FirstOrDefaultAsync(cancellationToken)
            : null;
        var sourceKey = await EnumKeyAsync(file.SourceEnumId, cancellationToken);
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
            original = new DocumentWebhookOriginalFile(file.OriginalFileName, file.ContentType, file.SizeBytes);
        }

        JsonNode? emailIntake = null;
        if (!string.IsNullOrWhiteSpace(file.EmailIntakeJson))
        {
            try
            {
                emailIntake = JsonNode.Parse(file.EmailIntakeJson);
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
            file.EmailMessageId,
            original,
            DateTimeOffset.UtcNow,
            file.EmailFrom,
            file.EmailSubject,
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
        var sourceKey = await EnumKeyAsync(file.SourceEnumId, cancellationToken);
        var payload = new
        {
            Event = eventName,
            EventId = eventId,
            QueueId = file.QueueId,
            BatchId = file.BatchId,
            FileId = file.Id,
            BusinessId = file.BusinessId,
            Source = sourceKey,
            OriginalFile = new
            {
                file.OriginalFileName,
                file.ContentType,
                file.SizeBytes,
            },
            file.EmailMessageId,
            file.EmailFrom,
            file.EmailSubject,
            OccurredAt = DateTimeOffset.UtcNow,
        };
        return JsonSerializer.Serialize(payload, DocumentWebhookPayload.JsonOptions);
    }

    private async Task<string?> EnumKeyAsync(long enumId, CancellationToken cancellationToken) =>
        await db.CorEnums.AsNoTracking().Where(e => e.Id == enumId).Select(e => e.EnumKey).FirstOrDefaultAsync(cancellationToken);
}
