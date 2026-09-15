namespace Documate.Api.Modules.PlatformAdmin.Features.Businesses;

using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize(Policy = PlatformAdminAuth.PolicyName)]
[Route("api/admin/businesses")]
public sealed class AdminBusinessesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public Task<PagedAdminBusinessListDto> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new ListAdminBusinessesQuery(page, pageSize, search, tenantId, isActive), cancellationToken);

    [HttpGet("{businessId}")]
    public async Task<ActionResult<AdminBusinessDetailDto>> Get(
        string businessId,
        CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetAdminBusinessQuery(businessId), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }
}

public sealed record AdminBusinessListItemDto(
    Guid Id,
    string IdenBusinessId,
    string Name,
    Guid TenantId,
    string TenantName,
    string IdenTenantId,
    bool IsActive,
    string? IntakeEmailSlug,
    DateTimeOffset CreatedAt);

public sealed record PagedAdminBusinessListDto(
    IReadOnlyList<AdminBusinessListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AdminQueueSummaryDto(Guid Id, string Name, bool IsDefault);
public sealed record AdminAgentSummaryDto(Guid Id, string Name, bool IsActive);

public sealed record AdminRecentFileStatsDto(int Total, int Failed, DateTimeOffset? LastCreatedAt);

public sealed record AdminBusinessDetailDto(
    Guid Id,
    string IdenBusinessId,
    string Name,
    Guid TenantId,
    string TenantName,
    string IdenTenantId,
    bool IsActive,
    string? IntakeEmailSlug,
    DateTimeOffset CreatedAt,
    IReadOnlyList<AdminQueueSummaryDto> Queues,
    IReadOnlyList<AdminAgentSummaryDto> Agents,
    AdminRecentFileStatsDto RecentFileStats);

public sealed record ListAdminBusinessesQuery(
    int Page,
    int PageSize,
    string? Search,
    Guid? TenantId,
    bool? IsActive) : IRequest<PagedAdminBusinessListDto>;

public sealed record GetAdminBusinessQuery(string BusinessId) : IRequest<AdminBusinessDetailDto?>;

file static class AdminBusinessPaging
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

public sealed class ListAdminBusinessesHandler(DocumateDbContext db)
    : IRequestHandler<ListAdminBusinessesQuery, PagedAdminBusinessListDto>
{
    public async Task<PagedAdminBusinessListDto> Handle(
        ListAdminBusinessesQuery request,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = AdminBusinessPaging.Normalize(request.Page, request.PageSize);

        var q =
            from b in db.CorTenantBusinesses.AsNoTracking()
            where !b.IsDeleted
            join t in db.CorTenants.AsNoTracking() on b.TenantId equals t.Id
            where !t.IsDeleted
            select new { Business = b, Tenant = t };

        if (request.TenantId is Guid tid)
        {
            q = q.Where(x => x.Business.TenantId == tid);
        }

        if (request.IsActive is bool active)
        {
            q = q.Where(x => x.Business.IsActive == active);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            q = q.Where(x =>
                x.Business.Name.Contains(term)
                || x.Business.IdenBusinessId.Contains(term)
                || x.Tenant.Name.Contains(term)
                || x.Tenant.IdenTenantId.Contains(term));
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderBy(x => x.Business.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminBusinessListItemDto(
                x.Business.Id,
                x.Business.IdenBusinessId,
                x.Business.Name,
                x.Tenant.Id,
                x.Tenant.Name,
                x.Tenant.IdenTenantId,
                x.Business.IsActive,
                x.Business.IntakeEmailSlug,
                x.Business.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedAdminBusinessListDto(items, total, page, pageSize);
    }
}

public sealed class GetAdminBusinessHandler(DocumateDbContext db, ICorEnumIdResolver enums)
    : IRequestHandler<GetAdminBusinessQuery, AdminBusinessDetailDto?>
{
    public async Task<AdminBusinessDetailDto?> Handle(
        GetAdminBusinessQuery request,
        CancellationToken cancellationToken)
    {
        var row = await (
            from b in db.CorTenantBusinesses.AsNoTracking()
            where !b.IsDeleted && b.IdenBusinessId == request.BusinessId
            join t in db.CorTenants.AsNoTracking() on b.TenantId equals t.Id
            where !t.IsDeleted
            select new { Business = b, Tenant = t }).FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var queues = await db.OpsQueues.AsNoTracking()
            .Where(q => q.BusinessId == request.BusinessId && !q.IsDeleted)
            .OrderBy(q => q.Name)
            .Select(q => new AdminQueueSummaryDto(q.Id, q.Name, q.IsDefault))
            .ToListAsync(cancellationToken);

        var agents = await db.OpsAgents.AsNoTracking()
            .Where(a => a.BusinessId == request.BusinessId && !a.IsDeleted)
            .OrderBy(a => a.Name)
            .Select(a => new AdminAgentSummaryDto(a.Id, a.Name, a.IsActive))
            .ToListAsync(cancellationToken);

        var since = DateTimeOffset.UtcNow.AddDays(-7);
        long? failedId = null;
        try
        {
            failedId = enums.Require("file_public_status", "failed");
        }
        catch (InvalidOperationException)
        {
            // leave null
        }

        var recentFiles = db.OpsFiles.AsNoTracking()
            .Where(f => f.BusinessId == request.BusinessId && !f.IsDeleted && f.CreatedAt >= since);

        var total = await recentFiles.CountAsync(cancellationToken);
        var failed = failedId is long fid
            ? await recentFiles.CountAsync(f => f.PublicStatusEnumId == fid, cancellationToken)
            : 0;
        var last = await recentFiles
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => (DateTimeOffset?)f.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new AdminBusinessDetailDto(
            row.Business.Id,
            row.Business.IdenBusinessId,
            row.Business.Name,
            row.Tenant.Id,
            row.Tenant.Name,
            row.Tenant.IdenTenantId,
            row.Business.IsActive,
            row.Business.IntakeEmailSlug,
            row.Business.CreatedAt,
            queues,
            agents,
            new AdminRecentFileStatsDto(total, failed, last));
    }
}
