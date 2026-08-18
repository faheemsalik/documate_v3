namespace Documate.Api.Modules.FrontendSupport.Features.Catalogs;

using Documate.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize]
[Route("api/app/catalogs")]
public sealed class CatalogsController(IMediator mediator) : ControllerBase
{
    [HttpGet("document-types")]
    public Task<IReadOnlyList<DocumentTypeDto>> ListDocumentTypes(CancellationToken cancellationToken) =>
        mediator.Send(new ListDocumentTypesQuery(), cancellationToken);

    [HttpGet("providers")]
    public Task<IReadOnlyList<ProviderDto>> ListProviders(CancellationToken cancellationToken) =>
        mediator.Send(new ListProvidersQuery(), cancellationToken);

    [HttpGet("agent-templates")]
    public Task<IReadOnlyList<AgentTemplateDto>> ListAgentTemplates(CancellationToken cancellationToken) =>
        mediator.Send(new ListAgentTemplatesQuery(), cancellationToken);

    [HttpGet("agent-templates/{key}")]
    public async Task<ActionResult<AgentTemplateDto>> GetAgentTemplate(string key, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetAgentTemplateByKeyQuery(key), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }
}

public sealed record DocumentTypeDto(long Id, string DocumentTypeKey, string Name, string? Description);
public sealed record ProviderDto(long Id, string ProviderKey, string Name, string? VendorHint, long CategoryEnumId);
public sealed record AgentTemplateDto(
    long Id,
    string AgentTemplateKey,
    string Name,
    string? Description,
    long DocumentTypeId,
    string DocumentTypeKey,
    string DefaultSchemaJson,
    string DefaultInstructions,
    long? DefaultProviderId,
    int Version);

public sealed record ListDocumentTypesQuery : IRequest<IReadOnlyList<DocumentTypeDto>>;
public sealed record ListProvidersQuery : IRequest<IReadOnlyList<ProviderDto>>;
public sealed record ListAgentTemplatesQuery : IRequest<IReadOnlyList<AgentTemplateDto>>;
public sealed record GetAgentTemplateByKeyQuery(string Key) : IRequest<AgentTemplateDto?>;

public sealed class ListDocumentTypesHandler(DocumateDbContext db)
    : IRequestHandler<ListDocumentTypesQuery, IReadOnlyList<DocumentTypeDto>>
{
    public async Task<IReadOnlyList<DocumentTypeDto>> Handle(ListDocumentTypesQuery request, CancellationToken cancellationToken)
    {
        return await db.CorDocumentTypes.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new DocumentTypeDto(x.Id, x.DocumentTypeKey, x.Name, x.Description))
            .ToListAsync(cancellationToken);
    }
}

public sealed class ListProvidersHandler(DocumateDbContext db)
    : IRequestHandler<ListProvidersQuery, IReadOnlyList<ProviderDto>>
{
    public async Task<IReadOnlyList<ProviderDto>> Handle(ListProvidersQuery request, CancellationToken cancellationToken)
    {
        return await db.CorProviders.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new ProviderDto(x.Id, x.ProviderKey, x.Name, x.VendorHint, x.CategoryEnumId))
            .ToListAsync(cancellationToken);
    }
}

public sealed class ListAgentTemplatesHandler(DocumateDbContext db)
    : IRequestHandler<ListAgentTemplatesQuery, IReadOnlyList<AgentTemplateDto>>
{
    public async Task<IReadOnlyList<AgentTemplateDto>> Handle(ListAgentTemplatesQuery request, CancellationToken cancellationToken)
    {
        return await (
            from t in db.CorAgentTemplates.AsNoTracking()
            join d in db.CorDocumentTypes.AsNoTracking() on t.DocumentTypeId equals d.Id
            where t.IsPublished
            orderby t.Name
            select new AgentTemplateDto(
                t.Id,
                t.AgentTemplateKey,
                t.Name,
                t.Description,
                t.DocumentTypeId,
                d.DocumentTypeKey,
                t.DefaultSchemaJson,
                t.DefaultInstructions,
                t.DefaultProviderId,
                t.Version)
        ).ToListAsync(cancellationToken);
    }
}

public sealed class GetAgentTemplateByKeyHandler(DocumateDbContext db)
    : IRequestHandler<GetAgentTemplateByKeyQuery, AgentTemplateDto?>
{
    public async Task<AgentTemplateDto?> Handle(GetAgentTemplateByKeyQuery request, CancellationToken cancellationToken)
    {
        return await (
            from t in db.CorAgentTemplates.AsNoTracking()
            join d in db.CorDocumentTypes.AsNoTracking() on t.DocumentTypeId equals d.Id
            where t.AgentTemplateKey == request.Key && t.IsPublished
            select new AgentTemplateDto(
                t.Id,
                t.AgentTemplateKey,
                t.Name,
                t.Description,
                t.DocumentTypeId,
                d.DocumentTypeKey,
                t.DefaultSchemaJson,
                t.DefaultInstructions,
                t.DefaultProviderId,
                t.Version)
        ).FirstOrDefaultAsync(cancellationToken);
    }
}
