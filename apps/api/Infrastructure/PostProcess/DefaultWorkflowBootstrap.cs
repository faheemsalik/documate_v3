namespace Documate.Api.Infrastructure.PostProcess;

using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Ensures each Business has the Phase 1 normalize_fields_v1 workflow (DQ-1101).</summary>
public interface IDefaultWorkflowBootstrap
{
    Task<CorWorkflowDefinition> EnsureNormalizeFieldsAsync(
        string businessId,
        string? userId,
        CancellationToken cancellationToken = default);
}

public sealed class DefaultWorkflowBootstrap(DocumateDbContext db) : IDefaultWorkflowBootstrap
{
    public const string NormalizeFieldsKey = "normalize_fields_v1";

    private const string DefinitionJson =
        """{"version":1,"steps":[{"tool":"normalize_date","fields":["*"]},{"tool":"normalize_currency","fields":["*"]}]}""";

    public async Task<CorWorkflowDefinition> EnsureNormalizeFieldsAsync(
        string businessId,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var existing = await db.CorWorkflowDefinitions
            .FirstOrDefaultAsync(
                w => w.BusinessId == businessId && w.WorkflowKey == NormalizeFieldsKey && !w.IsDeleted,
                cancellationToken);

        if (existing is not null)
        {
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.UpdatedByUserId = userId;
                await db.SaveChangesAsync(cancellationToken);
            }

            return existing;
        }

        var row = new CorWorkflowDefinition
        {
            BusinessId = businessId,
            WorkflowKey = NormalizeFieldsKey,
            Name = "Normalize dates & currency",
            DefinitionJson = DefinitionJson,
            IsActive = true,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
        };
        db.CorWorkflowDefinitions.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return row;
    }
}
