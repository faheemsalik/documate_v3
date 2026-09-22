namespace Documate.Api.Modules.PlatformAdmin.Features.Tenants;

using System.Security.Claims;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.PostProcess;
using Documate.Api.Infrastructure.Queues;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize(Policy = PlatformAdminAuth.PolicyName)]
[Route("api/admin/tenants")]
public sealed class AdminTenantsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public Task<PagedAdminTenantListDto> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new ListAdminTenantsQuery(page, pageSize, search, isActive), cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminTenantDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetAdminTenantQuery(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<AdminTenantDetailDto>> Create(
        [FromBody] CreateAdminTenantRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
        {
            return BadRequest(new { error = "Name is required." });
        }

        try
        {
            var userId = User.FindFirstValue(AuthClaimTypes.UserId)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "ops-admin";
            var created = await mediator.Send(
                new CreateAdminTenantCommand(
                    body.Name.Trim(),
                    body.IdenTenantId,
                    body.ProviderModeKey,
                    body.InitialBusinessName,
                    body.InitialIdenBusinessId,
                    userId),
                cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public sealed record CreateAdminTenantRequest(
    string Name,
    string? IdenTenantId,
    string? ProviderModeKey,
    string? InitialBusinessName,
    /// <summary>When linking an existing Iden tenant, pass the Iden business id to mirror (no local Guid mint).</summary>
    string? InitialIdenBusinessId = null);

public sealed record AdminTenantListItemDto(
    Guid Id,
    string IdenTenantId,
    string Name,
    string? ProviderModeKey,
    bool IsActive,
    int BusinessCount,
    DateTimeOffset CreatedAt);

public sealed record PagedAdminTenantListDto(
    IReadOnlyList<AdminTenantListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AdminBusinessSummaryDto(
    Guid Id,
    string IdenBusinessId,
    string Name,
    bool IsActive,
    string? IntakeEmailSlug,
    DateTimeOffset CreatedAt);

public sealed record AdminTenantDetailDto(
    Guid Id,
    string IdenTenantId,
    string Name,
    string? ProviderModeKey,
    bool IsActive,
    DateTimeOffset CreatedAt,
    IReadOnlyList<AdminBusinessSummaryDto> Businesses);

public sealed record ListAdminTenantsQuery(
    int Page,
    int PageSize,
    string? Search,
    bool? IsActive) : IRequest<PagedAdminTenantListDto>;

public sealed record GetAdminTenantQuery(Guid Id) : IRequest<AdminTenantDetailDto?>;

public sealed record CreateAdminTenantCommand(
    string Name,
    string? IdenTenantId,
    string? ProviderModeKey,
    string? InitialBusinessName,
    string? InitialIdenBusinessId,
    string UserId) : IRequest<AdminTenantDetailDto>;

file static class AdminTenantPaging
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

public sealed class ListAdminTenantsHandler(DocumateDbContext db)
    : IRequestHandler<ListAdminTenantsQuery, PagedAdminTenantListDto>
{
    public async Task<PagedAdminTenantListDto> Handle(
        ListAdminTenantsQuery request,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = AdminTenantPaging.Normalize(request.Page, request.PageSize);
        var q = db.CorTenants.AsNoTracking().Where(t => !t.IsDeleted);

        if (request.IsActive is bool active)
        {
            q = q.Where(t => t.IsActive == active);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            q = q.Where(t => t.Name.Contains(term) || t.IdenTenantId.Contains(term));
        }

        var total = await q.CountAsync(cancellationToken);
        var rows = await q
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.IdenTenantId,
                t.Name,
                t.ProviderModeEnumId,
                t.IsActive,
                BusinessCount = t.Businesses.Count(b => !b.IsDeleted),
                t.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var modeIds = rows.Select(r => r.ProviderModeEnumId).Distinct().ToList();
        var modes = await db.CorEnums.AsNoTracking()
            .Where(e => modeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.EnumKey, cancellationToken);

        var items = rows.Select(r => new AdminTenantListItemDto(
            r.Id,
            r.IdenTenantId,
            r.Name,
            modes.GetValueOrDefault(r.ProviderModeEnumId),
            r.IsActive,
            r.BusinessCount,
            r.CreatedAt)).ToList();

        return new PagedAdminTenantListDto(items, total, page, pageSize);
    }
}

public sealed class GetAdminTenantHandler(DocumateDbContext db)
    : IRequestHandler<GetAdminTenantQuery, AdminTenantDetailDto?>
{
    public async Task<AdminTenantDetailDto?> Handle(GetAdminTenantQuery request, CancellationToken cancellationToken)
    {
        var tenant = await db.CorTenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.Id && !t.IsDeleted, cancellationToken);
        if (tenant is null)
        {
            return null;
        }

        var modeKey = await db.CorEnums.AsNoTracking()
            .Where(e => e.Id == tenant.ProviderModeEnumId)
            .Select(e => e.EnumKey)
            .FirstOrDefaultAsync(cancellationToken);

        var businesses = await db.CorTenantBusinesses.AsNoTracking()
            .Where(b => b.TenantId == tenant.Id && !b.IsDeleted)
            .OrderBy(b => b.Name)
            .Select(b => new AdminBusinessSummaryDto(
                b.Id,
                b.IdenBusinessId,
                b.Name,
                b.IsActive,
                b.IntakeEmailSlug,
                b.CreatedAt))
            .ToListAsync(cancellationToken);

        return new AdminTenantDetailDto(
            tenant.Id,
            tenant.IdenTenantId,
            tenant.Name,
            modeKey,
            tenant.IsActive,
            tenant.CreatedAt,
            businesses);
    }
}

public sealed class CreateAdminTenantHandler(
    DocumateDbContext db,
    ICorEnumIdResolver enums,
    IDefaultQueueBootstrap defaultQueues,
    IDefaultWorkflowBootstrap defaultWorkflows,
    Documate.Api.Infrastructure.Iden.IIdenClient iden)
    : IRequestHandler<CreateAdminTenantCommand, AdminTenantDetailDto>
{
    public async Task<AdminTenantDetailDto> Handle(
        CreateAdminTenantCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Name.Length > 256)
        {
            throw new InvalidOperationException("Name must be at most 256 characters.");
        }

        string idenTenantId;
        string? idenBusinessId = string.IsNullOrWhiteSpace(request.InitialIdenBusinessId)
            ? null
            : request.InitialIdenBusinessId.Trim();
        string tenantName = request.Name;
        string? businessName = string.IsNullOrWhiteSpace(request.InitialBusinessName)
            ? null
            : request.InitialBusinessName.Trim();

        if (iden.IsConfigured && string.IsNullOrWhiteSpace(request.IdenTenantId))
        {
            // DQ-2005: Iden-first when ServiceClient configured and no explicit IdenTenantId override.
            var created = await iden.CreateTenantWithFirstBusinessAsync(
                tenantName,
                businessName ?? tenantName,
                productId: null,
                cancellationToken);
            idenTenantId = created.TenantId;
            idenBusinessId = created.BusinessId;
            tenantName = created.TenantName;
            businessName = created.BusinessName ?? businessName;
        }
        else
        {
            idenTenantId = string.IsNullOrWhiteSpace(request.IdenTenantId)
                ? throw new InvalidOperationException(
                    "Iden is not configured — provide IdenTenantId from Iden, or configure Iden:Documate ServiceClient.")
                : request.IdenTenantId.Trim();
        }

        if (await db.CorTenants.AnyAsync(t => t.IdenTenantId == idenTenantId && !t.IsDeleted, cancellationToken))
        {
            throw new InvalidOperationException($"IdenTenantId '{idenTenantId}' already exists.");
        }

        var modeKey = string.IsNullOrWhiteSpace(request.ProviderModeKey) ? "mode_1" : request.ProviderModeKey.Trim();
        var modeId = enums.Require("provider_mode", modeKey);

        var tenant = new CorTenant
        {
            IdenTenantId = idenTenantId,
            Name = tenantName,
            ProviderModeEnumId = modeId,
            IsActive = true,
            SyncStatus = Documate.Api.Infrastructure.Iden.TenancySyncStatuses.Ok,
            LastSyncedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = request.UserId,
            UpdatedByUserId = request.UserId,
        };
        db.CorTenants.Add(tenant);
        await db.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(businessName) || !string.IsNullOrWhiteSpace(idenBusinessId))
        {
            var name = (businessName ?? tenantName).Trim();
            if (name.Length > 256)
            {
                throw new InvalidOperationException("InitialBusinessName must be at most 256 characters.");
            }

            var businessId = idenBusinessId
                ?? throw new InvalidOperationException(
                    "Business Iden id required — leave IdenTenantId blank to create via Iden, or pass InitialIdenBusinessId when linking.");

            db.CorTenantBusinesses.Add(new CorTenantBusiness
            {
                TenantId = tenant.Id,
                IdenBusinessId = businessId,
                Name = name,
                TenantName = tenant.Name,
                IsActive = true,
                SyncStatus = Documate.Api.Infrastructure.Iden.TenancySyncStatuses.Ok,
                LastSyncedAtUtc = DateTimeOffset.UtcNow,
                CreatedByUserId = request.UserId,
                UpdatedByUserId = request.UserId,
            });
            await db.SaveChangesAsync(cancellationToken);

            await defaultQueues.EnsureDefaultAsync(businessId, request.UserId, cancellationToken);
            await defaultWorkflows.EnsureNormalizeFieldsAsync(businessId, request.UserId, cancellationToken);
        }

        var detail = await new GetAdminTenantHandler(db).Handle(new GetAdminTenantQuery(tenant.Id), cancellationToken);
        return detail!;
    }
}
