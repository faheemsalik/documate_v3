namespace Documate.Api.Modules.FrontendSupport.Features.PublicActions;

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
[Authorize]
[Route("api/app")]
public sealed class PublicActionsController(IMediator mediator) : ControllerBase
{
    [HttpGet("business/public-actions")]
    public Task<PublicActionsSettingsDto> GetBusiness(CancellationToken cancellationToken) =>
        mediator.Send(new GetBusinessPublicActionsQuery(), cancellationToken);

    [HttpPut("business/public-actions")]
    public Task<PublicActionsSettingsDto> PutBusiness(
        [FromBody] UpsertPublicActionsRequest request,
        CancellationToken cancellationToken) =>
        mediator.Send(new UpsertBusinessPublicActionsCommand(request), cancellationToken);

    [HttpGet("queues/{queueId:guid}/public-actions")]
    public async Task<ActionResult<QueuePublicActionsSettingsDto>> GetQueue(
        Guid queueId,
        CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetQueuePublicActionsQuery(queueId), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("queues/{queueId:guid}/public-actions")]
    public async Task<ActionResult<QueuePublicActionsSettingsDto>> PutQueue(
        Guid queueId,
        [FromBody] UpsertQueuePublicActionsRequest request,
        CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new UpsertQueuePublicActionsCommand(queueId, request), cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("notifications/in-app")]
    public Task<IReadOnlyList<InAppNotificationDto>> ListInApp(
        [FromQuery] bool unreadOnly = false,
        CancellationToken cancellationToken = default) =>
        mediator.Send(new ListInAppNotificationsQuery(unreadOnly), cancellationToken);

