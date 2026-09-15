namespace Documate.Api.Modules.FrontendSupport.Features.Business;

using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.PostProcess;
using Documate.Api.Infrastructure.Queues;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize]
[Route("api/app/businesses")]
public sealed class BusinessesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BusinessListItemDto>>> List(CancellationToken cancellationToken)
    {
        var items = await mediator.Send(new ListBusinessesQuery(), cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<BusinessListItemDto>> Create(
        [FromBody] CreateBusinessRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "name is required." });
        }

        var created = await mediator.Send(new CreateBusinessCommand(request.Name.Trim()), cancellationToken);
        return CreatedAtAction(nameof(List), created);
    }
}

public sealed record CreateBusinessRequest(string Name);

public sealed record BusinessListItemDto(
    string BusinessId,
    string Name,
    string TenantName,
    bool IsActive,
    bool IsCurrent);

public sealed record ListBusinessesQuery : IRequest<IReadOnlyList<BusinessListItemDto>>;
public sealed record CreateBusinessCommand(string Name) : IRequest<BusinessListItemDto>;

public sealed class ListBusinessesHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<ListBusinessesQuery, IReadOnlyList<BusinessListItemDto>>
{
    public async Task<IReadOnlyList<BusinessListItemDto>> Handle(
        ListBusinessesQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await db.CorTenantBusinesses.AsNoTracking()
            .Include(b => b.Tenant)
            .Where(b => b.Tenant!.IdenTenantId == business.TenantId && !b.IsDeleted)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

        return rows.Select(row => new BusinessListItemDto(
            row.IdenBusinessId,
            row.Name,
            row.TenantName,
            row.IsActive,
            string.Equals(row.IdenBusinessId, business.BusinessId, StringComparison.Ordinal))).ToList();
    }
}

public sealed class CreateBusinessHandler(
    DocumateDbContext db,
    IBusinessContext business,
    IDefaultQueueBootstrap defaultQueues,
    IDefaultWorkflowBootstrap defaultWorkflows)
    : IRequestHandler<CreateBusinessCommand, BusinessListItemDto>
{
    public async Task<BusinessListItemDto> Handle(
        CreateBusinessCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Name.Length > 256)
        {
            throw new InvalidOperationException("name must be at most 256 characters.");
        }

        var tenant = await db.CorTenants
            .FirstOrDefaultAsync(t => t.IdenTenantId == business.TenantId, cancellationToken)
            ?? throw new InvalidOperationException("Tenant not found.");

        var newBusinessId = Guid.NewGuid().ToString();
        var row = new Domain.CorTenantBusiness
        {
            TenantId = tenant.Id,
            IdenBusinessId = newBusinessId,
            Name = request.Name,
            TenantName = tenant.Name,
            IsActive = true,
            CreatedByUserId = business.UserId,
            UpdatedByUserId = business.UserId,
        };
        db.CorTenantBusinesses.Add(row);
        await db.SaveChangesAsync(cancellationToken);

        await defaultQueues.EnsureDefaultAsync(newBusinessId, business.UserId, cancellationToken);
        await defaultWorkflows.EnsureNormalizeFieldsAsync(newBusinessId, business.UserId, cancellationToken);

        return new BusinessListItemDto(
            row.IdenBusinessId,
            row.Name,
            row.TenantName,
            row.IsActive,
            IsCurrent: false);
    }
}
