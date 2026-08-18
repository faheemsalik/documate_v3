namespace Documate.Api.Modules.FrontendSupport.Features.Me;

using Documate.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Route("api/app/me")]
public sealed class MeController(IBusinessContext businessContext) : ControllerBase
{
    [HttpGet]
    public ActionResult<MeResponse> Get() =>
        Ok(new MeResponse(
            businessContext.UserId,
            businessContext.TenantId,
            businessContext.BusinessId,
            businessContext.TenantName,
            businessContext.BusinessName));
}

public sealed record MeResponse(
    string UserId,
    string TenantId,
    string BusinessId,
    string? TenantName,
    string? BusinessName);
