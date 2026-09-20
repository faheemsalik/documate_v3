namespace Documate.Api.Modules.PlatformAdmin.Features.ActionBindings;

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.PublicEvents;
using Documate.Api.Infrastructure.Webhooks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Authorize(Policy = PlatformAdminAuth.PolicyName)]
[Route("api/admin/action-bindings")]
public sealed class AdminActionBindingsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public Task<PagedAdminActionBindingListDto> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? businessId = null,
        [FromQuery] string? actionTypeKey = null,
        [FromQuery] bool? enabled = null,
        [FromQuery] string? scope = null,
        CancellationToken cancellationToken = default) =>
        mediator.Send(
            new ListAdminActionBindingsQuery(page, pageSize, search, businessId, actionTypeKey, enabled, scope),
            cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminActionBindingDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetAdminActionBindingQuery(id), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<AdminActionBindingDetailDto>> Create(
        [FromBody] CreateAdminActionBindingRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.BusinessId) || string.IsNullOrWhiteSpace(body.ActionTypeKey))
        {
            return BadRequest(new { error = "BusinessId and ActionTypeKey are required." });
        }

        try
        {
            var dto = await mediator.Send(
                new CreateAdminActionBindingCommand(body, AdminUserId()),
                cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists", StringComparison.Ordinal))
        {
            return Conflict(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminActionBindingDetailDto>> Update(
        Guid id,
        [FromBody] UpdateAdminActionBindingRequest body,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await mediator.Send(
                new UpdateAdminActionBindingCommand(id, body, AdminUserId()),
                cancellationToken);
            return dto is null ? NotFound() : Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private string AdminUserId() =>
        User.FindFirstValue(AuthClaimTypes.UserId)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? "ops-admin";
}

public sealed record AdminPublicEventToggleDto(string EventKey, bool Enabled);

public sealed record AdminActionBindingListItemDto(
    Guid Id,
    string BusinessId,
    string BusinessName,
    Guid TenantId,
    string TenantName,
    Guid? QueueId,
    string? QueueName,
    string ActionTypeKey,
    bool Enabled,
    IReadOnlyList<string> EventKeys,
    string? WebhookUrl,
    bool HasSecret,
    string? EmailAudience,
    int RecipientCount,
    bool? QueuePublicActionsInherit,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PagedAdminActionBindingListDto(
    IReadOnlyList<AdminActionBindingListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AdminActionBindingDetailDto(
    Guid Id,
    string BusinessId,
    string BusinessName,
    Guid TenantId,
    string TenantName,
    Guid? QueueId,
    string? QueueName,
    string ActionTypeKey,
    bool Enabled,
    IReadOnlyList<AdminPublicEventToggleDto> Events,
    string? WebhookUrl,
    bool HasSecret,
    string? EmailAudience,
    IReadOnlyList<string> Recipients,
    bool? QueuePublicActionsInherit,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateAdminActionBindingRequest(
    string BusinessId,
    Guid? QueueId,
    string ActionTypeKey,
    bool Enabled,
    IReadOnlyList<string>? EventKeys,
    string? Url,
    string? Secret,
    string? Audience,
    IReadOnlyList<string>? Recipients);

public sealed record UpdateAdminActionBindingRequest(
    bool Enabled,
    IReadOnlyList<string> EventKeys,
    string? Url,
    string? Secret,
    string? Audience,
    IReadOnlyList<string>? Recipients);

public sealed record ListAdminActionBindingsQuery(
    int Page,
    int PageSize,
    string? Search,
    string? BusinessId,
    string? ActionTypeKey,
    bool? Enabled,
    string? Scope) : IRequest<PagedAdminActionBindingListDto>;

public sealed record GetAdminActionBindingQuery(Guid Id) : IRequest<AdminActionBindingDetailDto?>;

public sealed record CreateAdminActionBindingCommand(CreateAdminActionBindingRequest Request, string UserId)
    : IRequest<AdminActionBindingDetailDto>;

public sealed record UpdateAdminActionBindingCommand(
    Guid Id,
    UpdateAdminActionBindingRequest Request,
    string UserId) : IRequest<AdminActionBindingDetailDto?>;

file static class AdminActionBindingPaging
{
    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        var p = page < 1 ? 1 : page;
        var s = pageSize switch
        {
            < 1 => 50,
            > 200 => 200,
            _ => pageSize,
        };
        return (p, s);
    }
}

file static class AdminActionBindingMapping
{
    public static string CanonicalEventKeysJson(IEnumerable<string>? eventKeys)
    {
        var keys = (eventKeys ?? [])
            .Select(PublicEventCatalog.Canonicalize)
            .Where(k => PublicEventCatalog.AllEvents.Contains(k, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        return JsonSerializer.Serialize(keys);
    }

    public static IReadOnlyList<string> DefaultEventKeys(string actionType) =>
        actionType switch
        {
            PublicEventCatalog.ActionWebhook => PublicEventCatalog.DefaultBusinessWebhookEvents,
            PublicEventCatalog.ActionEmail => [PublicEventCatalog.DocumentFailed],
            PublicEventCatalog.ActionInApp => [PublicEventCatalog.DocumentFailed, PublicEventCatalog.FileReceived],
            _ => [],
        };

    public static void EnsureActionType(string actionType)
    {
        if (actionType is not (
            PublicEventCatalog.ActionWebhook
            or PublicEventCatalog.ActionEmail
            or PublicEventCatalog.ActionInApp))
        {
            throw new InvalidOperationException($"Unknown action type '{actionType}'.");
        }
    }

    public static IReadOnlyList<AdminPublicEventToggleDto> Toggles(IReadOnlyList<string> enabled)
    {
        var set = enabled.ToHashSet(StringComparer.Ordinal);
        return PublicEventCatalog.AllEvents
            .Select(e => new AdminPublicEventToggleDto(e, set.Contains(e)))
            .ToList();
    }

    public static (string? Url, bool HasSecret, string? Audience, IReadOnlyList<string> Recipients) ParseConfig(
        string actionType,
        string? configJson)
    {
        string? url = null;
        var hasSecret = false;
        string? audience = null;
        IReadOnlyList<string> recipients = [];
        if (string.IsNullOrWhiteSpace(configJson))
        {
            return (url, hasSecret, audience, recipients);
        }

        try
        {
            using var doc = JsonDocument.Parse(configJson);
            var root = doc.RootElement;
            if (actionType == PublicEventCatalog.ActionWebhook)
            {
                if (root.TryGetProperty("url", out var u))
                {
                    url = u.GetString();
                }

                hasSecret = root.TryGetProperty("secretProtected", out var s)
                            && !string.IsNullOrWhiteSpace(s.GetString());
            }
            else if (actionType == PublicEventCatalog.ActionEmail)
            {
                if (root.TryGetProperty("audience", out var a))
                {
                    audience = a.GetString();
                }

                if (root.TryGetProperty("recipients", out var r) && r.ValueKind == JsonValueKind.Array)
                {
                    recipients = r.EnumerateArray()
                        .Select(i => i.GetString())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Cast<string>()
                        .ToList();
                }
            }
        }
        catch (JsonException)
        {
            // ignore malformed config
        }

        return (url, hasSecret, audience, recipients);
    }

    public static string BuildConfigJson(
        string actionType,
        UpdateAdminActionBindingRequest request,
        string? existingConfigJson,
        IWebhookSecretProtector secrets)
    {
        if (actionType == PublicEventCatalog.ActionWebhook)
        {
            string? secretProtected = null;
            if (!string.IsNullOrWhiteSpace(request.Secret))
            {
                secretProtected = secrets.Protect(request.Secret);
            }
            else if (existingConfigJson is not null)
            {
                try
                {
                    using var doc = JsonDocument.Parse(existingConfigJson);
                    if (doc.RootElement.TryGetProperty("secretProtected", out var s))
                    {
                        secretProtected = s.GetString();
                    }
                }
                catch (JsonException)
                {
                    // ignore
                }
            }

            return JsonSerializer.Serialize(new
            {
                url = string.IsNullOrWhiteSpace(request.Url) ? null : request.Url.Trim(),
                secretProtected,
            });
        }

        if (actionType == PublicEventCatalog.ActionEmail)
        {
            var audience = string.IsNullOrWhiteSpace(request.Audience) ? "partner" : request.Audience.Trim();
            if (audience is not ("partner" or "platform"))
            {
                throw new InvalidOperationException("Audience must be 'partner' or 'platform'.");
            }

            return JsonSerializer.Serialize(new
            {
                audience,
                recipients = request.Recipients ?? [],
            });
        }

        return ActionBindingDefaults.EmptyConfigJson();
    }

    public static async Task SyncLegacyQueueWebhookAsync(
        DocumateDbContext db,
        string businessId,
        Guid queueId,
        bool enabled,
        string? url,
        string? secret,
        string? secretProtected,
        string? userId,
        CancellationToken cancellationToken)
    {
        var queue = await db.OpsQueues.FirstOrDefaultAsync(
            q => q.Id == queueId && q.BusinessId == businessId, cancellationToken);
        if (queue is null)
        {
            return;
        }

        queue.WebhookEnabled = enabled;
        queue.WebhookUrl = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        if (!string.IsNullOrWhiteSpace(secret) && secretProtected is not null)
        {
            queue.WebhookSecretProtected = secretProtected;
            queue.WebhookSecretHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)))
                .ToLowerInvariant();
        }

        queue.UpdatedByUserId = userId;
    }

    public static async Task<AdminActionBindingDetailDto?> LoadDetailAsync(
        DocumateDbContext db,
        Guid id,
        CancellationToken cancellationToken)
    {
        var row = await db.OpsActionBindings.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var biz = await (
            from b in db.CorTenantBusinesses.AsNoTracking()
            where !b.IsDeleted && b.IdenBusinessId == row.BusinessId
            join t in db.CorTenants.AsNoTracking() on b.TenantId equals t.Id
            where !t.IsDeleted
            select new { Business = b, Tenant = t }).FirstOrDefaultAsync(cancellationToken);
        if (biz is null)
        {
            return null;
        }

        string? queueName = null;
        bool? inherit = null;
        if (row.QueueId is Guid qid)
        {
            var queue = await db.OpsQueues.AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == qid, cancellationToken);
            queueName = queue?.Name;
            inherit = queue?.PublicActionsInherit;
        }

        var keys = ActionBindingResolver.ParseEventKeys(row.EventKeysJson);
        var (url, hasSecret, audience, recipients) = ParseConfig(row.ActionTypeKey, row.ConfigJson);
        return new AdminActionBindingDetailDto(
            row.Id,
            row.BusinessId,
            biz.Business.Name,
            biz.Tenant.Id,
            biz.Tenant.Name,
            row.QueueId,
            queueName,
            row.ActionTypeKey,
            row.Enabled,
            Toggles(keys),
            url,
            hasSecret,
            audience,
            recipients,
            inherit,
            row.CreatedAt,
            row.UpdatedAt);
    }
}

public sealed class ListAdminActionBindingsHandler(DocumateDbContext db)
    : IRequestHandler<ListAdminActionBindingsQuery, PagedAdminActionBindingListDto>
{
    public async Task<PagedAdminActionBindingListDto> Handle(
        ListAdminActionBindingsQuery request,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = AdminActionBindingPaging.Normalize(request.Page, request.PageSize);

        var q =
            from b in db.OpsActionBindings.AsNoTracking()
            where !b.IsDeleted
            join biz in db.CorTenantBusinesses.AsNoTracking() on b.BusinessId equals biz.IdenBusinessId
            where !biz.IsDeleted
            join t in db.CorTenants.AsNoTracking() on biz.TenantId equals t.Id
            where !t.IsDeleted
            join queue in db.OpsQueues.AsNoTracking() on b.QueueId equals queue.Id into qj
            from queue in qj.DefaultIfEmpty()
            select new { Binding = b, Business = biz, Tenant = t, Queue = queue };

        if (!string.IsNullOrWhiteSpace(request.BusinessId))
        {
            var bid = request.BusinessId.Trim();
            q = q.Where(x => x.Binding.BusinessId == bid);
        }

        if (!string.IsNullOrWhiteSpace(request.ActionTypeKey))
        {
            var type = request.ActionTypeKey.Trim();
            q = q.Where(x => x.Binding.ActionTypeKey == type);
        }

        if (request.Enabled is bool enabled)
        {
            q = q.Where(x => x.Binding.Enabled == enabled);
        }

        if (string.Equals(request.Scope, "business", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(x => x.Binding.QueueId == null);
        }
        else if (string.Equals(request.Scope, "queue", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(x => x.Binding.QueueId != null);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            q = q.Where(x =>
                x.Business.Name.Contains(term)
                || x.Business.IdenBusinessId.Contains(term)
                || x.Tenant.Name.Contains(term)
                || x.Tenant.IdenTenantId.Contains(term)
                || x.Binding.ActionTypeKey.Contains(term)
                || (x.Queue != null && x.Queue.Name.Contains(term)));
        }

        var total = await q.CountAsync(cancellationToken);
        var rows = await q
            .OrderBy(x => x.Business.Name)
            .ThenBy(x => x.Binding.ActionTypeKey)
            .ThenBy(x => x.Binding.QueueId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(x =>
        {
            var keys = ActionBindingResolver.ParseEventKeys(x.Binding.EventKeysJson);
            var (url, hasSecret, audience, recipients) =
                AdminActionBindingMapping.ParseConfig(x.Binding.ActionTypeKey, x.Binding.ConfigJson);
            return new AdminActionBindingListItemDto(
                x.Binding.Id,
                x.Binding.BusinessId,
                x.Business.Name,
                x.Tenant.Id,
                x.Tenant.Name,
                x.Binding.QueueId,
                x.Queue?.Name,
                x.Binding.ActionTypeKey,
                x.Binding.Enabled,
                keys,
                url,
                hasSecret,
                audience,
                recipients.Count,
                x.Queue?.PublicActionsInherit,
                x.Binding.CreatedAt,
                x.Binding.UpdatedAt);
        }).ToList();

        return new PagedAdminActionBindingListDto(items, total, page, pageSize);
    }
}

public sealed class GetAdminActionBindingHandler(DocumateDbContext db)
    : IRequestHandler<GetAdminActionBindingQuery, AdminActionBindingDetailDto?>
{
    public Task<AdminActionBindingDetailDto?> Handle(
        GetAdminActionBindingQuery request,
        CancellationToken cancellationToken) =>
        AdminActionBindingMapping.LoadDetailAsync(db, request.Id, cancellationToken);
}

public sealed class CreateAdminActionBindingHandler(DocumateDbContext db, IWebhookSecretProtector secrets)
    : IRequestHandler<CreateAdminActionBindingCommand, AdminActionBindingDetailDto>
{
    public async Task<AdminActionBindingDetailDto> Handle(
        CreateAdminActionBindingCommand command,
        CancellationToken cancellationToken)
    {
        var req = command.Request;
        var actionType = req.ActionTypeKey.Trim();
        AdminActionBindingMapping.EnsureActionType(actionType);

        var businessId = req.BusinessId.Trim();
        var businessExists = await db.CorTenantBusinesses.AsNoTracking()
            .AnyAsync(b => b.IdenBusinessId == businessId && !b.IsDeleted, cancellationToken);
        if (!businessExists)
        {
            throw new InvalidOperationException($"Business '{businessId}' was not found.");
        }

        if (req.QueueId is Guid qid)
        {
            var queueOk = await db.OpsQueues.AsNoTracking()
                .AnyAsync(q => q.Id == qid && q.BusinessId == businessId && !q.IsDeleted, cancellationToken);
            if (!queueOk)
            {
                throw new InvalidOperationException("Queue was not found for this business.");
            }
        }

        var exists = await db.OpsActionBindings.IgnoreQueryFilters()
            .AnyAsync(
                b => b.BusinessId == businessId
                     && b.QueueId == req.QueueId
                     && b.ActionTypeKey == actionType
                     && !b.IsDeleted,
                cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("An action binding already exists for this business, queue, and type.");
        }

        var eventKeys = req.EventKeys is { Count: > 0 }
            ? req.EventKeys
            : AdminActionBindingMapping.DefaultEventKeys(actionType);
        var updateShape = new UpdateAdminActionBindingRequest(
            req.Enabled,
            eventKeys,
            req.Url,
            req.Secret,
            req.Audience,
            req.Recipients);
        var config = AdminActionBindingMapping.BuildConfigJson(actionType, updateShape, null, secrets);

        var row = new OpsActionBinding
        {
            BusinessId = businessId,
            QueueId = req.QueueId,
            ActionTypeKey = actionType,
            Enabled = req.Enabled,
            ConfigJson = config,
            EventKeysJson = AdminActionBindingMapping.CanonicalEventKeysJson(eventKeys),
            CreatedByUserId = command.UserId,
            UpdatedByUserId = command.UserId,
        };
        db.OpsActionBindings.Add(row);

        if (actionType == PublicEventCatalog.ActionWebhook && req.QueueId is Guid queueId)
        {
            string? secretProtected = null;
            try
            {
                using var doc = JsonDocument.Parse(config);
                if (doc.RootElement.TryGetProperty("secretProtected", out var s))
                {
                    secretProtected = s.GetString();
                }
            }
            catch (JsonException)
            {
                // ignore
            }

            await AdminActionBindingMapping.SyncLegacyQueueWebhookAsync(
                db, businessId, queueId, req.Enabled, req.Url, req.Secret, secretProtected, command.UserId, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return (await AdminActionBindingMapping.LoadDetailAsync(db, row.Id, cancellationToken))!;
    }
}

public sealed class UpdateAdminActionBindingHandler(DocumateDbContext db, IWebhookSecretProtector secrets)
    : IRequestHandler<UpdateAdminActionBindingCommand, AdminActionBindingDetailDto?>
{
    public async Task<AdminActionBindingDetailDto?> Handle(
        UpdateAdminActionBindingCommand command,
        CancellationToken cancellationToken)
    {
        var row = await db.OpsActionBindings.FirstOrDefaultAsync(
            b => b.Id == command.Id && !b.IsDeleted, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var req = command.Request;
        row.Enabled = req.Enabled;
        row.EventKeysJson = AdminActionBindingMapping.CanonicalEventKeysJson(req.EventKeys);
        row.ConfigJson = AdminActionBindingMapping.BuildConfigJson(row.ActionTypeKey, req, row.ConfigJson, secrets);
        row.UpdatedByUserId = command.UserId;

        if (row.ActionTypeKey == PublicEventCatalog.ActionWebhook && row.QueueId is Guid queueId)
        {
            string? secretProtected = null;
            try
            {
                using var doc = JsonDocument.Parse(row.ConfigJson);
                if (doc.RootElement.TryGetProperty("secretProtected", out var s))
                {
                    secretProtected = s.GetString();
                }
            }
            catch (JsonException)
            {
                // ignore
            }

            await AdminActionBindingMapping.SyncLegacyQueueWebhookAsync(
                db,
                row.BusinessId,
                queueId,
                req.Enabled,
                req.Url,
                req.Secret,
                secretProtected,
                command.UserId,
                cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return await AdminActionBindingMapping.LoadDetailAsync(db, row.Id, cancellationToken);
    }
}
