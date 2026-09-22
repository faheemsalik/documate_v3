using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Iden;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>App-side F2 API key management (Documate-owned integration keys — DR-KEY-1 A / Band 20).</summary>
[ApiController]
[Authorize]
[Route("api/app/api-keys")]
public sealed class ApiKeysController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApiKeyListItemDto>>> List(CancellationToken cancellationToken)
    {
        try
        {
            var items = await mediator.Send(new ListApiKeysQuery(), cancellationToken);
            return Ok(items);
        }
        catch (FeatureDeniedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Reason, feature = ex.CapabilityKey });
        }
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

        try
        {
            var created = await mediator.Send(new CreateApiKeyCommand(body.Name, body.ExpiresAt), cancellationToken);
            return CreatedAtAction(nameof(List), created);
        }
        catch (FeatureDeniedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Reason, feature = ex.CapabilityKey });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var ok = await mediator.Send(new RevokeApiKeyCommand(id), cancellationToken);
            return ok ? NoContent() : NotFound();
        }
        catch (FeatureDeniedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Reason, feature = ex.CapabilityKey });
        }
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

public sealed class ListApiKeysHandler(IApiKeyService apiKeys, IFeatureEnforce enforce)
    : IRequestHandler<ListApiKeysQuery, IReadOnlyList<ApiKeyListItemDto>>
{
    public async Task<IReadOnlyList<ApiKeyListItemDto>> Handle(ListApiKeysQuery request, CancellationToken cancellationToken)
    {
        await enforce.EnsureAllowedAsync(FeatureKeys.CustomerApiKeysList, cancellationToken);
        var items = await apiKeys.ListAsync(cancellationToken);
        return items.Select(i => new ApiKeyListItemDto(
            i.Id, i.Name, i.KeyPrefix, i.IsActive, i.ExpiresAt, i.LastUsedAt, i.CreatedAt)).ToList();
    }
}

public sealed class CreateApiKeyHandler(IApiKeyService apiKeys, IFeatureEnforce enforce)
    : IRequestHandler<CreateApiKeyCommand, CreatedApiKeyDto>
{
    public async Task<CreatedApiKeyDto> Handle(CreateApiKeyCommand request, CancellationToken cancellationToken)
    {
        await enforce.EnsureAllowedAsync(FeatureKeys.CustomerApiKeysManage, cancellationToken);
        var created = await apiKeys.CreateAsync(request.Name, request.ExpiresAt, cancellationToken);
        return new CreatedApiKeyDto(created.Id, created.Name, created.KeyPrefix, created.RawKey, created.ExpiresAt);
    }
}

public sealed class RevokeApiKeyHandler(IApiKeyService apiKeys, IFeatureEnforce enforce)
    : IRequestHandler<RevokeApiKeyCommand, bool>
{
    public async Task<bool> Handle(RevokeApiKeyCommand request, CancellationToken cancellationToken)
    {
        await enforce.EnsureAllowedAsync(FeatureKeys.CustomerApiKeysManage, cancellationToken);
        return await apiKeys.RevokeAsync(request.Id, cancellationToken);
    }
}
