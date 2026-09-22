# Documate v3 — Iden Integration — Dispatch Queue

> **Document type:** Dispatch queue (Phase 3)  
> **Status:** 🔄 **Band 20 ready** — Phase 2 approved 2026-09-22  
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
| ✅ Complete | 0 |
| 🔄 In Progress | 0 |
| ⬜ Ready | 1 (DQ-2001) |
| ⏸ Parked | 0 |
| ❌ Cancelled | 0 |

### Finalized decisions

- Exploration locks + **DR-SYNC-1 A**, **DR-KEY-1 A**
- Catalog: 3 customer modules + 3 admin modules; role templates; Users & permissions page with **mockup gate (W6 → W6b)**
- 3A: Documate owns per-business integration keys; ServiceClient = product/CI only
- Customer app = internal + BuContext + roles; business-wide data

### Pending decisions

None for queue start. Catalog treated **approved** with Phase 2 approve (amend via DQ if needed).

### Assumptions

- Iden UAT/local + ServiceClient credentials available before DQ-2001/2005/2009
- Platform seeds modules before feature sync (DQ-2009)
- `SessionPolicies:BySoftwareKey:documate` agreed with ops

### Risks

| Risk | Mitigation |
|------|------------|
| Iden contract drift | Follow For-Integrators README + api-contracts; file Iden defects |
| Mockup skipped | DQ-2011 must complete before DQ-2012/2013 |
| API key on `/api/app` | DQ-2015 hardens scheme routing |
| Band 15 duplicate | DQ-2017 cancels/parks stubs |

---

## Dispatch Index

| DQ | Band | Title | Status | Depends on |
|----|------|--------|--------|------------|
| **DQ-2001** | 20 | API: Iden client + secrets/options + service token | ⬜ | — |
| **DQ-2002** | 20 | API: JWT/BuContext → `IBusinessContext`; `Auth:Mode=Iden` | ⬜ | DQ-2001 |
| **DQ-2003** | 20 | API: Tenancy mirror SyncStatus + write-through provisioner | ⬜ | DQ-2002 |
| **DQ-2004** | 20 | API: Reconcile Hangfire job (auto-create; DR-SYNC-1 A) | ⬜ | DQ-2003 |
| **DQ-2005** | 20 | API: Admin create tenant/business Iden-first | ⬜ | DQ-2003 |
| **DQ-2006** | 20 | Admin FE: tenant/business create forms | ⬜ | DQ-2005 |
| **DQ-2007** | 20 | API: App auth endpoints for Iden session bridge (if needed) | ⬜ | DQ-2002 |
| **DQ-2008** | 20 | Customer FE: Iden login + BuContext select + silent refresh | ⬜ | DQ-2002 |
| **DQ-2009** | 20 | API: Feature catalog manifest + CI sync tool + role seed | ⬜ | DQ-2001 |
| **DQ-2010** | 20 | API: `IFeatureEnforce` wrapper | ⬜ | DQ-2001, DQ-2009 |
| **DQ-2011** | 20 | Design: Users & permissions **mockup** (list + edit) — approve gate | ⬜ | DQ-2009 |
| **DQ-2012** | 20 | API: Users invite / role assign / deactivate | ⬜ | DQ-2010, DQ-2011 |
| **DQ-2013** | 20 | Customer FE: `/users` + `/users/:id` per approved mockup | ⬜ | DQ-2012 |
| **DQ-2014** | 20 | API: Enforce FeatureKeys on privileged FrontendSupport handlers | ⬜ | DQ-2010, DQ-2013 |
| **DQ-2015** | 20 | API: External API key harden (`/api/app` ≠ ApiKey); 3A docs | ⬜ | DQ-2003 |
| **DQ-2016** | 20 | Docs: architecture Iden pack (constraints, auth-iden, pattern, critical-rules) | ⬜ | DQ-2009 |
| **DQ-2017** | 20 | Cleanup Band 15 DQ-1501–1507 + auth regression suite | ⬜ | DQ-2008, DQ-2014, DQ-2015 |

**Suggested next:** **DQ-2001**

**Critical path:**  
2001 → 2002 → 2003 → 2004/2005  
2001 → 2009 → 2010 → 2011 → 2012 → 2013 → 2014  
2002 → 2008  
2003 → 2015  
2009 → 2016  
… → 2017

After 2001: **2002** and **2009** can run in parallel if batched.

