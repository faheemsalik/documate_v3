namespace Documate.Api.Infrastructure.Auth;

using System.Security.Cryptography;
using System.Text;
using Documate.Api.Domain;
using Documate.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public interface IApiKeyService
{
    Task<CreatedApiKeyResult> CreateAsync(string name, DateTimeOffset? expiresAt, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApiKeyListItem>> ListAsync(CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CorTenantApiKey?> ValidateRawKeyAsync(string rawKey, CancellationToken cancellationToken = default);
}

public sealed record CreatedApiKeyResult(Guid Id, string Name, string KeyPrefix, string RawKey, DateTimeOffset? ExpiresAt);
public sealed record ApiKeyListItem(Guid Id, string Name, string KeyPrefix, bool IsActive, DateTimeOffset? ExpiresAt, DateTimeOffset? LastUsedAt, DateTimeOffset CreatedAt);

public sealed class ApiKeyService(DocumateDbContext db, IBusinessContext business) : IApiKeyService
{
    public const string KeyHeaderName = "X-Api-Key";

    public async Task<CreatedApiKeyResult> CreateAsync(
        string name,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        var prefixToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        var prefix = $"dm_{prefixToken}";
        var rawKey = $"{prefix}_{secret}";

        var row = new CorTenantApiKey
        {
            BusinessId = business.BusinessId,
            Name = name.Trim(),
            KeyPrefix = prefix,
            KeyHash = HashKey(rawKey),
            IsActive = true,
            ExpiresAt = expiresAt,
            CreatedByUserId = business.UserId,
            UpdatedByUserId = business.UserId,
        };

        db.CorTenantApiKeys.Add(row);
        await db.SaveChangesAsync(cancellationToken);

        return new CreatedApiKeyResult(row.Id, row.Name, row.KeyPrefix, rawKey, row.ExpiresAt);
    }

    public async Task<IReadOnlyList<ApiKeyListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await db.CorTenantApiKeys.AsNoTracking()
            .Where(k => k.BusinessId == business.BusinessId && !k.IsDeleted)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new ApiKeyListItem(
                k.Id,
                k.Name,
                k.KeyPrefix,
                k.IsActive,
                k.ExpiresAt,
                k.LastUsedAt,
                k.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await db.CorTenantApiKeys.FirstOrDefaultAsync(
            k => k.Id == id && k.BusinessId == business.BusinessId && !k.IsDeleted,
            cancellationToken);
        if (row is null)
        {
            return false;
        }

        row.IsActive = false;
        row.IsDeleted = true;
        row.DeletedAt = DateTimeOffset.UtcNow;
        row.DeletedByUserId = business.UserId;
        row.UpdatedByUserId = business.UserId;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<CorTenantApiKey?> ValidateRawKeyAsync(string rawKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return null;
        }

        rawKey = rawKey.Trim();
        var underscore = rawKey.LastIndexOf('_');
        if (underscore <= 0)
        {
            return null;
        }

        // prefix is dm_{8hex} — everything before the final _secret segment
        var prefix = rawKey[..underscore];
        if (prefix.Length > 32 || !prefix.StartsWith("dm_", StringComparison.Ordinal))
        {
            return null;
        }

        var candidates = await db.CorTenantApiKeys
            .Where(k => k.KeyPrefix == prefix && k.IsActive && !k.IsDeleted)
            .ToListAsync(cancellationToken);

        var hash = HashKey(rawKey);
        var match = candidates.FirstOrDefault(k =>
            FixedTimeEquals(k.KeyHash, hash)
            && (k.ExpiresAt is null || k.ExpiresAt > DateTimeOffset.UtcNow));

        if (match is null)
        {
            return null;
        }

        match.LastUsedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return match;
    }

    public static string HashKey(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}