    [HttpPost("notifications/in-app/{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var ok = await mediator.Send(new MarkInAppNotificationReadCommand(id), cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}

public sealed record PublicEventToggleDto(string EventKey, bool Enabled);

public sealed record WebhookActionDto(
    bool Enabled,
    string? Url,
    bool HasSecret,
    IReadOnlyList<PublicEventToggleDto> Events);

public sealed record EmailActionDto(
    bool Enabled,
    string Audience,
    IReadOnlyList<string> Recipients,
    IReadOnlyList<PublicEventToggleDto> Events);

public sealed record InAppActionDto(
    bool Enabled,
    IReadOnlyList<PublicEventToggleDto> Events);

public sealed record PublicActionsSettingsDto(
    WebhookActionDto Webhook,
    EmailActionDto Email,
    InAppActionDto InApp);

public sealed record QueuePublicActionsSettingsDto(
    bool Inherit,
    PublicActionsSettingsDto Effective,
    PublicActionsSettingsDto? Override);

public sealed record UpsertPublicActionsRequest(
    WebhookActionUpsert? Webhook,
    EmailActionUpsert? Email,
    InAppActionUpsert? InApp);

public sealed record UpsertQueuePublicActionsRequest(
    bool Inherit,
    UpsertPublicActionsRequest? Override);

public sealed record WebhookActionUpsert(
    bool Enabled,
    string? Url,
    string? Secret,
    IReadOnlyList<string> EventKeys);

public sealed record EmailActionUpsert(
    bool Enabled,
    string Audience,
    IReadOnlyList<string> Recipients,
    IReadOnlyList<string> EventKeys);

public sealed record InAppActionUpsert(bool Enabled, IReadOnlyList<string> EventKeys);

public sealed record InAppNotificationDto(
    Guid Id,
    string EventName,
    string EventId,
    string Title,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

public sealed record GetBusinessPublicActionsQuery : IRequest<PublicActionsSettingsDto>;
public sealed record UpsertBusinessPublicActionsCommand(UpsertPublicActionsRequest Request)
    : IRequest<PublicActionsSettingsDto>;
public sealed record GetQueuePublicActionsQuery(Guid QueueId) : IRequest<QueuePublicActionsSettingsDto?>;
public sealed record UpsertQueuePublicActionsCommand(Guid QueueId, UpsertQueuePublicActionsRequest Request)
    : IRequest<QueuePublicActionsSettingsDto?>;
public sealed record ListInAppNotificationsQuery(bool UnreadOnly) : IRequest<IReadOnlyList<InAppNotificationDto>>;
public sealed record MarkInAppNotificationReadCommand(Guid Id) : IRequest<bool>;

internal static class PublicActionsCatalogUi
{
    public static readonly string[] AllEvents = PublicEventCatalog.AllEvents;

    public static IReadOnlyList<PublicEventToggleDto> Toggles(IReadOnlyList<string> enabled)
    {
        var enabledSet = enabled
            .Select(PublicEventCatalog.Canonicalize)
            .ToHashSet(StringComparer.Ordinal);
        return AllEvents
            .Select(e => new PublicEventToggleDto(e, enabledSet.Contains(e)))
            .ToList();
    }
}

public static class PublicActionsMapping
{
    public static async Task<PublicActionsSettingsDto> LoadScopeAsync(
        DocumateDbContext db,
        string businessId,
        Guid? queueId,
        CancellationToken cancellationToken)
    {
        var rows = await db.OpsActionBindings.AsNoTracking()
            .Where(b => b.BusinessId == businessId && b.QueueId == queueId && !b.IsDeleted)
            .ToListAsync(cancellationToken);

        var webhook = rows.FirstOrDefault(r => r.ActionTypeKey == PublicEventCatalog.ActionWebhook);
        var email = rows.FirstOrDefault(r => r.ActionTypeKey == PublicEventCatalog.ActionEmail);
        var inApp = rows.FirstOrDefault(r => r.ActionTypeKey == PublicEventCatalog.ActionInApp);

        return new PublicActionsSettingsDto(
            MapWebhook(webhook),
            MapEmail(email),
            MapInApp(inApp));
    }

    public static WebhookActionDto MapWebhook(OpsActionBinding? row)
    {
        var keys = ActionBindingResolver.ParseEventKeys(row?.EventKeysJson);
        string? url = null;
        var hasSecret = false;
        if (row?.ConfigJson is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(row.ConfigJson);
                if (doc.RootElement.TryGetProperty("url", out var u))
                {
                    url = u.GetString();
                }

                hasSecret = doc.RootElement.TryGetProperty("secretProtected", out var s)
                            && !string.IsNullOrWhiteSpace(s.GetString());
            }
            catch (JsonException)
            {
                // ignore
            }
        }

        return new WebhookActionDto(row?.Enabled ?? false, url, hasSecret, PublicActionsCatalogUi.Toggles(keys));
    }

    public static EmailActionDto MapEmail(OpsActionBinding? row)
    {
        var keys = ActionBindingResolver.ParseEventKeys(row?.EventKeysJson);
        var audience = "partner";
        var recipients = new List<string>();
        if (row?.ConfigJson is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(row.ConfigJson);
                if (doc.RootElement.TryGetProperty("audience", out var a))
                {
                    audience = a.GetString() ?? audience;
                }

                if (doc.RootElement.TryGetProperty("recipients", out var r) && r.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in r.EnumerateArray())
                    {
                        var email = item.GetString();
                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            recipients.Add(email!);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // ignore
            }
        }

        return new EmailActionDto(row?.Enabled ?? false, audience, recipients, PublicActionsCatalogUi.Toggles(keys));
    }

    public static InAppActionDto MapInApp(OpsActionBinding? row)
    {
        var keys = ActionBindingResolver.ParseEventKeys(row?.EventKeysJson);
        return new InAppActionDto(row?.Enabled ?? false, PublicActionsCatalogUi.Toggles(keys));
    }

    public static string CanonicalEventKeysJson(IEnumerable<string> eventKeys) =>
        JsonSerializer.Serialize(
            eventKeys
                .Select(PublicEventCatalog.Canonicalize)
                .Where(k => k.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList());

    public static async Task UpsertScopeAsync(
        DocumateDbContext db,
        IWebhookSecretProtector secrets,
        string businessId,
        Guid? queueId,
        string? userId,
        UpsertPublicActionsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Webhook is not null)
        {
            await UpsertWebhookAsync(db, secrets, businessId, queueId, userId, request.Webhook, cancellationToken);
        }

        if (request.Email is not null)
        {
            await UpsertTypedAsync(
                db,
                businessId,
                queueId,
                userId,
                PublicEventCatalog.ActionEmail,
                request.Email.Enabled,
                request.Email.EventKeys,
                JsonSerializer.Serialize(new { audience = request.Email.Audience, recipients = request.Email.Recipients }),
                cancellationToken);
        }

        if (request.InApp is not null)
        {
            await UpsertTypedAsync(
                db,
                businessId,
                queueId,
                userId,
                PublicEventCatalog.ActionInApp,
                request.InApp.Enabled,
                request.InApp.EventKeys,
                "{}",
                cancellationToken);
        }
    }

    private static async Task UpsertWebhookAsync(
        DocumateDbContext db,
        IWebhookSecretProtector secrets,
        string businessId,
        Guid? queueId,
        string? userId,
        WebhookActionUpsert upsert,
        CancellationToken cancellationToken)
    {
        var row = await db.OpsActionBindings.FirstOrDefaultAsync(
            b => b.BusinessId == businessId
                 && b.QueueId == queueId
                 && b.ActionTypeKey == PublicEventCatalog.ActionWebhook
                 && !b.IsDeleted,
            cancellationToken);

        string? secretProtected = null;
        if (!string.IsNullOrWhiteSpace(upsert.Secret))
        {
            secretProtected = secrets.Protect(upsert.Secret);
        }
        else if (row?.ConfigJson is not null)
        {
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
        }

        var config = JsonSerializer.Serialize(new
        {
            url = string.IsNullOrWhiteSpace(upsert.Url) ? null : upsert.Url.Trim(),
            secretProtected,
        });

        if (row is null)
        {
            db.OpsActionBindings.Add(new OpsActionBinding
            {
                BusinessId = businessId,
                QueueId = queueId,
                ActionTypeKey = PublicEventCatalog.ActionWebhook,
                Enabled = upsert.Enabled,
                ConfigJson = config,
                EventKeysJson = CanonicalEventKeysJson(upsert.EventKeys),
                CreatedByUserId = userId,
                UpdatedByUserId = userId,
            });
        }
        else
        {
            row.Enabled = upsert.Enabled;
            row.ConfigJson = config;
            row.EventKeysJson = CanonicalEventKeysJson(upsert.EventKeys);
            row.UpdatedByUserId = userId;
        }

        // Keep legacy OpsQueue columns in sync when queue-scoped or when updating business (donor for inherit).
        if (queueId is Guid qid)
        {
            var queue = await db.OpsQueues.FirstOrDefaultAsync(
                q => q.Id == qid && q.BusinessId == businessId, cancellationToken);
            if (queue is not null)
            {
                queue.WebhookEnabled = upsert.Enabled;
                queue.WebhookUrl = string.IsNullOrWhiteSpace(upsert.Url) ? null : upsert.Url.Trim();
                if (!string.IsNullOrWhiteSpace(upsert.Secret))
                {
                    queue.WebhookSecretProtected = secretProtected;
                    queue.WebhookSecretHash = Convert.ToHexString(
                        System.Security.Cryptography.SHA256.HashData(
                            System.Text.Encoding.UTF8.GetBytes(upsert.Secret))).ToLowerInvariant();
                }

                queue.UpdatedByUserId = userId;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpsertTypedAsync(
        DocumateDbContext db,
        string businessId,
        Guid? queueId,
        string? userId,
        string actionType,
        bool enabled,
        IReadOnlyList<string> eventKeys,
        string configJson,
        CancellationToken cancellationToken)
    {
        var row = await db.OpsActionBindings.FirstOrDefaultAsync(
            b => b.BusinessId == businessId
                 && b.QueueId == queueId
                 && b.ActionTypeKey == actionType
                 && !b.IsDeleted,
            cancellationToken);
        if (row is null)
        {
            db.OpsActionBindings.Add(new OpsActionBinding
            {
                BusinessId = businessId,
                QueueId = queueId,
                ActionTypeKey = actionType,
                Enabled = enabled,
                ConfigJson = configJson,
                EventKeysJson = CanonicalEventKeysJson(eventKeys),
                CreatedByUserId = userId,
                UpdatedByUserId = userId,
            });
        }
        else
        {
            row.Enabled = enabled;
            row.ConfigJson = configJson;
            row.EventKeysJson = CanonicalEventKeysJson(eventKeys);
            row.UpdatedByUserId = userId;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class GetBusinessPublicActionsHandler(DocumateDbContext db, IBusinessContext business, IActionBindingBootstrap bootstrap)
    : IRequestHandler<GetBusinessPublicActionsQuery, PublicActionsSettingsDto>
{
    public async Task<PublicActionsSettingsDto> Handle(GetBusinessPublicActionsQuery request, CancellationToken cancellationToken)
    {
        await bootstrap.EnsureBusinessDefaultsAsync(business.BusinessId, business.UserId, cancellationToken);
        return await PublicActionsMapping.LoadScopeAsync(db, business.BusinessId, null, cancellationToken);
    }
}

public sealed class UpsertBusinessPublicActionsHandler(
    DocumateDbContext db,
    IBusinessContext business,
    IWebhookSecretProtector secrets)
    : IRequestHandler<UpsertBusinessPublicActionsCommand, PublicActionsSettingsDto>
{
    public async Task<PublicActionsSettingsDto> Handle(
        UpsertBusinessPublicActionsCommand command,
        CancellationToken cancellationToken)
    {
        await PublicActionsMapping.UpsertScopeAsync(
            db, secrets, business.BusinessId, null, business.UserId, command.Request, cancellationToken);
        return await PublicActionsMapping.LoadScopeAsync(db, business.BusinessId, null, cancellationToken);
    }
}

public sealed class GetQueuePublicActionsHandler(DocumateDbContext db, IBusinessContext business, IActionBindingBootstrap bootstrap)
    : IRequestHandler<GetQueuePublicActionsQuery, QueuePublicActionsSettingsDto?>
{
    public async Task<QueuePublicActionsSettingsDto?> Handle(
        GetQueuePublicActionsQuery request,
        CancellationToken cancellationToken)
    {
        var queue = await db.OpsQueues.AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == request.QueueId && q.BusinessId == business.BusinessId, cancellationToken);
        if (queue is null)
        {
            return null;
        }

        await bootstrap.EnsureBusinessDefaultsAsync(business.BusinessId, business.UserId, cancellationToken);
        var businessSettings = await PublicActionsMapping.LoadScopeAsync(db, business.BusinessId, null, cancellationToken);
        var overrideSettings = await PublicActionsMapping.LoadScopeAsync(db, business.BusinessId, queue.Id, cancellationToken);
        var effective = queue.PublicActionsInherit ? businessSettings : overrideSettings;
        return new QueuePublicActionsSettingsDto(queue.PublicActionsInherit, effective, queue.PublicActionsInherit ? null : overrideSettings);
    }
}

public sealed class UpsertQueuePublicActionsHandler(
    DocumateDbContext db,
    IBusinessContext business,
    IWebhookSecretProtector secrets)
    : IRequestHandler<UpsertQueuePublicActionsCommand, QueuePublicActionsSettingsDto?>
{
    public async Task<QueuePublicActionsSettingsDto?> Handle(
        UpsertQueuePublicActionsCommand command,
        CancellationToken cancellationToken)
    {
        var queue = await db.OpsQueues.FirstOrDefaultAsync(
            q => q.Id == command.QueueId && q.BusinessId == business.BusinessId, cancellationToken);
        if (queue is null)
        {
            return null;
        }

        queue.PublicActionsInherit = command.Request.Inherit;
        queue.UpdatedByUserId = business.UserId;
        await db.SaveChangesAsync(cancellationToken);

        if (!command.Request.Inherit && command.Request.Override is not null)
        {
            await PublicActionsMapping.UpsertScopeAsync(
                db, secrets, business.BusinessId, queue.Id, business.UserId, command.Request.Override, cancellationToken);
        }

        var businessSettings = await PublicActionsMapping.LoadScopeAsync(db, business.BusinessId, null, cancellationToken);
        var overrideSettings = await PublicActionsMapping.LoadScopeAsync(db, business.BusinessId, queue.Id, cancellationToken);
        var effective = queue.PublicActionsInherit ? businessSettings : overrideSettings;
        return new QueuePublicActionsSettingsDto(queue.PublicActionsInherit, effective, queue.PublicActionsInherit ? null : overrideSettings);
    }
}

public sealed class ListInAppNotificationsHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<ListInAppNotificationsQuery, IReadOnlyList<InAppNotificationDto>>
{
    public async Task<IReadOnlyList<InAppNotificationDto>> Handle(
        ListInAppNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var q = db.OpsInAppNotifications.AsNoTracking()
            .Where(n => n.BusinessId == business.BusinessId && !n.IsDeleted);
        if (request.UnreadOnly)
        {
            q = q.Where(n => n.ReadAt == null);
        }

        var rows = await q.OrderByDescending(n => n.CreatedAt).Take(100).ToListAsync(cancellationToken);
        return rows.Select(n => new InAppNotificationDto(
            n.Id, n.EventName, n.EventId, n.Title, n.Body, n.CreatedAt, n.ReadAt)).ToList();
    }
}

public sealed class MarkInAppNotificationReadHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<MarkInAppNotificationReadCommand, bool>
{
    public async Task<bool> Handle(MarkInAppNotificationReadCommand command, CancellationToken cancellationToken)
    {
        var row = await db.OpsInAppNotifications.FirstOrDefaultAsync(
            n => n.Id == command.Id && n.BusinessId == business.BusinessId && !n.IsDeleted, cancellationToken);
        if (row is null)
        {
            return false;
        }

        row.ReadAt = DateTimeOffset.UtcNow;
        row.UpdatedByUserId = business.UserId;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
