namespace Documate.Api.Modules.FrontendSupport.Features.Auth;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/app/auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>Interim static login until Iden — validates credentials from Auth:InterimFeGate.</summary>
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
}

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    string UserId,
    string TenantId,
    string BusinessId);

public sealed record LoginCommand(string Username, string Password) : IRequest<LoginResponse?>;
