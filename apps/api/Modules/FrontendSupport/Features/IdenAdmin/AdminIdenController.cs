namespace Documate.Api.Modules.FrontendSupport.Features.IdenAdmin;

using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Iden;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>Ops helpers for Iden catalog sync (Band 20). PlatformAdmin only.</summary>
[ApiController]
[Authorize(Policy = PlatformAdminAuth.PolicyName)]
[Route("api/admin/iden")]
public sealed class AdminIdenController(IIdenClient iden) : ControllerBase
{
    [HttpPost("catalog/sync")]
    public async Task<ActionResult> SyncCatalog(CancellationToken cancellationToken)
    {
        if (!iden.IsConfigured)
        {
            return BadRequest(new { error = "Iden is not configured." });
        }

        await iden.SyncCatalogFeaturesAsync(cancellationToken);
        return Ok(new { status = "ok" });
    }

    [HttpGet("status")]
    public ActionResult Status() =>
        Ok(new { configured = iden.IsConfigured });
}
