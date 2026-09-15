namespace Documate.Api.Infrastructure.Settings;

using System.Collections.Concurrent;
using System.Text.Json;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class MemoryCachedSystemSettings(IServiceScopeFactory scopeFactory) : ISystemSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);
    private volatile bool _loaded;

    public string? GetRawJson(string key)
    {
        EnsureLoaded();
        return _cache.TryGetValue(key, out var v) ? v : null;
    }

    public T GetOrDefault<T>(string key, T fallback)
    {
        var raw = GetRawJson(key);
        if (string.IsNullOrWhiteSpace(raw) || raw == "null")
        {
            return fallback;
        }

        try
        {
            var value = JsonSerializer.Deserialize<T>(raw, JsonOptions);
            return value is null ? fallback : value;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    public void Invalidate(string? key = null)
    {
        if (key is null)
        {
            _cache.Clear();
            _loaded = false;
            return;
        }

        _cache.TryRemove(key, out _);
    }

    public async Task UpsertAsync(
        string key,
        string valueJson,
        string? updatedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (!SystemSettingKeys.IsAllowlisted(key))
        {
            throw new InvalidOperationException($"Setting key '{key}' is not allowlisted.");
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DocumateDbContext>();
        var row = await db.CorSystemSettings.FirstOrDefaultAsync(x => x.SettingKey == key, cancellationToken);
        if (row is null)
        {
            row = new CorSystemSetting
            {
                SettingKey = key,
                ValueJson = valueJson,
                CreatedByUserId = updatedByUserId,
                UpdatedByUserId = updatedByUserId,
            };
            db.CorSystemSettings.Add(row);
        }
        else
        {
            row.ValueJson = valueJson;
            row.UpdatedByUserId = updatedByUserId;
        }

        await db.SaveChangesAsync(cancellationToken);
        _cache[key] = valueJson;
        _loaded = true;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return new Dictionary<string, string>(_cache, StringComparer.OrdinalIgnoreCase);
    }

    private void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        EnsureLoadedAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_loaded)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DocumateDbContext>();
        var rows = await db.CorSystemSettings.AsNoTracking()
            .Select(x => new { x.SettingKey, x.ValueJson })
            .ToListAsync(cancellationToken);
        foreach (var row in rows)
        {
            _cache[row.SettingKey] = row.ValueJson;
        }

        _loaded = true;
    }
}
