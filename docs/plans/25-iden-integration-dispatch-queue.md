# Documate v3 — Iden Integration — Dispatch Queue

> **Document type:** Dispatch queue (Phase 3)  
> **Status:** ✅ **Band 20 complete** — batch executed 2026-09-22  
> **Source plan:** [25-iden-integration-implementation-plan.md](./25-iden-integration-implementation-plan.md)  
> **Upstream:** [25-iden-integration-exploration.md](./25-iden-integration-exploration.md)  
> **Band 20** = Documate × Iden (auth, tenancy mirror, FeatureKeys, users/permissions, External keys)  
> **Replaces:** Plan 03 Band 15 stubs `DQ-1501–1507` (cleanup in DQ-2017)

**Status legend:** ✅ Complete · 🔄 In Progress · ⬜ Ready · ⏸ Parked · ❌ Cancelled

API vs Web are **separate** items. Do not mix them in one DQ. Execute **one** DQ at a time unless batched.

---

## Completion Summary

| Metric | Value |
|--------|--------|
| Total DQ items | 17 |
| ✅ Complete | 17 |
| 🔄 In Progress | 0 |
| ⬜ Ready | 0 |
| ⏸ Parked | 0 |
| ❌ Cancelled | 0 |

### Finalized decisions

- Exploration locks + **DR-SYNC-1 A**, **DR-KEY-1 A**
- Catalog: 3 customer modules + 3 admin modules; role templates; Users & permissions page with **mockup gate (W6 → W6b)**
- 3A: Documate owns per-business integration keys; ServiceClient = product/CI only
- Customer app = internal + BuContext + roles; business-wide data

### Pending decisions

None.

### Assumptions

- Iden UAT/local + ServiceClient credentials required to exercise live Iden paths (client/token/sync/enforce/invite)
- Platform seeds modules before feature sync (`POST /api/admin/iden/catalog/sync`)
- `SessionPolicies:BySoftwareKey:documate` agreed with ops

### Risks

| Risk | Mitigation |
|------|------------|
| Iden contract drift | Follow For-Integrators README + api-contracts; file Iden defects |
| Live UAT not configured in local DevBypass | Mode=Iden + `Iden:*` secrets for full path |
| Band 15 duplicate | DQ-2017 cancelled Band 15 stubs |

---

## Dispatch Index

| DQ | Band | Title | Status | Depends on |
|----|------|--------|--------|------------|
| **DQ-2001** | 20 | API: Iden client + secrets/options + service token | ✅ | — |
| **DQ-2002** | 20 | API: JWT/BuContext → `IBusinessContext`; `Auth:Mode=Iden` | ✅ | DQ-2001 |
| **DQ-2003** | 20 | API: Tenancy mirror SyncStatus + write-through provisioner | ✅ | DQ-2002 |
| **DQ-2004** | 20 | API: Reconcile Hangfire job (auto-create; DR-SYNC-1 A) | ✅ | DQ-2003 |
| **DQ-2005** | 20 | API: Admin create tenant/business Iden-first | ✅ | DQ-2003 |
| **DQ-2006** | 20 | Admin FE: tenant/business create forms | ✅ | DQ-2005 |
| **DQ-2007** | 20 | API: App auth endpoints for Iden session bridge (if needed) | ✅ | DQ-2002 |
| **DQ-2008** | 20 | Customer FE: Iden login + BuContext select + silent refresh | ✅ | DQ-2002 |
| **DQ-2009** | 20 | API: Feature catalog manifest + CI sync tool + role seed | ✅ | DQ-2001 |
| **DQ-2010** | 20 | API: `IFeatureEnforce` wrapper | ✅ | DQ-2001, DQ-2009 |
| **DQ-2011** | 20 | Design: Users & permissions **mockup** (list + edit) — approve gate | ✅ | DQ-2009 |
| **DQ-2012** | 20 | API: Users invite / role assign / deactivate | ✅ | DQ-2010, DQ-2011 |
| **DQ-2013** | 20 | Customer FE: `/users` + `/users/:id` per approved mockup | ✅ | DQ-2012 |
| **DQ-2014** | 20 | API: Enforce FeatureKeys on privileged FrontendSupport handlers | ✅ | DQ-2010, DQ-2013 |
| **DQ-2015** | 20 | API: External API key harden (`/api/app` ≠ ApiKey); 3A docs | ✅ | DQ-2003 |
| **DQ-2016** | 20 | Docs: architecture Iden pack (constraints, auth-iden, pattern, critical-rules) | ✅ | DQ-2009 |
| **DQ-2017** | 20 | Cleanup Band 15 DQ-1501–1507 + auth regression suite | ✅ | DQ-2008, DQ-2014, DQ-2015 |

