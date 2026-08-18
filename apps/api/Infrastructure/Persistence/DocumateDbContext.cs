namespace Documate.Api.Infrastructure.Persistence;

using Documate.Api.Domain;
using Microsoft.EntityFrameworkCore;

public sealed class DocumateDbContext(DbContextOptions<DocumateDbContext> options) : DbContext(options)
{
    public DbSet<CorTenant> CorTenants => Set<CorTenant>();
    public DbSet<CorTenantBusiness> CorTenantBusinesses => Set<CorTenantBusiness>();
    public DbSet<CorEnumType> CorEnumTypes => Set<CorEnumType>();
    public DbSet<CorEnum> CorEnums => Set<CorEnum>();
    public DbSet<CorProvider> CorProviders => Set<CorProvider>();
    public DbSet<CorDocumentType> CorDocumentTypes => Set<CorDocumentType>();
    public DbSet<CorAgentTemplate> CorAgentTemplates => Set<CorAgentTemplate>();
    public DbSet<CorWorkflowDefinition> CorWorkflowDefinitions => Set<CorWorkflowDefinition>();
    public DbSet<CorTenantApiKey> CorTenantApiKeys => Set<CorTenantApiKey>();
    public DbSet<OpsAgent> OpsAgents => Set<OpsAgent>();
    public DbSet<OpsQueue> OpsQueues => Set<OpsQueue>();
    public DbSet<OpsQueueRoute> OpsQueueRoutes => Set<OpsQueueRoute>();
    public DbSet<OpsQueueEmailAllowlistEntry> OpsQueueEmailAllowlistEntries => Set<OpsQueueEmailAllowlistEntry>();
    public DbSet<OpsBatch> OpsBatches => Set<OpsBatch>();
    public DbSet<OpsFile> OpsFiles => Set<OpsFile>();
    public DbSet<OpsDocument> OpsDocuments => Set<OpsDocument>();
    public DbSet<OpsIntakeRejection> OpsIntakeRejections => Set<OpsIntakeRejection>();
    public DbSet<OpsWorkEvent> OpsWorkEvents => Set<OpsWorkEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocumateDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
                if (entry.Entity is WireFacingEntity wire && wire.Id == Guid.Empty)
                {
                    wire.Id = Guid.NewGuid();
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
