namespace Documate.Api.Infrastructure.Notifications;

using Documate.Api.Infrastructure.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

/// <summary>DR1-B: log always; SMTP only when Notifications:Enabled. Never throws to pipeline.</summary>
public sealed class OpsAlertSender(
    IOptionsMonitor<NotificationOptions> options,
    ILogger<OpsAlertSender> logger) : IOpsAlertSender
{
    public async Task NotifyLlmExtractFailedAsync(
        OpsLlmFailureAlert alert,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogError(
                "LLM extract failed: BusinessId={BusinessId} FileId={FileId} DocumentId={DocumentId} QueueId={QueueId} Provider={Provider} Code={Code} Message={Message}",
                alert.BusinessId,
                alert.FileId,
                alert.DocumentId,
                alert.QueueId,
                alert.ProviderKey,
                alert.ErrorCode,
                alert.ErrorMessage);

            var opts = options.CurrentValue;
            if (!opts.Enabled)
            {
                return;
            }

            var smtp = opts.Smtp;
            if (string.IsNullOrWhiteSpace(smtp.Host)
                || string.IsNullOrWhiteSpace(smtp.From)
                || string.IsNullOrWhiteSpace(opts.ToAddress))
            {
                logger.LogWarning("Notifications enabled but Smtp Host/From or ToAddress missing; skip email.");
                return;
            }

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(smtp.From));
            message.To.Add(MailboxAddress.Parse(opts.ToAddress));
            message.Subject = $"[Documate] LLM extract failed — File {alert.FileId}";
            message.Body = new TextPart("plain")
            {
                Text = $"""
                    Documate LLM extract failure (ids + error only; no OCR payload).

                    OccurredAtUtc: {alert.OccurredAtUtc:O}
                    BusinessId: {alert.BusinessId}
                    QueueId: {alert.QueueId}
                    FileId: {alert.FileId}
                    DocumentId: {alert.DocumentId}
                    ProviderKey: {alert.ProviderKey}
                    ErrorCode: {alert.ErrorCode}
                    ErrorMessage: {alert.ErrorMessage}
                    """,
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(
                smtp.Host,
                smtp.Port <= 0 ? 587 : smtp.Port,
                SecureSocketOptions.StartTlsWhenAvailable,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(smtp.User))
            {
                await client.AuthenticateAsync(smtp.User, smtp.Password ?? "", cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ops alert send failed (best-effort; status unchanged)");
        }
    }
}