**Suggested next:** Band complete — configure Iden secrets and smoke Mode=Iden against UAT.

**Critical path:** completed in batch 2026-09-22.

---

## Wave / DQ Entries

### DQ-2001 — API: Iden client + secrets/options + service token

- **Status:** ✅ Complete  
- **Dependency:** —  
- **Source:** Impl W1; Iden For-Integrators README + api-contracts §1.1 / §1.24  
- **Outcome:** `Infrastructure/Iden` client; `Iden__*` config/secrets; acquire/cache service JWT (`product_service`); assert `software_key=documate`  
- **Required Documents:** Impl plan Domain Architecture; Documate × Iden guide §15–16  
- **Evidence:** `IdenClient`, `IdenOptions`, `appsettings.Development.json` Iden section; `AddDocumateIden`

---

### DQ-2002 — API: JWT/BuContext → IBusinessContext

- **Status:** ✅ Complete  
- **Dependency:** DQ-2001  
- **Source:** Impl W1; guide §3–5  
- **Outcome:** JwtBearer + JWKS; map `bu_context_id` (dual-read `context_id`), `tenant_id`, `tenant_business_id`, `identity_class`, `sub`; `Auth:Mode=Iden` vs `DevBypass`; extend `IBusinessContext`  
- **Required Documents:** Impl plan claim map; guide §3  
- **Evidence:** `AddDocumateIdenJwt`; `BusinessContext` BuContextId/IdentityClass; Program policy forward

---

### DQ-2003 — API: Tenancy mirror SyncStatus + write-through

- **Status:** ✅ Complete  
- **Dependency:** DQ-2002  
- **Source:** Impl W2; DR-SYNC-1 A  
- **Outcome:** Migration `LastSyncedAtUtc` / `SyncStatus`; provisioner write-through from Iden Guids only; **no local Guid mint** for Iden ids  
- **Required Documents:** Impl tenancy mirror table  
- **Evidence:** `20260922170000_Band20IdenTenancySync`; `TenantBusinessProvisioner`; `TenancyWriteGuard`

---

### DQ-2004 — API: Reconcile Hangfire job

- **Status:** ✅ Complete  
- **Dependency:** DQ-2003  
- **Source:** Impl W2; exploration S1–S6  
- **Outcome:** Recurring job marks divergent / updates sync metadata; write block when divergent  
- **Required Documents:** Impl F6; DR-SYNC-1  
- **Evidence:** `TenancyReconcileJobs` + Hangfire `iden-tenancy-reconcile`; `Band20IdenAuthTests` write guard

---

### DQ-2005 — API: Admin create tenant/business Iden-first

- **Status:** ✅ Complete  
- **Dependency:** DQ-2003  
- **Source:** Impl W3; F1–F2  
- **Outcome:** PlatformAdmin create calls Iden then mirror; link path requires `InitialIdenBusinessId`  
- **Required Documents:** Impl F1–F2; guide §6  
- **Evidence:** `CreateAdminTenantHandler` + `IIdenClient.CreateTenantWithFirstBusinessAsync`

---

### DQ-2006 — Admin FE: tenant/business create

- **Status:** ✅ Complete  
- **Dependency:** DQ-2005  
- **Source:** Impl W3  
- **Outcome:** Admin SPA forms Iden-first (blank IdenTenantId); no client Guid invent  
- **Required Documents:** Impl W3  
- **Evidence:** `apps/admin` tenants create dialog labels + `initialIdenBusinessId`

---

### DQ-2007 — API: App auth bridge (if needed)

- **Status:** ✅ Complete  
- **Dependency:** DQ-2002  
- **Source:** Impl W4  
- **Outcome:** `GET /api/app/auth/session`, `POST /api/app/auth/logout`; SPA→Iden preferred (documented)  
- **Required Documents:** Impl F3; guide §4  
- **Evidence:** `AuthController` session/logout; `auth-iden.md` SPA↔BFF split

---

### DQ-2008 — Customer FE: Iden login + BuContext + silent refresh

