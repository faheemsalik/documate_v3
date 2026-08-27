namespace Documate.Api.Modules.FrontendSupport.Features.Queues;

using System.Security.Cryptography;
using System.Text;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

[ApiController]
[Authorize]
[Route("api/app/queues")]
public sealed class QueuesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<QueueDto>> List(CancellationToken cancellationToken) =>
        mediator.Send(new ListQueuesQuery(), cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QueueDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetQueueByIdQuery(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<QueueDto>> Create([FromBody] CreateQueueRequest request, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new CreateQueueCommand(request), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<QueueDto>> Update(Guid id, [FromBody] UpdateQueueRequest request, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new UpdateQueueCommand(id, request), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var ok = await mediator.Send(new DeleteQueueCommand(id), cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/routes")]
    public async Task<ActionResult<IReadOnlyList<QueueRouteDto>>> ListRoutes(Guid id, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new ListQueueRoutesQuery(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("{id:guid}/routes")]
    public async Task<ActionResult<IReadOnlyList<QueueRouteDto>>> ReplaceRoutes(
        Guid id,
        [FromBody] ReplaceQueueRoutesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await mediator.Send(new ReplaceQueueRoutesCommand(id, request), cancellationToken);
            return dto is null ? NotFound() : Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/routing/lock")]
    public async Task<ActionResult<QueueDto>> LockRouting(Guid id, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new LockQueueRoutingCommand(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("{id:guid}/webhook")]
    public async Task<ActionResult<QueueDto>> UpdateWebhook(
        Guid id,
        [FromBody] UpdateQueueWebhookRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new UpdateQueueWebhookCommand(id, request), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("{id:guid}/email/mint")]
    public async Task<ActionResult<QueueEmailAddressDto>> MintEmail(Guid id, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new MintQueueEmailCommand(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("{id:guid}/email")]
    public async Task<ActionResult<QueueDto>> UpdateEmailSettings(
        Guid id,
        [FromBody] UpdateQueueEmailSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new UpdateQueueEmailSettingsCommand(id, request), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("{id:guid}/allowlist")]
    public async Task<ActionResult<IReadOnlyList<AllowlistEntryDto>>> ListAllowlist(Guid id, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new ListAllowlistQuery(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("{id:guid}/allowlist")]
    public async Task<ActionResult<AllowlistEntryDto>> AddAllowlist(
        Guid id,
        [FromBody] CreateAllowlistEntryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await mediator.Send(new CreateAllowlistEntryCommand(id, request), cancellationToken);
            return dto is null ? NotFound() : Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}/allowlist/{entryId:long}")]
    public async Task<IActionResult> DeleteAllowlist(Guid id, long entryId, CancellationToken cancellationToken)
    {
        var ok = await mediator.Send(new DeleteAllowlistEntryCommand(id, entryId), cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}

public sealed record QueueDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    bool RoutingLocked,
    DateTimeOffset? RoutingLockedAt,
    bool WebhookEnabled,
    string? WebhookUrl,
    bool HasWebhookSecret,
    bool EmailIntakeEnabled,
    string? EmailAddress,
    string? EmailLocalPart,
    string? EmailDomain,
    int EmailAddressVersion,
    long AllowlistModeEnumId,
    string? AllowlistModeKey,
    long WorkflowModeEnumId,
    string? WorkflowModeKey,
    long? WorkflowId);

public sealed record QueueDetailDto(
    QueueDto Queue,
    IReadOnlyList<QueueRouteDto> Routes,
    IReadOnlyList<AllowlistEntryDto> Allowlist);

public sealed record QueueRouteDto(long Id, long DocumentTypeId, string? DocumentTypeKey, Guid AgentId, string? AgentName);
public sealed record AllowlistEntryDto(long Id, long MatchTypeEnumId, string? MatchTypeKey, string Value);
public sealed record QueueEmailAddressDto(string EmailAddress, string LocalPart, string Domain, int Version);

public sealed record CreateQueueRequest(string Name, string? Description);
public sealed record UpdateQueueRequest(string Name, string? Description, bool IsActive);
public sealed record ReplaceQueueRoutesRequest(IReadOnlyList<QueueRouteUpsert> Routes);
public sealed record QueueRouteUpsert(long DocumentTypeId, Guid AgentId);
public sealed record UpdateQueueWebhookRequest(bool Enabled, string? Url, string? Secret);
public sealed record UpdateQueueEmailSettingsRequest(bool EmailIntakeEnabled, long AllowlistModeEnumId);
public sealed record CreateAllowlistEntryRequest(string MatchTypeKey, string Value);

public sealed record ListQueuesQuery : IRequest<IReadOnlyList<QueueDto>>;
public sealed record GetQueueByIdQuery(Guid Id) : IRequest<QueueDetailDto?>;
public sealed record CreateQueueCommand(CreateQueueRequest Request) : IRequest<QueueDto>;
public sealed record UpdateQueueCommand(Guid Id, UpdateQueueRequest Request) : IRequest<QueueDto?>;
public sealed record DeleteQueueCommand(Guid Id) : IRequest<bool>;
public sealed record ListQueueRoutesQuery(Guid QueueId) : IRequest<IReadOnlyList<QueueRouteDto>?>;
public sealed record ReplaceQueueRoutesCommand(Guid QueueId, ReplaceQueueRoutesRequest Request) : IRequest<IReadOnlyList<QueueRouteDto>?>;
public sealed record LockQueueRoutingCommand(Guid QueueId) : IRequest<QueueDto?>;
public sealed record UpdateQueueWebhookCommand(Guid QueueId, UpdateQueueWebhookRequest Request) : IRequest<QueueDto?>;
public sealed record MintQueueEmailCommand(Guid QueueId) : IRequest<QueueEmailAddressDto?>;
public sealed record UpdateQueueEmailSettingsCommand(Guid QueueId, UpdateQueueEmailSettingsRequest Request) : IRequest<QueueDto?>;
public sealed record ListAllowlistQuery(Guid QueueId) : IRequest<IReadOnlyList<AllowlistEntryDto>?>;
public sealed record CreateAllowlistEntryCommand(Guid QueueId, CreateAllowlistEntryRequest Request) : IRequest<AllowlistEntryDto?>;
public sealed record DeleteAllowlistEntryCommand(Guid QueueId, long EntryId) : IRequest<bool>;

internal static class QueueHelpers
{
    public static string HashSecret(string secret)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string? FormatEmail(OpsQueue q) =>
        string.IsNullOrWhiteSpace(q.EmailLocalPart) || string.IsNullOrWhiteSpace(q.EmailDomain)
            ? null
            : $"{q.EmailLocalPart}@{q.EmailDomain}";

    public static async Task<QueueDto> ToDtoAsync(DocumateDbContext db, ICorEnumIdResolver enums, OpsQueue q, CancellationToken ct)
    {
        string? allowKey = null;
        string? workflowKey = null;
        _ = enums.TryGet("allowlist_mode", "open", out _); // ensure resolver warm
        var allow = await db.CorEnums.AsNoTracking().FirstOrDefaultAsync(e => e.Id == q.AllowlistModeEnumId, ct);
        var workflow = await db.CorEnums.AsNoTracking().FirstOrDefaultAsync(e => e.Id == q.WorkflowModeEnumId, ct);
        allowKey = allow?.EnumKey;
        workflowKey = workflow?.EnumKey;

        return new QueueDto(
            q.Id,
            q.Name,
            q.Description,
            q.IsActive,
            q.RoutingLocked,
            q.RoutingLockedAt,
            q.WebhookEnabled,
            q.WebhookUrl,
            !string.IsNullOrEmpty(q.WebhookSecretProtected) || !string.IsNullOrEmpty(q.WebhookSecretHash),
            q.EmailIntakeEnabled,
            FormatEmail(q),
            q.EmailLocalPart,
            q.EmailDomain,
            q.EmailAddressVersion,
            q.AllowlistModeEnumId,
            allowKey,
            q.WorkflowModeEnumId,
            workflowKey,
            q.WorkflowId);
    }
}

public sealed class ListQueuesHandler(DocumateDbContext db, IBusinessContext business, ICorEnumIdResolver enums)
    : IRequestHandler<ListQueuesQuery, IReadOnlyList<QueueDto>>
{
    public async Task<IReadOnlyList<QueueDto>> Handle(ListQueuesQuery request, CancellationToken cancellationToken)
    {
        var rows = await db.OpsQueues.AsNoTracking()
            .Where(q => q.BusinessId == business.BusinessId)
            .OrderBy(q => q.Name)
            .ToListAsync(cancellationToken);

        var list = new List<QueueDto>();
        foreach (var q in rows)
        {
            list.Add(await QueueHelpers.ToDtoAsync(db, enums, q, cancellationToken));
        }

        return list;
    }
}

public sealed class GetQueueByIdHandler(DocumateDbContext db, IBusinessContext business, ICorEnumIdResolver enums)
    : IRequestHandler<GetQueueByIdQuery, QueueDetailDto?>
{
    public async Task<QueueDetailDto?> Handle(GetQueueByIdQuery request, CancellationToken cancellationToken)
    {
        var q = await db.OpsQueues.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.BusinessId == business.BusinessId, cancellationToken);
        if (q is null)
        {
            return null;
        }

        var routes = await LoadRoutes(db, q.Id, cancellationToken);
        var allowlist = await LoadAllowlist(db, q.Id, cancellationToken);
        return new QueueDetailDto(await QueueHelpers.ToDtoAsync(db, enums, q, cancellationToken), routes, allowlist);
    }

    internal static async Task<IReadOnlyList<QueueRouteDto>> LoadRoutes(DocumateDbContext db, Guid queueId, CancellationToken ct) =>
        await (
            from r in db.OpsQueueRoutes.AsNoTracking()
            join d in db.CorDocumentTypes.AsNoTracking() on r.DocumentTypeId equals d.Id
            join a in db.OpsAgents.AsNoTracking() on r.AgentId equals a.Id
            where r.QueueId == queueId
            orderby d.Name
            select new QueueRouteDto(r.Id, r.DocumentTypeId, d.DocumentTypeKey, r.AgentId, a.Name)
        ).ToListAsync(ct);

    internal static async Task<IReadOnlyList<AllowlistEntryDto>> LoadAllowlist(DocumateDbContext db, Guid queueId, CancellationToken ct) =>
        await (
            from e in db.OpsQueueEmailAllowlistEntries.AsNoTracking()
            join m in db.CorEnums.AsNoTracking() on e.MatchTypeEnumId equals m.Id
            where e.QueueId == queueId
            orderby e.Value
            select new AllowlistEntryDto(e.Id, e.MatchTypeEnumId, m.EnumKey, e.Value)
        ).ToListAsync(ct);
}

public sealed class CreateQueueHandler(DocumateDbContext db, IBusinessContext business, ICorEnumIdResolver enums)
    : IRequestHandler<CreateQueueCommand, QueueDto>
{
    public async Task<QueueDto> Handle(CreateQueueCommand command, CancellationToken cancellationToken)
    {
        var openId = enums.Require("allowlist_mode", "open");
        var inheritId = enums.Require("workflow_mode", "inherit_agent_default");

        var q = new OpsQueue
        {
            BusinessId = business.BusinessId,
            Name = command.Request.Name.Trim(),
            Description = command.Request.Description,
            AllowlistModeEnumId = openId,
            WorkflowModeEnumId = inheritId,
            IsActive = true,
            CreatedByUserId = business.UserId,
            UpdatedByUserId = business.UserId,
        };

        db.OpsQueues.Add(q);
        await db.SaveChangesAsync(cancellationToken);
        return await QueueHelpers.ToDtoAsync(db, enums, q, cancellationToken);
    }
}

public sealed class UpdateQueueHandler(DocumateDbContext db, IBusinessContext business, ICorEnumIdResolver enums)
    : IRequestHandler<UpdateQueueCommand, QueueDto?>
{
    public async Task<QueueDto?> Handle(UpdateQueueCommand command, CancellationToken cancellationToken)
    {
        var q = await db.OpsQueues.FirstOrDefaultAsync(
            x => x.Id == command.Id && x.BusinessId == business.BusinessId,
            cancellationToken);
        if (q is null)
        {
            return null;
        }

        q.Name = command.Request.Name.Trim();
        q.Description = command.Request.Description;
        q.IsActive = command.Request.IsActive;
        q.UpdatedByUserId = business.UserId;
        await db.SaveChangesAsync(cancellationToken);
        return await QueueHelpers.ToDtoAsync(db, enums, q, cancellationToken);
    }
}

public sealed class DeleteQueueHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<DeleteQueueCommand, bool>
{
    public async Task<bool> Handle(DeleteQueueCommand command, CancellationToken cancellationToken)
    {
        var q = await db.OpsQueues.FirstOrDefaultAsync(
            x => x.Id == command.Id && x.BusinessId == business.BusinessId,
            cancellationToken);
        if (q is null)
        {
            return false;
        }

        q.IsDeleted = true;
        q.DeletedAt = DateTimeOffset.UtcNow;
        q.DeletedByUserId = business.UserId;
        q.UpdatedByUserId = business.UserId;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class ListQueueRoutesHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<ListQueueRoutesQuery, IReadOnlyList<QueueRouteDto>?>
{
    public async Task<IReadOnlyList<QueueRouteDto>?> Handle(ListQueueRoutesQuery request, CancellationToken cancellationToken)
    {
        var exists = await db.OpsQueues.AsNoTracking().AnyAsync(
            q => q.Id == request.QueueId && q.BusinessId == business.BusinessId,
            cancellationToken);
        if (!exists)
        {
            return null;
        }

        return await GetQueueByIdHandler.LoadRoutes(db, request.QueueId, cancellationToken);
    }
}

public sealed class ReplaceQueueRoutesHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<ReplaceQueueRoutesCommand, IReadOnlyList<QueueRouteDto>?>
{
    public async Task<IReadOnlyList<QueueRouteDto>?> Handle(ReplaceQueueRoutesCommand command, CancellationToken cancellationToken)
    {
        var q = await db.OpsQueues.FirstOrDefaultAsync(
            x => x.Id == command.QueueId && x.BusinessId == business.BusinessId,
            cancellationToken);
        if (q is null)
        {
            return null;
        }

        if (q.RoutingLocked)
        {
            throw new InvalidOperationException("Routing is locked after the first File; create a new OpsQueue to change type→OpsAgent map.");
        }

        var routes = command.Request.Routes ?? [];
        var typeIds = routes.Select(r => r.DocumentTypeId).ToList();
        if (typeIds.Count != typeIds.Distinct().Count())
        {
            throw new InvalidOperationException("Duplicate DocumentTypeId in routes.");
        }

        foreach (var route in routes)
        {
            var typeOk = await db.CorDocumentTypes.AnyAsync(d => d.Id == route.DocumentTypeId && d.IsActive, cancellationToken);
            if (!typeOk)
            {
                throw new InvalidOperationException($"DocumentType {route.DocumentTypeId} not found.");
            }

            var agentOk = await db.OpsAgents.AnyAsync(
                a => a.Id == route.AgentId && a.BusinessId == business.BusinessId && a.IsActive,
                cancellationToken);
            if (!agentOk)
            {
                throw new InvalidOperationException($"OpsAgent {route.AgentId} not found in this Business.");
            }
        }

        var existing = await db.OpsQueueRoutes.Where(r => r.QueueId == q.Id).ToListAsync(cancellationToken);
        foreach (var row in existing)
        {
            row.IsDeleted = true;
            row.DeletedAt = DateTimeOffset.UtcNow;
            row.DeletedByUserId = business.UserId;
        }

        foreach (var route in routes)
        {
            db.OpsQueueRoutes.Add(new OpsQueueRoute
            {
                BusinessId = business.BusinessId,
                QueueId = q.Id,
                DocumentTypeId = route.DocumentTypeId,
                AgentId = route.AgentId,
                CreatedByUserId = business.UserId,
                UpdatedByUserId = business.UserId,
            });
        }

        q.UpdatedByUserId = business.UserId;
        await db.SaveChangesAsync(cancellationToken);
        return await GetQueueByIdHandler.LoadRoutes(db, q.Id, cancellationToken);
    }
}

public sealed class LockQueueRoutingHandler(DocumateDbContext db, IBusinessContext business, ICorEnumIdResolver enums)
    : IRequestHandler<LockQueueRoutingCommand, QueueDto?>
{
    public async Task<QueueDto?> Handle(LockQueueRoutingCommand command, CancellationToken cancellationToken)
    {
        var q = await db.OpsQueues.FirstOrDefaultAsync(
            x => x.Id == command.QueueId && x.BusinessId == business.BusinessId,
            cancellationToken);
        if (q is null)
        {
            return null;
        }

        if (!q.RoutingLocked)
        {
            q.RoutingLocked = true;
            q.RoutingLockedAt = DateTimeOffset.UtcNow;
            q.UpdatedByUserId = business.UserId;
            await db.SaveChangesAsync(cancellationToken);
        }

        return await QueueHelpers.ToDtoAsync(db, enums, q, cancellationToken);
    }
}

public sealed class UpdateQueueWebhookHandler(
    DocumateDbContext db,
    IBusinessContext business,
    ICorEnumIdResolver enums,
    Documate.Api.Infrastructure.Webhooks.IWebhookSecretProtector secrets)
    : IRequestHandler<UpdateQueueWebhookCommand, QueueDto?>
{
    public async Task<QueueDto?> Handle(UpdateQueueWebhookCommand command, CancellationToken cancellationToken)
    {
        var q = await db.OpsQueues.FirstOrDefaultAsync(
            x => x.Id == command.QueueId && x.BusinessId == business.BusinessId,
            cancellationToken);
        if (q is null)
        {
            return null;
        }

        var request = command.Request;
        q.WebhookEnabled = request.Enabled;
        q.WebhookUrl = string.IsNullOrWhiteSpace(request.Url) ? null : request.Url.Trim();
        if (!string.IsNullOrWhiteSpace(request.Secret))
        {
            q.WebhookSecretHash = QueueHelpers.HashSecret(request.Secret);
            q.WebhookSecretProtected = secrets.Protect(request.Secret);
        }

        q.UpdatedByUserId = business.UserId;
        await db.SaveChangesAsync(cancellationToken);
        return await QueueHelpers.ToDtoAsync(db, enums, q, cancellationToken);
    }
}

public sealed class MintQueueEmailHandler(
    DocumateDbContext db,
    IBusinessContext business,
    IOptions<EmailIntakeOptions> emailOptions)
    : IRequestHandler<MintQueueEmailCommand, QueueEmailAddressDto?>
{
    public async Task<QueueEmailAddressDto?> Handle(MintQueueEmailCommand command, CancellationToken cancellationToken)
    {
        var q = await db.OpsQueues.FirstOrDefaultAsync(
            x => x.Id == command.QueueId && x.BusinessId == business.BusinessId,
            cancellationToken);
        if (q is null)
        {
            return null;
        }

        var domain = emailOptions.Value.DefaultDomain;
        var local = $"q{Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant()}";
        q.EmailLocalPart = local;
        q.EmailDomain = domain;
        q.EmailAddressVersion += 1;
        q.UpdatedByUserId = business.UserId;
        await db.SaveChangesAsync(cancellationToken);

        return new QueueEmailAddressDto($"{local}@{domain}", local, domain, q.EmailAddressVersion);
    }
}

public sealed class UpdateQueueEmailSettingsHandler(DocumateDbContext db, IBusinessContext business, ICorEnumIdResolver enums)
    : IRequestHandler<UpdateQueueEmailSettingsCommand, QueueDto?>
{
    public async Task<QueueDto?> Handle(UpdateQueueEmailSettingsCommand command, CancellationToken cancellationToken)
    {
        var q = await db.OpsQueues.FirstOrDefaultAsync(
            x => x.Id == command.QueueId && x.BusinessId == business.BusinessId,
            cancellationToken);
        if (q is null)
        {
            return null;
        }

        var mode = await db.CorEnums.AsNoTracking()
            .Join(db.CorEnumTypes.AsNoTracking(), e => e.TypeId, t => t.Id, (e, t) => new { e, t })
            .FirstOrDefaultAsync(
                x => x.e.Id == command.Request.AllowlistModeEnumId && x.t.EnumTypeKey == "allowlist_mode",
                cancellationToken)
            ?? throw new InvalidOperationException("AllowlistModeEnumId must belong to allowlist_mode.");

        q.EmailIntakeEnabled = command.Request.EmailIntakeEnabled;
        q.AllowlistModeEnumId = mode.e.Id;
        q.UpdatedByUserId = business.UserId;
        await db.SaveChangesAsync(cancellationToken);
        return await QueueHelpers.ToDtoAsync(db, enums, q, cancellationToken);
    }
}

public sealed class ListAllowlistHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<ListAllowlistQuery, IReadOnlyList<AllowlistEntryDto>?>
{
    public async Task<IReadOnlyList<AllowlistEntryDto>?> Handle(ListAllowlistQuery request, CancellationToken cancellationToken)
    {
        var exists = await db.OpsQueues.AsNoTracking().AnyAsync(
            q => q.Id == request.QueueId && q.BusinessId == business.BusinessId,
            cancellationToken);
        if (!exists)
        {
            return null;
        }

        return await GetQueueByIdHandler.LoadAllowlist(db, request.QueueId, cancellationToken);
    }
}

public sealed class CreateAllowlistEntryHandler(DocumateDbContext db, IBusinessContext business, ICorEnumIdResolver enums)
    : IRequestHandler<CreateAllowlistEntryCommand, AllowlistEntryDto?>
{
    public async Task<AllowlistEntryDto?> Handle(CreateAllowlistEntryCommand command, CancellationToken cancellationToken)
    {
        var q = await db.OpsQueues.AsNoTracking().FirstOrDefaultAsync(
            x => x.Id == command.QueueId && x.BusinessId == business.BusinessId,
            cancellationToken);
        if (q is null)
        {
            return null;
        }

        var matchTypeKey = command.Request.MatchTypeKey.Trim().ToLowerInvariant();
        if (matchTypeKey is not ("email" or "domain"))
        {
            throw new InvalidOperationException("MatchTypeKey must be 'email' or 'domain'.");
        }

        var matchTypeId = enums.Require("allowlist_match_type", matchTypeKey);
        var value = command.Request.Value.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Value is required.");
        }

        var entry = new OpsQueueEmailAllowlistEntry
        {
            BusinessId = business.BusinessId,
            QueueId = q.Id,
            MatchTypeEnumId = matchTypeId,
            Value = value,
            CreatedByUserId = business.UserId,
            UpdatedByUserId = business.UserId,
        };
        db.OpsQueueEmailAllowlistEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        return new AllowlistEntryDto(entry.Id, entry.MatchTypeEnumId, matchTypeKey, entry.Value);
    }
}

public sealed class DeleteAllowlistEntryHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<DeleteAllowlistEntryCommand, bool>
{
    public async Task<bool> Handle(DeleteAllowlistEntryCommand command, CancellationToken cancellationToken)
    {
        var entry = await (
            from e in db.OpsQueueEmailAllowlistEntries
            join q in db.OpsQueues on e.QueueId equals q.Id
            where e.Id == command.EntryId && e.QueueId == command.QueueId && q.BusinessId == business.BusinessId
            select e
        ).FirstOrDefaultAsync(cancellationToken);

        if (entry is null)
        {
            return false;
        }

        entry.IsDeleted = true;
        entry.DeletedAt = DateTimeOffset.UtcNow;
        entry.DeletedByUserId = business.UserId;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
