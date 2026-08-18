namespace Documate.Api.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Resolves CorEnum Ids for domain comparisons. Only place EnumKey is used for logic binding.
/// </summary>
public interface ICorEnumIdResolver
{
    long Require(string enumTypeKey, string enumKey);
    bool TryGet(string enumTypeKey, string enumKey, out long id);
}

public sealed class CorEnumIdResolver : ICorEnumIdResolver
{
    private readonly Dictionary<(string TypeKey, string EnumKey), long> _map = new();
    private readonly object _gate = new();

    public long Require(string enumTypeKey, string enumKey)
    {
        if (TryGet(enumTypeKey, enumKey, out var id))
        {
            return id;
        }

        throw new InvalidOperationException(
            $"CorEnum not found for type '{enumTypeKey}' key '{enumKey}'. Ensure system seeds ran.");
    }

    public bool TryGet(string enumTypeKey, string enumKey, out long id)
    {
        lock (_gate)
        {
            return _map.TryGetValue((enumTypeKey, enumKey), out id);
        }
    }

    public void ReplaceAll(IReadOnlyDictionary<(string TypeKey, string EnumKey), long> map)
    {
        lock (_gate)
        {
            _map.Clear();
            foreach (var kv in map)
            {
                _map[kv.Key] = kv.Value;
            }
        }
    }

    public static async Task<Dictionary<(string TypeKey, string EnumKey), long>> LoadMapAsync(
        DocumateDbContext db,
        CancellationToken cancellationToken = default)
    {
        var rows = await (
            from e in db.CorEnums.AsNoTracking()
            join t in db.CorEnumTypes.AsNoTracking() on e.TypeId equals t.Id
            where e.BusinessId == null
            select new { t.EnumTypeKey, e.EnumKey, e.Id }
        ).ToListAsync(cancellationToken);

        return rows.ToDictionary(x => (x.EnumTypeKey, x.EnumKey), x => x.Id);
    }
}