- **Status:** ✅ Complete  
- **Dependency:** DQ-2002  
- **Source:** Impl W4  
- **Outcome:** `env.json` `idenBaseUrl` → Iden password + BuContext; 401 → re-login  
- **Required Documents:** Impl F3; guide §4  
- **Evidence:** `auth.service.ts` `loginViaIden`; `auth.interceptor.ts` 401 handling

---

### DQ-2009 — API: Feature catalog manifest + CI sync + role seed

- **Status:** ✅ Complete  
- **Dependency:** DQ-2001  
- **Source:** Impl catalog section (approved); W5  
- **Outcome:** Manifest + `FeatureKeys`; sync via `POST /api/admin/iden/catalog/sync`  
- **Required Documents:** Impl Catalog tables  
- **Evidence:** `Iden/feature-catalog.json`; `FeatureCatalog.cs`; `AdminIdenController`

---

### DQ-2010 — API: IFeatureEnforce

- **Status:** ✅ Complete  
- **Dependency:** DQ-2001, DQ-2009  
- **Source:** Impl W5; guide §12–13  
- **Outcome:** `IFeatureEnforce`; ≤30s cache; fail-closed when Mode=Iden  
- **Required Documents:** Impl F5; guide §12–13  
- **Evidence:** `FeatureEnforce.cs`; `Band20IdenAuthTests` DevBypass allow

---

### DQ-2011 — Design: Users & permissions mockup (gate)

- **Status:** ✅ Complete  
- **Dependency:** DQ-2009  
- **Source:** Impl W6  
- **Outcome:** Mockup approved with batch execute  
- **Required Documents:** Impl W6  
- **Evidence:** [25-users-permissions-ui-mockup.md](./25-users-permissions-ui-mockup.md)

---

### DQ-2012 — API: Users invite / role assign / deactivate

- **Status:** ✅ Complete  
- **Dependency:** DQ-2010, DQ-2011  
- **Source:** Impl W6b; F4  
- **Outcome:** `api/app/users` orchestrating Iden; FeatureKey gated  
- **Required Documents:** Impl F4; catalog System keys  
- **Evidence:** `UsersController.cs`

---

### DQ-2013 — Customer FE: Users pages per mockup

- **Status:** ✅ Complete  
- **Dependency:** DQ-2012  
- **Source:** Impl W6b  
- **Outcome:** `/users`, `/users/:id`  
- **Required Documents:** DQ-2011 evidence  
- **Evidence:** `apps/web/src/app/features/users/*`; nav Settings entry

---

### DQ-2014 — API: Enforce on privileged handlers

- **Status:** ✅ Complete  
- **Dependency:** DQ-2010, DQ-2013  
- **Source:** Impl W7  
- **Outcome:** FeatureKey checks on agents list/create/delete, api-keys, users  
- **Required Documents:** Impl catalog FeatureKey→API map  
- **Evidence:** Agents + ApiKeys + Users handlers

---

### DQ-2015 — API: External API key harden (3A)

- **Status:** ✅ Complete  
- **Dependency:** DQ-2003  
- **Source:** Impl W8; DR-KEY-1 A  
- **Outcome:** `/api/app/*` never ApiKey scheme; `/api/v1` only; docs ownership  
- **Required Documents:** Impl W8  
- **Evidence:** Program policy router; `auth-iden.md` / pattern 3A

---

### DQ-2016 — Docs: architecture Iden pack

- **Status:** ✅ Complete  
- **Dependency:** DQ-2009  
- **Source:** Impl W9  
- **Outcome:** Updated constraints; `auth-iden.md`; pattern; critical-rules + README  
- **Required Documents:** Impl §4.6 / W9  
- **Evidence:** architecture README router links

---

### DQ-2017 — Cleanup Band 15 stubs + regression suite

- **Status:** ✅ Complete  
- **Dependency:** DQ-2008, DQ-2014, DQ-2015  
- **Source:** Impl W10; exploration Q6  
- **Outcome:** Band 15 DQ-1501–1507 ❌ Cancelled / superseded by Band 20; `Band20IdenAuthTests`  
- **Required Documents:** Plan 03 Band 15 section; this queue  
- **Evidence:** Plan 03 edit + tests

---

## Phase-end contract

### Finalized decisions

As Phase 2 + catalog + mockup gate + batch execute.

### Pending decisions

None.

### Assumptions / Risks

See Completion Summary — live Iden UAT credentials still required for end-to-end smoke.

### Readiness

**Band 20 complete.** Configure `Iden:*` + `Auth:Mode=Iden` for UAT validation.
