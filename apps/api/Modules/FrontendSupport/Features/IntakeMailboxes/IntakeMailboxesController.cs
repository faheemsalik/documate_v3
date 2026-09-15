namespace Documate.Api.Modules.FrontendSupport.Features.IntakeMailboxes;

using Documate.Api.Domain;
using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.EmailIntake;
using Documate.Api.Infrastructure.Options;
using Documate.Api.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

[ApiController]
[Route("api/app/intake-mailboxes")]
[Authorize]
public sealed class IntakeMailboxesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<IntakeMailboxDto>> List(CancellationToken cancellationToken) =>
        mediator.Send(new ListIntakeMailboxesQuery(), cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IntakeMailboxDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetIntakeMailboxQuery(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("typed")]
    public async Task<ActionResult<IntakeMailboxDto>> CreateTyped(
        [FromBody] CreateTypedMailboxRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await mediator.Send(new CreateTypedMailboxCommand(request), cancellationToken);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("multi")]
    public async Task<ActionResult<IntakeMailboxDto>> CreateMulti(CancellationToken cancellationToken)
    {
        try
        {
            var dto = await mediator.Send(new CreateMultiMailboxCommand(), cancellationToken);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/rotate")]
    public async Task<ActionResult<IntakeMailboxDto>> Rotate(Guid id, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new RotateIntakeMailboxCommand(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<IntakeMailboxDto>> Update(
        Guid id,
        [FromBody] UpdateIntakeMailboxRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await mediator.Send(new UpdateIntakeMailboxCommand(id, request), cancellationToken);
            return dto is null ? NotFound() : Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}/allowlist")]
    public async Task<ActionResult<IReadOnlyList<MailboxAllowlistEntryDto>>> ListAllowlist(
        Guid id,
        CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new ListMailboxAllowlistQuery(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("{id:guid}/allowlist")]
    public async Task<ActionResult<MailboxAllowlistEntryDto>> AddAllowlist(
        Guid id,
        [FromBody] CreateMailboxAllowlistRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await mediator.Send(new CreateMailboxAllowlistCommand(id, request), cancellationToken);
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
        var ok = await mediator.Send(new DeleteMailboxAllowlistCommand(id, entryId), cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/simulate")]
    [RequestSizeLimit(30_000_000)]
    public async Task<ActionResult<SimulateEmailIntakeResultDto>> Simulate(
        Guid id,
        [FromBody] SimulateEmailIntakeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await mediator.Send(new SimulateEmailIntakeCommand(id, request), cancellationToken);
            return dto is null ? NotFound() : Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public sealed record IntakeMailboxDto(
    Guid Id,
    Guid QueueId,
    string KindKey,
    Guid? AgentId,
    string? AgentName,
    bool Enabled,
    string EmailAddress,
    string EmailLocalPart,
    string EmailDomain,
    int EmailAddressVersion,
    long AllowlistModeEnumId,
    string? AllowlistModeKey);

public sealed record IntakeMailboxDetailDto(
    IntakeMailboxDto Mailbox,
    IReadOnlyList<MailboxAllowlistEntryDto> Allowlist);

public sealed record MailboxAllowlistEntryDto(long Id, long MatchTypeEnumId, string? MatchTypeKey, string Value);

public sealed record CreateTypedMailboxRequest(Guid AgentId);
public sealed record UpdateIntakeMailboxRequest(bool Enabled, long AllowlistModeEnumId);
public sealed record CreateMailboxAllowlistRequest(string MatchTypeKey, string Value);

public sealed record SimulateEmailIntakeRequest(
    string? From,
    string? Subject,
    string? MessageId,
    string? TextBody,
    IReadOnlyList<SimulateEmailAttachmentDto>? Attachments);

public sealed record SimulateEmailAttachmentDto(
    string FileName,
    string? ContentType,
    string ContentBase64);

public sealed record SimulateEmailIntakeResultDto(
    bool Accepted,
    string? RejectCode,
    string? RejectMessage,
    Guid? IntakeRejectionId,
    Guid? BatchId,
    IReadOnlyList<Guid> FileIds);

public sealed record ListIntakeMailboxesQuery : IRequest<IReadOnlyList<IntakeMailboxDto>>;
public sealed record GetIntakeMailboxQuery(Guid Id) : IRequest<IntakeMailboxDetailDto?>;
public sealed record CreateTypedMailboxCommand(CreateTypedMailboxRequest Request) : IRequest<IntakeMailboxDto>;
public sealed record CreateMultiMailboxCommand : IRequest<IntakeMailboxDto>;
public sealed record RotateIntakeMailboxCommand(Guid Id) : IRequest<IntakeMailboxDto?>;
public sealed record UpdateIntakeMailboxCommand(Guid Id, UpdateIntakeMailboxRequest Request) : IRequest<IntakeMailboxDto?>;
public sealed record ListMailboxAllowlistQuery(Guid MailboxId) : IRequest<IReadOnlyList<MailboxAllowlistEntryDto>?>;
public sealed record CreateMailboxAllowlistCommand(Guid MailboxId, CreateMailboxAllowlistRequest Request)
    : IRequest<MailboxAllowlistEntryDto?>;
public sealed record DeleteMailboxAllowlistCommand(Guid MailboxId, long EntryId) : IRequest<bool>;
public sealed record SimulateEmailIntakeCommand(Guid MailboxId, SimulateEmailIntakeRequest Request)
    : IRequest<SimulateEmailIntakeResultDto?>;

internal static class IntakeMailboxMapping
{
    public static async Task<IntakeMailboxDto> ToDtoAsync(
        OpsIntakeMailbox m,
        DocumateDbContext db,
        CancellationToken ct)
    {
        var kind = await db.CorEnums.AsNoTracking().FirstOrDefaultAsync(e => e.Id == m.KindEnumId, ct);
        var mode = await db.CorEnums.AsNoTracking().FirstOrDefaultAsync(e => e.Id == m.AllowlistModeEnumId, ct);
        string? agentName = null;
        if (m.AgentId is Guid aid)
        {
            agentName = await db.OpsAgents.AsNoTracking()
                .Where(a => a.Id == aid)
                .Select(a => a.Name)
                .FirstOrDefaultAsync(ct);
        }

        return new IntakeMailboxDto(
            m.Id,
            m.QueueId,
            kind?.EnumKey ?? "",
            m.AgentId,
            agentName,
            m.Enabled,
            IntakeAddressFormatter.FormatAddress(m.EmailLocalPart, m.EmailDomain),
            m.EmailLocalPart,
            m.EmailDomain,
            m.EmailAddressVersion,
            m.AllowlistModeEnumId,
            mode?.EnumKey);
    }
}

public sealed class ListIntakeMailboxesHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<ListIntakeMailboxesQuery, IReadOnlyList<IntakeMailboxDto>>
{
    public async Task<IReadOnlyList<IntakeMailboxDto>> Handle(
        ListIntakeMailboxesQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await db.OpsIntakeMailboxes.AsNoTracking()
            .Where(m => m.BusinessId == business.BusinessId && !m.IsDeleted)
            .OrderBy(m => m.SequenceId)
            .ToListAsync(cancellationToken);
        var list = new List<IntakeMailboxDto>();
        foreach (var m in rows)
        {
            list.Add(await IntakeMailboxMapping.ToDtoAsync(m, db, cancellationToken));
        }

        return list;
    }
}

public sealed class GetIntakeMailboxHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<GetIntakeMailboxQuery, IntakeMailboxDetailDto?>
{
    public async Task<IntakeMailboxDetailDto?> Handle(GetIntakeMailboxQuery request, CancellationToken cancellationToken)
    {
        var m = await db.OpsIntakeMailboxes.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == request.Id && x.BusinessId == business.BusinessId && !x.IsDeleted,
                cancellationToken);
        if (m is null)
        {
            return null;
        }

        var allow = await LoadAllowlist(db, m.Id, cancellationToken);
        return new IntakeMailboxDetailDto(await IntakeMailboxMapping.ToDtoAsync(m, db, cancellationToken), allow);
    }

    internal static async Task<IReadOnlyList<MailboxAllowlistEntryDto>> LoadAllowlist(
        DocumateDbContext db,
        Guid mailboxId,
        CancellationToken ct) =>
        await (
            from e in db.OpsIntakeMailboxAllowlistEntries.AsNoTracking()
            join mt in db.CorEnums.AsNoTracking() on e.MatchTypeEnumId equals mt.Id into mtj
            from m in mtj.DefaultIfEmpty()
            where e.MailboxId == mailboxId && !e.IsDeleted
            select new MailboxAllowlistEntryDto(e.Id, e.MatchTypeEnumId, m != null ? m.EnumKey : null, e.Value)
        ).ToListAsync(ct);
}

public sealed class CreateTypedMailboxHandler(
    DocumateDbContext db,
    IBusinessContext business,
    ICorEnumIdResolver enums,
    IOptions<EmailIntakeOptions> emailOptions)
    : IRequestHandler<CreateTypedMailboxCommand, IntakeMailboxDto>
{
    public async Task<IntakeMailboxDto> Handle(CreateTypedMailboxCommand command, CancellationToken cancellationToken)
    {
        var agent = await db.OpsAgents
            .FirstOrDefaultAsync(
                a => a.Id == command.Request.AgentId
                     && a.BusinessId == business.BusinessId
                     && !a.IsDeleted,
                cancellationToken)
            ?? throw new InvalidOperationException("Agent not found.");

        var exists = await db.OpsIntakeMailboxes.AnyAsync(
            m => m.AgentId == agent.Id && !m.IsDeleted,
            cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("This Agent already has a typed intake mailbox; rotate instead.");
        }

        var queue = await RequireDefaultQueueAsync(db, business.BusinessId, cancellationToken);
        await EnsureQueueRouteAsync(db, business, queue, agent, cancellationToken);

        var slug = await EnsureBusinessSlugAsync(db, business, cancellationToken);
        var purpose = IntakeAddressFormatter.Slugify(agent.Name, "agent");
        var local = await UniqueLocalPartAsync(db, emailOptions.Value.DefaultDomain, slug, purpose, cancellationToken);

        var mailbox = new OpsIntakeMailbox
        {
            BusinessId = business.BusinessId,
            QueueId = queue.Id,
            KindEnumId = enums.Require("intake_mailbox_kind", "typed_agent"),
            AgentId = agent.Id,
            Enabled = true,
            EmailLocalPart = local,
            EmailDomain = emailOptions.Value.DefaultDomain,
            EmailAddressVersion = 1,
            AllowlistModeEnumId = enums.Require("allowlist_mode", "open"),
            CreatedByUserId = business.UserId,
            UpdatedByUserId = business.UserId,
        };
        db.OpsIntakeMailboxes.Add(mailbox);
        await db.SaveChangesAsync(cancellationToken);
        return await IntakeMailboxMapping.ToDtoAsync(mailbox, db, cancellationToken);
    }

    internal static async Task<OpsQueue> RequireDefaultQueueAsync(
        DocumateDbContext db,
        string businessId,
        CancellationToken ct) =>
        await db.OpsQueues.FirstOrDefaultAsync(
            q => q.BusinessId == businessId && q.IsDefault && !q.IsDeleted,
            ct)
        ?? await db.OpsQueues.OrderBy(q => q.SequenceId)
            .FirstOrDefaultAsync(q => q.BusinessId == businessId && !q.IsDeleted, ct)
        ?? throw new InvalidOperationException("Default Queue missing for Business.");

    internal static async Task EnsureQueueRouteAsync(
        DocumateDbContext db,
        IBusinessContext business,
        OpsQueue queue,
        OpsAgent agent,
        CancellationToken ct)
    {
        var existing = await db.OpsQueueRoutes
            .FirstOrDefaultAsync(
                r => r.QueueId == queue.Id && r.DocumentTypeId == agent.DocumentTypeId && !r.IsDeleted,
                ct);
        if (existing is not null)
        {
            if (existing.AgentId != agent.Id)
            {
                throw new InvalidOperationException(
                    "DocumentType is already routed to a different Agent on the default Queue.");
            }

            return;
        }

        if (queue.RoutingLocked)
        {
            throw new InvalidOperationException(
                "Queue routing is locked and no route exists for this Agent's DocumentType.");
        }

        db.OpsQueueRoutes.Add(new OpsQueueRoute
        {
            BusinessId = business.BusinessId,
            QueueId = queue.Id,
            DocumentTypeId = agent.DocumentTypeId,
            AgentId = agent.Id,
            CreatedByUserId = business.UserId,
            UpdatedByUserId = business.UserId,
        });
        await db.SaveChangesAsync(ct);
    }

    internal static async Task<string> EnsureBusinessSlugAsync(
        DocumateDbContext db,
        IBusinessContext business,
        CancellationToken ct)
    {
        var row = await db.CorTenantBusinesses
            .FirstOrDefaultAsync(b => b.IdenBusinessId == business.BusinessId && !b.IsDeleted, ct)
            ?? throw new InvalidOperationException("Business not found.");

        if (!string.IsNullOrWhiteSpace(row.IntakeEmailSlug))
        {
            return row.IntakeEmailSlug;
        }

        var baseSlug = IntakeAddressFormatter.Slugify(
            !string.IsNullOrWhiteSpace(business.BusinessName) ? business.BusinessName : row.Name,
            "biz",
            20);
        var slug = baseSlug;
        var n = 2;
        while (await db.CorTenantBusinesses.AnyAsync(
                   b => b.IntakeEmailSlug == slug && b.Id != row.Id && !b.IsDeleted,
                   ct))
        {
            slug = IntakeAddressFormatter.Slugify($"{baseSlug}{n}", "biz", 20);
            n++;
        }

        row.IntakeEmailSlug = slug;
        row.UpdatedByUserId = business.UserId;
        await db.SaveChangesAsync(ct);
        return slug;
    }

    internal static async Task<string> UniqueLocalPartAsync(
        DocumateDbContext db,
        string domain,
        string bizSlug,
        string purpose,
        CancellationToken ct)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var local = IntakeAddressFormatter.BuildLocalPart(bizSlug, purpose);
            var taken = await db.OpsIntakeMailboxes.AnyAsync(
                m => m.EmailDomain == domain && m.EmailLocalPart == local && !m.IsDeleted,
                ct);
            if (!taken)
            {
                return local;
            }
        }

        throw new InvalidOperationException("Could not allocate a unique intake address.");
    }
}

public sealed class CreateMultiMailboxHandler(
    DocumateDbContext db,
    IBusinessContext business,
    ICorEnumIdResolver enums,
    IOptions<EmailIntakeOptions> emailOptions)
    : IRequestHandler<CreateMultiMailboxCommand, IntakeMailboxDto>
{
    public async Task<IntakeMailboxDto> Handle(CreateMultiMailboxCommand command, CancellationToken cancellationToken)
    {
        var queue = await CreateTypedMailboxHandler.RequireDefaultQueueAsync(db, business.BusinessId, cancellationToken);
        var slug = await CreateTypedMailboxHandler.EnsureBusinessSlugAsync(db, business, cancellationToken);
        var local = await CreateTypedMailboxHandler.UniqueLocalPartAsync(
            db,
            emailOptions.Value.DefaultDomain,
            slug,
            "multi",
            cancellationToken);

        var mailbox = new OpsIntakeMailbox
        {
            BusinessId = business.BusinessId,
            QueueId = queue.Id,
            KindEnumId = enums.Require("intake_mailbox_kind", "multi_type"),
            AgentId = null,
            Enabled = true,
            EmailLocalPart = local,
            EmailDomain = emailOptions.Value.DefaultDomain,
            EmailAddressVersion = 1,
            AllowlistModeEnumId = enums.Require("allowlist_mode", "open"),
            CreatedByUserId = business.UserId,
            UpdatedByUserId = business.UserId,
        };
        db.OpsIntakeMailboxes.Add(mailbox);
        await db.SaveChangesAsync(cancellationToken);
        return await IntakeMailboxMapping.ToDtoAsync(mailbox, db, cancellationToken);
    }
}

public sealed class RotateIntakeMailboxHandler(
    DocumateDbContext db,
    IBusinessContext business,
    IOptions<EmailIntakeOptions> emailOptions)
    : IRequestHandler<RotateIntakeMailboxCommand, IntakeMailboxDto?>
{
    public async Task<IntakeMailboxDto?> Handle(RotateIntakeMailboxCommand command, CancellationToken cancellationToken)
    {
        var m = await db.OpsIntakeMailboxes
            .FirstOrDefaultAsync(
                x => x.Id == command.Id && x.BusinessId == business.BusinessId && !x.IsDeleted,
                cancellationToken);
        if (m is null)
        {
            return null;
        }

        var parts = m.EmailLocalPart.Split('-', 3);
        var purpose = parts.Length >= 2 ? parts[1] : "mail";
        var slug = await CreateTypedMailboxHandler.EnsureBusinessSlugAsync(db, business, cancellationToken);
        m.EmailLocalPart = await CreateTypedMailboxHandler.UniqueLocalPartAsync(
            db,
            emailOptions.Value.DefaultDomain,
            slug,
            purpose,
            cancellationToken);
        m.EmailDomain = emailOptions.Value.DefaultDomain;
        m.EmailAddressVersion++;
        m.UpdatedByUserId = business.UserId;
        await db.SaveChangesAsync(cancellationToken);
        return await IntakeMailboxMapping.ToDtoAsync(m, db, cancellationToken);
    }
}

public sealed class UpdateIntakeMailboxHandler(DocumateDbContext db, IBusinessContext business, ICorEnumIdResolver enums)
    : IRequestHandler<UpdateIntakeMailboxCommand, IntakeMailboxDto?>
{
    public async Task<IntakeMailboxDto?> Handle(UpdateIntakeMailboxCommand command, CancellationToken cancellationToken)
    {
        var m = await db.OpsIntakeMailboxes
            .FirstOrDefaultAsync(
                x => x.Id == command.Id && x.BusinessId == business.BusinessId && !x.IsDeleted,
                cancellationToken);
        if (m is null)
        {
            return null;
        }

        _ = await db.CorEnums.AsNoTracking()
                .Join(db.CorEnumTypes.AsNoTracking(), e => e.TypeId, t => t.Id, (e, t) => new { e, t })
                .FirstOrDefaultAsync(
                    x => x.e.Id == command.Request.AllowlistModeEnumId && x.t.EnumTypeKey == "allowlist_mode",
                    cancellationToken)
            ?? throw new InvalidOperationException("AllowlistModeEnumId must belong to allowlist_mode.");

        m.Enabled = command.Request.Enabled;
        m.AllowlistModeEnumId = command.Request.AllowlistModeEnumId;
        m.UpdatedByUserId = business.UserId;
        await db.SaveChangesAsync(cancellationToken);
        _ = enums;
        return await IntakeMailboxMapping.ToDtoAsync(m, db, cancellationToken);
    }
}

public sealed class ListMailboxAllowlistHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<ListMailboxAllowlistQuery, IReadOnlyList<MailboxAllowlistEntryDto>?>
{
    public async Task<IReadOnlyList<MailboxAllowlistEntryDto>?> Handle(
        ListMailboxAllowlistQuery request,
        CancellationToken cancellationToken)
    {
        var ok = await db.OpsIntakeMailboxes.AnyAsync(
            m => m.Id == request.MailboxId && m.BusinessId == business.BusinessId && !m.IsDeleted,
            cancellationToken);
        if (!ok)
        {
            return null;
        }

        return await GetIntakeMailboxHandler.LoadAllowlist(db, request.MailboxId, cancellationToken);
    }
}

public sealed class CreateMailboxAllowlistHandler(
    DocumateDbContext db,
    IBusinessContext business,
    ICorEnumIdResolver enums)
    : IRequestHandler<CreateMailboxAllowlistCommand, MailboxAllowlistEntryDto?>
{
    public async Task<MailboxAllowlistEntryDto?> Handle(
        CreateMailboxAllowlistCommand command,
        CancellationToken cancellationToken)
    {
        var mailbox = await db.OpsIntakeMailboxes
            .FirstOrDefaultAsync(
                m => m.Id == command.MailboxId && m.BusinessId == business.BusinessId && !m.IsDeleted,
                cancellationToken);
        if (mailbox is null)
        {
            return null;
        }

        var matchKey = command.Request.MatchTypeKey.Trim().ToLowerInvariant();
        if (matchKey is not ("email" or "domain"))
        {
            throw new InvalidOperationException("MatchTypeKey must be email or domain.");
        }

        var entry = new OpsIntakeMailboxAllowlistEntry
        {
            BusinessId = business.BusinessId,
            MailboxId = mailbox.Id,
            MatchTypeEnumId = enums.Require("allowlist_match_type", matchKey),
            Value = command.Request.Value.Trim(),
            CreatedByUserId = business.UserId,
            UpdatedByUserId = business.UserId,
        };
        db.OpsIntakeMailboxAllowlistEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        return new MailboxAllowlistEntryDto(entry.Id, entry.MatchTypeEnumId, matchKey, entry.Value);
    }
}

public sealed class DeleteMailboxAllowlistHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<DeleteMailboxAllowlistCommand, bool>
{
    public async Task<bool> Handle(DeleteMailboxAllowlistCommand command, CancellationToken cancellationToken)
    {
        var entry = await (
            from e in db.OpsIntakeMailboxAllowlistEntries
            join m in db.OpsIntakeMailboxes on e.MailboxId equals m.Id
            where e.Id == command.EntryId
                  && e.MailboxId == command.MailboxId
                  && m.BusinessId == business.BusinessId
                  && !e.IsDeleted
            select e).FirstOrDefaultAsync(cancellationToken);
        if (entry is null)
        {
            return false;
        }

        entry.IsDeleted = true;
        entry.DeletedAt = DateTimeOffset.UtcNow;
        entry.DeletedByUserId = business.UserId;
        entry.UpdatedByUserId = business.UserId;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class SimulateEmailIntakeHandler(
    DocumateDbContext db,
    IBusinessContext business,
    IEmailIntakeProcessor processor)
    : IRequestHandler<SimulateEmailIntakeCommand, SimulateEmailIntakeResultDto?>
{
    public async Task<SimulateEmailIntakeResultDto?> Handle(
        SimulateEmailIntakeCommand command,
        CancellationToken cancellationToken)
    {
        var exists = await db.OpsIntakeMailboxes.AnyAsync(
            m => m.Id == command.MailboxId && m.BusinessId == business.BusinessId && !m.IsDeleted,
            cancellationToken);
        if (!exists)
        {
            return null;
        }

        var attachments = (command.Request.Attachments ?? [])
            .Select(a => new EmailIntakeAttachmentInput(
                a.FileName,
                a.ContentType,
                Convert.FromBase64String(a.ContentBase64)))
            .ToList();

        var result = await processor.ProcessAsync(
            new EmailIntakeProcessRequest(
                command.MailboxId,
                command.Request.From,
                command.Request.Subject,
                command.Request.MessageId ?? $"simulate-{Guid.NewGuid():N}",
                command.Request.TextBody,
                attachments),
            cancellationToken);

        return new SimulateEmailIntakeResultDto(
            result.Accepted,
            result.RejectCode,
            result.RejectMessage,
            result.IntakeRejectionId,
            result.BatchId,
            result.FileIds);
    }
}
