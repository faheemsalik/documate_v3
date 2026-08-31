namespace Documate.Api.Tests;

using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Work;

public class FilePublicStatusRollupTests
{
    private sealed class FixedEnumResolver : ICorEnumIdResolver
    {
        private readonly Dictionary<(string, string), long> _map = new()
        {
            [("document_public_status", "ready")] = 1,
            [("document_public_status", "failed")] = 2,
            [("document_public_status", "rejected")] = 3,
            [("document_public_status", "cancelled")] = 4,
            [("document_public_status", "received")] = 5,
            [("document_public_status", "processing")] = 6,
            [("file_public_status", "ready")] = 11,
            [("file_public_status", "partial_ready")] = 12,
            [("file_public_status", "failed")] = 13,
            [("file_public_status", "processing")] = 14,
            [("file_public_status", "cancelled")] = 15,
        };

        public long Require(string enumTypeKey, string enumKey) =>
            _map.TryGetValue((enumTypeKey, enumKey), out var id)
                ? id
                : throw new InvalidOperationException($"{enumTypeKey}/{enumKey}");

        public bool TryGet(string enumTypeKey, string enumKey, out long id) =>
            _map.TryGetValue((enumTypeKey, enumKey), out id!);
    }

    private static FilePublicStatusRollup.StatusIds Ids() =>
        FilePublicStatusRollup.Resolve(new FixedEnumResolver());

    [Fact]
    public void Whole_file_cancelled_is_not_overwritten()
    {
        var ids = Ids();
        var file = new OpsFile { PublicStatusEnumId = ids.FileCancelled };
        FilePublicStatusRollup.Apply(file, [new OpsDocument { PublicStatusEnumId = ids.DocReady }], ids);
        Assert.Equal(ids.FileCancelled, file.PublicStatusEnumId);
    }

    [Fact]
    public void Ready_plus_cancelled_is_partial_ready()
    {
        var ids = Ids();
        var file = new OpsFile { PublicStatusEnumId = ids.FileProcessing };
        FilePublicStatusRollup.Apply(
            file,
            [
                new OpsDocument { PublicStatusEnumId = ids.DocReady },
                new OpsDocument { PublicStatusEnumId = ids.DocCancelled, ErrorCode = "cancelled" },
            ],
            ids);
        Assert.Equal(ids.FilePartial, file.PublicStatusEnumId);
    }

    [Fact]
    public void All_cancelled_is_failed()
    {
        var ids = Ids();
        var file = new OpsFile { PublicStatusEnumId = ids.FileProcessing };
        FilePublicStatusRollup.Apply(
            file,
            [new OpsDocument { PublicStatusEnumId = ids.DocCancelled, ErrorCode = "cancelled" }],
            ids);
        Assert.Equal(ids.FileFailed, file.PublicStatusEnumId);
        Assert.Equal("cancelled", file.ErrorCode);
    }
}
