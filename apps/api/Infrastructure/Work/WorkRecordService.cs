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
    /// <summary>Batch intake: one queue/scope lookup, one insert SaveChanges, parallel blob uploads, one finalize SaveChanges (DQ-1402).</summary>
    Task<IReadOnlyList<OpsFile>> CreateFilesWithBlobsBatchAsync(
        IReadOnlyList<CreateFileWithBlobRequest> requests,
        CancellationToken cancellationToken = default,
        UploadIntakeTimer? timer = null);
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
    string? IntakeHintsJson = null,
    Guid? ReprocessOfFileId = null,
    string? ContentHash = null,
    string? EmailMessageId = null,
    string? EmailFrom = null,
    string? EmailSubject = null,
    string? EmailIntakeJson = null);

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
    private const int MaxBlobParallelism = 8;

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

    public async Task<OpsFile> CreateFileWithBlobAsync(
        CreateFileWithBlobRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await CreateFilesWithBlobsBatchAsync([request], cancellationToken);
        return created[0];
    }

    public async Task<IReadOnlyList<OpsFile>> CreateFilesWithBlobsBatchAsync(
        IReadOnlyList<CreateFileWithBlobRequest> requests,
        CancellationToken cancellationToken = default,
        UploadIntakeTimer? timer = null)
    {
        if (requests.Count == 0)
        {
            return [];
        }

        var queueId = requests[0].QueueId;
        if (requests.Any(r => r.QueueId != queueId))
        {
            throw new InvalidOperationException("All batch upload files must target the same queue.");
        }

        async Task<(int QueueSequenceId, long TenantSequenceId, long BusinessSequenceId, long ReceivedId, long StageId, long SubjectTypeId, long EventTypeId, string Bucket, List<OpsFile> Files)> InsertAsync(CancellationToken ct)
        {
            var queueSequenceId = await db.OpsQueues.AsNoTracking()
                .Where(q => q.Id == queueId && q.BusinessId == business.BusinessId)
                .Select(q => (int?)q.SequenceId)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("Queue not found for this Business.");

            var scope = await (
                from b in db.CorTenantBusinesses.AsNoTracking()
                join t in db.CorTenants.AsNoTracking() on b.TenantId equals t.Id
                where b.IdenBusinessId == business.BusinessId
                select new { TenantSequenceId = t.SequenceId, BusinessSequenceId = b.SequenceId }
            ).FirstAsync(ct);

            var receivedId = enums.Require("file_public_status", "received");
            var stageId = enums.Require("file_internal_stage", "received");
            var subjectTypeId = enums.Require("work_subject_type", "file");
            var eventTypeId = enums.Require("work_event_type", "status_changed");
            var bucket = storage.ResolveBucket();

            var files = new List<OpsFile>(requests.Count);
            foreach (var request in requests)
            {
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
                    StorageBucket = bucket,
                    StorageKey = "",
                    IntakeHintsJson = request.IntakeHintsJson,
                    ReprocessOfFileId = request.ReprocessOfFileId,
                    ContentHash = request.ContentHash,
                    EmailMessageId = request.EmailMessageId,
                    EmailFrom = request.EmailFrom,
                    EmailSubject = request.EmailSubject,
                    EmailIntakeJson = request.EmailIntakeJson,
                    CreatedByUserId = business.UserId,
                    UpdatedByUserId = business.UserId,
                };
                db.OpsFiles.Add(file);
                files.Add(file);
            }

            await db.SaveChangesAsync(ct);
            return (queueSequenceId, scope.TenantSequenceId, scope.BusinessSequenceId, receivedId, stageId, subjectTypeId, eventTypeId, bucket, files);
        }

        var insert = timer is null
            ? await InsertAsync(cancellationToken)
            : await timer.MeasureDbAsync(InsertAsync, cancellationToken);

        var uploadJobs = new List<(OpsFile File, CreateFileWithBlobRequest Request, string Key)>(insert.Files.Count);
        for (var i = 0; i < insert.Files.Count; i++)
        {
            var file = insert.Files[i];
            var request = requests[i];
            var key = storage.BuildFileKey(
                insert.TenantSequenceId,
                insert.BusinessSequenceId,
                insert.QueueSequenceId,
                file.SequenceId,
                request.OriginalFileName);
            file.StorageKey = key;
            uploadJobs.Add((file, request, key));
        }

        async Task UploadAllAsync(CancellationToken ct)
        {
            using var gate = new SemaphoreSlim(MaxBlobParallelism);
            await Task.WhenAll(uploadJobs.Select(async job =>
            {
                await gate.WaitAsync(ct);
                try
                {
                    if (job.Request.Content.CanSeek)
                    {
                        job.Request.Content.Position = 0;
                    }

                    await storage.UploadAsync(
                        new ObjectStoragePutRequest(
                            insert.Bucket,
                            job.Key,
                            job.Request.Content,
                            job.Request.ContentType,
                            new Dictionary<string, string>
                            {
                                ["TenantSequenceId"] = insert.TenantSequenceId.ToString(),
                                ["BusinessSequenceId"] = insert.BusinessSequenceId.ToString(),
                                ["QueueSequenceId"] = insert.QueueSequenceId.ToString(),
                                ["FileSequenceId"] = job.File.SequenceId.ToString(),
                                ["FileId"] = job.File.Id.ToString(),
                            }),
                        ct);
                }
                finally
                {
                    gate.Release();
                }
            }));
        }

        if (timer is null)
        {
            await UploadAllAsync(cancellationToken);
        }
        else
        {
            await timer.MeasureBlobAsync(UploadAllAsync, cancellationToken);
        }

        async Task FinalizeAsync(CancellationToken ct)
        {
            await db.OpsQueues
                .Where(q => q.Id == queueId
                            && q.BusinessId == business.BusinessId
                            && !q.RoutingLocked)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(q => q.RoutingLocked, true)
                        .SetProperty(q => q.RoutingLockedAt, DateTimeOffset.UtcNow)
                        .SetProperty(q => q.UpdatedByUserId, business.UserId)
                        .SetProperty(q => q.UpdatedAt, DateTimeOffset.UtcNow),
                    ct);

            foreach (var file in insert.Files)
            {
                db.OpsWorkEvents.Add(new OpsWorkEvent
                {
                    BusinessId = business.BusinessId,
                    SubjectTypeEnumId = insert.SubjectTypeId,
                    SubjectId = file.Id,
                    EventTypeEnumId = insert.EventTypeId,
                    PayloadJson = """{"status":"received"}""",
                    CreatedByUserId = business.UserId,
                    UpdatedByUserId = business.UserId,
                });
            }

            await db.SaveChangesAsync(ct);
        }

        if (timer is null)
        {
            await FinalizeAsync(cancellationToken);
        }
        else
        {
            await timer.MeasureDbAsync(FinalizeAsync, cancellationToken);
        }

        return insert.Files;
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
