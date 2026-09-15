namespace Documate.Api.Tests;

using Documate.Api.Infrastructure.EmailIntake;

public class EmailIntakeMimeRetentionTests
{
    [Fact]
    public void NormalizePrefix_adds_trailing_slash()
    {
        Assert.Equal("inbound/", EmailIntakeMimeRetentionService.NormalizePrefix("inbound"));
        Assert.Equal("inbound/", EmailIntakeMimeRetentionService.NormalizePrefix("inbound/"));
        Assert.Equal("", EmailIntakeMimeRetentionService.NormalizePrefix(null));
    }

    [Theory]
    [InlineData(40, 30, true)]
    [InlineData(10, 30, false)]
    [InlineData(30, 30, true)]
    public void IsExpired_uses_cutoff(int ageDays, int retentionDays, bool expired)
    {
        var now = DateTimeOffset.Parse("2026-09-14T12:00:00Z");
        var lastMod = now.AddDays(-ageDays);
        var cutoff = now.AddDays(-retentionDays);
        Assert.Equal(expired, EmailIntakeMimeRetentionService.IsExpired(lastMod, cutoff));
    }
}
