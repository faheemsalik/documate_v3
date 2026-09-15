namespace Documate.Api.Infrastructure.Persistence.Seeding;

using Microsoft.EntityFrameworkCore;

/// <summary>Ensures system CorEnum seeds exist and refreshes the Id resolver cache.</summary>
public sealed class CorEnumSeedHostedService(
    IServiceScopeFactory scopeFactory,
    CorEnumIdResolver resolver) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DocumateDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        await CorEnumSeeder.SeedSystemAsync(db, cancellationToken);
        var map = await CorEnumIdResolver.LoadMapAsync(db, cancellationToken);
        resolver.ReplaceAll(map);
        await PlatformCatalogSeeder.SeedAsync(db, resolver, cancellationToken);
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        await Documate.Api.Infrastructure.Settings.SystemSettingsSeeder.SeedMissingAsync(db, config, cancellationToken);
        scope.ServiceProvider.GetRequiredService<Documate.Api.Infrastructure.Settings.ISystemSettings>().Invalidate();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
