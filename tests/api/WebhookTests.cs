namespace Documate.Api.Tests;

using System.Text;
using Documate.Api.Infrastructure.Webhooks;

public class DocumentWebhookPayloadTests
{
    [Fact]
    public void Sign_and_verify_hmac()
    {
        var body = DocumentWebhookPayload.Serialize(new { event_id = "abc", status = "ready" });
        var header = DocumentWebhookPayload.SignBody("smoke-secret", body);
        Assert.StartsWith("sha256=", header);
        Assert.True(DocumentWebhookPayload.Verify("smoke-secret", body, header));
        Assert.False(DocumentWebhookPayload.Verify("other", body, header));
    }

    [Fact]
    public void Serializes_snake_case_event_fields()
    {
        var payload = new DocumentWebhookBody(
            DocumentWebhookPayload.EventName,
            Guid.Parse("11111111-1111-1111-1111-111111111111").ToString("D"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            null,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "ready",
            "invoice",
            null,
            null,
            null,
            "api",
            null,
            null,
            DateTimeOffset.Parse("2026-08-18T00:00:00Z"));
        var json = Encoding.UTF8.GetString(DocumentWebhookPayload.Serialize(payload));
        Assert.Contains("\"event\":\"document.terminal\"", json);
        Assert.Contains("\"queue_id\":", json);
        Assert.Contains("\"document_id\":", json);
        Assert.Contains("\"occurred_at\":", json);
    }
}
