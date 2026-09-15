namespace Documate.Api.Infrastructure.Settings;

public interface ISystemSettings
{
    string? GetRawJson(string key);

    T GetOrDefault<T>(string key, T fallback);

    void Invalidate(string? key = null);

    Task UpsertAsync(string key, string valueJson, string? updatedByUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default);
}
