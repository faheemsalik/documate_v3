namespace Documate.Api.Tests;

using Documate.Api.Infrastructure.Persistence;
using Documate.Api.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class SystemSettingsTests
{
    [Fact]
    public void Allowlist_rejects_unknown_keys()
    {
        Assert.False(SystemSettingKeys.IsAllowlisted("EmailIntake:InboundWebhookSecret"));
        Assert.False(SystemSettingKeys.IsAllowlisted("Aws:SecretKey"));
        Assert.True(SystemSettingKeys.IsAllowlisted(SystemSettingKeys.EmailMimeRetentionDays));
        Assert.True(SystemSettingKeys.IsAllowlisted(SystemSettingKeys.PipelineIntelligenceT1ProviderKey));
        Assert.True(SystemSettingKeys.IsAllowlisted(SystemSettingKeys.PipelineExtractProviderKey));
    }

    [Fact]
    public async Task Seeder_is_idempotent_and_does_not_overwrite()
    {
        await using var db = CreateDb();
        var config = BuildConfig();

        await SystemSettingsSeeder.SeedMissingAsync(db, config);
        var firstCount = await db.CorSystemSettings.CountAsync();
        Assert.Equal(SystemSettingKeys.All.Count, firstCount);

        var row = await db.CorSystemSettings.SingleAsync(x => x.SettingKey == SystemSettingKeys.EmailMimeRetentionDays);
        row.ValueJson = "99";
        await db.SaveChangesAsync();

        await SystemSettingsSeeder.SeedMissingAsync(db, config);
        Assert.Equal(firstCount, await db.CorSystemSettings.CountAsync());
        Assert.Equal("99", (await db.CorSystemSettings.SingleAsync(x => x.SettingKey == SystemSettingKeys.EmailMimeRetentionDays)).ValueJson);
    }

    [Fact]
    public async Task Cache_upsert_rejects_unknown_key_and_invalidate_reloads()
    {
        await using var db = CreateDb();
        await SystemSettingsSeeder.SeedMissingAsync(db, BuildConfig());

        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton<IServiceScopeFactory>(sp => new SimpleScopeFactory(sp));
        var provider = services.BuildServiceProvider();
        var settings = new MemoryCachedSystemSettings(provider.GetRequiredService<IServiceScopeFactory>());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            settings.UpsertAsync("not:allowlisted", "1", "test"));

        Assert.Equal(30, settings.GetOrDefault(SystemSettingKeys.EmailMimeRetentionDays, 0));

        var tracked = await db.CorSystemSettings.SingleAsync(x => x.SettingKey == SystemSettingKeys.EmailMimeRetentionDays);
        tracked.ValueJson = "45";
        await db.SaveChangesAsync();

        // Cache still has seed value until invalidate.
        Assert.Equal(30, settings.GetOrDefault(SystemSettingKeys.EmailMimeRetentionDays, 0));
        settings.Invalidate();
        Assert.Equal(45, settings.GetOrDefault(SystemSettingKeys.EmailMimeRetentionDays, 0));

        await settings.UpsertAsync(SystemSettingKeys.EmailMimeRetentionDays, "60", "ops");
        Assert.Equal(60, settings.GetOrDefault(SystemSettingKeys.EmailMimeRetentionDays, 0));
    }

    private static DocumateDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<DocumateDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DocumateDbContext(options);
    }

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailIntake:DefaultDomain"] = "docsintake.com",
                ["EmailIntake:MaxAttachmentBytes"] = "100",
                ["EmailIntake:MaxTotalAttachmentBytes"] = "200",
                ["EmailIntake:MaxAttachments"] = "5",
                ["EmailIntake:AllowedExtensions:0"] = ".pdf",
                ["EmailIntake:RateLimitPerMailboxPerMinute"] = "10",
                ["EmailIntake:RateLimitPerMailboxPerHour"] = "100",
                ["EmailIntake:S3Bucket"] = "bucket",
                ["EmailIntake:S3Prefix"] = "inbound/",
                ["EmailIntake:AwsRegion"] = "us-east-1",
                ["EmailIntake:MimeRetentionDays"] = "30",
                ["EmailIntake:BodyExcerptMaxChars"] = "4096",
                ["Pipeline:SyncWaitTimeoutSeconds"] = "60",
                ["Pipeline:SyncMaxPages"] = "20",
                ["Pipeline:SyncMaxBytes"] = "1000000",
            })
            .Build();

    private sealed class SimpleScopeFactory(IServiceProvider root) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new SimpleScope(root);
    }

    private sealed class SimpleScope(IServiceProvider provider) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = provider;
        public void Dispose() { }
    }
}
