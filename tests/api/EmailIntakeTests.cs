namespace Documate.Api.Tests;

using Documate.Api.Infrastructure.EmailIntake;
using Documate.Api.Infrastructure.Settings;

public class EmailAllowlistMatcherTests
{
    [Fact]
    public void Open_mode_always_matches()
    {
        Assert.True(EmailAllowlistMatcher.Matches("anyone@x.com", "open", []));
    }

    [Fact]
    public void Enforced_exact_email_matches()
    {
        Assert.True(EmailAllowlistMatcher.Matches(
            "Ap@Supplier.COM",
            "allowlist_enforced",
            [("email", "ap@supplier.com")]));
    }

    [Fact]
    public void Enforced_domain_matches()
    {
        Assert.True(EmailAllowlistMatcher.Matches(
            "ap@supplier.com",
            "allowlist_enforced",
            [("domain", "supplier.com")]));
    }

    [Fact]
    public void Enforced_rejects_unknown()
    {
        Assert.False(EmailAllowlistMatcher.Matches(
            "evil@other.com",
            "allowlist_enforced",
            [("domain", "supplier.com")]));
    }

    [Fact]
    public void Enforced_parses_display_name_from()
    {
        Assert.True(EmailAllowlistMatcher.Matches(
            "AP Desk <ap@supplier.com>",
            "allowlist_enforced",
            [("email", "ap@supplier.com")]));
    }

    [Fact]
    public void Preferred_accepts_miss_but_reports_not_on_list()
    {
        var decision = EmailAllowlistMatcher.Evaluate(
            "evil@other.com",
            "allowlist_preferred",
            [("domain", "supplier.com")]);
        Assert.True(decision.Accept);
        Assert.False(decision.OnList);
    }

    [Fact]
    public void Enforced_empty_list_rejects()
    {
        Assert.False(EmailAllowlistMatcher.Matches("a@b.com", "allowlist_enforced", []));
    }
}

public class MemoryEmailIntakeRateLimiterTests
{
    private sealed class StubEmailIntakeSettings(EmailIntakeEffectiveSettings current) : IEmailIntakeSettings
    {
        public EmailIntakeEffectiveSettings Current { get; } = current;
    }

    [Fact]
    public void Blocks_after_per_minute_limit()
    {
        var limiter = new MemoryEmailIntakeRateLimiter(new StubEmailIntakeSettings(new EmailIntakeEffectiveSettings
        {
            RateLimitPerMailboxPerMinute = 2,
            RateLimitPerMailboxPerHour = 100,
        }));
        var id = Guid.NewGuid();
        Assert.True(limiter.TryAcquire(id));
        Assert.True(limiter.TryAcquire(id));
        Assert.False(limiter.TryAcquire(id));
    }
}

public class IntakeAddressFormatterTests
{
    [Fact]
    public void Builds_local_part_under_64()
    {
        var local = IntakeAddressFormatter.BuildLocalPart("mcm", "invoice", "k7x9m2p4q8w1n3v6h4j2g8");
        Assert.Equal("mcm-invoice-k7x9m2p4q8w1n3v6h4j2g8", local);
        Assert.True(local.Length <= 64);
    }

    [Fact]
    public void Slugify_strips_junk()
    {
        Assert.Equal("woodvale-ap", IntakeAddressFormatter.Slugify("Woodvale AP!!!", "biz"));
    }
}

public class HeuristicEmailIntakeDecisionAgentTests
{
    private readonly HeuristicEmailIntakeDecisionAgent _agent = new();
    private readonly HashSet<string> _ext = [".pdf", ".txt"];

    [Fact]
    public void Prefers_attachments()
    {
        var decision = _agent.Decide(
            new EmailIntakeDecisionContext(
                false,
                "a@b.com",
                "Invoice",
                "see attached",
                [new EmailIntakeCandidateAttachment("inv.pdf", "application/pdf", [1, 2, 3])]),
            _ext);
        Assert.Equal(EmailIntakeDecisionAction.Process, decision.Action);
        Assert.Single(decision.Targets);
        Assert.Equal("attachment", decision.Targets[0].Kind);
    }

    [Fact]
    public void Body_only_when_substantial()
    {
        var body = string.Join('\n', Enumerable.Repeat("Invoice line item amount 100.00", 10));
        var decision = _agent.Decide(
            new EmailIntakeDecisionContext(false, "a@b.com", "Invoice", body, []),
            _ext);
        Assert.Equal(EmailIntakeDecisionAction.Process, decision.Action);
        Assert.Equal("body", decision.Targets[0].Kind);
    }

    [Fact]
    public void Ambiguous_rejects()
    {
        var decision = _agent.Decide(
            new EmailIntakeDecisionContext(false, "a@b.com", "Hi", "thanks", []),
            _ext);
        Assert.Equal(EmailIntakeDecisionAction.Reject, decision.Action);
        Assert.Equal("ambiguous_email", decision.RejectCode);
    }

    [Fact]
    public void Partial_skips_disallowed_keeps_good()
    {
        var decision = _agent.Decide(
            new EmailIntakeDecisionContext(
                false,
                "a@b.com",
                "sub",
                "short",
                [
                    new EmailIntakeCandidateAttachment("ok.pdf", "application/pdf", [1, 2, 3]),
                    new EmailIntakeCandidateAttachment("note.exe", "application/octet-stream", [1]),
                    new EmailIntakeCandidateAttachment("empty.pdf", "application/pdf", []),
                ]),
            _ext);

        Assert.Equal(EmailIntakeDecisionAction.ProcessPartial, decision.Action);
        Assert.Single(decision.Targets);
        Assert.Equal("ok.pdf", decision.Targets[0].FileName);
        Assert.Equal(2, decision.SkippedAttachments.Count);
        Assert.Contains(decision.SkippedAttachments, s => s.Reason == "extension_not_allowed");
        Assert.Contains(decision.SkippedAttachments, s => s.Reason == "empty_attachment");
    }

    [Fact]
    public void All_skipped_rejects_no_processable_attachments()
    {
        var decision = _agent.Decide(
            new EmailIntakeDecisionContext(
                false,
                "a@b.com",
                "sub",
                "too short",
                [new EmailIntakeCandidateAttachment("note.exe", null, [1])]),
            _ext);

        Assert.Equal(EmailIntakeDecisionAction.Reject, decision.Action);
        Assert.Equal("no_processable_attachments", decision.RejectCode);
    }
}
