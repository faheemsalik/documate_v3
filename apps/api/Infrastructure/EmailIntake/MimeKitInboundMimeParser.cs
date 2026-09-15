namespace Documate.Api.Infrastructure.EmailIntake;

using System.Text;
using MimeKit;

public sealed record ParsedInboundEmail(
    string? ToLocalPart,
    string? ToDomain,
    string? ToAddress,
    string? From,
    string? FromName,
    string? ReplyTo,
    string? ResentFrom,
    string? Subject,
    string? MessageId,
    string? TextBody,
    IReadOnlyList<EmailIntakeAttachmentInput> Attachments);

public interface IInboundMimeParser
{
    ParsedInboundEmail Parse(Stream mimeStream);
}

public sealed class MimeKitInboundMimeParser : IInboundMimeParser
{
    public ParsedInboundEmail Parse(Stream mimeStream)
    {
        var message = MimeMessage.Load(mimeStream);
        string? local = null;
        string? domain = null;
        string? toAddress = null;
        var to = message.To.Mailboxes.FirstOrDefault()
                 ?? message.Cc.Mailboxes.FirstOrDefault();
        if (to is not null)
        {
            local = to.LocalPart;
            domain = to.Domain;
            toAddress = to.Address;
        }

        var fromMb = message.From.Mailboxes.FirstOrDefault();
        var from = fromMb?.Address;
        var fromName = string.IsNullOrWhiteSpace(fromMb?.Name) ? null : fromMb!.Name.Trim();

        var replyMb = message.ReplyTo.Mailboxes.FirstOrDefault();
        var replyTo = replyMb is null
            ? null
            : (string.IsNullOrWhiteSpace(replyMb.Name) ? replyMb.Address : $"{replyMb.Name} <{replyMb.Address}>");
        var resentFromHeader = message.Headers["Resent-From"];
        var resentFrom = string.IsNullOrWhiteSpace(resentFromHeader) ? null : resentFromHeader.Trim();

        var textBody = message.TextBody;
        if (string.IsNullOrWhiteSpace(textBody) && !string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            textBody = StripTags(message.HtmlBody);
        }

        var attachments = new List<EmailIntakeAttachmentInput>();
        foreach (var part in message.BodyParts.OfType<MimePart>())
        {
            var isAttachment = part.IsAttachment
                               || part.ContentDisposition?.Disposition == ContentDisposition.Attachment
                               || (part.ContentType.IsMimeType("image", "*")
                                   && part.ContentDisposition?.Disposition == ContentDisposition.Inline
                                   && part.FileName is not null);

            if (!isAttachment && part.ContentType.MimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!isAttachment && part.FileName is null)
            {
                continue;
            }

            using var ms = new MemoryStream();
            if (part.Content is null)
            {
                continue;
            }

            part.Content.DecodeTo(ms);
            var name = part.FileName;
            if (string.IsNullOrWhiteSpace(name))
            {
                var ext = part.ContentType.MediaSubtype ?? "bin";
                name = $"attachment.{ext}";
            }

            attachments.Add(new EmailIntakeAttachmentInput(
                name,
                part.ContentType.MimeType,
                ms.ToArray()));
        }

        return new ParsedInboundEmail(
            local,
            domain,
            toAddress,
            from,
            fromName,
            replyTo,
            resentFrom,
            message.Subject,
            message.MessageId,
            textBody,
            attachments);
    }

    private static string StripTags(string html)
    {
        var sb = new StringBuilder(html.Length);
        var inTag = false;
        foreach (var c in html)
        {
            if (c == '<')
            {
                inTag = true;
                continue;
            }

            if (c == '>')
            {
                inTag = false;
                continue;
            }

            if (!inTag)
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
