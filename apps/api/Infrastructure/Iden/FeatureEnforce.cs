namespace Documate.Api.Infrastructure.Iden;

using System.Collections.Concurrent;
using Documate.Api.Infrastructure.Auth;
using Microsoft.Extensions.Options;

public interface IFeatureEnforce
{
    Task EnsureAllowedAsync(string capabilityKey, CancellationToken cancellationToken = default);

    Task<IdenEnforceResult> CheckAsync(string capabilityKey, CancellationToken cancellationToken = default);
}

/// <summary>Calls Iden /enforce/check. Fail-closed for mutations when Iden configured; DevBypass allows all.</summary>
public sealed class FeatureEnforce(
    IIdenClient iden,
    IBusinessContext business,
    IHttpContextAccessor httpContextAccessor,
    IOptions<AuthOptions> authOptions,
    ILogger<FeatureEnforce> logger) : IFeatureEnforce
{
    private static readonly ConcurrentDictionary<string, (IdenEnforceResult Result, DateTimeOffset Expires)> Cache = new();

    public async Task EnsureAllowedAsync(string capabilityKey, CancellationToken cancellationToken = default)
    {
        var result = await CheckAsync(capabilityKey, cancellationToken);
        if (!result.Allowed)
        {
            throw new FeatureDeniedException(capabilityKey, result.Reason ?? "permission_denied");
        }
    }

    public async Task<IdenEnforceResult> CheckAsync(string capabilityKey, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(authOptions.Value.Mode, "Iden", StringComparison.OrdinalIgnoreCase)
            || !iden.IsConfigured)
        {
            // Local DevBypass / unconfigured Iden: allow (Phase 1 bridge).
            return new IdenEnforceResult(true, null, null, null);
        }

        var buContextId = business.BuContextId
            ?? httpContextAccessor.HttpContext?.User.FindFirst(AuthClaimTypes.BuContextId)?.Value
            ?? httpContextAccessor.HttpContext?.User.FindFirst("bu_context_id")?.Value
            ?? httpContextAccessor.HttpContext?.User.FindFirst("context_id")?.Value;

        if (string.IsNullOrWhiteSpace(buContextId))
        {
            logger.LogWarning("Enforce denied: missing bu_context_id for {CapabilityKey}", capabilityKey);
            return new IdenEnforceResult(false, "context_missing", null, null);
        }

        var cacheKey = $"{buContextId}|{capabilityKey}";
        if (Cache.TryGetValue(cacheKey, out var cached) && cached.Expires > DateTimeOffset.UtcNow)
        {
            return cached.Result;
        }

        try
        {
            var userToken = ReadBearer();
            var result = await iden.EnforceCheckAsync(buContextId, capabilityKey, userToken, cancellationToken);
            Cache[cacheKey] = (result, DateTimeOffset.UtcNow.AddSeconds(30));
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Iden enforce failed for {CapabilityKey} — fail-closed", capabilityKey);
            return new IdenEnforceResult(false, "iden_unreachable", "full", null);
        }
    }

    private string? ReadBearer()
    {
        var header = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return header["Bearer ".Length..].Trim();
    }
}

public sealed class FeatureDeniedException(string capabilityKey, string reason) : Exception($"Feature denied: {capabilityKey} ({reason})")
{
    public string CapabilityKey { get; } = capabilityKey;
    public string Reason { get; } = reason;
}
