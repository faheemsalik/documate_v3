using System.Text;

namespace Documate.Api.Infrastructure.EmailIntake;

public enum EmailIntakeDecisionAction
{
    Process,
    Reject,
    ProcessPartial,
}

public sealed record EmailIntakeTarget(
    string Kind,
    string FileName,
    string? ContentType,
    byte[] Content,
    string Reason);

public sealed record EmailIntakeSkippedAttachment(string FileName, string Reason);

public sealed record EmailIntakeDecision(
    EmailIntakeDecisionAction Action,
    IReadOnlyList<EmailIntakeTarget> Targets,
    IReadOnlyList<EmailIntakeSkippedAttachment> SkippedAttachments,
    string? RejectCode,
    string? RejectMessage,
    string ReasonsJson);

public sealed record EmailIntakeDecisionContext(
    bool IsTypedMailbox,
    string? From,
    string? Subject,
    string? TextBody,
    IReadOnlyList<EmailIntakeCandidateAttachment> Attachments);

public sealed record EmailIntakeCandidateAttachment(
    string FileName,
    string? ContentType,
    byte[] Content);

public interface IEmailIntakeDecisionAgent
{
    EmailIntakeDecision Decide(EmailIntakeDecisionContext context, IReadOnlySet<string> allowedExtensions);
}

/// <summary>Heuristic skeleton (Plan 14 DR-EI5 + F3): attachments preferred; skip bad parts; body-as-doc when alone.</summary>
public sealed class HeuristicEmailIntakeDecisionAgent : IEmailIntakeDecisionAgent
{
    public EmailIntakeDecision Decide(EmailIntakeDecisionContext context, IReadOnlySet<string> allowedExtensions)
    {
        var allowed = new List<EmailIntakeTarget>();
        var skipped = new List<EmailIntakeSkippedAttachment>();
        foreach (var a in context.Attachments)
        {
            var ext = Path.GetExtension(a.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
            {
                skipped.Add(new EmailIntakeSkippedAttachment(a.FileName, "extension_not_allowed"));
                continue;
            }

            if (a.Content.Length == 0)
            {
                skipped.Add(new EmailIntakeSkippedAttachment(a.FileName, "empty_attachment"));
                continue;
            }

            allowed.Add(new EmailIntakeTarget(
                "attachment",
                a.FileName,
                a.ContentType,
                a.Content,
                "allowed_attachment"));
        }

        var body = (context.TextBody ?? "").Trim();
        var bodyLooksLikeDoc = body.Length >= 80
            && (body.Contains('\n') || body.Length >= 200);

        if (allowed.Count > 0)
        {
            var action = skipped.Count > 0
                ? EmailIntakeDecisionAction.ProcessPartial
                : EmailIntakeDecisionAction.Process;
            return new EmailIntakeDecision(
                action,
                allowed,
                skipped,
                null,
                null,
                skipped.Count > 0
                    ? """{"policy":"attachments_partial"}"""
                    : """{"policy":"attachments"}""");
        }

        if (bodyLooksLikeDoc)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            return new EmailIntakeDecision(
                skipped.Count > 0 ? EmailIntakeDecisionAction.ProcessPartial : EmailIntakeDecisionAction.Process,
                [new EmailIntakeTarget("body", "email-body.txt", "text/plain", bytes, "body_as_document")],
                skipped,
                null,
                null,
                """{"policy":"body_only"}""");
        }

        if (skipped.Count > 0 || context.Attachments.Count > 0)
        {
            return new EmailIntakeDecision(
                EmailIntakeDecisionAction.Reject,
                [],
                skipped,
                "no_processable_attachments",
                "No processable attachments and body is not a clear document.",
                """{"policy":"reject_no_processable"}""");
        }

        return new EmailIntakeDecision(
            EmailIntakeDecisionAction.Reject,
            [],
            skipped,
            "ambiguous_email",
            "No processable attachments and body is not a clear document.",
            """{"policy":"reject_ambiguous"}""");
    }
}
