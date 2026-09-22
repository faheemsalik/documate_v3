# Iden constraints (engineering)

Iden is the external identity product. Documate integrates with it; Documate does **not** become a second identity island.

**Band 20** implements live wiring — see [auth-iden.md](./auth-iden.md) and [patterns/iden-tenancy-users-features.md](../patterns/iden-tenancy-users-features.md).

## Iden tenancy model (given)

Iden provides **two levels**:

1. **Tenant** — top organization.
2. **Business** — child under a Tenant.

Users and membership across Businesses are owned by Iden. Documate consumes validated context; it only mirrors Tenant/Business for Documate-specific settings (`CorTenant`, `CorTenantBusiness`) with `SyncStatus` / `LastSyncedAtUtc` (DR-SYNC-1 A).

**Documate isolation unit = Business** — operational rows carry **`BusinessId` only** (`IdenBusinessId`). Tenant linkage and **`TenantName` projection** live on `CorTenantBusiness`.

## MUST

1. Human authentication goes through **Iden** when `Auth:Mode=Iden` (JwtBearer + BuContext claims).
2. Tenant **and Business** + user identity in API handlers comes from validated tokens / `IBusinessContext` (`tenant_id`, `tenant_business_id`, `bu_context_id` / dual-read `context_id`, `sub`, `identity_class`).
3. Handlers scope data by **Business** (never Tenant-flat across all Businesses unless an explicit cross-business admin feature is designed).
4. **FeatureKeys** are code constants + `Iden/feature-catalog.json`; enforce via `IFeatureEnforce` → Iden `/enforce/check` (fail-closed when Mode=Iden).
5. Machine integration keys for External `/api/v1` are **Documate-owned** per business (DR-KEY-1 A). Documate ServiceClient is product/CI only — never a customer business API key.
6. Admin tenancy create is **Iden-first** (`POST /api/admin/tenants` with blank `IdenTenantId` when ServiceClient configured) — **no local Guid mint** for Iden ids.

## MUST NOT

1. Do not create a parallel user/password store in Documate for product login.
2. Do not invent a third org hierarchy that duplicates Iden Tenant/Business.
3. Do not invent claim names or FeatureKeys in random features — use `FeatureKeys` + catalog.
4. Do not accept ApiKey scheme on `/api/app/*` (DQ-2015).
5. Do not write product data when `CorTenantBusiness.SyncStatus=divergent` (DR-SYNC-1 A).

## Related

- [auth-iden.md](./auth-iden.md) — modes, claims, SPA split
- [auth-wiring-placeholder.md](./auth-wiring-placeholder.md) — historical Phase 1 bridge notes (superseded for Mode=Iden)
- Plan 25 Band 20 dispatch queue
