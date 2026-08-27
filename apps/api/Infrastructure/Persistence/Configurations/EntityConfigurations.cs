namespace Documate.Api.Infrastructure.Persistence.Configurations;

using Documate.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal static class EntityConfigHelpers
{
    public static void ConfigureAudit<T>(EntityTypeBuilder<T> b) where T : AuditableEntity
    {
        b.Property(x => x.CreatedByUserId).HasMaxLength(128);
        b.Property(x => x.UpdatedByUserId).HasMaxLength(128);
        b.Property(x => x.DeletedByUserId).HasMaxLength(128);
        b.HasQueryFilter(x => !x.IsDeleted);
    }

    public static void ConfigureWireFacing<T>(EntityTypeBuilder<T> b) where T : WireFacingEntity
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.SequenceId).ValueGeneratedOnAdd().UseIdentityColumn();
        b.HasIndex(x => x.SequenceId).IsUnique();
        ConfigureAudit(b);
    }

    public static void ConfigureCatalog<T>(EntityTypeBuilder<T> b) where T : CatalogEntity
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        ConfigureAudit(b);
    }

    public static void ConfigureRowVersion<T>(EntityTypeBuilder<T> b) where T : class, IHasRowVersion
    {
        b.Property(x => x.RowVersion).IsRowVersion();
    }

    public static void BusinessId<T>(EntityTypeBuilder<T> b) where T : class
    {
        b.Property("BusinessId").HasMaxLength(64).IsRequired();
        b.HasIndex("BusinessId");
    }
}

