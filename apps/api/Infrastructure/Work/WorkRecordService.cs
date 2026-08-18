namespace Documate.Api.Infrastructure.Work;

using Documate.Api.Domain;
using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

public interface IWorkRecordService
{
    Task<OpsBatch?> CreateBatchAsync(Guid queueId, long sourceEnumId, int fileCount, string? emailMessageId, CancellationToken cancellationToken = default);
    Task<OpsFile> CreateFileWithBlobAsync(CreateFileWithBlobRequest request, CancellationToken cancellationToken = default);
    Task<OpsDocument> CreateDocumentAsync(CreateDocumentRequest request, CancellationToken cancellationToken = default);
    Task<OpsIntakeRejection> CreateIntakeRejectionAsync(CreateIntakeRejectionRequest request, CancellationToken cancellationToken = default);
    Task AppendWorkEventAsync(AppendWorkEventRequest request, CancellationToken cancellationToken = default);
    Task<OpsFile?> GetFileAsync(Guid fileId, CancellationToken cancellationToken = default);
    Task UpdateFileStatusAsync(Guid fileId, long publicStatusEnumId, long? internalStageEnumId, CancellationToken cancellationToken = default);
}

public sealed record CreateFileWithBlobRequest(
    Guid QueueId,
    Guid? BatchId,
    long SourceEnumId,
    string OriginalFileName,
    string? ContentType,
    Stream Content,
    long? SizeBytes = null,
    string? IntakeHintsJson = null);

public sealed record CreateDocumentRequest(
    Guid QueueId,
    Guid FileId,
    Guid? BatchId,
    long? DocumentTypeId,
    Guid? AgentId,
    long PublicStatusEnumId,
    int? PageStart = null,
    int? PageEnd = null,
    string? SliceRefJson = null);

public sealed record CreateIntakeRejectionRequest(
    Guid QueueId,
    long SourceEnumId,
    string? ErrorCode,
    string? ErrorMessage,
    string? EmailFrom = null,
    string? EmailSubject = null,
    string? EmailMessageId = null);

public sealed record AppendWorkEventRequest(
    long SubjectTypeEnumId,
    Guid SubjectId,
    long EventTypeEnumId,
    string? PayloadJson = null,
    long? ProviderId = null);

