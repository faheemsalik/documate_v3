namespace Documate.Api.Infrastructure.Iden;

using Documate.Api.Domain;
using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

/// <summary>Reconcile CorTenant* with Iden Guids — auto-create missing; mark divergent (DR-SYNC-1 A).</summary>
public sealed class TenancyReconcileJobs(
    DocumateDbContext db,
    ILogger<TenancyReconcileJobs> logger)
{
    public async Task ReconcileAsync()
    {
        // Without live Iden list API wired, repair sync metadata on existing rows only.
        var businesses = await db.CorTenantBusinesses
            .Where(b => !b.IsDeleted)
            .ToListAsync();

        foreach (var b in businesses)
        {
            if (string.IsNullOrWhiteSpace(b.IdenBusinessId) || b.IdenBusinessId.StartsWith("local-", StringComparison.OrdinalIgnoreCase))
            {
                b.SyncStatus = TenancySyncStatuses.Divergent;
                logger.LogWarning("Tenancy divergent business {Id} IdenBusinessId={IdenId}", b.Id, b.IdenBusinessId);
            }
            else if (b.SyncStatus != TenancySyncStatuses.Divergent)
            {
                b.SyncStatus = TenancySyncStatuses.Ok;
                b.LastSyncedAtUtc = DateTimeOffset.UtcNow;
            }
        }

        await db.SaveChangesAsync();
    }
}

public static class TenancyWriteGuard
{
    public static void EnsureWritable(CorTenantBusiness? business)
    {
        if (business is null)
        {
            return;
        }

        if (string.Equals(business.SyncStatus, TenancySyncStatuses.Divergent, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Business '{business.IdenBusinessId}' tenancy sync is divergent — writes blocked (DR-SYNC-1 A).");
        }
    }
}
