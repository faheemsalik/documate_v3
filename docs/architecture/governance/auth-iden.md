# Auth × Iden (Band 20)

Retires the “auth wiring TBD” placeholder for **customer** auth when `Auth:Mode=Iden`.

## Localhost vs Iden (recommended)

| Surface | Localhost (default) | Iden-enabled env |
|---------|---------------------|------------------|
| Customer SPA (`apps/web`) | `env.json` → `"authMode": "interim"` → InterimFeGate (`admin`) | `"authMode": "iden"` + `idenBaseUrl` |
| Customer API `/api/app` | `Auth:Mode=DevBypass` in `appsettings.Development.json` (+ Secrets Manager InterimFeGate token) | `Auth:Mode=Iden` + `Iden:Jwt` / ServiceClient |
| Ops admin SPA (`apps/admin`) | **AdminGate** (`ops-admin`) — not Iden in Band 20 | Still AdminGate unless a later band migrates PlatformAdmin to Iden |

Having `idenBaseUrl` filled in does **not** switch the SPA to Iden unless `authMode` is `"iden"`. That lets you keep Iden URL ready while localhost still uses old gates.

### Why “old admins still worked” with `idenBaseUrl` set

1. **API still `Auth:Mode=DevBypass`** — InterimFeGate bearer tokens remain valid for `/api/app`.
2. **SPA may still have a prior Interim session** in `sessionStorage` (`documate.interimAccessToken`) — no re-login needed until Sign out.
3. **Ops admin is a separate gate** — AdminGate never went through Iden.

## Modes (API)

Set on the **API**, not in the Angular env file:

| Where | Key |
|-------|-----|
| Local file | `apps/api/appsettings.Development.json` → `"Auth": { "Mode": "DevBypass" \| "Iden" }` |
| Override | env var `Auth__Mode=Iden` |
| Secrets | AWS secret `documate/dev/api` can also supply `Auth:Mode` if merged |

| `Auth:Mode` | `/api/app` scheme | Login |
|-------------|-------------------|--------|
| `DevBypass` (non-prod) | DevBypass + InterimFeGate token via `POST /api/app/auth/login` | SPA `authMode=interim` |
| `Iden` | JwtBearer (`IdenJwt`) validating Iden JWKS/authority | SPA `authMode=iden` → Iden; Interim login returns 401 |

`/api/admin` remains **AdminGate** (ops). Band 20 did **not** move ops-admin to Iden — keep AdminGate on localhost (and typically everywhere until a PlatformAdmin×Iden band).

`/api/v1` remains **ApiKey** only.

## Customer SPA env (`apps/web/public/env.json`)

```json
{
  "apiBaseUrl": "http://localhost:5172",
  "authMode": "interim",
  "idenBaseUrl": "http://localhost:5301/api/v1",
  "idenSoftwareKey": "…"
}
```

To try Iden login locally: set `"authMode": "iden"`, set API `Auth:Mode=Iden` + `Iden:*` Jwt/ServiceClient, hard-refresh / Sign out, then log in with an Iden identity that has a BuContext for your business.

## Shifting ops-admin to Iden (future)

Not implemented in Band 20. Today:

1. Localhost: keep `Auth:AdminGate` (username/password/token in Secrets Manager).
2. Later: PlatformAdmin would authenticate via Iden (ops identity class / software scopes) and retire AdminGate — requires a dedicated plan (out of Band 20).

## Claim → `IBusinessContext`

| Claim | Context |
|-------|---------|
| `sub` | `UserId` |
| `tenant_id` | `TenantId` |
| `tenant_business_id` | `BusinessId` |
| `bu_context_id` (dual-read `context_id`) | `BuContextId` |
| `identity_class` | `IdentityClass` |
| `tenant_name` / `business_name` | names (mirror) |

## Enforce

`IFeatureEnforce.EnsureAllowedAsync(FeatureKeys.…)` on privileged handlers. Cache ≤30s. Fail-closed when Mode=Iden and Iden unreachable.
