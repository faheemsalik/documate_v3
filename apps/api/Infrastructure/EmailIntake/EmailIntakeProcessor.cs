namespace Documate.Api.Infrastructure.EmailIntake;

using System.Text.Json;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Auth;
using Documate.Api.Infrastructure.Settings;
using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Pipeline;
using Documate.Api.Infrastructure.Work;
using Microsoft.EntityFrameworkCore;

public sealed record EmailIntakeAttachmentInput(
    string FileName,
    string? ContentType,
    byte[] Content);

public sealed record EmailIntakeProcessRequest(
    Guid MailboxId,
    string? From,
    string? Subject,
    string? MessageId,
    string? TextBody,
    IReadOnlyList<EmailIntakeAttachmentInput> Attachments,
    string? FromName = null,
    string? ReplyTo = null,
    string? ResentFrom = null,
    string? ToAddress = null);

public sealed record EmailIntakeProcessResult(
    bool Accepted,
    string? RejectCode,
    string? RejectMessage,
    Guid? IntakeRejectionId,
    Guid? BatchId,
    IReadOnlyList<Guid> FileIds);

public interface IEmailIntakeProcessor
{
    Task<EmailIntakeProcessResult> ProcessAsync(EmailIntakeProcessRequest request, CancellationToken cancellationToken = default);
}