public sealed class WorkRecordService(
    DocumateDbContext db,
    IBusinessContext business,
    IObjectStorage storage,
    ICorEnumIdResolver enums) : IWorkRecordService
{
    public async Task<OpsBatch?> CreateBatchAsync(
        Guid queueId,
        long sourceEnumId,
        int fileCount,
        string? emailMessageId,
        CancellationToken cancellationToken = default)
    {
        if (fileCount < 2)
        {
            return null;
        }

        await EnsureQueueAsync(queueId, cancellationToken);
        var batch = new OpsBatch
        {
            BusinessId = business.BusinessId,
            QueueId = queueId,
            SourceEnumId = sourceEnumId,
            FileCount = fileCount,
            EmailMessageId = emailMessageId,
            CreatedByUserId = business.UserId,
            UpdatedByUserId = business.UserId,
        };
        db.OpsBatches.Add(batch);
        await db.SaveChangesAsync(cancellationToken);
        return batch;
    }

    public async Task<OpsFile> CreateFileWithBlobAsync(CreateFileWithBlobRequest request, CancellationToken cancellationToken = default)
    {
        var queue = await EnsureQueueAsync(request.QueueId, cancellationToken);
        var receivedId = enums.Require("file_public_status", "received");
        var stageId = enums.Require("file_internal_stage", "received");

        var file = new OpsFile
        {
            BusinessId = business.BusinessId,
            QueueId = request.QueueId,
            BatchId = request.BatchId,
            SourceEnumId = request.SourceEnumId,
            PublicStatusEnumId = receivedId,
            InternalStageEnumId = stageId,
            OriginalFileName = request.OriginalFileName,
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes ?? (request.Content.CanSeek ? request.Content.Length : 0),
            StorageBucket = storage.ResolveBucket(),
            StorageKey = "", // set after Id assigned
            IntakeHintsJson = request.IntakeHintsJson,
            CreatedByUserId = business.UserId,
            UpdatedByUserId = business.UserId,
        };

        db.OpsFiles.Add(file);
        await db.SaveChangesAsync(cancellationToken);

        var scope = await (
            from b in db.CorTenantBusinesses.AsNoTracking()
            join t in db.CorTenants.AsNoTracking() on b.TenantId equals t.Id
            where b.IdenBusinessId == business.BusinessId
            select new { TenantSequenceId = t.SequenceId, BusinessSequenceId = b.SequenceId }
        ).FirstAsync(cancellationToken);

        file.StorageKey = storage.BuildFileKey(
            scope.TenantSequenceId,
            scope.BusinessSequenceId,
            queue.SequenceId,
            file.SequenceId,
            request.OriginalFileName);
        await storage.UploadAsync(
            new ObjectStoragePutRequest(
                file.StorageBucket,
                file.StorageKey,
                request.Content,
                request.ContentType,
                new Dictionary<string, string>
                {
                    ["TenantSequenceId"] = scope.TenantSequenceId.ToString(),
                    ["BusinessSequenceId"] = scope.BusinessSequenceId.ToString(),
                    ["QueueSequenceId"] = queue.SequenceId.ToString(),
                    ["FileSequenceId"] = file.SequenceId.ToString(),
                    ["FileId"] = file.Id.ToString(),
                }),
            cancellationToken);

        if (!queue.RoutingLocked)
        {
            queue.RoutingLocked = true;
            queue.RoutingLockedAt = DateTimeOffset.UtcNow;
            queue.UpdatedByUserId = business.UserId;
        }

        await db.SaveChangesAsync(cancellationToken);

        await AppendWorkEventAsync(
            new AppendWorkEventRequest(
                enums.Require("work_subject_type", "file"),
                file.Id,
                enums.Require("work_event_type", "status_changed"),
                """{"status":"received"}"""),
            cancellationToken);

        return file;
    }

    public async Task<OpsDocument> CreateDocumentAsync(CreateDocumentRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureQueueAsync(request.QueueId, cancellationToken);
        _ = await GetFileAsync(request.FileId, cancellationToken)
            ?? throw new InvalidOperationException("File not found.");

        var doc = new OpsDocument
        {
            BusinessId = business.BusinessId,
            QueueId = request.QueueId,
            FileId = request.FileId,
            BatchId = request.BatchId,
            DocumentTypeId = request.DocumentTypeId,
            AgentId = request.AgentId,
            PublicStatusEnumId = request.PublicStatusEnumId,
            PageStart = request.PageStart,
            PageEnd = request.PageEnd,
            SliceRefJson = request.SliceRefJson,
            CreatedByUserId = business.UserId,
            UpdatedByUserId = business.UserId,
        };
        db.OpsDocuments.Add(doc);
        await db.SaveChangesAsync(cancellationToken);

        await AppendWorkEventAsync(
            new AppendWorkEventRequest(
                enums.Require("work_subject_type", "document"),
                doc.Id,
                enums.Require("work_event_type", "status_changed"),
                """{"status":"created"}"""),
            cancellationToken);

        return doc;
    }

    public async Task<OpsIntakeRejection> CreateIntakeRejectionAsync(
        CreateIntakeRejectionRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureQueueAsync(request.QueueId, cancellationToken);
        var row = new OpsIntakeRejection
        {
            BusinessId = business.BusinessId,
            QueueId = request.QueueId,
            SourceEnumId = request.SourceEnumId,
            ErrorCode = request.ErrorCode,
            ErrorMessage = request.ErrorMessage,
            EmailFrom = request.EmailFrom,
            EmailSubject = request.EmailSubject,
            EmailMessageId = request.EmailMessageId,
            CreatedByUserId = business.UserId,
            UpdatedByUserId = business.UserId,
        };
        db.OpsIntakeRejections.Add(row);
        await db.SaveChangesAsync(cancellationToken);

        await AppendWorkEventAsync(
            new AppendWorkEventRequest(
                enums.Require("work_subject_type", "intake_rejection"),
                row.Id,
                enums.Require("work_event_type", "status_changed"),
                """{"status":"rejected"}"""),
            cancellationToken);

        return row;
    }

    public async Task AppendWorkEventAsync(AppendWorkEventRequest request, CancellationToken cancellationToken = default)
    {
        db.OpsWorkEvents.Add(new OpsWorkEvent
        {
            BusinessId = business.BusinessId,
            SubjectTypeEnumId = request.SubjectTypeEnumId,
            SubjectId = request.SubjectId,
            EventTypeEnumId = request.EventTypeEnumId,
            ProviderId = request.ProviderId,
            PayloadJson = request.PayloadJson,
            CreatedByUserId = business.UserId,
            UpdatedByUserId = business.UserId,
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<OpsFile?> GetFileAsync(Guid fileId, CancellationToken cancellationToken = default) =>
        db.OpsFiles.FirstOrDefaultAsync(
            f => f.Id == fileId && f.BusinessId == business.BusinessId,
            cancellationToken);

    public async Task UpdateFileStatusAsync(
        Guid fileId,
        long publicStatusEnumId,
        long? internalStageEnumId,
        CancellationToken cancellationToken = default)
    {
        var file = await GetFileAsync(fileId, cancellationToken)
            ?? throw new InvalidOperationException("File not found.");
        file.PublicStatusEnumId = publicStatusEnumId;
        file.InternalStageEnumId = internalStageEnumId;
        file.UpdatedByUserId = business.UserId;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<OpsQueue> EnsureQueueAsync(Guid queueId, CancellationToken cancellationToken)
    {
        var queue = await db.OpsQueues.FirstOrDefaultAsync(
            q => q.Id == queueId && q.BusinessId == business.BusinessId,
            cancellationToken);
        return queue ?? throw new InvalidOperationException("Queue not found for this Business.");
    }
}
