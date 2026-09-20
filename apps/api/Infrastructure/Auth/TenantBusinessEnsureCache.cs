namespace Documate.Api.Infrastructure.Auth;

using System.Collections.Concurrent;

/// <summary>
/// Process-lifetime cache of tenant/business keys that have already been provisioned
/// (CorTenant / CorTenantBusiness / default queue / default workflow).
/// Skips repeated ensure SQL on every authenticated request.
/// </summary>
public sealed class TenantBusinessEnsureCache
{
    private readonly ConcurrentDictionary<string, byte> _ensured = new(StringComparer.Ordinal);

    public bool IsEnsured(string tenantId, string businessId) =>
        _ensured.ContainsKey(Key(tenantId, businessId));

    public void MarkEnsured(string tenantId, string businessId) =>
        _ensured.TryAdd(Key(tenantId, businessId), 0);

    private static string Key(string tenantId, string businessId) => $"{tenantId}|{businessId}";
}
