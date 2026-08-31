namespace Documate.Api.Infrastructure.Queues;

using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// When a Business has exactly one Queue, map a new Agent onto that Queue for its DocumentType.
/// </summary>
public interface IAgentQueueRouteAutoMapper
{
    Task TryAutoMapAsync(OpsAgent agent, CancellationToken cancellationToken = default);
}

public sealed class AgentQueueRouteAutoMapper(DocumateDbContext db) : IAgentQueueRouteAutoMapper
{
    public async Task TryAutoMapAsync(OpsAgent agent, CancellationToken cancellationToken = default)
    {
        var queues = await db.OpsQueues
            .Where(q => q.BusinessId == agent.BusinessId)
            .OrderByDescending(q => q.IsDefault)
            .ThenBy(q => q.SequenceId)
            .ToListAsync(cancellationToken);

        if (queues.Count == 0)
        {
            throw new InvalidOperationException(
                "No Queue for this Business; default channel is missing. Ensure Business provisioning completed.");
        }

        if (queues.Count > 1)
        {
            // Multi-channel Business: caller must configure QueueRoute explicitly.
            return;
        }

        var queue = queues[0];
        if (queue.RoutingLocked)
        {
            // Agent is created; route map is frozen — no auto-route.
            return;
        }

        var conflict = await db.OpsQueueRoutes.AnyAsync(
            r => r.QueueId == queue.Id && r.DocumentTypeId == agent.DocumentTypeId,
            cancellationToken);
        if (conflict)
        {
            throw new InvalidOperationException(
                "This DocumentType is already routed on the Queue; cannot auto-map a second Agent for the same type.");
        }

        db.OpsQueueRoutes.Add(new OpsQueueRoute
        {
            BusinessId = agent.BusinessId,
            QueueId = queue.Id,
            DocumentTypeId = agent.DocumentTypeId,
            AgentId = agent.Id,
            CreatedByUserId = agent.CreatedByUserId,
            UpdatedByUserId = agent.UpdatedByUserId,
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
