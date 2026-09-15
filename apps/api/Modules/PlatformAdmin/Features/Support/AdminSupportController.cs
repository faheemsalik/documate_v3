namespace Documate.Api.Modules.PlatformAdmin.Features.Support;

using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize(Policy = PlatformAdminAuth.PolicyName)]
[Route("api/admin/support")]
public sealed class AdminSupportController(IMediator mediator) : ControllerBase
{
    [HttpGet("lookup")]
    public Task<AdminSupportLookupDto> Lookup(
        [FromQuery] string q,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new AdminSupportLookupQuery(q ?? ""), cancellationToken);
}

public sealed record AdminSupportHitDto(
    string Kind,
    string Id,
    string Label,
    string? Secondary,
    string? DeepLink);

public sealed record AdminSupportLookupDto(string Query, IReadOnlyList<AdminSupportHitDto> Hits);

public sealed record AdminSupportLookupQuery(string Query) : IRequest<AdminSupportLookupDto>;

public sealed class AdminSupportLookupHandler(DocumateDbContext db)
    : IRequestHandler<AdminSupportLookupQuery, AdminSupportLookupDto>
{
    public async Task<AdminSupportLookupDto> Handle(
        AdminSupportLookupQuery request,
        CancellationToken cancellationToken)
    {
        var q = (request.Query ?? "").Trim();
        if (q.Length < 2)
        {
            return new AdminSupportLookupDto(q, []);
        }

        var hits = new List<AdminSupportHitDto>();

        var tenants = await db.CorTenants.AsNoTracking()
            .Where(t => !t.IsDeleted && (t.Name.Contains(q) || t.IdenTenantId.Contains(q)))
            .OrderBy(t => t.Name)
            .Take(8)
            .Select(t => new AdminSupportHitDto(
                "tenant",
                t.Id.ToString(),
                t.Name,
                t.IdenTenantId,
                $"/tenants/{t.Id}"))
            .ToListAsync(cancellationToken);
        hits.AddRange(tenants);

        var businesses = await db.CorTenantBusinesses.AsNoTracking()
            .Where(b => !b.IsDeleted && (b.Name.Contains(q) || b.IdenBusinessId.Contains(q)))
            .OrderBy(b => b.Name)
            .Take(8)
            .Select(b => new AdminSupportHitDto(
                "business",
                b.IdenBusinessId,
                b.Name,
                b.TenantName,
                $"/businesses/{b.IdenBusinessId}"))
            .ToListAsync(cancellationToken);
        hits.AddRange(businesses);

        if (Guid.TryParse(q, out var guid))
        {
            var file = await db.OpsFiles.AsNoTracking()
                .Where(f => !f.IsDeleted && f.Id == guid)
                .Select(f => new { f.Id, f.OriginalFileName, f.BusinessId })
                .FirstOrDefaultAsync(cancellationToken);
            if (file is not null)
            {
                hits.Add(new AdminSupportHitDto(
                    "file",
                    file.Id.ToString(),
                    file.OriginalFileName ?? file.Id.ToString(),
                    file.BusinessId,
                    $"/ops?tab=files&fileId={file.Id}&businessId={file.BusinessId}"));
            }

            var doc = await db.OpsDocuments.AsNoTracking()
                .Where(d => !d.IsDeleted && d.Id == guid)
                .Select(d => new { d.Id, d.FileId, d.BusinessId })
                .FirstOrDefaultAsync(cancellationToken);
            if (doc is not null)
            {
                hits.Add(new AdminSupportHitDto(
                    "document",
                    doc.Id.ToString(),
                    doc.Id.ToString(),
                    doc.BusinessId,
                    $"/ops?tab=documents&documentId={doc.Id}&businessId={doc.BusinessId}"));
            }
        }
        else
        {
            var files = await db.OpsFiles.AsNoTracking()
                .Where(f => !f.IsDeleted && f.OriginalFileName != null && f.OriginalFileName.Contains(q))
                .OrderByDescending(f => f.CreatedAt)
                .Take(5)
                .Select(f => new AdminSupportHitDto(
                    "file",
                    f.Id.ToString(),
                    f.OriginalFileName!,
                    f.BusinessId,
                    $"/ops?tab=files&fileId={f.Id}&businessId={f.BusinessId}"))
                .ToListAsync(cancellationToken);
            hits.AddRange(files);
        }

        return new AdminSupportLookupDto(q, hits);
    }
}
