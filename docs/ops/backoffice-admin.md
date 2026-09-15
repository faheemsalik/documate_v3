# Documate back office admin (Band 17)

## Run locally

| Launcher | What |
|----------|------|
| VS Code **Admin: Chrome** | Starts `run-admin` task + opens http://localhost:4203 |
| VS Code **Full stack: API + Admin** | API + admin together |
| Task **run-admin** / **script: run-admin** | `scripts/run-admin.ps1` only |
| Task **build-admin** / **script: build-admin** | `scripts/build-admin.ps1` |

1. API on `http://localhost:5172` with `Auth:AdminGate` enabled (`appsettings.Development.json`).
2. From repo root: `.\scripts\run-admin.ps1` (port **4203**; proxies `/api` to the API).
3. Sign in with AdminGate username/password (`ops-admin` / configured password).
4. Shell loads `GET /api/admin/me` after login.

## Auth

- Scheme: `AdminGate` for `/api/admin/*`
- Policy: `PlatformAdmin` (claim `documate_platform_admin`)
- Customer `InterimFeGate` is separate — do not reuse customer tokens for admin.

## Admin API surface (Band 17)

| Area | Endpoints |
|------|-----------|
| Me | `GET /api/admin/me` |
| Ops | `GET /api/admin/ops/files`, `GET /api/admin/ops/documents` |
| Analytics | `GET /api/admin/analytics/{summary,volume,by-business,hourly,stage-timings}` |
| Tenants | `GET/POST /api/admin/tenants`, `GET /api/admin/tenants/{id}` |
| Businesses | `GET /api/admin/businesses`, `GET /api/admin/businesses/{businessId}` |
| Support | `GET /api/admin/support/lookup?q=` |
| Monitoring | `GET /api/admin/monitoring/snapshot` |
| Settings | `GET/PUT /api/admin/system-settings` (Plan 15 keys) |

Config: `Admin:HangfireDashboardUrl` (default `/hangfire`), optional `Admin:DatadogDashboardUrl`.

Analytics buckets are **UTC**. Stage timings are **approximate** (compute-on-read from work events).

## Screens

| Route | Screen |
|-------|--------|
| `/dashboard` | INT-01 |
| `/ops` | INT-20 (query: `businessId`, `tab`, `date`) |
| `/support` | INT-11 |
| `/tenants`, `/tenants/:id` | INT-02 / INT-03 |
| `/businesses`, `/businesses/:businessId` | INT-04 / INT-05 |
| `/monitoring` | INT-09 |
| `/settings` | INT-08 |

## Publish / deploy

**Yes — publish admin separately from the customer web app.**

| Artifact | Script / output | Typical host |
|----------|-----------------|--------------|
| API | `scripts/api_publish-release.ps1` → IIS API site | e.g. `api2.documate.ai` |
| Customer SPA | `scripts/build-web.ps1 -Configuration production` → `apps/web/dist/...` | e.g. `app.documate.ai` |
| Admin SPA | `scripts/build-admin.ps1 -Configuration production` → `apps/admin/dist/admin` | e.g. `admin.documate.ai` (or internal host) |

Admin is its own Angular build (static files + `env.json` for API origin). It is **not** included in the API zip. Deploy it as a second static site (or second IIS site) with its own `env.json` pointing at the same API. Keep AdminGate credentials out of the customer portal deploy.
