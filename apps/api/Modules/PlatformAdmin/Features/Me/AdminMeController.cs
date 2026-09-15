namespace Documate.Api.Modules.PlatformAdmin.Features.Me;

using System.Security.Claims;
using Documate.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize(Policy = PlatformAdminAuth.PolicyName)]
[Route("api/admin/me")]
public sealed class AdminMeController : ControllerBase
{
    [HttpGet]
    public ActionResult<AdminMeResponse> Get()
    {
        var userId = User.FindFirstValue(AuthClaimTypes.UserId)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "";
        var isPlatformAdmin = User.HasClaim(AuthClaimTypes.PlatformAdmin, "true")
            || User.IsInRole(PlatformAdminAuth.RoleName);

        return Ok(new AdminMeResponse(userId, isPlatformAdmin));
    }
}

public sealed record AdminMeResponse(string UserId, bool IsPlatformAdmin);