internal sealed class CorTenantConfiguration : IEntityTypeConfiguration<CorTenant>
{
    public void Configure(EntityTypeBuilder<CorTenant> b)
    {
        EntityConfigHelpers.ConfigureWireFacing(b);
        EntityConfigHelpers.ConfigureRowVersion(b);
        b.Property(x => x.IdenTenantId).HasMaxLength(64).IsRequired();
        b.HasIndex(x => x.IdenTenantId).IsUnique();
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.HasOne(x => x.ProviderMode).WithMany().HasForeignKey(x => x.ProviderModeEnumId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CorTenantBusinessConfiguration : IEntityTypeConfiguration<CorTenantBusiness>
{
    public void Configure(EntityTypeBuilder<CorTenantBusiness> b)
    {
        EntityConfigHelpers.ConfigureWireFacing(b);
        EntityConfigHelpers.ConfigureRowVersion(b);
        b.Property(x => x.IdenBusinessId).HasMaxLength(64).IsRequired();
        b.HasIndex(x => x.IdenBusinessId).IsUnique();
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.Property(x => x.TenantName).HasMaxLength(256).IsRequired();
        b.HasOne(x => x.Tenant).WithMany(x => x.Businesses).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CorEnumTypeConfiguration : IEntityTypeConfiguration<CorEnumType>
{
    public void Configure(EntityTypeBuilder<CorEnumType> b)
    {
        EntityConfigHelpers.ConfigureCatalog(b);
        b.Property(x => x.EnumTypeKey).HasMaxLength(128).IsRequired();
        b.HasIndex(x => x.EnumTypeKey).IsUnique();
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.Property(x => x.Scope).HasMaxLength(32).IsRequired();
    }
}

internal sealed class CorEnumConfiguration : IEntityTypeConfiguration<CorEnum>
{
    public void Configure(EntityTypeBuilder<CorEnum> b)
    {
        EntityConfigHelpers.ConfigureCatalog(b);
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.Property(x => x.EnumKey).HasMaxLength(128).IsRequired();
        b.Property(x => x.ShortName).HasMaxLength(64);
        b.Property(x => x.DisplayStyle).HasMaxLength(64);
        b.Property(x => x.BusinessId).HasMaxLength(64);
        b.HasOne(x => x.Type).WithMany(x => x.Values).HasForeignKey(x => x.TypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TypeId, x.EnumKey, x.BusinessId }).IsUnique();
    }
}

internal sealed class CorProviderConfiguration : IEntityTypeConfiguration<CorProvider>
{
    public void Configure(EntityTypeBuilder<CorProvider> b)
    {
        EntityConfigHelpers.ConfigureCatalog(b);
        b.Property(x => x.ProviderKey).HasMaxLength(128).IsRequired();
        b.HasIndex(x => x.ProviderKey).IsUnique();
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.Property(x => x.VendorHint).HasMaxLength(128);
        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryEnumId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CorDocumentTypeConfiguration : IEntityTypeConfiguration<CorDocumentType>
{
    public void Configure(EntityTypeBuilder<CorDocumentType> b)
    {
        EntityConfigHelpers.ConfigureCatalog(b);
        b.Property(x => x.DocumentTypeKey).HasMaxLength(128).IsRequired();
        b.HasIndex(x => x.DocumentTypeKey).IsUnique();
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1024);
    }
}

internal sealed class CorAgentTemplateConfiguration : IEntityTypeConfiguration<CorAgentTemplate>
{
    public void Configure(EntityTypeBuilder<CorAgentTemplate> b)
    {
        EntityConfigHelpers.ConfigureCatalog(b);
        b.Property(x => x.AgentTemplateKey).HasMaxLength(128).IsRequired();
        b.HasIndex(x => x.AgentTemplateKey).IsUnique();
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.Property(x => x.DefaultSchemaJson).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.DefaultInstructions).HasColumnType("nvarchar(max)").IsRequired();
        b.HasOne(x => x.DocumentType).WithMany().HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DefaultProvider).WithMany().HasForeignKey(x => x.DefaultProviderId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CorWorkflowDefinitionConfiguration : IEntityTypeConfiguration<CorWorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<CorWorkflowDefinition> b)
    {
        EntityConfigHelpers.ConfigureCatalog(b);
        EntityConfigHelpers.ConfigureRowVersion(b);
        EntityConfigHelpers.BusinessId(b);
        b.Property(x => x.WorkflowKey).HasMaxLength(128);
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.Property(x => x.DefinitionJson).HasColumnType("nvarchar(max)").IsRequired();
        b.HasIndex(x => new { x.BusinessId, x.WorkflowKey });
    }
}

internal sealed class CorTenantApiKeyConfiguration : IEntityTypeConfiguration<CorTenantApiKey>
{
    public void Configure(EntityTypeBuilder<CorTenantApiKey> b)
    {
        EntityConfigHelpers.ConfigureWireFacing(b);
        EntityConfigHelpers.ConfigureRowVersion(b);
        EntityConfigHelpers.BusinessId(b);
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.Property(x => x.KeyPrefix).HasMaxLength(32).IsRequired();
        b.Property(x => x.KeyHash).HasMaxLength(256).IsRequired();
        b.HasIndex(x => x.KeyPrefix);
    }
}

internal sealed class OpsAgentConfiguration : IEntityTypeConfiguration<OpsAgent>
{
    public void Configure(EntityTypeBuilder<OpsAgent> b)
    {
        EntityConfigHelpers.ConfigureWireFacing(b);
        EntityConfigHelpers.ConfigureRowVersion(b);
        EntityConfigHelpers.BusinessId(b);
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.Property(x => x.OutputSchemaJson).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.Instructions).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.ProviderStrategyJson).HasColumnType("nvarchar(max)");
        b.HasOne(x => x.DocumentType).WithMany().HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SourceTemplate).WithMany().HasForeignKey(x => x.SourceTemplateId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DefaultWorkflow).WithMany().HasForeignKey(x => x.DefaultWorkflowId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DefaultProvider).WithMany().HasForeignKey(x => x.DefaultProviderId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OpsQueueConfiguration : IEntityTypeConfiguration<OpsQueue>
{
    public void Configure(EntityTypeBuilder<OpsQueue> b)
    {
        EntityConfigHelpers.ConfigureWireFacing(b);
        EntityConfigHelpers.ConfigureRowVersion(b);
        EntityConfigHelpers.BusinessId(b);
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.Property(x => x.WebhookUrl).HasMaxLength(2048);
        b.Property(x => x.WebhookSecretHash).HasMaxLength(256);
        b.Property(x => x.WebhookSecretProtected).HasColumnType("nvarchar(max)");
        b.Property(x => x.EmailLocalPart).HasMaxLength(128);
        b.Property(x => x.EmailDomain).HasMaxLength(256);
        b.HasOne(x => x.AllowlistMode).WithMany().HasForeignKey(x => x.AllowlistModeEnumId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.WorkflowMode).WithMany().HasForeignKey(x => x.WorkflowModeEnumId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Workflow).WithMany().HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OpsQueueRouteConfiguration : IEntityTypeConfiguration<OpsQueueRoute>
{
    public void Configure(EntityTypeBuilder<OpsQueueRoute> b)
    {
        EntityConfigHelpers.ConfigureCatalog(b);
        EntityConfigHelpers.BusinessId(b);
        b.HasOne(x => x.Queue).WithMany(x => x.Routes).HasForeignKey(x => x.QueueId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DocumentType).WithMany().HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Agent).WithMany().HasForeignKey(x => x.AgentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.QueueId, x.DocumentTypeId }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

internal sealed class OpsQueueEmailAllowlistEntryConfiguration : IEntityTypeConfiguration<OpsQueueEmailAllowlistEntry>
{
    public void Configure(EntityTypeBuilder<OpsQueueEmailAllowlistEntry> b)
    {
        EntityConfigHelpers.ConfigureCatalog(b);
        EntityConfigHelpers.BusinessId(b);
        b.Property(x => x.Value).HasMaxLength(512).IsRequired();
        b.HasOne(x => x.Queue).WithMany(x => x.AllowlistEntries).HasForeignKey(x => x.QueueId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MatchType).WithMany().HasForeignKey(x => x.MatchTypeEnumId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OpsBatchConfiguration : IEntityTypeConfiguration<OpsBatch>
{
    public void Configure(EntityTypeBuilder<OpsBatch> b)
    {
        EntityConfigHelpers.ConfigureWireFacing(b);
        EntityConfigHelpers.BusinessId(b);
        b.Property(x => x.EmailMessageId).HasMaxLength(512);
        b.HasOne(x => x.Queue).WithMany().HasForeignKey(x => x.QueueId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Source).WithMany().HasForeignKey(x => x.SourceEnumId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OpsFileConfiguration : IEntityTypeConfiguration<OpsFile>
{
    public void Configure(EntityTypeBuilder<OpsFile> b)
    {
        EntityConfigHelpers.ConfigureWireFacing(b);
        EntityConfigHelpers.ConfigureRowVersion(b);
        EntityConfigHelpers.BusinessId(b);
        b.Property(x => x.OriginalFileName).HasMaxLength(512);
        b.Property(x => x.ContentType).HasMaxLength(256);
        b.Property(x => x.StorageKey).HasMaxLength(1024).IsRequired();
        b.Property(x => x.StorageBucket).HasMaxLength(256);
        b.Property(x => x.ContentHash).HasMaxLength(128);
        b.Property(x => x.EmailMessageId).HasMaxLength(512);
        b.Property(x => x.EmailFrom).HasMaxLength(512);
        b.Property(x => x.EmailSubject).HasMaxLength(1024);
        b.Property(x => x.IntakeHintsJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.ErrorCode).HasMaxLength(128);
        b.Property(x => x.ErrorMessage).HasMaxLength(4000);
        b.Property(x => x.CancelledByUserId).HasMaxLength(128);
        b.HasOne(x => x.Queue).WithMany().HasForeignKey(x => x.QueueId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Batch).WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReprocessOfFile).WithMany().HasForeignKey(x => x.ReprocessOfFileId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Source).WithMany().HasForeignKey(x => x.SourceEnumId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.PublicStatus).WithMany().HasForeignKey(x => x.PublicStatusEnumId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.InternalStage).WithMany().HasForeignKey(x => x.InternalStageEnumId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OpsDocumentConfiguration : IEntityTypeConfiguration<OpsDocument>
{
    public void Configure(EntityTypeBuilder<OpsDocument> b)
    {
        EntityConfigHelpers.ConfigureWireFacing(b);
        EntityConfigHelpers.ConfigureRowVersion(b);
        EntityConfigHelpers.BusinessId(b);
        b.Property(x => x.SliceRefJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.ResultJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.ErrorCode).HasMaxLength(128);
        b.Property(x => x.ErrorMessage).HasMaxLength(4000);
        b.Property(x => x.FailedStage).HasMaxLength(128);
        b.Property(x => x.WebhookLastError).HasMaxLength(4000);
        b.Property(x => x.CancelledByUserId).HasMaxLength(128);
        b.HasOne(x => x.Queue).WithMany().HasForeignKey(x => x.QueueId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.File).WithMany(x => x.Documents).HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Batch).WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DocumentType).WithMany().HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Agent).WithMany().HasForeignKey(x => x.AgentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Provider).WithMany().HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.PublicStatus).WithMany().HasForeignKey(x => x.PublicStatusEnumId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.InternalStage).WithMany().HasForeignKey(x => x.InternalStageEnumId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.WebhookStatus).WithMany().HasForeignKey(x => x.WebhookStatusEnumId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OpsIntakeRejectionConfiguration : IEntityTypeConfiguration<OpsIntakeRejection>
{
    public void Configure(EntityTypeBuilder<OpsIntakeRejection> b)
    {
        EntityConfigHelpers.ConfigureWireFacing(b);
        EntityConfigHelpers.BusinessId(b);
        b.Property(x => x.ErrorCode).HasMaxLength(128);
        b.Property(x => x.ErrorMessage).HasMaxLength(4000);
        b.Property(x => x.EmailMessageId).HasMaxLength(512);
        b.Property(x => x.EmailFrom).HasMaxLength(512);
        b.Property(x => x.EmailSubject).HasMaxLength(1024);
        b.HasOne(x => x.Queue).WithMany().HasForeignKey(x => x.QueueId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Source).WithMany().HasForeignKey(x => x.SourceEnumId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OpsWorkEventConfiguration : IEntityTypeConfiguration<OpsWorkEvent>
{
    public void Configure(EntityTypeBuilder<OpsWorkEvent> b)
    {
        EntityConfigHelpers.ConfigureCatalog(b);
        EntityConfigHelpers.BusinessId(b);
        b.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)");
        b.HasOne(x => x.SubjectType).WithMany().HasForeignKey(x => x.SubjectTypeEnumId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.EventType).WithMany().HasForeignKey(x => x.EventTypeEnumId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Provider).WithMany().HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.BusinessId, x.SubjectTypeEnumId, x.SubjectId });
    }
}
