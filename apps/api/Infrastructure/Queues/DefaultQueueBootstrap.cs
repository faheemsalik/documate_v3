namespace Documate.Api.Infrastructure.Queues;

using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Ensures each Business has exactly one default Queue (intake channel).</summary>
public interface IDefaultQueueBootstrap
{
    Task<OpsQueue> EnsureDefaultAsync(string businessId, string? userId, CancellationToken cancellationToken = default);
    Task<Guid?> GetDefaultQueueIdAsync(string businessId, CancellationToken cancellationToken = default);
}

public sealed class DefaultQueueBootstrap(
    DocumateDbContext db,
    ICorEnumIdResolver enums) : IDefaultQueueBootstrap
{
    public const string DefaultQueueName = "Default channel";

    public async Task<OpsQueue> EnsureDefaultAsync(
        string businessId,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var existingDefault = await db.OpsQueues
            .FirstOrDefaultAsync(q => q.BusinessId == businessId && q.IsDefault, cancellationToken);
        if (existingDefault is not null)
        {
            return existingDefault;
        }

        // Promote oldest active queue if any exist without a default (backfill / pre-K1 data).
        var promote = await db.OpsQueues
            .Where(q => q.BusinessId == businessId)
            .OrderBy(q => q.SequenceId)
            .FirstOrDefaultAsync(cancellationToken);

        if (promote is not null)
        {
            promote.IsDefault = true;
            promote.UpdatedByUserId = userId;
            await db.SaveChangesAsync(cancellationToken);
            return promote;
        }

        var openId = enums.Require("allowlist_mode", "open");
        var inheritId = enums.Require("workflow_mode", "inherit_agent_default");

        var created = new OpsQueue
        {
            BusinessId = businessId,
            Name = DefaultQueueName,
            Description = "System default intake channel",
            IsDefault = true,
            AllowlistModeEnumId = openId,
            WorkflowModeEnumId = inheritId,
            IsActive = true,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
        };
        db.OpsQueues.Add(created);
        await db.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task<Guid?> GetDefaultQueueIdAsync(string businessId, CancellationToken cancellationToken = default)
    {
        return await db.OpsQueues.AsNoTracking()
            .Where(q => q.BusinessId == businessId && q.IsDefault)
            .Select(q => (Guid?)q.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
