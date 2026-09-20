namespace Documate.Api.Modules.PlatformAdmin.Features.DocumentTypes;

using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize(Policy = PlatformAdminAuth.PolicyName)]
[Route("api/admin/document-types")]
public sealed class AdminDocumentTypesController(DocumateDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminDocumentTypeDto>>> List(CancellationToken cancellationToken)
    {
        var list = await db.CorDocumentTypes.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new AdminDocumentTypeDto(x.Id, x.DocumentTypeKey, x.Name))
            .ToListAsync(cancellationToken);
        return Ok(list);
    }
}

public sealed record AdminDocumentTypeDto(long Id, string DocumentTypeKey, string Name);
