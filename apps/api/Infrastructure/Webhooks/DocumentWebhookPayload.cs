namespace Documate.Api.Infrastructure.Webhooks;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

public static class DocumentWebhookPayload
{
    public const string EventName = "document.terminal";
    public const string SignatureHeader = "X-Documate-Signature";
    public const string EventHeader = "X-Documate-Event";
    public const string DeliveryHeader = "X-Documate-Delivery";

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string SignBody(string secret, byte[] body) =>
        "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body))
            .ToLowerInvariant();

    public static bool Verify(string secret, byte[] body, string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return false;
        }

        var expected = SignBody(secret, body);
        var a = Encoding.UTF8.GetBytes(expected);
        var b = Encoding.UTF8.GetBytes(header.Trim());
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    public static byte[] Serialize(object payload) =>
        JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
}

public sealed record DocumentWebhookBody(
    string Event,
    string EventId,
    Guid QueueId,
    Guid? BatchId,
    Guid FileId,
    Guid DocumentId,
    string Status,
    string? DocumentType,
    Guid? AgentId,
    JsonNode? Data,
    DocumentWebhookError? Error,
    string? Source,
    string? EmailMessageId,
    DocumentWebhookOriginalFile? OriginalFile,
    DateTimeOffset OccurredAt);

public sealed record DocumentWebhookError(string? Code, string? Message);

public sealed record DocumentWebhookOriginalFile(string? FileName, string? ContentType, long SizeBytes);