---

## Wave / DQ Entries

### DQ-2001 — API: Iden client + secrets/options + service token

- **Status:** ⬜ Ready  
- **Dependency:** —  
- **Source:** Impl W1; Iden For-Integrators README + api-contracts §1.1 / §1.24  
- **Outcome:** `Infrastructure/Iden` client (NuGet or typed HttpClient); `Iden__*` config/secrets; acquire/cache service JWT (`product_service`); assert `software_key=documate`  
- **Required Documents:** Impl plan Domain Architecture; Documate × Iden guide §15–16  
- **Evidence:** Unit/integration test service token; config keys documented  

---

### DQ-2002 — API: JWT/BuContext → IBusinessContext

- **Status:** ⬜ Ready  
- **Dependency:** DQ-2001  
- **Source:** Impl W1; guide §3–5  
- **Outcome:** JwtBearer + JWKS; map `bu_context_id` (dual-read `context_id`), `tenant_id`, `tenant_business_id`, `identity_class`, `sub`; `Auth:Mode=Iden` vs `DevBypass` (non-prod only); extend `IBusinessContext`  
- **Required Documents:** Impl plan claim map; guide §3  
- **Evidence:** Test claim mapping; DevBypass still works when Mode=DevBypass  

---

### DQ-2003 — API: Tenancy mirror SyncStatus + write-through

- **Status:** ⬜ Ready  
- **Dependency:** DQ-2002  
- **Source:** Impl W2; DR-SYNC-1 A  
- **Outcome:** Migration `LastSyncedAtUtc` / `SyncStatus` on `CorTenant`/`CorTenantBusiness`; provisioner write-through from Iden Guids only; read-repair on auth; **no local Guid mint** for Iden ids  
- **Required Documents:** Impl tenancy mirror table  
- **Evidence:** Migration applied; create path rejects invented Guids  

---

### DQ-2004 — API: Reconcile Hangfire job

- **Status:** ⬜ Ready  
- **Dependency:** DQ-2003  
- **Source:** Impl W2; exploration S1–S6  
- **Outcome:** Recurring job auto-creates missing mirrors; updates names/status; marks divergent; **blocks writes** when divergent (DR-SYNC-1 A)  
- **Required Documents:** Impl F6; DR-SYNC-1  
- **Evidence:** Test auto-create + divergent write block  

---

### DQ-2005 — API: Admin create tenant/business Iden-first

- **Status:** ⬜ Ready  
- **Dependency:** DQ-2003  
- **Source:** Impl W3; F1–F2  
- **Outcome:** PlatformAdmin APIs call Iden `tenant:write`/`business:write` then mirror + seed roles + Documate defaults; remove local Guid business create  
- **Required Documents:** Impl F1–F2; guide §6  
- **Evidence:** Integration test against Iden UAT or recorded contract test  

---

### DQ-2006 — Admin FE: tenant/business create

- **Status:** ⬜ Ready  
- **Dependency:** DQ-2005  
- **Source:** Impl W3  
- **Outcome:** Admin SPA forms call new APIs; no client-side Guid invent  
- **Required Documents:** Impl W3  
- **Evidence:** Manual or e2e smoke on create flow  

---

### DQ-2007 — API: App auth bridge (if needed)

- **Status:** ⬜ Ready  
- **Dependency:** DQ-2002  
- **Source:** Impl W4  
- **Outcome:** Any Documate `/api/app/auth` helpers needed for SPA (me, logout coordination); prefer direct Iden from SPA for login/refresh per guide — document chosen split  
- **Required Documents:** Impl F3; guide §4  
- **Evidence:** Contract note in PR for SPA↔Iden vs BFF  

---

### DQ-2008 — Customer FE: Iden login + BuContext + silent refresh

- **Status:** ⬜ Ready  
- **Dependency:** DQ-2002  
- **Source:** Impl W4  
- **Outcome:** Replace InterimFeGate for `Auth:Mode=Iden`; login → BuContext select → silent refresh on 401; reuse detection → full re-login  
- **Required Documents:** Impl F3; guide §4  
- **Evidence:** Manual login smoke; refresh path verified  

---

### DQ-2009 — API: Feature catalog manifest + CI sync + role seed

