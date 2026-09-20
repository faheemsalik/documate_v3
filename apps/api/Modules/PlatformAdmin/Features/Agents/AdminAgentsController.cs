namespace Documate.Api.Modules.PlatformAdmin.Features.Agents;

using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Extract;
using Documate.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize(Policy = PlatformAdminAuth.PolicyName)]
[Route("api/admin/agents")]
public sealed class AdminAgentsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public Task<PagedAdminAgentListDto> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] string? businessId = null,
        [FromQuery] string? name = null,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new ListAdminAgentsQuery(page, pageSize, tenantId, businessId, name), cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminAgentDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetAdminAgentQuery(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("{id:guid}/prompt-preview")]
    public async Task<ActionResult<AdminAgentPromptPreviewDto>> PreviewGet(
        Guid id,
        CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetAdminAgentPromptPreviewQuery(id, null), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("{id:guid}/prompt-preview")]
    public async Task<ActionResult<AdminAgentPromptPreviewDto>> PreviewPost(
        Guid id,
        [FromBody] AdminAgentPromptPreviewRequest? body,
        CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetAdminAgentPromptPreviewQuery(id, body), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("{id:guid}/suggest-system-prompt")]
    public async Task<ActionResult<AdminAgentSuggestSystemPromptDto>> SuggestSystemPrompt(
        Guid id,
        CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new SuggestAdminAgentSystemPromptQuery(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("{id:guid}/system-prompt")]
    public async Task<ActionResult<AdminAgentDetailDto>> UpdateSystemPrompt(
        Guid id,
        [FromBody] UpdateAdminAgentSystemPromptRequest body,
        CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(
            new UpdateAdminAgentSystemPromptCommand(id, body.SystemPrompt ?? ""),
            cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }
}

public sealed record AdminAgentListItemDto(
    Guid Id,
    string Name,
    string BusinessId,
    string BusinessName,
    Guid TenantId,
    string TenantName,
    string? DocumentTypeKey,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record PagedAdminAgentListDto(
    IReadOnlyList<AdminAgentListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AdminAgentDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string BusinessId,
    string BusinessName,
    Guid TenantId,
    string TenantName,
    long DocumentTypeId,
    string? DocumentTypeKey,
    string OutputSchemaJson,
    int SchemaVersion,
    string Instructions,
    string SystemPrompt,
    string PostProcessPrompt,
    long? SourceTemplateId,
    long? DefaultProviderId,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record AdminAgentPromptPreviewRequest(
    string? SystemPrompt,
    string? Instructions,
    string? OutputSchemaJson,
    string? PostProcessPrompt);

public sealed record AdminAgentPromptPreviewDto(string SystemPrompt, string UserPrompt);

public sealed record UpdateAdminAgentSystemPromptRequest(string? SystemPrompt);

public sealed record AdminAgentSuggestSystemPromptDto(string SystemPrompt);

public sealed record ListAdminAgentsQuery(
    int Page,
    int PageSize,
    Guid? TenantId,
    string? BusinessId,
    string? Name) : IRequest<PagedAdminAgentListDto>;

public sealed record GetAdminAgentQuery(Guid Id) : IRequest<AdminAgentDetailDto?>;

public sealed record GetAdminAgentPromptPreviewQuery(Guid Id, AdminAgentPromptPreviewRequest? Overrides)
    : IRequest<AdminAgentPromptPreviewDto?>;

public sealed record SuggestAdminAgentSystemPromptQuery(Guid Id)
    : IRequest<AdminAgentSuggestSystemPromptDto?>;

public sealed record UpdateAdminAgentSystemPromptCommand(Guid Id, string SystemPrompt)
    : IRequest<AdminAgentDetailDto?>;

file static class AdminAgentPaging
{
    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        var p = page < 1 ? 1 : page;
        var s = pageSize switch
        {
            < 1 => 50,
            > 200 => 200,
            _ => pageSize,
        };
        return (p, s);
    }
}

public sealed class ListAdminAgentsHandler(DocumateDbContext db)
    : IRequestHandler<ListAdminAgentsQuery, PagedAdminAgentListDto>
{
    public async Task<PagedAdminAgentListDto> Handle(
        ListAdminAgentsQuery request,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = AdminAgentPaging.Normalize(request.Page, request.PageSize);

        var q =
            from a in db.OpsAgents.AsNoTracking()
            join d in db.CorDocumentTypes.AsNoTracking() on a.DocumentTypeId equals d.Id
            join b in db.CorTenantBusinesses.AsNoTracking() on a.BusinessId equals b.IdenBusinessId
            join t in db.CorTenants.AsNoTracking() on b.TenantId equals t.Id
            select new { Agent = a, Type = d, Business = b, Tenant = t };

        if (request.TenantId is Guid tid)
        {
            q = q.Where(x => x.Business.TenantId == tid);
        }

        if (!string.IsNullOrWhiteSpace(request.BusinessId))
        {
            var bid = request.BusinessId.Trim();
            q = q.Where(x => x.Agent.BusinessId == bid);
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var term = request.Name.Trim();
            q = q.Where(x => x.Agent.Name.Contains(term));
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderBy(x => x.Agent.Name)
            .ThenBy(x => x.Business.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminAgentListItemDto(
                x.Agent.Id,
                x.Agent.Name,
                x.Agent.BusinessId,
                x.Business.Name,
                x.Tenant.Id,
                x.Tenant.Name,
                x.Type.DocumentTypeKey,
                x.Agent.IsActive,
                x.Agent.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedAdminAgentListDto(items, total, page, pageSize);
    }
}

public sealed class GetAdminAgentHandler(DocumateDbContext db)
    : IRequestHandler<GetAdminAgentQuery, AdminAgentDetailDto?>
{
    public async Task<AdminAgentDetailDto?> Handle(
        GetAdminAgentQuery request,
        CancellationToken cancellationToken)
    {
        var agent = await db.OpsAgents.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (agent is null)
        {
            return null;
        }

        var typeKey = await db.CorDocumentTypes.AsNoTracking()
            .Where(d => d.Id == agent.DocumentTypeId)
            .Select(d => d.DocumentTypeKey)
            .FirstOrDefaultAsync(cancellationToken);

        var biz = await (
            from b in db.CorTenantBusinesses.AsNoTracking()
            where b.IdenBusinessId == agent.BusinessId
            join t in db.CorTenants.AsNoTracking() on b.TenantId equals t.Id
            select new { Business = b, Tenant = t }).FirstOrDefaultAsync(cancellationToken);

        return new AdminAgentDetailDto(
            agent.Id,
            agent.Name,
            agent.Description,
            agent.BusinessId,
            biz?.Business.Name ?? agent.BusinessId,
            biz?.Tenant.Id ?? Guid.Empty,
            biz?.Tenant.Name ?? "",
            agent.DocumentTypeId,
            typeKey,
            agent.OutputSchemaJson,
            agent.SchemaVersion,
            agent.Instructions,
            agent.SystemPrompt,
            agent.PostProcessPrompt,
            agent.SourceTemplateId,
            agent.DefaultProviderId,
            agent.IsActive,
            agent.CreatedAt);
    }
}

public sealed class GetAdminAgentPromptPreviewHandler(DocumateDbContext db, IExtractPromptComposer composer)
    : IRequestHandler<GetAdminAgentPromptPreviewQuery, AdminAgentPromptPreviewDto?>
{
    public async Task<AdminAgentPromptPreviewDto?> Handle(
        GetAdminAgentPromptPreviewQuery request,
        CancellationToken cancellationToken)
    {
        var agent = await db.OpsAgents.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (agent is null)
        {
            return null;
        }

        var o = request.Overrides;
        var composed = composer.Compose(
            string.IsNullOrWhiteSpace(o?.SystemPrompt) ? agent.SystemPrompt : o.SystemPrompt,
            o?.Instructions ?? agent.Instructions,
            string.IsNullOrWhiteSpace(o?.OutputSchemaJson) ? agent.OutputSchemaJson : o.OutputSchemaJson,
            o?.PostProcessPrompt ?? agent.PostProcessPrompt,
            documentText: null);

        return new AdminAgentPromptPreviewDto(composed.SystemMessage, composed.UserMessage);
    }
}

public sealed class SuggestAdminAgentSystemPromptHandler(DocumateDbContext db, IExtractPromptComposer composer)
    : IRequestHandler<SuggestAdminAgentSystemPromptQuery, AdminAgentSuggestSystemPromptDto?>
{
    public async Task<AdminAgentSuggestSystemPromptDto?> Handle(
        SuggestAdminAgentSystemPromptQuery request,
        CancellationToken cancellationToken)
    {
        var agent = await db.OpsAgents.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (agent is null)
        {
            return null;
        }

        var draft = composer.SuggestSystemPrompt(
            agent.Instructions,
            agent.OutputSchemaJson,
            agent.PostProcessPrompt);
        return new AdminAgentSuggestSystemPromptDto(draft);
    }
}

public sealed class UpdateAdminAgentSystemPromptHandler(DocumateDbContext db)
    : IRequestHandler<UpdateAdminAgentSystemPromptCommand, AdminAgentDetailDto?>
{
    public async Task<AdminAgentDetailDto?> Handle(
        UpdateAdminAgentSystemPromptCommand request,
        CancellationToken cancellationToken)
    {
        var agent = await db.OpsAgents.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
        if (agent is null)
        {
            return null;
        }

        agent.SystemPrompt = request.SystemPrompt.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return await new GetAdminAgentHandler(db).Handle(new GetAdminAgentQuery(request.Id), cancellationToken);
    }
}
