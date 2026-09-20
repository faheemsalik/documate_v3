namespace Documate.Api.Modules.FrontendSupport.Features.Agents;

using Documate.Api.Domain;
using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Extract;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Queues;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize]
[Route("api/app/agents")]
public sealed class AgentsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AgentDto>> List(CancellationToken cancellationToken) =>
        mediator.Send(new ListAgentsQuery(), cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AgentDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetAgentByIdQuery(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("{id:guid}/prompt-preview")]
    public async Task<ActionResult<AgentPromptPreviewDto>> PreviewGet(Guid id, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetAgentPromptPreviewQuery(id, null), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("{id:guid}/prompt-preview")]
    public async Task<ActionResult<AgentPromptPreviewDto>> PreviewPost(
        Guid id,
        [FromBody] AgentPromptPreviewRequest? body,
        CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetAgentPromptPreviewQuery(id, body), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<AgentDto>> Create([FromBody] CreateAgentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var dto = await mediator.Send(new CreateAgentCommand(request), cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AgentDto>> Update(Guid id, [FromBody] UpdateAgentRequest request, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new UpdateAgentCommand(id, request), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var ok = await mediator.Send(new DeleteAgentCommand(id), cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("clone-from-template")]
    public async Task<ActionResult<AgentDto>> CloneFromTemplate(
        [FromBody] CloneAgentFromTemplateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await mediator.Send(new CloneAgentFromTemplateCommand(request), cancellationToken);
            return dto is null ? NotFound() : CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}

public sealed record AgentDto(
    Guid Id,
    string Name,
    string? Description,
    long DocumentTypeId,
    string? DocumentTypeKey,
    string OutputSchemaJson,
    int SchemaVersion,
    string Instructions,
    string PostProcessPrompt,
    string AdditionalDocumentInstructions,
    long? SourceTemplateId,
    long? DefaultWorkflowId,
    long? DefaultProviderId,
    bool IsActive);

public sealed record CreateAgentRequest(
    string Name,
    string? Description,
    long DocumentTypeId,
    string OutputSchemaJson,
    string Instructions,
    string? PostProcessPrompt,
    string? AdditionalDocumentInstructions,
    long? DefaultWorkflowId,
    long? DefaultProviderId,
    int? SchemaVersion);

public sealed record UpdateAgentRequest(
    string Name,
    string? Description,
    long DocumentTypeId,
    string OutputSchemaJson,
    string Instructions,
    string? PostProcessPrompt,
    string? AdditionalDocumentInstructions,
    long? DefaultWorkflowId,
    long? DefaultProviderId,
    int SchemaVersion,
    bool IsActive);

public sealed record CloneAgentFromTemplateRequest(
    string AgentTemplateKey,
    string? Name,
    string? Description);

public sealed record AgentPromptPreviewRequest(
    string? Instructions,
    string? OutputSchemaJson,
    string? AdditionalDocumentInstructions);

public sealed record AgentPromptPreviewDto(string UserPrompt);

public sealed record ListAgentsQuery : IRequest<IReadOnlyList<AgentDto>>;
public sealed record GetAgentByIdQuery(Guid Id) : IRequest<AgentDto?>;
public sealed record GetAgentPromptPreviewQuery(Guid Id, AgentPromptPreviewRequest? Overrides)
    : IRequest<AgentPromptPreviewDto?>;
public sealed record CreateAgentCommand(CreateAgentRequest Request) : IRequest<AgentDto>;
public sealed record UpdateAgentCommand(Guid Id, UpdateAgentRequest Request) : IRequest<AgentDto?>;
public sealed record DeleteAgentCommand(Guid Id) : IRequest<bool>;
public sealed record CloneAgentFromTemplateCommand(CloneAgentFromTemplateRequest Request) : IRequest<AgentDto?>;

internal static class AgentMapping
{
    public static AgentDto ToDto(OpsAgent a, string? documentTypeKey = null) =>
        new(
            a.Id,
            a.Name,
            a.Description,
            a.DocumentTypeId,
            documentTypeKey,
            a.OutputSchemaJson,
            a.SchemaVersion,
            a.Instructions,
            a.PostProcessPrompt,
            a.AdditionalDocumentInstructions,
            a.SourceTemplateId,
            a.DefaultWorkflowId,
            a.DefaultProviderId,
            a.IsActive);
}

public sealed class ListAgentsHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<ListAgentsQuery, IReadOnlyList<AgentDto>>
{
    public async Task<IReadOnlyList<AgentDto>> Handle(ListAgentsQuery request, CancellationToken cancellationToken)
    {
        return await (
            from a in db.OpsAgents.AsNoTracking()
            join d in db.CorDocumentTypes.AsNoTracking() on a.DocumentTypeId equals d.Id
            where a.BusinessId == business.BusinessId
            orderby a.Name
            select new AgentDto(
                a.Id,
                a.Name,
                a.Description,
                a.DocumentTypeId,
                d.DocumentTypeKey,
                a.OutputSchemaJson,
                a.SchemaVersion,
                a.Instructions,
                a.PostProcessPrompt,
                a.AdditionalDocumentInstructions,
                a.SourceTemplateId,
                a.DefaultWorkflowId,
                a.DefaultProviderId,
                a.IsActive)
        ).ToListAsync(cancellationToken);
    }
}

public sealed class GetAgentByIdHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<GetAgentByIdQuery, AgentDto?>
{
    public async Task<AgentDto?> Handle(GetAgentByIdQuery request, CancellationToken cancellationToken)
    {
        return await (
            from a in db.OpsAgents.AsNoTracking()
            join d in db.CorDocumentTypes.AsNoTracking() on a.DocumentTypeId equals d.Id
            where a.Id == request.Id && a.BusinessId == business.BusinessId
            select new AgentDto(
                a.Id,
                a.Name,
                a.Description,
                a.DocumentTypeId,
                d.DocumentTypeKey,
                a.OutputSchemaJson,
                a.SchemaVersion,
                a.Instructions,
                a.PostProcessPrompt,
                a.AdditionalDocumentInstructions,
                a.SourceTemplateId,
                a.DefaultWorkflowId,
                a.DefaultProviderId,
                a.IsActive)
        ).FirstOrDefaultAsync(cancellationToken);
    }
}

public sealed class GetAgentPromptPreviewHandler(
    DocumateDbContext db,
    IBusinessContext business,
    IExtractPromptComposer composer)
    : IRequestHandler<GetAgentPromptPreviewQuery, AgentPromptPreviewDto?>
{
    public async Task<AgentPromptPreviewDto?> Handle(
        GetAgentPromptPreviewQuery request,
        CancellationToken cancellationToken)
    {
        var agent = await db.OpsAgents.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.BusinessId == business.BusinessId, cancellationToken);
        if (agent is null)
        {
            return null;
        }

        var o = request.Overrides;
        var composed = composer.Compose(
            agent.SystemPrompt,
            o?.Instructions ?? agent.Instructions,
            string.IsNullOrWhiteSpace(o?.OutputSchemaJson) ? agent.OutputSchemaJson : o.OutputSchemaJson,
            o?.AdditionalDocumentInstructions ?? agent.AdditionalDocumentInstructions,
            documentText: null);

        return new AgentPromptPreviewDto(composed.UserMessage);
    }
}

public sealed class CreateAgentHandler(
    DocumateDbContext db,
    IBusinessContext business,
    IAgentQueueRouteAutoMapper autoMap)
    : IRequestHandler<CreateAgentCommand, AgentDto>
{
    public async Task<AgentDto> Handle(CreateAgentCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var documentType = await db.CorDocumentTypes.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DocumentTypeId && d.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"DocumentType {request.DocumentTypeId} not found.");

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var agent = new OpsAgent
            {
                BusinessId = business.BusinessId,
                Name = request.Name.Trim(),
                Description = request.Description,
                DocumentTypeId = request.DocumentTypeId,
                OutputSchemaJson = string.IsNullOrWhiteSpace(request.OutputSchemaJson) ? "{}" : request.OutputSchemaJson,
                Instructions = request.Instructions ?? "",
                SystemPrompt = ExtractPromptDefaults.SystemPrompt,
                PostProcessPrompt = request.PostProcessPrompt ?? "",
                AdditionalDocumentInstructions = request.AdditionalDocumentInstructions ?? "",
                DefaultWorkflowId = request.DefaultWorkflowId,
                DefaultProviderId = request.DefaultProviderId,
                SchemaVersion = request.SchemaVersion ?? 1,
                IsActive = true,
                CreatedByUserId = business.UserId,
                UpdatedByUserId = business.UserId,
            };

            db.OpsAgents.Add(agent);
            await db.SaveChangesAsync(cancellationToken);
            await autoMap.TryAutoMapAsync(agent, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return AgentMapping.ToDto(agent, documentType.DocumentTypeKey);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public sealed class UpdateAgentHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<UpdateAgentCommand, AgentDto?>
{
    public async Task<AgentDto?> Handle(UpdateAgentCommand command, CancellationToken cancellationToken)
    {
        var agent = await db.OpsAgents
            .FirstOrDefaultAsync(a => a.Id == command.Id && a.BusinessId == business.BusinessId, cancellationToken);
        if (agent is null)
        {
            return null;
        }

        var request = command.Request;
        var documentType = await db.CorDocumentTypes.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DocumentTypeId && d.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"DocumentType {request.DocumentTypeId} not found.");

        agent.Name = request.Name.Trim();
        agent.Description = request.Description;
        agent.DocumentTypeId = request.DocumentTypeId;
        agent.OutputSchemaJson = string.IsNullOrWhiteSpace(request.OutputSchemaJson) ? "{}" : request.OutputSchemaJson;
        agent.Instructions = request.Instructions ?? "";
        if (request.PostProcessPrompt is not null)
        {
            agent.PostProcessPrompt = request.PostProcessPrompt;
        }

        if (request.AdditionalDocumentInstructions is not null)
        {
            agent.AdditionalDocumentInstructions = request.AdditionalDocumentInstructions;
        }

        agent.DefaultWorkflowId = request.DefaultWorkflowId;
        agent.DefaultProviderId = request.DefaultProviderId;
        agent.SchemaVersion = request.SchemaVersion;
        agent.IsActive = request.IsActive;
        agent.UpdatedByUserId = business.UserId;

        await db.SaveChangesAsync(cancellationToken);
        return AgentMapping.ToDto(agent, documentType.DocumentTypeKey);
    }
}

public sealed class DeleteAgentHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<DeleteAgentCommand, bool>
{
    public async Task<bool> Handle(DeleteAgentCommand command, CancellationToken cancellationToken)
    {
        var agent = await db.OpsAgents
            .FirstOrDefaultAsync(a => a.Id == command.Id && a.BusinessId == business.BusinessId, cancellationToken);
        if (agent is null)
        {
            return false;
        }

        agent.IsDeleted = true;
        agent.DeletedAt = DateTimeOffset.UtcNow;
        agent.DeletedByUserId = business.UserId;
        agent.UpdatedByUserId = business.UserId;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class CloneAgentFromTemplateHandler(
    DocumateDbContext db,
    IBusinessContext business,
    IAgentQueueRouteAutoMapper autoMap)
    : IRequestHandler<CloneAgentFromTemplateCommand, AgentDto?>
{
    public async Task<AgentDto?> Handle(CloneAgentFromTemplateCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var template = await (
            from t in db.CorAgentTemplates.AsNoTracking()
            join d in db.CorDocumentTypes.AsNoTracking() on t.DocumentTypeId equals d.Id
            where t.AgentTemplateKey == request.AgentTemplateKey && t.IsPublished
            select new { Template = t, DocumentTypeKey = d.DocumentTypeKey }
        ).FirstOrDefaultAsync(cancellationToken);

        if (template is null)
        {
            return null;
        }

        var tmpl = template.Template;
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var agent = new OpsAgent
            {
                BusinessId = business.BusinessId,
                Name = string.IsNullOrWhiteSpace(request.Name) ? tmpl.Name : request.Name.Trim(),
                Description = request.Description ?? tmpl.Description,
                DocumentTypeId = tmpl.DocumentTypeId,
                OutputSchemaJson = tmpl.DefaultSchemaJson,
                Instructions = tmpl.DefaultInstructions,
                SystemPrompt = string.IsNullOrWhiteSpace(tmpl.SystemPrompt)
                    ? ExtractPromptDefaults.SystemPrompt
                    : tmpl.SystemPrompt,
                PostProcessPrompt = tmpl.DefaultPostProcessPrompt ?? "",
                AdditionalDocumentInstructions = tmpl.DefaultAdditionalDocumentInstructions ?? "",
                SourceTemplateId = tmpl.Id,
                DefaultProviderId = tmpl.DefaultProviderId,
                DefaultWorkflowId = null,
                SchemaVersion = 1,
                IsActive = true,
                CreatedByUserId = business.UserId,
                UpdatedByUserId = business.UserId,
            };

            db.OpsAgents.Add(agent);
            await db.SaveChangesAsync(cancellationToken);
            await autoMap.TryAutoMapAsync(agent, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return AgentMapping.ToDto(agent, template.DocumentTypeKey);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
