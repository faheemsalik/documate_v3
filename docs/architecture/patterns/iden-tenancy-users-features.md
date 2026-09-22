# Pattern: Iden tenancy, users, and FeatureKeys

## Tenancy mirror

1. Iden Guids are source of truth for `CorTenant.IdenTenantId` / `CorTenantBusiness.IdenBusinessId`.
2. On authenticated app requests, `TenantBusinessProvisioner` write-through creates/updates mirror rows (`SyncStatus=ok`).
3. Hangfire `iden-tenancy-reconcile` marks divergent ids and timestamps.
4. `TenancyWriteGuard` blocks product writes when divergent.

### Admin create (Iden-first)

`POST /api/admin/tenants` with **blank** `IdenTenantId` → ServiceClient creates tenant+business in Iden → mirror.  
Linking existing: pass `IdenTenantId` + `InitialIdenBusinessId` (no Guid invent).

Admin UI: `apps/admin` Tenants → Create (labels match Iden-first).

## Feature catalog

- Manifest: `apps/api/Iden/feature-catalog.json` (+ `FeatureKeys` / `FeatureCatalogSeed` in code).
- Sync: `POST /api/admin/iden/catalog/sync` (PlatformAdmin) → ServiceClient upserts features.
- Modules: 3 customer + 3 admin (see plan 25 catalog).

### Adding a FeatureKey

1. Add constant to `FeatureKeys`.
2. Add row to catalog JSON / seed.
3. Sync to Iden.
4. Call `IFeatureEnforce` in the handler.
5. Grant via role templates in Iden (`documate.admin|editor|files_operator|viewer`).

## Users & permissions

- API: `api/app/users` (list / invite / role / deactivate) — orchestrates Iden members.
- UI: `/users`, `/users/:id` (mockup: `docs/plans/25-users-permissions-ui-mockup.md`).
- Access is **role template**, not free-form FeatureKey matrix (MVP).

## External keys (3A)

- Managed in customer UI `/api-keys` → Documate tables bound to `IdenBusinessId`.
- Valid only on `/api/v1`. Never on `/api/app`.
