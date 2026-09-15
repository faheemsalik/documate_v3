namespace Documate.Api.Infrastructure.Options;

public sealed class EmailIntakeOptions
{
    public const string SectionName = "EmailIntake";

    /// <summary>Domain used when minting intake addresses (e.g. docsintake.com).</summary>
    public string DefaultDomain { get; set; } = "docsintake.com";

    public long MaxAttachmentBytes { get; set; } = 25 * 1024 * 1024;
    /// <summary>Sum of all attachment bytes in one message (default 50 MB).</summary>
    public long MaxTotalAttachmentBytes { get; set; } = 50 * 1024 * 1024;
    public int MaxAttachments { get; set; } = 20;
    public string[] AllowedExtensions { get; set; } =
        [".pdf", ".png", ".jpg", ".jpeg", ".tif", ".tiff", ".docx", ".xlsx", ".txt"];

    /// <summary>Max accepted messages per mailbox per rolling minute (0 = disabled).</summary>
    public int RateLimitPerMailboxPerMinute { get; set; } = 20;

    /// <summary>Max accepted messages per mailbox per rolling hour (0 = disabled).</summary>
    public int RateLimitPerMailboxPerHour { get; set; } = 200;

    /// <summary>S3 bucket for SES receipt rule raw MIME.</summary>
    public string? S3Bucket { get; set; }
    public string? S3Prefix { get; set; }
    public string? AwsRegion { get; set; }

    /// <summary>Days to retain raw SES MIME in S3 (Plan 14 F1a). Moved to DB via system settings.</summary>
    public int MimeRetentionDays { get; set; } = 30;

    /// <summary>Max chars of email body stored in EmailIntakeJson excerpt.</summary>
    public int BodyExcerptMaxChars { get; set; } = 4096;

    /// <summary>Optional shared secret for SNS→HTTP webhook (Plan 14 EI-5). Secret — AWS Secrets Manager (Plan 17).</summary>
    public string? InboundWebhookSecret { get; set; }
}
