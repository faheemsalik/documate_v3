namespace Documate.Api.Modules.FrontendSupport.Features.Me;

using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Queues;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Route("api/app/me")]
public sealed class MeController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public Task<MeResponse> Get(CancellationToken cancellationToken) =>
        mediator.Send(new GetMeQuery(), cancellationToken);
}

public sealed record MeResponse(
    string UserId,
    string TenantId,
    string BusinessId,
    string? TenantName,
    string? BusinessName,
    Guid? DefaultQueueId,
    string? BuContextId,
    string? IdentityClass);

public sealed record GetMeQuery : IRequest<MeResponse>;

public sealed class GetMeHandler(IBusinessContext businessContext, IDefaultQueueBootstrap defaultQueues)
    : IRequestHandler<GetMeQuery, MeResponse>
{
    public async Task<MeResponse> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        Guid? defaultQueueId = null;
        if (!string.IsNullOrWhiteSpace(businessContext.BusinessId))
        {
            defaultQueueId = await defaultQueues.GetDefaultQueueIdAsync(
                businessContext.BusinessId,
                cancellationToken);
        }

        return new MeResponse(
            businessContext.UserId,
            businessContext.TenantId,
            businessContext.BusinessId,
            businessContext.TenantName,
            businessContext.BusinessName,
            defaultQueueId,
            businessContext.BuContextId,
            businessContext.IdentityClass);
    }
}
