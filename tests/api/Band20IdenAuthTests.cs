using Documate.Api.Infrastructure.Iden;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Documate.Api.Infrastructure.Auth;

namespace Documate.Api.Tests;

public class Band20IdenAuthTests
{
    [Fact]
    public void FeatureCatalog_Seed_HasCustomerAndAdminModules()
    {
        var doc = FeatureCatalogSeed.Create();
        Assert.Contains(doc.Modules, m => m.ModuleKey == "customer.system");
        Assert.Contains(doc.Modules, m => m.ModuleKey == "admin.tenancy");
        Assert.Contains(doc.Features, f => f.FeatureKey == FeatureKeys.CustomerUsersList);
        Assert.Contains(doc.Features, f => f.FeatureKey == FeatureKeys.CustomerAgentsDelete);
    }

    [Fact]
    public void FeatureKeys_Constants_AreStable()
    {
        Assert.Equal("documate.customer.agents.list", FeatureKeys.CustomerAgentsList);
        Assert.Equal("documate.customer.users.permissions.manage", FeatureKeys.CustomerUsersPermissionsManage);
    }

    [Fact]
    public void TenancyWriteGuard_BlocksDivergent()
    {
        var biz = new Documate.Api.Domain.CorTenantBusiness
        {
            IdenBusinessId = "x",
            SyncStatus = TenancySyncStatuses.Divergent,
        };
        Assert.Throws<InvalidOperationException>(() => TenancyWriteGuard.EnsureWritable(biz));
    }

    [Fact]
    public void TenancyWriteGuard_AllowsOk()
    {
        var biz = new Documate.Api.Domain.CorTenantBusiness
        {
            IdenBusinessId = "x",
            SyncStatus = TenancySyncStatuses.Ok,
        };
        TenancyWriteGuard.EnsureWritable(biz);
    }

    [Fact]
    public async Task FeatureEnforce_DevBypass_AllowsWithoutBuContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.Configure<AuthOptions>(o => o.Mode = "DevBypass");
        services.Configure<IdenOptions>(_ => { });
        services.AddSingleton<IIdenClient, NullIdenClient>();
        services.AddScoped<IBusinessContext>(_ => new BusinessContext { IsAuthenticated = true });
        services.AddScoped<IFeatureEnforce, FeatureEnforce>();
        await using var sp = services.BuildServiceProvider();
        var enforce = sp.GetRequiredService<IFeatureEnforce>();
        var result = await enforce.CheckAsync(FeatureKeys.CustomerAgentsList);
        Assert.True(result.Allowed);
    }

    private sealed class NullIdenClient : IIdenClient
    {
        public bool IsConfigured => false;

        public Task<IdenServiceToken> GetServiceTokenAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IdenEnforceResult> EnforceCheckAsync(
            string buContextId,
            string capabilityKey,
            string? userAccessToken = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new IdenEnforceResult(false, "not_configured", null, null));

        public Task<IdenCreateTenantResult> CreateTenantWithFirstBusinessAsync(
            string tenantName,
            string firstBusinessName,
            string? productId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(string TenantId, string BusinessId)> CreateBusinessAsync(
            string tenantId,
            string businessName,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SyncCatalogFeaturesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<IdenMemberSummary>> ListBusinessMembersAsync(
            string tenantId,
            string businessId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IdenMemberSummary>>(Array.Empty<IdenMemberSummary>());

        public Task<IdenMemberSummary> InviteInternalMemberAsync(
            string tenantId,
            string businessId,
            string email,
            string displayName,
            string roleSysKey,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AssignMemberRoleAsync(
            string tenantId,
            string businessId,
            string memberId,
            string roleSysKey,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeactivateMemberAsync(
            string tenantId,
            string businessId,
            string memberId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
