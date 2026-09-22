namespace Documate.Api.Modules.FrontendSupport.Features.Users;

using System.Security.Claims;
using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Iden;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>Customer Users &amp; permissions API (Band 20 DQ-2012) — orchestrates Iden members/roles.</summary>
[ApiController]
[Authorize]
[Route("api/app/users")]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppUserDto>>> List(CancellationToken cancellationToken)
    {
        var items = await mediator.Send(new ListAppUsersQuery(), cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<AppUserDto>> Invite(
        [FromBody] InviteAppUserRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.Email))
        {
            return BadRequest(new { error = "Email is required." });
        }

        try
        {
            var created = await mediator.Send(
                new InviteAppUserCommand(
                    body.Email.Trim(),
                    body.DisplayName?.Trim() ?? body.Email.Trim(),
                    body.RoleSysKey?.Trim() ?? "documate.viewer"),
                cancellationToken);
            return Ok(created);
        }
        catch (FeatureDeniedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Reason, feature = ex.CapabilityKey });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{memberId}/role")]
    public async Task<ActionResult> AssignRole(
        string memberId,
        [FromBody] AssignAppUserRoleRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.RoleSysKey))
        {
            return BadRequest(new { error = "RoleSysKey is required." });
        }

        try
        {
            await mediator.Send(new AssignAppUserRoleCommand(memberId, body.RoleSysKey.Trim()), cancellationToken);
            return NoContent();
        }
        catch (FeatureDeniedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Reason, feature = ex.CapabilityKey });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{memberId}/deactivate")]
    public async Task<ActionResult> Deactivate(string memberId, CancellationToken cancellationToken)
    {
        try
        {
            await mediator.Send(new DeactivateAppUserCommand(memberId), cancellationToken);
            return NoContent();
        }
        catch (FeatureDeniedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Reason, feature = ex.CapabilityKey });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public sealed record AppUserDto(
    string MemberId,
    string IdentityId,
    string Email,
    string DisplayName,
    string? RoleSysKey,
    bool IsActive);

public sealed record InviteAppUserRequest(string Email, string? DisplayName, string? RoleSysKey);
public sealed record AssignAppUserRoleRequest(string RoleSysKey);

public sealed record ListAppUsersQuery : IRequest<IReadOnlyList<AppUserDto>>;
public sealed record InviteAppUserCommand(string Email, string DisplayName, string RoleSysKey) : IRequest<AppUserDto>;
public sealed record AssignAppUserRoleCommand(string MemberId, string RoleSysKey) : IRequest;
public sealed record DeactivateAppUserCommand(string MemberId) : IRequest;

public sealed class ListAppUsersHandler(IIdenClient iden, IBusinessContext business, IFeatureEnforce enforce)
    : IRequestHandler<ListAppUsersQuery, IReadOnlyList<AppUserDto>>
{
    public async Task<IReadOnlyList<AppUserDto>> Handle(ListAppUsersQuery request, CancellationToken cancellationToken)
    {
        await enforce.EnsureAllowedAsync(FeatureKeys.CustomerUsersList, cancellationToken);
        if (!iden.IsConfigured)
        {
            return Array.Empty<AppUserDto>();
        }

        var members = await iden.ListBusinessMembersAsync(business.TenantId, business.BusinessId, cancellationToken);
        return members
            .Select(m => new AppUserDto(m.MemberId, m.IdentityId, m.Email, m.DisplayName, m.RoleSysKey, m.IsActive))
            .ToList();
    }
}

public sealed class InviteAppUserHandler(IIdenClient iden, IBusinessContext business, IFeatureEnforce enforce)
    : IRequestHandler<InviteAppUserCommand, AppUserDto>
{
    public async Task<AppUserDto> Handle(InviteAppUserCommand request, CancellationToken cancellationToken)
    {
        await enforce.EnsureAllowedAsync(FeatureKeys.CustomerUsersInvite, cancellationToken);
        if (!iden.IsConfigured)
        {
            throw new InvalidOperationException("Iden is not configured — cannot invite users.");
        }

        var m = await iden.InviteInternalMemberAsync(
            business.TenantId,
            business.BusinessId,
            request.Email,
            request.DisplayName,
            request.RoleSysKey,
            cancellationToken);
        return new AppUserDto(m.MemberId, m.IdentityId, m.Email, m.DisplayName, m.RoleSysKey, m.IsActive);
    }
}

public sealed class AssignAppUserRoleHandler(IIdenClient iden, IBusinessContext business, IFeatureEnforce enforce)
    : IRequestHandler<AssignAppUserRoleCommand>
{
    public async Task Handle(AssignAppUserRoleCommand request, CancellationToken cancellationToken)
    {
        await enforce.EnsureAllowedAsync(FeatureKeys.CustomerUsersPermissionsManage, cancellationToken);
        if (!iden.IsConfigured)
        {
            throw new InvalidOperationException("Iden is not configured — cannot assign roles.");
        }

        await iden.AssignMemberRoleAsync(
            business.TenantId,
            business.BusinessId,
            request.MemberId,
            request.RoleSysKey,
            cancellationToken);
    }
}

public sealed class DeactivateAppUserHandler(IIdenClient iden, IBusinessContext business, IFeatureEnforce enforce)
    : IRequestHandler<DeactivateAppUserCommand>
{
    public async Task Handle(DeactivateAppUserCommand request, CancellationToken cancellationToken)
    {
        await enforce.EnsureAllowedAsync(FeatureKeys.CustomerUsersDeactivate, cancellationToken);
        if (!iden.IsConfigured)
        {
            throw new InvalidOperationException("Iden is not configured — cannot deactivate users.");
        }

        await iden.DeactivateMemberAsync(
            business.TenantId,
            business.BusinessId,
            request.MemberId,
            cancellationToken);
    }
}
