namespace Documate.Api.Infrastructure.PublicEvents;

/// <summary>Partner-facing public event names (human-friendly Title Case).</summary>
public static class PublicEventCatalog
{
    public const string FileReceived = "File received";
    public const string FileCompleted = "File completed";
    public const string DocumentReady = "Document ready";
    public const string DocumentFailed = "Document failed";
    public const string DocumentCancelled = "Document cancelled";

    public const string ActionWebhook = "webhook";
    public const string ActionEmail = "email";
    public const string ActionInApp = "in_app";

    public const string ResourceFile = "file";
    public const string ResourceDocument = "document";

    /// <summary>Default Business webhook event checklist (PE6).</summary>
    public static readonly string[] DefaultBusinessWebhookEvents =
    [
        FileReceived,
        DocumentReady,
        DocumentFailed,
        DocumentCancelled,
    ];

    public static readonly string[] AllEvents =
    [
        FileReceived,
        DocumentReady,
        DocumentFailed,
        DocumentCancelled,
        FileCompleted,
    ];

    public static string EventIdForFile(Guid fileId, string eventName) =>
        eventName switch
        {
            FileReceived => $"file:{fileId:D}:received",
            FileCompleted => $"file:{fileId:D}:completed",
            _ => $"file:{fileId:D}:{Slug(eventName)}",
        };

    public static string EventIdForDocument(Guid documentId, string eventName) =>
        eventName switch
        {
            DocumentReady => $"document:{documentId:D}:ready",
            DocumentFailed => $"document:{documentId:D}:failed",
            DocumentCancelled => $"document:{documentId:D}:cancelled",
            _ => $"document:{documentId:D}:{Slug(eventName)}",
        };

    public static string DocumentEventForPublicStatus(string statusKey) =>
        statusKey switch
        {
            "ready" => DocumentReady,
            "cancelled" => DocumentCancelled,
            "failed" or "rejected" => DocumentFailed,
            _ => DocumentFailed,
        };

    /// <summary>Map legacy dotted keys (pre human-friendly rename) to current catalog names.</summary>
    public static string Canonicalize(string? eventKey)
    {
        if (string.IsNullOrWhiteSpace(eventKey))
        {
            return "";
        }

        return eventKey.Trim() switch
        {
            "file.received" => FileReceived,
            "file.completed" => FileCompleted,
            "document.ready" => DocumentReady,
            "document.failed" => DocumentFailed,
            "document.cancelled" => DocumentCancelled,
            "document.terminal" => DocumentReady, // best-effort; terminal covered by typed events
            _ => eventKey.Trim(),
        };
    }

    public static bool EventEquals(string? a, string? b) =>
        string.Equals(Canonicalize(a), Canonicalize(b), StringComparison.Ordinal);

    private static string Slug(string eventName) =>
        string.Join('_', eventName.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
}

public static class ActionBindingDefaults
{
    public static string DefaultWebhookEventKeysJson() =>
        System.Text.Json.JsonSerializer.Serialize(PublicEventCatalog.DefaultBusinessWebhookEvents);

    public static string EmptyConfigJson() => "{}";
}
