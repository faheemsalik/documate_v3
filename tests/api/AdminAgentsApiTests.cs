namespace Documate.Api.Tests;

using System.Text.Json;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Extract;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Modules.FrontendSupport.Features.Agents;
using Documate.Api.Modules.PlatformAdmin.Features.Agents;
using Documate.Api.Modules.PlatformAdmin.Features.Ops;
using Microsoft.EntityFrameworkCore;

public class AdminAgentsApiTests
{
    private static readonly JsonSerializerOptions Camel = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void Admin_preview_dto_includes_system_prompt_app_preview_does_not()
    {
        var adminJson = JsonSerializer.Serialize(
            new AdminAgentPromptPreviewDto("ADMIN SYSTEM", "user body"),
            Camel);
        var appJson = JsonSerializer.Serialize(new AgentPromptPreviewDto("user only"), Camel);
        var detailNames = string.Join(' ', typeof(AdminAgentDetailDto).GetProperties().Select(p => p.Name));
        var listNames = string.Join(' ', typeof(AdminAgentListItemDto).GetProperties().Select(p => p.Name));
        var docNames = string.Join(' ', typeof(AdminDocumentDetailDto).GetProperties().Select(p => p.Name));
        var fileDocNames = string.Join(' ', typeof(AdminFileDocumentItemDto).GetProperties().Select(p => p.Name));

        Assert.Equal(["systemPrompt", "userPrompt"], PropertyNames(adminJson));
        Assert.Equal(["userPrompt"], PropertyNames(appJson));
        Assert.Contains("SystemPrompt", detailNames, StringComparison.Ordinal);
        Assert.DoesNotContain("SystemPrompt", listNames, StringComparison.Ordinal);
        Assert.Contains("ExtractSystemPrompt", docNames, StringComparison.Ordinal);
        Assert.Contains("ExtractUserPrompt", docNames, StringComparison.Ordinal);
        Assert.Contains("ExtractPromptCapturedAt", docNames, StringComparison.Ordinal);
        Assert.Contains("HasExtractPrompt", fileDocNames, StringComparison.Ordinal);
        Assert.DoesNotContain("ExtractSystemPrompt", fileDocNames, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Admin_preview_returns_composed_system_and_user_prompts()
    {
        await using var db = CreateDb();
        var id = Guid.NewGuid();
        db.OpsAgents.Add(new OpsAgent
        {
            Id = id,
            BusinessId = "biz-1",
            Name = "Invoice",
            DocumentTypeId = 1,
            OutputSchemaJson =
                """{"type":"object","properties":{"invoice_number":{"type":"string","description":"Supplier no"}}}""",
            Instructions = "Stored inst",
            SystemPrompt = "SECRET SYSTEM",
            PostProcessPrompt = "Stored post",
            AdditionalDocumentInstructions = "Stored extra",
            IsActive = true,
        });
        await db.SaveChangesAsync();

        var handler = new GetAdminAgentPromptPreviewHandler(db, new ExtractPromptComposer());
        var stored = await handler.Handle(new GetAdminAgentPromptPreviewQuery(id, null), CancellationToken.None);

        Assert.NotNull(stored);
        Assert.Equal("SECRET SYSTEM", stored.SystemPrompt);
        Assert.Contains("Stored inst", stored.UserPrompt, StringComparison.Ordinal);
        Assert.Contains("Stored extra", stored.UserPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain("Stored post", stored.UserPrompt, StringComparison.Ordinal);
        Assert.Contains("Supplier no", stored.UserPrompt, StringComparison.Ordinal);
        Assert.Contains(ExtractPromptDefaults.DocumentTextPlaceholder, stored.UserPrompt, StringComparison.Ordinal);
        Assert.Equal(["systemPrompt", "userPrompt"], PropertyNames(JsonSerializer.Serialize(stored, Camel)));

        var live = await handler.Handle(
            new GetAdminAgentPromptPreviewQuery(
                id,
                new AdminAgentPromptPreviewRequest("LIVE SYSTEM", "Live inst", null, "Live extra")),
            CancellationToken.None);
        Assert.NotNull(live);
        Assert.Equal("LIVE SYSTEM", live.SystemPrompt);
        Assert.Contains("Live inst", live.UserPrompt, StringComparison.Ordinal);
        Assert.Contains("Live extra", live.UserPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain("Stored inst", live.UserPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task List_filters_tenant_business_and_name_without_prompt_text()
    {
        await using var db = CreateDb();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        db.CorTenants.AddRange(
            new CorTenant { Id = tenantA, IdenTenantId = "ten-a", Name = "Tenant A", ProviderModeEnumId = 1 },
            new CorTenant { Id = tenantB, IdenTenantId = "ten-b", Name = "Tenant B", ProviderModeEnumId = 1 });
        db.CorTenantBusinesses.AddRange(
            new CorTenantBusiness
            {
                TenantId = tenantA,
                IdenBusinessId = "biz-a",
                Name = "Biz A",
                TenantName = "Tenant A",
            },
            new CorTenantBusiness
            {
                TenantId = tenantB,
                IdenBusinessId = "biz-b",
                Name = "Biz B",
                TenantName = "Tenant B",
            });
        db.CorDocumentTypes.Add(new CorDocumentType { Id = 1, DocumentTypeKey = "invoice", Name = "Invoice" });
        db.OpsAgents.AddRange(
            new OpsAgent
            {
                BusinessId = "biz-a",
                Name = "Invoice matcher",
                DocumentTypeId = 1,
                SystemPrompt = "DO NOT LIST",
                Instructions = "secret inst",
                OutputSchemaJson = "{}",
                PostProcessPrompt = "secret post",
                IsActive = true,
            },
            new OpsAgent
            {
                BusinessId = "biz-b",
                Name = "Delivery note",
                DocumentTypeId = 1,
                SystemPrompt = "other",
                IsActive = true,
            });
        await db.SaveChangesAsync();

        var handler = new ListAdminAgentsHandler(db);
        var byTenant = await handler.Handle(
            new ListAdminAgentsQuery(1, 50, tenantA, null, null),
            CancellationToken.None);
        Assert.Single(byTenant.Items);
        Assert.Equal("Invoice matcher", byTenant.Items[0].Name);
        Assert.Equal(tenantA, byTenant.Items[0].TenantId);
        Assert.Equal("Biz A", byTenant.Items[0].BusinessName);

        var byBiz = await handler.Handle(
            new ListAdminAgentsQuery(1, 50, null, "biz-b", null),
            CancellationToken.None);
        Assert.Single(byBiz.Items);
        Assert.Equal("Delivery note", byBiz.Items[0].Name);

        var byName = await handler.Handle(
            new ListAdminAgentsQuery(1, 50, null, null, "Invoice"),
            CancellationToken.None);
        Assert.Single(byName.Items);

        var json = JsonSerializer.Serialize(byTenant.Items[0], Camel);
        Assert.DoesNotContain("systemPrompt", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("instructions", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DO NOT LIST", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Document_detail_includes_stored_extract_prompts_and_empty_when_missing()
    {
        await using var db = CreateDb();
        var fileId = Guid.NewGuid();
        var withPromptId = Guid.NewGuid();
        var withoutPromptId = Guid.NewGuid();
        db.CorEnums.Add(new CorEnum { Id = 10, TypeId = 1, EnumKey = "ready", Name = "Ready" });
        db.OpsDocuments.AddRange(
            new OpsDocument
            {
                Id = withPromptId,
                BusinessId = "biz-1",
                QueueId = Guid.NewGuid(),
                FileId = fileId,
                PublicStatusEnumId = 10,
            },
            new OpsDocument
            {
                Id = withoutPromptId,
                BusinessId = "biz-1",
                QueueId = Guid.NewGuid(),
                FileId = fileId,
                PublicStatusEnumId = 10,
            });
        db.OpsDocumentExtractPrompts.Add(new OpsDocumentExtractPrompt
        {
            BusinessId = "biz-1",
            FileId = fileId,
            DocumentId = withPromptId,
            SystemPromptText = "SYS USED",
            UserPromptText = "USER USED",
        });
        await db.SaveChangesAsync();

        var handler = new GetAdminDocumentHandler(db);
        var withPrompt = await handler.Handle(new GetAdminDocumentQuery(withPromptId), CancellationToken.None);
        Assert.NotNull(withPrompt);
        Assert.Equal("SYS USED", withPrompt.ExtractSystemPrompt);
        Assert.Equal("USER USED", withPrompt.ExtractUserPrompt);
        Assert.NotNull(withPrompt.ExtractPromptCapturedAt);

        var missing = await handler.Handle(new GetAdminDocumentQuery(withoutPromptId), CancellationToken.None);
        Assert.NotNull(missing);
        Assert.Null(missing.ExtractSystemPrompt);
        Assert.Null(missing.ExtractUserPrompt);
        Assert.Null(missing.ExtractPromptCapturedAt);

        db.OpsFiles.Add(new OpsFile
        {
            Id = fileId,
            BusinessId = "biz-1",
            QueueId = Guid.NewGuid(),
            PublicStatusEnumId = 10,
            OriginalFileName = "scan.pdf",
        });
        await db.SaveChangesAsync();

        var list = new ListAdminFileDocumentsHandler(db);
        var items = await list.Handle(new ListAdminFileDocumentsQuery(fileId), CancellationToken.None);
        Assert.NotNull(items);
        Assert.Equal(2, items.Count);
        Assert.True(items.Single(i => i.Id == withPromptId).HasExtractPrompt);
        Assert.False(items.Single(i => i.Id == withoutPromptId).HasExtractPrompt);
        var listJson = JsonSerializer.Serialize(items, Camel);
        Assert.DoesNotContain("SYS USED", listJson, StringComparison.Ordinal);
        Assert.DoesNotContain("extractSystemPrompt", listJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Get_agent_includes_system_prompt_and_labels()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        db.CorTenants.Add(new CorTenant
        {
            Id = tenantId,
            IdenTenantId = "ten-1",
            Name = "Acme",
            ProviderModeEnumId = 1,
        });
        db.CorTenantBusinesses.Add(new CorTenantBusiness
        {
            TenantId = tenantId,
            IdenBusinessId = "biz-1",
            Name = "Acme Ops",
            TenantName = "Acme",
        });
        db.CorDocumentTypes.Add(new CorDocumentType { Id = 1, DocumentTypeKey = "invoice", Name = "Invoice" });
        db.OpsAgents.Add(new OpsAgent
        {
            Id = agentId,
            BusinessId = "biz-1",
            Name = "Invoice",
            Description = "desc",
            DocumentTypeId = 1,
            OutputSchemaJson = "{}",
            Instructions = "inst",
            SystemPrompt = "SYS",
            PostProcessPrompt = "post",
            AdditionalDocumentInstructions = "extra",
            IsActive = true,
        });
        await db.SaveChangesAsync();

        var dto = await new GetAdminAgentHandler(db)
            .Handle(new GetAdminAgentQuery(agentId), CancellationToken.None);
        Assert.NotNull(dto);
        Assert.Equal("SYS", dto.SystemPrompt);
        Assert.Equal("inst", dto.Instructions);
        Assert.Equal("post", dto.PostProcessPrompt);
        Assert.Equal("extra", dto.AdditionalDocumentInstructions);
        Assert.Equal("{}", dto.OutputSchemaJson);
        Assert.Equal("Acme Ops", dto.BusinessName);
        Assert.Equal("Acme", dto.TenantName);
        Assert.Equal(tenantId, dto.TenantId);
        Assert.Equal("invoice", dto.DocumentTypeKey);
    }

    private static IReadOnlyList<string> PropertyNames(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.EnumerateObject().Select(p => p.Name).ToArray();
    }

    private static DocumateDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<DocumateDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DocumateDbContext(options);
    }
}