- **Status:** ⬜ Ready  
- **Dependency:** DQ-2001  
- **Source:** Impl catalog section (approved); W5  
- **Outcome:** Manifest for 3 customer + 3 admin modules’ FeatureKeys; sync tool upserts via ServiceClient; seed `documate.admin/editor/files_operator/viewer` on business create  
- **Required Documents:** Impl Catalog tables  
- **Evidence:** Dry-run sync log; constants compile  

---

### DQ-2010 — API: IFeatureEnforce

- **Status:** ⬜ Ready  
- **Dependency:** DQ-2001, DQ-2009  
- **Source:** Impl W5; guide §12–13  
- **Outcome:** `IFeatureEnforce.Check(buContextId, capabilityKey)`; ≤30s cache; fail-closed on errors for mutations; reason→HTTP map  
- **Required Documents:** Impl F5; guide §12–13  
- **Evidence:** Unit tests allow/deny/fail-closed  

---

### DQ-2011 — Design: Users & permissions mockup (gate)

- **Status:** ⬜ Ready  
- **Dependency:** DQ-2009  
- **Source:** Impl W6  
- **Outcome:** Mockup for `/users` list + `/users/:id` edit/permissions (role picker, effective FeatureKey summary, invite, deactivate). **Developer approves before DQ-2012/2013.** Artifact linked in evidence.  
- **Required Documents:** Impl W6  
- **Evidence:** Mockup file/link + written approve note  

---

### DQ-2012 — API: Users invite / role assign / deactivate

- **Status:** ⬜ Ready  
- **Dependency:** DQ-2010, DQ-2011  
- **Source:** Impl W6b; F4  
- **Outcome:** FrontendSupport Users APIs orchestrating Iden identity/member/role; gated by System FeatureKeys  
- **Required Documents:** Impl F4; catalog System keys  
- **Evidence:** API tests with mocked Iden or UAT  

---

### DQ-2013 — Customer FE: Users pages per mockup

- **Status:** ⬜ Ready  
- **Dependency:** DQ-2012  
- **Source:** Impl W6b  
- **Outcome:** `/users`, `/users/:id` match approved mockup; role assign UX  
- **Required Documents:** DQ-2011 evidence  
- **Evidence:** Screenshot vs mockup; smoke  

---

### DQ-2014 — API: Enforce on privileged handlers

- **Status:** ⬜ Ready  
- **Dependency:** DQ-2010, DQ-2013  
- **Source:** Impl W7  
- **Outcome:** FeatureKey checks on agents, files/docs, channels, intake, api-keys, users, business profile mutations  
- **Required Documents:** Impl catalog FeatureKey→API map  
- **Evidence:** Deny tests per area sample  

---

### DQ-2015 — API: External API key harden (3A)

- **Status:** ⬜ Ready  
- **Dependency:** DQ-2003  
- **Source:** Impl W8; DR-KEY-1 A  
- **Outcome:** `/api/app/*` rejects ApiKey scheme; External `/api/v1` only; keys bound to `IdenBusinessId`; document ownership  
- **Required Documents:** Impl W8  
- **Evidence:** Test: X-Api-Key on `/api/app/agents` → 401; `/api/v1` still works  

---

### DQ-2016 — Docs: architecture Iden pack

- **Status:** ⬜ Ready  
- **Dependency:** DQ-2009  
- **Source:** Impl W9  
- **Outcome:** Update `iden-constraints.md`; add `auth-iden.md` (retire placeholder); add `patterns/iden-tenancy-users-features.md`; critical-rules + README router; how to add FeatureKeys/users/tenancy  
- **Required Documents:** Impl §4.6 / W9  
- **Evidence:** Doc links in architecture README  

---

### DQ-2017 — Cleanup Band 15 stubs + regression suite

- **Status:** ⬜ Ready  
- **Dependency:** DQ-2008, DQ-2014, DQ-2015  
- **Source:** Impl W10; exploration Q6  
- **Outcome:** Mark `DQ-1501–1507` ❌ Cancelled / superseded by Band 20 in plan 03 DQ; CI-friendly auth+tenancy+enforce+key isolation tests  
- **Required Documents:** Plan 03 Band 15 section; this queue  
- **Evidence:** Plan 03 edit + test project green  

---

## Phase-end contract

### Finalized decisions

As Phase 2 + catalog + mockup gate.

### Pending decisions

None.

### Assumptions / Risks

See Completion Summary.

### Readiness

**Ready for execution.** Ask which DQ to run (suggested: **DQ-2001**). Do not start code until the developer names a DQ (or batches).
