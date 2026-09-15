namespace Documate.Api.Modules.PlatformAdmin.Features.Providers;

using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize(Policy = PlatformAdminAuth.PolicyName)]
[Route("api/admin/providers")]
public sealed class AdminProvidersController(DocumateDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminProviderDto>>> List(
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        var query =
            from p in db.CorProviders.AsNoTracking()
            join e in db.CorEnums.AsNoTracking() on p.CategoryEnumId equals e.Id
            where p.IsActive
            select new { p, CategoryKey = e.EnumKey };

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim();
            query = query.Where(x => x.CategoryKey == cat);
        }

        var list = await query
            .OrderBy(x => x.p.Name)
            .Select(x => new AdminProviderDto(
                x.p.Id,
                x.p.ProviderKey,
                x.p.Name,
                x.p.VendorHint,
                x.CategoryKey))
            .ToListAsync(cancellationToken);

        return Ok(list);
    }
}

public sealed record AdminProviderDto(
    long Id,
    string ProviderKey,
    string Name,
    string? VendorHint,
    string CategoryKey);
