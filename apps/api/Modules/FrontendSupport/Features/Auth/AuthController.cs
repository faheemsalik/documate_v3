namespace Documate.Api.Modules.FrontendSupport.Features.Auth;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/app/auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Interim static login (Auth:InterimFeGate) when Auth:Mode=DevBypass.
    /// When Auth:Mode=Iden, SPA authenticates directly against Iden (see architecture/auth-iden.md);
    /// this endpoint remains for local bridge only.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new LoginCommand(request.Username, request.Password), cancellationToken);
        if (result is null)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        return Ok(result);
    }

    /// <summary>DQ-2007: session echo for SPA after Iden JWT attach (no password).</summary>
    [Authorize]
    [HttpGet("session")]
    public async Task<ActionResult<SessionResponse>> Session(CancellationToken cancellationToken)
    {
        var session = await mediator.Send(new GetAuthSessionQuery(), cancellationToken);
        return Ok(session);
    }

    [Authorize]
    [HttpPost("logout")]
    public ActionResult Logout() =>
        // JWT is client-held; SPA clears storage. Iden revoke (if any) is SPA→Iden direct.
        NoContent();
}

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    string UserId,
    string TenantId,
    string BusinessId);

public sealed record SessionResponse(
    string UserId,
    string TenantId,
    string BusinessId,
    string? BuContextId,
    string? IdentityClass,
    string AuthMode);

public sealed record LoginCommand(string Username, string Password) : IRequest<LoginResponse?>;

public sealed record GetAuthSessionQuery : IRequest<SessionResponse>;
