namespace Documate.Api.Modules.FrontendSupport.Features.SystemInfo;

using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/app/system")]
public sealed class SystemInfoController(IMediator mediator) : ControllerBase
{
    [HttpGet("ping")]
    public Task<PingResponse> Ping(CancellationToken cancellationToken) =>
        mediator.Send(new PingQuery(), cancellationToken);
}

public sealed record PingQuery : IRequest<PingResponse>;

public sealed class PingHandler : IRequestHandler<PingQuery, PingResponse>
{
    public Task<PingResponse> Handle(PingQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(new PingResponse("ok"));
}

public sealed record PingResponse(string Status);
