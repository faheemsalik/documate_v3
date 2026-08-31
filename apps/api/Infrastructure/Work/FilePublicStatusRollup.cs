namespace Documate.Api.Infrastructure.Work;

using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;

/// <summary>Plan 02 §6.2 file public-status rollup (when children exist; whole-file cancel overrides).</summary>
public static class FilePublicStatusRollup
{
    public sealed record StatusIds(
        long DocReady,
        long DocFailed,
        long DocRejected,
        long DocCancelled,
        long DocReceived,
        long DocProcessing,
        long FileReady,
        long FilePartial,
        long FileFailed,
        long FileProcessing,
        long FileCancelled);

    public static StatusIds Resolve(ICorEnumIdResolver enums) => new(
        enums.Require("document_public_status", "ready"),
        enums.Require("document_public_status", "failed"),
        enums.Require("document_public_status", "rejected"),
        enums.Require("document_public_status", "cancelled"),
        enums.Require("document_public_status", "received"),
        enums.Require("document_public_status", "processing"),
        enums.Require("file_public_status", "ready"),
        enums.Require("file_public_status", "partial_ready"),
        enums.Require("file_public_status", "failed"),
        enums.Require("file_public_status", "processing"),
        enums.Require("file_public_status", "cancelled"));

    /// <summary>
    /// Applies Plan 02 rollup to <paramref name="file"/> from <paramref name="documents"/>.
    /// Does nothing when the file is already whole-file <c>cancelled</c>.
    /// When there are no documents, leaves file status unchanged (except clears error when all ready path N/A).
    /// </summary>
    public static void Apply(OpsFile file, IReadOnlyList<OpsDocument> documents, StatusIds ids)
    {
        if (file.PublicStatusEnumId == ids.FileCancelled)
        {
            return;
        }

        if (documents.Count == 0)
        {
            return;
        }

        var anyActive = documents.Any(d =>
            d.PublicStatusEnumId == ids.DocReceived || d.PublicStatusEnumId == ids.DocProcessing);
        if (anyActive)
        {
            file.PublicStatusEnumId = ids.FileProcessing;
            file.CompletedAt = null;
            return;
        }

        var anyReady = documents.Any(d => d.PublicStatusEnumId == ids.DocReady);
        var anyBad = documents.Any(d =>
            d.PublicStatusEnumId == ids.DocFailed
            || d.PublicStatusEnumId == ids.DocRejected
            || d.PublicStatusEnumId == ids.DocCancelled);

        if (anyReady && !anyBad)
        {
            file.PublicStatusEnumId = ids.FileReady;
            file.ErrorCode = null;
            file.ErrorMessage = null;
            file.CompletedAt ??= DateTimeOffset.UtcNow;
            return;
        }

        if (anyReady && anyBad)
        {
            file.PublicStatusEnumId = ids.FilePartial;
            file.CompletedAt ??= DateTimeOffset.UtcNow;
            return;
        }

        // Zero Ready, all terminal bad → Failed
        file.PublicStatusEnumId = ids.FileFailed;
        var codes = documents
            .Where(d =>
                (d.PublicStatusEnumId == ids.DocFailed
                 || d.PublicStatusEnumId == ids.DocRejected
                 || d.PublicStatusEnumId == ids.DocCancelled)
                && !string.IsNullOrWhiteSpace(d.ErrorCode))
            .Select(d => d.ErrorCode!)
            .Distinct()
            .ToList();
        file.ErrorCode = codes.Count == 1 ? codes[0] : (codes.Count == 0 ? "failed" : "failed");
        file.CompletedAt ??= DateTimeOffset.UtcNow;
    }
}
