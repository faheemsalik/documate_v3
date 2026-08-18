namespace Documate.Api.Modules.FrontendSupport.Features.ApiKeys;

using Documate.Api.Infrastructure.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>App-side F2 API key management (temporary bridge — retire Band 15 / DQ-1506).</summary>
[ApiController]
[Authorize]
[Route("api/app/api-keys")]
public sealed class ApiKeysController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApiKeyListItemDto>>> List(CancellationToken cancellationToken)
    {
        var items = await mediator.Send(new ListApiKeysQuery(), cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<CreatedApiKeyDto>> Create(
        [FromBody] CreateApiKeyRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
        {
            return BadRequest(new { error = "name is required" });
        }

        var created = await mediator.Send(new CreateApiKeyCommand(body.Name, body.ExpiresAt), cancellationToken);
        return CreatedAtAction(nameof(List), created);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken cancellationToken)
    {
        var ok = await mediator.Send(new RevokeApiKeyCommand(id), cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}

public sealed record CreateApiKeyRequest(string Name, DateTimeOffset? ExpiresAt);
public sealed record CreatedApiKeyDto(Guid Id, string Name, string KeyPrefix, string ApiKey, DateTimeOffset? ExpiresAt);
public sealed record ApiKeyListItemDto(
    Guid Id,
    string Name,
    string KeyPrefix,
    bool IsActive,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset CreatedAt);

public sealed record ListApiKeysQuery : IRequest<IReadOnlyList<ApiKeyListItemDto>>;
public sealed record CreateApiKeyCommand(string Name, DateTimeOffset? ExpiresAt) : IRequest<CreatedApiKeyDto>;
public sealed record RevokeApiKeyCommand(Guid Id) : IRequest<bool>;

public sealed class ListApiKeysHandler(IApiKeyService apiKeys) : IRequestHandler<ListApiKeysQuery, IReadOnlyList<ApiKeyListItemDto>>
{
    public async Task<IReadOnlyList<ApiKeyListItemDto>> Handle(ListApiKeysQuery request, CancellationToken cancellationToken)
    {
        var items = await apiKeys.ListAsync(cancellationToken);
        return items.Select(i => new ApiKeyListItemDto(
            i.Id, i.Name, i.KeyPrefix, i.IsActive, i.ExpiresAt, i.LastUsedAt, i.CreatedAt)).ToList();
    }
}

public sealed class CreateApiKeyHandler(IApiKeyService apiKeys) : IRequestHandler<CreateApiKeyCommand, CreatedApiKeyDto>
{
    public async Task<CreatedApiKeyDto> Handle(CreateApiKeyCommand request, CancellationToken cancellationToken)
    {
        var created = await apiKeys.CreateAsync(request.Name, request.ExpiresAt, cancellationToken);
        return new CreatedApiKeyDto(created.Id, created.Name, created.KeyPrefix, created.RawKey, created.ExpiresAt);
    }
}

public sealed class RevokeApiKeyHandler(IApiKeyService apiKeys) : IRequestHandler<RevokeApiKeyCommand, bool>
{
    public Task<bool> Handle(RevokeApiKeyCommand request, CancellationToken cancellationToken) =>
        apiKeys.RevokeAsync(request.Id, cancellationToken);
}
