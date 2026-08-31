namespace Documate.Api.Modules.FrontendSupport.Features.Business;

using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize]
[Route("api/app/business/profile")]
public sealed class BusinessProfileController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<BusinessProfileDto>> Get(CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetBusinessProfileQuery(), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut]
    public async Task<ActionResult<BusinessProfileDto>> Update(
        [FromBody] UpdateBusinessProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "name is required." });
        }

        var dto = await mediator.Send(new UpdateBusinessProfileCommand(request.Name.Trim()), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }
}

public sealed record BusinessProfileDto(
    string BusinessId,
    string TenantId,
    string TenantName,
    string Name,
    bool IsActive);

public sealed record UpdateBusinessProfileRequest(string Name);

public sealed record GetBusinessProfileQuery : IRequest<BusinessProfileDto?>;

public sealed record UpdateBusinessProfileCommand(string Name) : IRequest<BusinessProfileDto?>;

public sealed class GetBusinessProfileHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<GetBusinessProfileQuery, BusinessProfileDto?>
{
    public async Task<BusinessProfileDto?> Handle(GetBusinessProfileQuery request, CancellationToken cancellationToken)
    {
        var row = await db.CorTenantBusinesses.AsNoTracking()
            .FirstOrDefaultAsync(b => b.IdenBusinessId == business.BusinessId && !b.IsDeleted, cancellationToken);
        if (row is null)
        {
            return null;
        }

        return ToDto(row, business.TenantId);
    }

    internal static BusinessProfileDto ToDto(Domain.CorTenantBusiness row, string tenantId) =>
        new(
            row.IdenBusinessId,
            tenantId,
            row.TenantName,
            row.Name,
            row.IsActive);
}

public sealed class UpdateBusinessProfileHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<UpdateBusinessProfileCommand, BusinessProfileDto?>
{
    public async Task<BusinessProfileDto?> Handle(
        UpdateBusinessProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Name.Length > 256)
        {
            throw new InvalidOperationException("name must be at most 256 characters.");
        }

        var row = await db.CorTenantBusinesses
            .FirstOrDefaultAsync(b => b.IdenBusinessId == business.BusinessId && !b.IsDeleted, cancellationToken);
        if (row is null)
        {
            return null;
        }

        row.Name = request.Name;
        row.UpdatedByUserId = business.UserId;
        await db.SaveChangesAsync(cancellationToken);

        return GetBusinessProfileHandler.ToDto(row, business.TenantId);
    }
}
