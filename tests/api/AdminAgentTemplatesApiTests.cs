namespace Documate.Api.Tests;

using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Modules.PlatformAdmin.Features.AgentTemplates;
using Microsoft.EntityFrameworkCore;

public class AdminAgentTemplatesApiTests
{
    [Fact]
    public async Task List_includes_cloned_agent_counts_without_throwing()
    {
        await using var db = CreateDb();
        db.CorDocumentTypes.Add(new CorDocumentType { Id = 1, DocumentTypeKey = "invoice", Name = "Invoice" });
        db.CorAgentTemplates.Add(new CorAgentTemplate
        {
            Id = 10,
            AgentTemplateKey = "invoice_v1",
            Name = "Invoice",
            DocumentTypeId = 1,
            DefaultSchemaJson = "{}",
            DefaultInstructions = "",
            SystemPrompt = "sys",
            DefaultPostProcessPrompt = "",
            DefaultAdditionalDocumentInstructions = "",
            IsPublished = true,
            Version = 1,
        });
        db.OpsAgents.Add(new OpsAgent
        {
            Id = Guid.NewGuid(),
            BusinessId = "biz-1",
            Name = "Clone",
            DocumentTypeId = 1,
            OutputSchemaJson = "{}",
            SourceTemplateId = 10,
            SystemPrompt = "sys",
            PostProcessPrompt = "",
            AdditionalDocumentInstructions = "",
            IsActive = true,
        });
        await db.SaveChangesAsync();

        var list = await new ListAdminAgentTemplatesHandler(db)
            .Handle(new ListAdminAgentTemplatesQuery(), CancellationToken.None);

        Assert.Single(list);
        Assert.Equal(1, list[0].ClonedAgentCount);
        Assert.Equal("invoice", list[0].DocumentTypeKey);
        Assert.Equal("sys", list[0].SystemPrompt);
    }

    private static DocumateDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<DocumateDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DocumateDbContext(options);
    }
}
