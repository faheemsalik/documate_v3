namespace Documate.Api.Modules.FrontendSupport.Features.Agents;

using Documate.Api.Domain;
using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Persistence;
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

    [HttpPost]
    public async Task<ActionResult<AgentDto>> Create([FromBody] CreateAgentRequest request, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new CreateAgentCommand(request), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
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
        var dto = await mediator.Send(new CloneAgentFromTemplateCommand(request), cancellationToken);
        return dto is null ? NotFound() : CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
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
    long? DefaultWorkflowId,
    long? DefaultProviderId,
    int? SchemaVersion);

public sealed record UpdateAgentRequest(
    string Name,
    string? Description,
    long DocumentTypeId,
    string OutputSchemaJson,
    string Instructions,
    long? DefaultWorkflowId,
    long? DefaultProviderId,
    int SchemaVersion,
    bool IsActive);

public sealed record CloneAgentFromTemplateRequest(
    string AgentTemplateKey,
    string? Name,
    string? Description);

public sealed record ListAgentsQuery : IRequest<IReadOnlyList<AgentDto>>;
public sealed record GetAgentByIdQuery(Guid Id) : IRequest<AgentDto?>;
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
                a.SourceTemplateId,
                a.DefaultWorkflowId,
                a.DefaultProviderId,
                a.IsActive)
        ).FirstOrDefaultAsync(cancellationToken);
    }
}

public sealed class CreateAgentHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<CreateAgentCommand, AgentDto>
{
    public async Task<AgentDto> Handle(CreateAgentCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var documentType = await db.CorDocumentTypes.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DocumentTypeId && d.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"DocumentType {request.DocumentTypeId} not found.");

        var agent = new OpsAgent
        {
            BusinessId = business.BusinessId,
            Name = request.Name.Trim(),
            Description = request.Description,
            DocumentTypeId = request.DocumentTypeId,
            OutputSchemaJson = string.IsNullOrWhiteSpace(request.OutputSchemaJson) ? "{}" : request.OutputSchemaJson,
            Instructions = request.Instructions ?? "",
            DefaultWorkflowId = request.DefaultWorkflowId,
            DefaultProviderId = request.DefaultProviderId,
            SchemaVersion = request.SchemaVersion ?? 1,
            IsActive = true,
            CreatedByUserId = business.UserId,
            UpdatedByUserId = business.UserId,
        };

        db.OpsAgents.Add(agent);
        await db.SaveChangesAsync(cancellationToken);
        return AgentMapping.ToDto(agent, documentType.DocumentTypeKey);
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

public sealed class CloneAgentFromTemplateHandler(DocumateDbContext db, IBusinessContext business)
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
        var agent = new OpsAgent
        {
            BusinessId = business.BusinessId,
            Name = string.IsNullOrWhiteSpace(request.Name) ? tmpl.Name : request.Name.Trim(),
            Description = request.Description ?? tmpl.Description,
            DocumentTypeId = tmpl.DocumentTypeId,
            OutputSchemaJson = tmpl.DefaultSchemaJson,
            Instructions = tmpl.DefaultInstructions,
            SourceTemplateId = tmpl.Id,
            DefaultProviderId = tmpl.DefaultProviderId,
            SchemaVersion = 1,
            IsActive = true,
            CreatedByUserId = business.UserId,
            UpdatedByUserId = business.UserId,
        };

        db.OpsAgents.Add(agent);
        await db.SaveChangesAsync(cancellationToken);
        return AgentMapping.ToDto(agent, template.DocumentTypeKey);
    }
}
