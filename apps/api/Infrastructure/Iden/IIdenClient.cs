namespace Documate.Api.Infrastructure.Iden;

public static class TenancySyncStatuses
{
    public const string Ok = "ok";
    public const string Pending = "pending";
    public const string Divergent = "divergent";
}

public sealed record IdenServiceToken(
    string AccessToken,
    int ExpiresIn,
    string SoftwareId,
    string SoftwareKey,
    IReadOnlyList<string> Scopes);

public sealed record IdenEnforceResult(
    bool Allowed,
    string? Reason,
    string? BlockLevel,
    int? StageIndex);

public sealed record IdenCreateTenantResult(
    string TenantId,
    string? BusinessId,
    string TenantName,
    string? BusinessName);

public interface IIdenClient
{
    bool IsConfigured { get; }

    Task<IdenServiceToken> GetServiceTokenAsync(CancellationToken cancellationToken = default);

    Task<IdenEnforceResult> EnforceCheckAsync(
        string buContextId,
        string capabilityKey,
        string? userAccessToken = null,
        CancellationToken cancellationToken = default);

    Task<IdenCreateTenantResult> CreateTenantWithFirstBusinessAsync(
        string tenantName,
        string firstBusinessName,
        string? productId,
        CancellationToken cancellationToken = default);

    Task<(string TenantId, string BusinessId)> CreateBusinessAsync(
        string tenantId,
        string businessName,
        CancellationToken cancellationToken = default);

    Task SyncCatalogFeaturesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IdenMemberSummary>> ListBusinessMembersAsync(
        string tenantId,
        string businessId,
        CancellationToken cancellationToken = default);

    Task<IdenMemberSummary> InviteInternalMemberAsync(
        string tenantId,
        string businessId,
        string email,
        string displayName,
        string roleSysKey,
        CancellationToken cancellationToken = default);

    Task AssignMemberRoleAsync(
        string tenantId,
        string businessId,
        string memberId,
        string roleSysKey,
        CancellationToken cancellationToken = default);

    Task DeactivateMemberAsync(
        string tenantId,
        string businessId,
        string memberId,
        CancellationToken cancellationToken = default);
}

public sealed record IdenMemberSummary(
    string MemberId,
    string IdentityId,
    string Email,
    string DisplayName,
    string? RoleSysKey,
    bool IsActive);
