namespace Documate.Api.Tests;

using System.Text.Json;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Extract;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Modules.FrontendSupport.Features.Agents;
using Documate.Api.Modules.FrontendSupport.Features.Catalogs;
using Microsoft.EntityFrameworkCore;

public class AgentsAppApiTests
{
    private static readonly JsonSerializerOptions Camel = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void App_agent_and_catalog_dtos_never_serialize_system_prompt()
    {
        var agentJson = JsonSerializer.Serialize(
            new AgentDto(
                Guid.NewGuid(),
                "A",
                null,
                1,
                "invoice",
                "{}",
                1,
                "inst",
                "post",
                "extra",
                null,
                null,
                null,
                true),
            Camel);
        var catalogJson = JsonSerializer.Serialize(
            new AgentTemplateDto(1, "k", "n", null, 1, "invoice", "{}", "inst", "post", "extra", null, 1),
            Camel);
        var previewJson = JsonSerializer.Serialize(new AgentPromptPreviewDto("user only"), Camel);

        Assert.Contains("postProcessPrompt", agentJson, StringComparison.Ordinal);
        Assert.Contains("additionalDocumentInstructions", agentJson, StringComparison.Ordinal);
        Assert.Contains("defaultPostProcessPrompt", catalogJson, StringComparison.Ordinal);
        Assert.Contains("defaultAdditionalDocumentInstructions", catalogJson, StringComparison.Ordinal);
        Assert.Equal(["userPrompt"], PropertyNames(previewJson));
        Assert.DoesNotContain("systemPrompt", agentJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("systemPrompt", catalogJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("systemPrompt", previewJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "systemPrompt",
            string.Join(' ', typeof(AgentDto).GetProperties().Select(p => p.Name)),
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "systemPrompt",
            string.Join(' ', typeof(AgentTemplateDto).GetProperties().Select(p => p.Name)),
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "systemPrompt",
            string.Join(' ', typeof(CreateAgentRequest).GetProperties().Select(p => p.Name)),
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "systemPrompt",
            string.Join(' ', typeof(UpdateAgentRequest).GetProperties().Select(p => p.Name)),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Preview_uses_stored_system_prompt_but_returns_user_prompt_only()
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

        var handler = new GetAgentPromptPreviewHandler(
            db,
            new BusinessContext { BusinessId = "biz-1", IsAuthenticated = true },
            new ExtractPromptComposer());

        var live = await handler.Handle(
            new GetAgentPromptPreviewQuery(id, new AgentPromptPreviewRequest("Live inst", null, "Live extra")),
            CancellationToken.None);

        Assert.NotNull(live);
        Assert.Contains("Live inst", live.UserPrompt, StringComparison.Ordinal);
        Assert.Contains("Live extra", live.UserPrompt, StringComparison.Ordinal);
        Assert.Contains("Additional document instructions:", live.UserPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain("Stored post", live.UserPrompt, StringComparison.Ordinal);
        Assert.Contains("Supplier no", live.UserPrompt, StringComparison.Ordinal);
        Assert.Contains(ExtractPromptDefaults.DocumentTextPlaceholder, live.UserPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET SYSTEM", live.UserPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain("Stored inst", live.UserPrompt, StringComparison.Ordinal);
        Assert.Equal(["userPrompt"], PropertyNames(JsonSerializer.Serialize(live, Camel)));

        var stored = await handler.Handle(new GetAgentPromptPreviewQuery(id, null), CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Contains("Stored inst", stored.UserPrompt, StringComparison.Ordinal);
        Assert.Contains("Stored extra", stored.UserPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain("Stored post", stored.UserPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET SYSTEM", stored.UserPrompt, StringComparison.Ordinal);

        var otherBiz = new GetAgentPromptPreviewHandler(
            db,
            new BusinessContext { BusinessId = "other", IsAuthenticated = true },
            new ExtractPromptComposer());
        Assert.Null(await otherBiz.Handle(new GetAgentPromptPreviewQuery(id, null), CancellationToken.None));
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
