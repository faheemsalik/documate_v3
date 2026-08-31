namespace Documate.Api.Infrastructure.Options;

public sealed class OcrOptions
{
    public const string SectionName = "Ocr";

    public string PrimaryProviderKey { get; set; } = "aws_textract";
    public string SecondaryProviderKey { get; set; } = "google_document_ai";
    public int SyncMaxPages { get; set; } = 8;

    public TextractOptions Textract { get; set; } = new();
    public GoogleDocumentAiOptions GoogleDocumentAi { get; set; } = new();
}

public sealed class TextractOptions
{
    public string? Region { get; set; } = "us-west-2";
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
}

public sealed class GoogleDocumentAiOptions
{
    public string? ProjectId { get; set; }
    public string? Location { get; set; } = "us";
    public string? ProcessorId { get; set; }
    /// <summary>Optional path or raw JSON for service account. Prefer GOOGLE_APPLICATION_CREDENTIALS.</summary>
    public string? CredentialsJson { get; set; }
}

public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    public string DefaultProviderKey { get; set; } = "gpt_5_6";
    public Dictionary<string, LlmProviderOptions> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class LlmProviderOptions
{
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
    public string? BaseUrl { get; set; }
}

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    public bool Enabled { get; set; }
    public string ToAddress { get; set; } = "faheem@manticsoftware.com";
    public SmtpOptions Smtp { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string? From { get; set; }
}
