namespace Documate.Api.Tests;

using Documate.Api.Infrastructure.EmailIntake;
using System.Text.Json;

public class EmailSenderChainResolverTests
{
    [Fact]
    public void Forwarder_from_only_when_no_originator_hints()
    {
        var result = EmailSenderChainResolver.Resolve(
            "Alex <user@partner.com>",
            "Alex",
            replyTo: null,
            resentFrom: null,
            textBody: "Please process.",
            intakeAddress: "ap@docsintake.com");

        Assert.Equal("user@partner.com", result.From.Email);
        Assert.Equal("Alex", result.From.Name);
        Assert.Null(result.Originator);
        Assert.Equal(2, result.Chain.Count);
        Assert.Equal("forwarder", result.Chain[0].Role);
        Assert.Equal("intake", result.Chain[1].Role);
    }

    [Fact]
    public void Originator_from_reply_to_when_different()
    {
        var result = EmailSenderChainResolver.Resolve(
            "user@partner.com",
            "Alex",
            replyTo: "Vendor AP <ap@vendor.com>",
            resentFrom: null,
            textBody: null,
            intakeAddress: "box@docsintake.com");

        Assert.Equal("ap@vendor.com", result.Originator?.Email);
        Assert.Equal("Vendor AP", result.Originator?.Name);
        Assert.Equal("originator", result.Chain[0].Role);
        Assert.Equal("forwarder", result.Chain[1].Role);
        Assert.Equal("intake", result.Chain[2].Role);
    }

    [Fact]
    public void Originator_from_body_From_header()
    {
        var body = """
            ---------- Forwarded message ---------
            From: AP Desk <ap@vendor.com>
            Subject: Invoice

            See attached.
            """;
        var result = EmailSenderChainResolver.Resolve(
            "user@partner.com",
            null,
            null,
            null,
            body,
            "box@docsintake.com");

        Assert.Equal("ap@vendor.com", result.Originator?.Email);
    }

    [Fact]
    public void Excerpt_truncates_at_max()
    {
        var (excerpt, truncated) = EmailBodyExcerpt.Truncate(new string('a', 10), 5);
        Assert.Equal("aaaaa", excerpt);
        Assert.True(truncated);
    }

    [Fact]
    public void Json_builder_includes_excerpt_and_chain()
    {
        var chain = EmailSenderChainResolver.Resolve(
            "user@partner.com",
            "Alex",
            "ap@vendor.com",
            null,
            "hello body",
            "box@docsintake.com");
        var json = EmailIntakeJsonBuilder.Build(chain, "hello body", false);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("user@partner.com", doc.RootElement.GetProperty("from").GetProperty("email").GetString());
        Assert.Equal("ap@vendor.com", doc.RootElement.GetProperty("originator").GetProperty("email").GetString());
        Assert.Equal("hello body", doc.RootElement.GetProperty("emailBodyExcerpt").GetString());
        Assert.False(doc.RootElement.GetProperty("emailBodyTruncated").GetBoolean());
    }
}
