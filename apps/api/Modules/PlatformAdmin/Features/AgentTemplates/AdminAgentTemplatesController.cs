namespace Documate.Api.Modules.PlatformAdmin.Features.AgentTemplates;

using System.Security.Claims;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Extract;
using Documate.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize(Policy = PlatformAdminAuth.PolicyName)]
[Route("api/admin/agent-templates")]
public sealed class AdminAgentTemplatesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AdminAgentTemplateDto>> List(CancellationToken cancellationToken) =>
        mediator.Send(new ListAdminAgentTemplatesQuery(), cancellationToken);

    [HttpGet("{id:long}")]
    public async Task<ActionResult<AdminAgentTemplateDto>> Get(long id, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetAdminAgentTemplateQuery(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<AdminAgentTemplateDto>> Create(
        [FromBody] CreateAdminAgentTemplateRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.AgentTemplateKey) || string.IsNullOrWhiteSpace(body.Name))
        {
            return BadRequest(new { error = "AgentTemplateKey and Name are required." });
        }

        try
        {
            var dto = await mediator.Send(
                new CreateAdminAgentTemplateCommand(body, AdminUserId()),
                cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<AdminAgentTemplateUpdateResultDto>> Update(
        long id,
        [FromBody] UpdateAdminAgentTemplateRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
        {
            return BadRequest(new { error = "Name is required." });
        }

        try
        {
            var dto = await mediator.Send(
                new UpdateAdminAgentTemplateCommand(id, body, AdminUserId()),
                cancellationToken);
            return dto is null ? NotFound() : Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    private string AdminUserId() =>
        User.FindFirstValue(AuthClaimTypes.UserId)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? "ops-admin";
}

public sealed record AdminAgentTemplateDto(
    long Id,
    string AgentTemplateKey,
    string Name,
    string? Description,
    long DocumentTypeId,
    string DocumentTypeKey,
    string DefaultSchemaJson,
    string DefaultInstructions,
    string SystemPrompt,
    string DefaultPostProcessPrompt,
    long? DefaultProviderId,
    bool IsPublished,
    int Version,
    int ClonedAgentCount);

public sealed record AdminAgentTemplateUpdateResultDto(
    AdminAgentTemplateDto Template,
    int ClonedAgentCount,
    bool PushedSystemPrompt);

public sealed record CreateAdminAgentTemplateRequest(
    string AgentTemplateKey,
    string Name,
    string? Description,
    long DocumentTypeId,
    string DefaultSchemaJson,
    string DefaultInstructions,
    string? SystemPrompt,
    string? DefaultPostProcessPrompt,
    long? DefaultProviderId,
    bool IsPublished);

public sealed record UpdateAdminAgentTemplateRequest(
    string Name,
    string? Description,
    long DocumentTypeId,
    string DefaultSchemaJson,
    string DefaultInstructions,
    string? SystemPrompt,
    string? DefaultPostProcessPrompt,
    long? DefaultProviderId,
    bool IsPublished,
    bool PushSystemPrompt);

public sealed record ListAdminAgentTemplatesQuery : IRequest<IReadOnlyList<AdminAgentTemplateDto>>;
public sealed record GetAdminAgentTemplateQuery(long Id) : IRequest<AdminAgentTemplateDto?>;
public sealed record CreateAdminAgentTemplateCommand(CreateAdminAgentTemplateRequest Request, string UserId)
    : IRequest<AdminAgentTemplateDto>;
public sealed record UpdateAdminAgentTemplateCommand(
    long Id,
    UpdateAdminAgentTemplateRequest Request,
    string UserId) : IRequest<AdminAgentTemplateUpdateResultDto?>;

file static class AdminAgentTemplateMapping
{
    /// <summary>
    /// Clone counts are loaded separately — a correlated <c>db.OpsAgents.Count(...)</c>
    /// inside the template projection fails EF SQL translation (admin list 500).
    /// </summary>
    public static async Task<IReadOnlyList<AdminAgentTemplateDto>> ListAsync(
        DocumateDbContext db,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from t in db.CorAgentTemplates.AsNoTracking()
            join d in db.CorDocumentTypes.AsNoTracking() on t.DocumentTypeId equals d.Id
            orderby t.Name
            select new
            {
                t.Id,
                t.AgentTemplateKey,
                t.Name,
                t.Description,
                t.DocumentTypeId,
                d.DocumentTypeKey,
                t.DefaultSchemaJson,
                t.DefaultInstructions,
                t.SystemPrompt,
                t.DefaultPostProcessPrompt,
                t.DefaultProviderId,
                t.IsPublished,
                t.Version,
            }).ToListAsync(cancellationToken);

        var ids = rows.Select(r => r.Id).ToList();
        var counts = ids.Count == 0
            ? new Dictionary<long, int>()
            : await db.OpsAgents.AsNoTracking()
                .Where(a => a.SourceTemplateId != null && ids.Contains(a.SourceTemplateId.Value))
                .GroupBy(a => a.SourceTemplateId!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        return rows
            .Select(r => new AdminAgentTemplateDto(
                r.Id,
                r.AgentTemplateKey,
                r.Name,
                r.Description,
                r.DocumentTypeId,
                r.DocumentTypeKey,
                r.DefaultSchemaJson,
                r.DefaultInstructions,
                r.SystemPrompt,
                r.DefaultPostProcessPrompt,
                r.DefaultProviderId,
                r.IsPublished,
                r.Version,
                counts.GetValueOrDefault(r.Id)))
            .ToList();
    }

    public static async Task<AdminAgentTemplateDto?> GetAsync(
        DocumateDbContext db,
        long id,
        CancellationToken cancellationToken)
    {
        var row = await (
            from t in db.CorAgentTemplates.AsNoTracking()
            join d in db.CorDocumentTypes.AsNoTracking() on t.DocumentTypeId equals d.Id
            where t.Id == id
            select new
            {
                t.Id,
                t.AgentTemplateKey,
                t.Name,
                t.Description,
                t.DocumentTypeId,
                d.DocumentTypeKey,
                t.DefaultSchemaJson,
                t.DefaultInstructions,
                t.SystemPrompt,
                t.DefaultPostProcessPrompt,
                t.DefaultProviderId,
                t.IsPublished,
                t.Version,
            }).FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var clones = await db.OpsAgents.AsNoTracking()
            .CountAsync(a => a.SourceTemplateId == id, cancellationToken);

        return new AdminAgentTemplateDto(
            row.Id,
            row.AgentTemplateKey,
            row.Name,
            row.Description,
            row.DocumentTypeId,
            row.DocumentTypeKey,
            row.DefaultSchemaJson,
            row.DefaultInstructions,
            row.SystemPrompt,
            row.DefaultPostProcessPrompt,
            row.DefaultProviderId,
            row.IsPublished,
            row.Version,
            clones);
    }
}

public sealed class ListAdminAgentTemplatesHandler(DocumateDbContext db)
    : IRequestHandler<ListAdminAgentTemplatesQuery, IReadOnlyList<AdminAgentTemplateDto>>
{
    public Task<IReadOnlyList<AdminAgentTemplateDto>> Handle(
        ListAdminAgentTemplatesQuery request,
        CancellationToken cancellationToken) =>
        AdminAgentTemplateMapping.ListAsync(db, cancellationToken);
}

public sealed class GetAdminAgentTemplateHandler(DocumateDbContext db)
    : IRequestHandler<GetAdminAgentTemplateQuery, AdminAgentTemplateDto?>
{
    public Task<AdminAgentTemplateDto?> Handle(
        GetAdminAgentTemplateQuery request,
        CancellationToken cancellationToken) =>
        AdminAgentTemplateMapping.GetAsync(db, request.Id, cancellationToken);
}

public sealed class CreateAdminAgentTemplateHandler(DocumateDbContext db)
    : IRequestHandler<CreateAdminAgentTemplateCommand, AdminAgentTemplateDto>
{
    public async Task<AdminAgentTemplateDto> Handle(
        CreateAdminAgentTemplateCommand command,
        CancellationToken cancellationToken)
    {
        var req = command.Request;
        var key = req.AgentTemplateKey.Trim();
        var exists = await db.CorAgentTemplates.IgnoreQueryFilters()
            .AnyAsync(t => t.AgentTemplateKey == key, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Agent template key '{key}' already exists.");
        }

        var typeOk = await db.CorDocumentTypes.AnyAsync(d => d.Id == req.DocumentTypeId, cancellationToken);
        if (!typeOk)
        {
            throw new InvalidOperationException($"Document type {req.DocumentTypeId} was not found.");
        }

        var row = new CorAgentTemplate
        {
            AgentTemplateKey = key,
            Name = req.Name.Trim(),
            Description = req.Description,
            DocumentTypeId = req.DocumentTypeId,
            DefaultSchemaJson = string.IsNullOrWhiteSpace(req.DefaultSchemaJson) ? "{}" : req.DefaultSchemaJson,
            DefaultInstructions = req.DefaultInstructions ?? "",
            SystemPrompt = string.IsNullOrWhiteSpace(req.SystemPrompt)
                ? ExtractPromptDefaults.SystemPrompt
                : req.SystemPrompt.Trim(),
            DefaultPostProcessPrompt = req.DefaultPostProcessPrompt ?? "",
            DefaultProviderId = req.DefaultProviderId,
            IsPublished = req.IsPublished,
            Version = 1,
            CreatedByUserId = command.UserId,
            UpdatedByUserId = command.UserId,
        };
        db.CorAgentTemplates.Add(row);
        await db.SaveChangesAsync(cancellationToken);

        return (await AdminAgentTemplateMapping.GetAsync(db, row.Id, cancellationToken))!;
    }
}

public sealed class UpdateAdminAgentTemplateHandler(DocumateDbContext db)
    : IRequestHandler<UpdateAdminAgentTemplateCommand, AdminAgentTemplateUpdateResultDto?>
{
    public async Task<AdminAgentTemplateUpdateResultDto?> Handle(
        UpdateAdminAgentTemplateCommand command,
        CancellationToken cancellationToken)
    {
        var row = await db.CorAgentTemplates.FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var req = command.Request;
        var typeOk = await db.CorDocumentTypes.AnyAsync(d => d.Id == req.DocumentTypeId, cancellationToken);
        if (!typeOk)
        {
            throw new InvalidOperationException($"Document type {req.DocumentTypeId} was not found.");
        }

        var systemPrompt = string.IsNullOrWhiteSpace(req.SystemPrompt)
            ? ExtractPromptDefaults.SystemPrompt
            : req.SystemPrompt.Trim();

        row.Name = req.Name.Trim();
        row.Description = req.Description;
        row.DocumentTypeId = req.DocumentTypeId;
        row.DefaultSchemaJson = string.IsNullOrWhiteSpace(req.DefaultSchemaJson) ? "{}" : req.DefaultSchemaJson;
        row.DefaultInstructions = req.DefaultInstructions ?? "";
        row.SystemPrompt = systemPrompt;
        row.DefaultPostProcessPrompt = req.DefaultPostProcessPrompt ?? "";
        row.DefaultProviderId = req.DefaultProviderId;
        row.IsPublished = req.IsPublished;
        row.Version += 1;
        row.UpdatedByUserId = command.UserId;

        var pushed = false;
        if (req.PushSystemPrompt)
        {
            var clones = await db.OpsAgents
                .Where(a => a.SourceTemplateId == row.Id)
                .ToListAsync(cancellationToken);
            foreach (var agent in clones)
            {
                agent.SystemPrompt = systemPrompt;
                agent.UpdatedByUserId = command.UserId;
            }

            pushed = true;
        }

        await db.SaveChangesAsync(cancellationToken);

        var dto = (await AdminAgentTemplateMapping.GetAsync(db, row.Id, cancellationToken))!;
        return new AdminAgentTemplateUpdateResultDto(dto, dto.ClonedAgentCount, pushed);
    }
}
