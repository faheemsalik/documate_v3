namespace Documate.Api.Tests;

using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Persistence.Seeding;

public class CorEnumResolverTests
{
    [Fact]
    public void Seed_catalog_has_required_phase1_types()
    {
        var keys = CorEnumSeedCatalog.Types.Select(t => t.TypeKey).ToHashSet();
        Assert.Contains("provider_mode", keys);
        Assert.Contains("file_public_status", keys);
        Assert.Contains("document_public_status", keys);
        Assert.Contains("webhook_delivery_status", keys);
    }

    [Fact]
    public void Resolver_Require_throws_when_missing()
    {
        var resolver = new CorEnumIdResolver();
        Assert.Throws<InvalidOperationException>(() => resolver.Require("provider_mode", "mode_1"));
    }

    [Fact]
    public void Resolver_Require_returns_seeded_id()
    {
        var resolver = new CorEnumIdResolver();
        resolver.ReplaceAll(new Dictionary<(string, string), long>
        {
            [("provider_mode", "mode_1")] = 42,
        });
        Assert.Equal(42, resolver.Require("provider_mode", "mode_1"));
    }
}
