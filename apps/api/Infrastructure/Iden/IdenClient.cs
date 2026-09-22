namespace Documate.Api.Infrastructure.Iden;

using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

/// <summary>Typed HTTP client for Iden product_service calls (Band 20). Uses ServiceClient token.</summary>
public sealed class IdenClient(
    IHttpClientFactory httpClientFactory,
    IOptions<IdenOptions> options,
    ILogger<IdenClient> logger) : IIdenClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly ConcurrentDictionary<string, (IdenServiceToken Token, DateTimeOffset ExpiresAt)> _tokenCache = new();

    public bool IsConfigured
    {
        get
        {
            var o = options.Value;
            return !string.IsNullOrWhiteSpace(o.BaseUrl)
                && !string.IsNullOrWhiteSpace(o.Documate.ClientId)
                && !string.IsNullOrWhiteSpace(o.Documate.ClientSecret);
        }
    }

    public async Task<IdenServiceToken> GetServiceTokenAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        const string cacheKey = "default";
        if (_tokenCache.TryGetValue(cacheKey, out var cached)
            && cached.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return cached.Token;
        }

        var client = CreateClient();
        var o = options.Value;
        using var response = await client.PostAsJsonAsync(
            "auth/service/token",
            new { clientId = o.Documate.ClientId, clientSecret = o.Documate.ClientSecret },
            JsonOptions,
            cancellationToken);

        await EnsureSuccess(response, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ServiceTokenResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Iden service token response was empty.");

        if (!string.Equals(body.SoftwareKey, o.SoftwareKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Iden software_key mismatch: expected '{o.SoftwareKey}', got '{body.SoftwareKey}'.");
        }

        var token = new IdenServiceToken(
            body.AccessToken,
            body.ExpiresIn,
            body.SoftwareId ?? "",
            body.SoftwareKey ?? o.SoftwareKey,
            body.Scopes ?? Array.Empty<string>());

        _tokenCache[cacheKey] = (token, DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, body.ExpiresIn - 60)));
        return token;
    }

    public async Task<IdenEnforceResult> EnforceCheckAsync(
        string buContextId,
        string capabilityKey,
        string? userAccessToken = null,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var client = CreateClient();
        var bearer = userAccessToken;
        if (string.IsNullOrWhiteSpace(bearer))
        {
            bearer = (await GetServiceTokenAsync(cancellationToken)).AccessToken;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "enforce/check")
        {
            Content = JsonContent.Create(
                new { buContextId, capabilityKey },
                options: JsonOptions),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

        using var response = await client.SendAsync(request, cancellationToken);
        await EnsureSuccess(response, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<EnforceResponse>(JsonOptions, cancellationToken)
            ?? new EnforceResponse();

        return new IdenEnforceResult(body.Allowed, body.Reason, body.BlockLevel, body.StageIndex);
    }

    public async Task<IdenCreateTenantResult> CreateTenantWithFirstBusinessAsync(
        string tenantName,
        string firstBusinessName,
        string? productId,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var token = await GetServiceTokenAsync(cancellationToken);
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "tenants")
        {
            Content = JsonContent.Create(
                new
                {
                    tenantName,
                    firstBusiness = new
                    {
                        name = firstBusinessName,
                        productId,
                    },
                },
                options: JsonOptions),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        await EnsureSuccess(response, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<CreateTenantResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Iden create tenant response empty.");

        var tenantId = body.TenantId ?? body.Id ?? "";
        var businessId = body.FirstBusinessId ?? body.BusinessId ?? body.FirstBusiness?.Id;
        return new IdenCreateTenantResult(
            tenantId,
            businessId,
            body.TenantName ?? tenantName,
            body.FirstBusiness?.Name ?? firstBusinessName);
    }

    public async Task<(string TenantId, string BusinessId)> CreateBusinessAsync(
        string tenantId,
        string businessName,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var token = await GetServiceTokenAsync(cancellationToken);
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"tenants/{tenantId}/businesses")
        {
            Content = JsonContent.Create(new { name = businessName }, options: JsonOptions),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        await EnsureSuccess(response, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<CreateBusinessResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Iden create business response empty.");

        return (tenantId, body.BusinessId ?? body.Id ?? "");
    }

    public async Task SyncCatalogFeaturesAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var catalog = FeatureCatalogDocument.LoadEmbeddedOrFile();
        var token = await GetServiceTokenAsync(cancellationToken);
        var client = CreateClient();

        // Resolve modules by key, then upsert features (create when missing).
        using var modulesReq = new HttpRequestMessage(HttpMethod.Get, "catalog/modules");
        modulesReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var modulesRes = await client.SendAsync(modulesReq, cancellationToken);
        await EnsureSuccess(modulesRes, cancellationToken);
        var modulesBody = await modulesRes.Content.ReadFromJsonAsync<ModulesListResponse>(JsonOptions, cancellationToken);
        var moduleByKey = (modulesBody?.Items ?? modulesBody?.Modules ?? Array.Empty<ModuleRow>())
            .Where(m => !string.IsNullOrWhiteSpace(m.ModuleKey) && !string.IsNullOrWhiteSpace(m.ModuleId))
            .ToDictionary(m => m.ModuleKey!, m => m.ModuleId!, StringComparer.OrdinalIgnoreCase);

        foreach (var feature in catalog.Features)
        {
            if (!moduleByKey.TryGetValue(feature.ModuleKey, out var moduleId))
            {
                logger.LogWarning(
                    "Iden catalog sync skipped FeatureKey {FeatureKey}: module {ModuleKey} not found (platform must seed modules first).",
                    feature.FeatureKey,
                    feature.ModuleKey);
                continue;
            }

            using var createReq = new HttpRequestMessage(HttpMethod.Post, "catalog/features")
            {
                Content = JsonContent.Create(
                    new
                    {
                        moduleId,
                        featureKey = feature.FeatureKey,
                        name = feature.Name,
                        capabilityTypeEnumKey = feature.CapabilityType,
                        sortOrder = feature.SortOrder,
                        isActive = feature.IsActive,
                    },
                    options: JsonOptions),
            };
            createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            using var createRes = await client.SendAsync(createReq, cancellationToken);
            if (createRes.IsSuccessStatusCode)
            {
                continue;
            }

            // Conflict / already exists — best-effort; log and continue.
            var err = await createRes.Content.ReadAsStringAsync(cancellationToken);
            logger.LogInformation(
                "Iden feature upsert for {FeatureKey} returned {Status}: {Body}",
                feature.FeatureKey,
                (int)createRes.StatusCode,
                err);
        }
    }

    public Task<IReadOnlyList<IdenMemberSummary>> ListBusinessMembersAsync(
        string tenantId,
        string businessId,
        CancellationToken cancellationToken = default)
    {
        // Wire-level list shapes vary; return empty until Iden UAT contract is exercised (DQ-2012 expands).
        _ = (tenantId, businessId, cancellationToken);
        EnsureConfigured();
        return Task.FromResult<IReadOnlyList<IdenMemberSummary>>(Array.Empty<IdenMemberSummary>());
    }

    public async Task<IdenMemberSummary> InviteInternalMemberAsync(
        string tenantId,
        string businessId,
        string email,
        string displayName,
        string roleSysKey,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var token = await GetServiceTokenAsync(cancellationToken);
        var client = CreateClient();

        using var idReq = new HttpRequestMessage(HttpMethod.Post, "identities")
        {
            Content = JsonContent.Create(
                new
                {
                    email,
                    displayName,
                    identityClass = "internal",
                },
                options: JsonOptions),
        };
        idReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var idRes = await client.SendAsync(idReq, cancellationToken);
        await EnsureSuccess(idRes, cancellationToken);
        var identity = await idRes.Content.ReadFromJsonAsync<IdentityResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Iden identity create empty.");

        var identityId = identity.IdentityId ?? identity.Id ?? "";
        using var memReq = new HttpRequestMessage(HttpMethod.Post, $"tenants/{tenantId}/businesses/{businessId}/members")
        {
            Content = JsonContent.Create(new { identityId }, options: JsonOptions),
        };
        memReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var memRes = await client.SendAsync(memReq, cancellationToken);
        await EnsureSuccess(memRes, cancellationToken);
        var member = await memRes.Content.ReadFromJsonAsync<MemberResponse>(JsonOptions, cancellationToken);
        var memberId = member?.MemberId ?? member?.Id ?? "";

        if (!string.IsNullOrWhiteSpace(roleSysKey) && !string.IsNullOrWhiteSpace(memberId))
        {
            await AssignMemberRoleAsync(tenantId, businessId, memberId, roleSysKey, cancellationToken);
        }

        using var inviteReq = new HttpRequestMessage(HttpMethod.Post, $"identities/{identityId}/invite");
        inviteReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        _ = await client.SendAsync(inviteReq, cancellationToken);

        return new IdenMemberSummary(memberId, identityId, email, displayName, roleSysKey, true);
    }

    public Task AssignMemberRoleAsync(
        string tenantId,
        string businessId,
        string memberId,
        string roleSysKey,
        CancellationToken cancellationToken = default)
    {
        // Role id resolution requires listing roles by SysKey — completed when UAT role seed ids known.
        _ = (tenantId, businessId, memberId, roleSysKey, cancellationToken);
        EnsureConfigured();
        logger.LogInformation(
            "AssignMemberRole queued for member {MemberId} role {Role} (roleId lookup pending UAT seed).",
            memberId,
            roleSysKey);
        return Task.CompletedTask;
    }

    public async Task DeactivateMemberAsync(
        string tenantId,
        string businessId,
        string memberId,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var token = await GetServiceTokenAsync(cancellationToken);
        var client = CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"tenants/{tenantId}/businesses/{businessId}/members/{memberId}")
        {
            Content = JsonContent.Create(new { isActive = false }, options: JsonOptions),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var response = await client.SendAsync(request, cancellationToken);
        await EnsureSuccess(response, cancellationToken);
    }

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("iden");
        var baseUrl = options.Value.BaseUrl.TrimEnd('/') + "/";
        client.BaseAddress = new Uri(baseUrl);
        return client;
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Iden is not configured. Set Iden:BaseUrl, Iden:Documate:ClientId, Iden:Documate:ClientSecret.");
        }
    }

    private static async Task EnsureSuccess(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            $"Iden HTTP {(int)response.StatusCode}: {body}");
    }

    private sealed class ServiceTokenResponse
    {
        public string AccessToken { get; set; } = "";
        public int ExpiresIn { get; set; }
        public string? SoftwareId { get; set; }
        public string? SoftwareKey { get; set; }
        public string[]? Scopes { get; set; }
    }

    private sealed class EnforceResponse
    {
        public bool Allowed { get; set; }
        public string? Reason { get; set; }
        public string? BlockLevel { get; set; }
        public int? StageIndex { get; set; }
    }

    private sealed class CreateTenantResponse
    {
        public string? Id { get; set; }
        public string? TenantId { get; set; }
        public string? TenantName { get; set; }
        public string? BusinessId { get; set; }
        public string? FirstBusinessId { get; set; }
        public CreateBusinessResponse? FirstBusiness { get; set; }
    }

    private sealed class CreateBusinessResponse
    {
        public string? Id { get; set; }
        public string? BusinessId { get; set; }
        public string? Name { get; set; }
    }

    private sealed class ModulesListResponse
    {
        public ModuleRow[]? Items { get; set; }
        public ModuleRow[]? Modules { get; set; }
    }

    private sealed class ModuleRow
    {
        public string? ModuleId { get; set; }
        public string? Id { get; set; }
        public string? ModuleKey { get; set; }
    }

    private sealed class IdentityResponse
    {
        public string? IdentityId { get; set; }
        public string? Id { get; set; }
    }

    private sealed class MemberResponse
    {
        public string? MemberId { get; set; }
        public string? Id { get; set; }
    }
}