public sealed class EmailIntakeProcessor(
    DocumateDbContext db,
    IBusinessContext business,
    ICorEnumIdResolver enums,
    IEmailIntakeDecisionAgent decisionAgent,
    IWorkRecordService work,
    IWorkDispatcher dispatcher,
    IEmailIntakeRateLimiter rateLimiter,
    IEmailIntakeMetrics metrics,
    IEmailIntakeSettings emailIntakeSettings,
    ILogger<EmailIntakeProcessor> logger) : IEmailIntakeProcessor
{
    public async Task<EmailIntakeProcessResult> ProcessAsync(
        EmailIntakeProcessRequest request,
        CancellationToken cancellationToken = default)
    {
        using var _ = metrics.MeasureProcess();
        var opts = emailIntakeSettings.Current;
        var allowedExt = new HashSet<string>(
            opts.AllowedExtensions.Select(e => e.StartsWith('.') ? e.ToLowerInvariant() : "." + e.ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);

        var mailbox = await db.OpsIntakeMailboxes
            .Include(m => m.AllowlistEntries)
            .FirstOrDefaultAsync(
                m => m.Id == request.MailboxId
                     && m.BusinessId == business.BusinessId
                     && !m.IsDeleted,
                cancellationToken)
            ?? throw new InvalidOperationException("Intake mailbox not found.");

        var sourceEmailId = enums.Require("intake_source", "email");
        var kindKey = await KindKeyAsync(mailbox.KindEnumId, cancellationToken);
        var isTyped = string.Equals(kindKey, "typed_agent", StringComparison.OrdinalIgnoreCase);

        if (!mailbox.Enabled)
        {
            return await RejectAsync(
                mailbox,
                sourceEmailId,
                request,
                "email_intake_disabled",
                "Mailbox intake is disabled.",
                cancellationToken);
        }

        if (!rateLimiter.TryAcquire(mailbox.Id))
        {
            metrics.RateLimited();
            return await RejectAsync(
                mailbox,
                sourceEmailId,
                request,
                "rate_limited",
                "Mailbox rate limit exceeded.",
                cancellationToken);
        }

        if (isTyped)
        {
            if (mailbox.AgentId is null)
            {
                return await RejectAsync(
                    mailbox,
                    sourceEmailId,
                    request,
                    "mailbox_agent_unavailable",
                    "Typed mailbox has no Agent.",
                    cancellationToken);
            }

            var agentOk = await db.OpsAgents.AnyAsync(
                a => a.Id == mailbox.AgentId
                     && a.BusinessId == business.BusinessId
                     && a.IsActive
                     && !a.IsDeleted,
                cancellationToken);
            if (!agentOk)
            {
                return await RejectAsync(
                    mailbox,
                    sourceEmailId,
                    request,
                    "mailbox_agent_unavailable",
                    "Bound Agent is missing or inactive.",
                    cancellationToken);
            }
        }

        var modeKey = await EnumKeyAsync(mailbox.AllowlistModeEnumId, cancellationToken) ?? "open";
        var entries = new List<(string, string)>();
        foreach (var e in mailbox.AllowlistEntries.Where(x => !x.IsDeleted))
        {
            var mt = await EnumKeyAsync(e.MatchTypeEnumId, cancellationToken) ?? "email";
            entries.Add((mt, e.Value));
        }

        var allow = EmailAllowlistMatcher.Evaluate(request.From, modeKey, entries);
        if (!allow.Accept)
        {
            return await RejectAsync(
                mailbox,
                sourceEmailId,
                request,
                "allowlist_rejected",
                "Sender is not on the mailbox allowlist.",
                cancellationToken);
        }

        if (string.Equals(allow.ModeKey, "allowlist_preferred", StringComparison.OrdinalIgnoreCase)
            && !allow.OnList)
        {
            metrics.AllowlistPreferredMiss();
            logger.LogInformation(
                "Email intake allowlist_preferred miss mailbox={MailboxId} from={From}",
                mailbox.Id,
                EmailAllowlistMatcher.NormalizeAddress(request.From));
        }

        if (request.Attachments.Count > opts.MaxAttachments)
        {
            return await RejectAsync(
                mailbox,
                sourceEmailId,
                request,
                "too_many_attachments",
                $"More than {opts.MaxAttachments} attachments.",
                cancellationToken);
        }

        var sizeSkipped = new List<EmailIntakeSkippedAttachment>();
        var sizeOk = new List<EmailIntakeAttachmentInput>();
        long totalBytes = 0;
        foreach (var a in request.Attachments)
        {
            if (a.Content.LongLength > opts.MaxAttachmentBytes)
            {
                sizeSkipped.Add(new EmailIntakeSkippedAttachment(a.FileName, "attachment_too_large"));
                continue;
            }

            totalBytes += a.Content.LongLength;
            sizeOk.Add(a);
        }

        if (opts.MaxTotalAttachmentBytes > 0 && totalBytes > opts.MaxTotalAttachmentBytes)
        {
            return await RejectAsync(
                mailbox,
                sourceEmailId,
                request,
                "attachments_total_too_large",
                $"Total attachment size exceeds {opts.MaxTotalAttachmentBytes} bytes.",
                cancellationToken);
        }

        var decision = decisionAgent.Decide(
            new EmailIntakeDecisionContext(
                isTyped,
                request.From,
                request.Subject,
                request.TextBody,
                sizeOk
                    .Select(a => new EmailIntakeCandidateAttachment(a.FileName, a.ContentType, a.Content))
                    .ToList()),
            allowedExt);

        var skipped = sizeSkipped.Concat(decision.SkippedAttachments).ToList();

        if (decision.Action == EmailIntakeDecisionAction.Reject || decision.Targets.Count == 0)
        {
            var code = decision.RejectCode
                       ?? (skipped.Count > 0 ? "no_processable_attachments" : "ambiguous_email");
            return await RejectAsync(
                mailbox,
                sourceEmailId,
                request,
                code,
                decision.RejectMessage ?? "Intake decision rejected the message.",
                cancellationToken);
        }

        string? documentTypeKey = null;
        if (isTyped && mailbox.AgentId is Guid agentId)
        {
            documentTypeKey = await (
                from a in db.OpsAgents.AsNoTracking()
                join dt in db.CorDocumentTypes.AsNoTracking() on a.DocumentTypeId equals dt.Id
                where a.Id == agentId
                select dt.DocumentTypeKey).FirstOrDefaultAsync(cancellationToken);
        }

        var intakeHintsJson = documentTypeKey is null
            ? null
            : JsonSerializer.Serialize(new { documentTypeKey });

        var hasAttachmentTargets = decision.Targets.Any(t =>
            string.Equals(t.Kind, "attachment", StringComparison.OrdinalIgnoreCase));
        string? excerpt = null;
        var truncated = false;
        if (hasAttachmentTargets)
        {
            (excerpt, truncated) = EmailBodyExcerpt.Truncate(request.TextBody, opts.BodyExcerptMaxChars);
        }

        var chain = EmailSenderChainResolver.Resolve(
            request.From,
            request.FromName,
            request.ReplyTo,
            request.ResentFrom,
            request.TextBody,
            request.ToAddress ?? $"{mailbox.EmailLocalPart}@{mailbox.EmailDomain}");
        var emailIntakeJson = EmailIntakeJsonBuilder.Build(chain, excerpt, truncated, skipped);

        var batch = await work.CreateBatchAsync(
            mailbox.QueueId,
            sourceEmailId,
            decision.Targets.Count,
            request.MessageId,
            cancellationToken);

        var fileIds = new List<Guid>();
        foreach (var target in decision.Targets)
        {
            await using var stream = new MemoryStream(target.Content, writable: false);
            var file = await work.CreateFileWithBlobAsync(
                new CreateFileWithBlobRequest(
                    mailbox.QueueId,
                    batch?.Id,
                    sourceEmailId,
                    target.FileName,
                    target.ContentType,
                    stream,
                    target.Content.LongLength,
                    intakeHintsJson,
                    EmailMessageId: request.MessageId,
                    EmailFrom: request.From,
                    EmailSubject: request.Subject,
                    EmailIntakeJson: emailIntakeJson),
                cancellationToken);

            await dispatcher.EnqueueFileAsync(
                new FileWorkItem(file.Id, business.BusinessId, business.UserId),
                cancellationToken);
            fileIds.Add(file.Id);
        }

        metrics.Accepted(fileIds.Count);
        logger.LogInformation(
            "Email intake accepted mailbox={MailboxId} files={FileCount} decision={Reasons}",
            mailbox.Id,
            fileIds.Count,
            decision.ReasonsJson);

        return new EmailIntakeProcessResult(true, null, null, null, batch?.Id, fileIds);
    }

    private async Task<EmailIntakeProcessResult> RejectAsync(
        OpsIntakeMailbox mailbox,
        long sourceEmailId,
        EmailIntakeProcessRequest request,
        string code,
        string message,
        CancellationToken cancellationToken)
    {
        var row = await work.CreateIntakeRejectionAsync(
            new CreateIntakeRejectionRequest(
                mailbox.QueueId,
                sourceEmailId,
                code,
                message,
                request.From,
                request.Subject,
                request.MessageId),
            cancellationToken);

        metrics.Rejected(code);
        logger.LogInformation(
            "Email intake rejected mailbox={MailboxId} code={Code}",
            mailbox.Id,
            code);

        return new EmailIntakeProcessResult(false, code, message, row.Id, null, []);
    }

    private async Task<string?> KindKeyAsync(long enumId, CancellationToken ct) =>
        await EnumKeyAsync(enumId, ct);

    private async Task<string?> EnumKeyAsync(long enumId, CancellationToken ct) =>
        await db.CorEnums.AsNoTracking()
            .Where(e => e.Id == enumId)
            .Select(e => e.EnumKey)
            .FirstOrDefaultAsync(ct);
}
