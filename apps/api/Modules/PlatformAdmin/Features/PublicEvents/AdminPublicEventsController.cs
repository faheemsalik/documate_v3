namespace Documate.Api.Modules.PlatformAdmin.Features.PublicEvents;

using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.PublicEvents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize(Policy = PlatformAdminAuth.PolicyName)]
[Route("api/admin/public-events")]
public sealed class AdminPublicEventsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public Task<AdminPublicEventsCatalogDto> Get(CancellationToken cancellationToken) =>
        mediator.Send(new GetAdminPublicEventsCatalogQuery(), cancellationToken);
}

public sealed record AdminPublicEventDto(string EventKey, string ResourceTypeKey);

public sealed record AdminPublicActionTypeDto(string ActionTypeKey, string Name);

public sealed record AdminPublicEventsCatalogDto(
    IReadOnlyList<AdminPublicEventDto> Events,
    IReadOnlyList<AdminPublicActionTypeDto> ActionTypes);

public sealed record GetAdminPublicEventsCatalogQuery : IRequest<AdminPublicEventsCatalogDto>;

public sealed class GetAdminPublicEventsCatalogHandler
    : IRequestHandler<GetAdminPublicEventsCatalogQuery, AdminPublicEventsCatalogDto>
{
    public Task<AdminPublicEventsCatalogDto> Handle(
        GetAdminPublicEventsCatalogQuery request,
        CancellationToken cancellationToken)
    {
        var events = PublicEventCatalog.AllEvents
            .Select(key => new AdminPublicEventDto(key, ResourceTypeFor(key)))
            .ToList();

        IReadOnlyList<AdminPublicActionTypeDto> actionTypes =
        [
            new(PublicEventCatalog.ActionWebhook, "Webhook"),
            new(PublicEventCatalog.ActionEmail, "Email"),
            new(PublicEventCatalog.ActionInApp, "In-app"),
        ];

        return Task.FromResult(new AdminPublicEventsCatalogDto(events, actionTypes));
    }

    private static string ResourceTypeFor(string eventKey) =>
        eventKey is PublicEventCatalog.FileReceived or PublicEventCatalog.FileCompleted
            ? PublicEventCatalog.ResourceFile
            : PublicEventCatalog.ResourceDocument;
}
