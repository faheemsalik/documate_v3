# 25 — Documate × Iden Integration (Implementation Plan)

> **Status:** Phase 2 — **approved** 2026-09-22 → Phase 3 dispatch queue  
> **Type:** Engineering implementation plan  
> **Upstream:** [25-iden-integration-exploration.md](./25-iden-integration-exploration.md) (Phase 1 ✅)  
> **Iden contract entry:** [`For-Integrators/README.md`](D:/Work/dev-infra/iden-dev-infra/docs/For-Integrators/README.md) → Documate guide + handbook; wire details in [`iden-api-contracts.md`](D:/Work/dev-infra/iden-dev-infra/docs/plans/04-contracts/iden-api-contracts.md)  
> **Downstream:** [25-iden-integration-dispatch-queue.md](./25-iden-integration-dispatch-queue.md) (Band 20)

**Outcome:** Humans authenticate via Iden (BuContext); Documate mirrors tenancy without drift; admin creates Tenant/Business through Iden; customer app manages internal users with FeatureKey roles (business-wide data) via a **Users & permissions** page; FeatureKeys sync from a repo manifest (3 customer + 3 admin modules); privileged APIs call `/enforce/check`; External keeps Documate-managed per-business API keys (3A); architecture docs teach the pattern.

**Out of this plan:** SyncBridge entitlement commands; Iden Ops / AdminGate redesign; portal/`UserLinkedRecord` MVP; creator-owned row ACL; replacing ServiceClient with customer-held keys.

---

## Planning flow

| Phase | Document | Status |
|-------|----------|--------|
| 1 — Exploration | [25-iden-integration-exploration.md](./25-iden-integration-exploration.md) | ✅ Approved |
| 2 — Implementation plan | **This file** | ✅ Approved 2026-09-22 |
| 3 — Dispatch queue | [25-iden-integration-dispatch-queue.md](./25-iden-integration-dispatch-queue.md) | 🔄 Band 20 — ask which DQ |

---

## Delivery Principles

1. **Iden is SoT** for Tenant, TenantBusiness, identities, roles, FeatureKeys, runtime allow/deny. Documate never invents a permissions DB or parallel login passwords.
2. **Customer app staff = internal + BuContext + roles.** Data scope = whole `BusinessId` for allowed FeatureKeys (not creator-owned).
3. **Iden-first writes.** Admin/customer provisioning calls Iden, then write-through mirrors `CorTenant` / `CorTenantBusiness`. No local Guid mint for Iden ids.
4. **BuContext now.** Prefer `bu_context_id` and `/bu-contexts`; dual-read `context_id` until Iden sunset (2026-12-31).
5. **Enforce on privileged human actions.** `POST /enforce/check { buContextId, capabilityKey }`; fail-closed on Iden errors for mutations.
6. **3A refined.** Documate owns per-business integration API keys; Iden ServiceClient is product/CI only (vault).
7. **FeatureKeys as code + CI.** Manifest in repo → sync tool → Iden catalog; constants used in API + (UX-only) Angular guards.
8. **Architecture docs updated in-band** so future features follow one pattern.
9. **Copy existing slices.** Extend `Infrastructure/Auth`, `TenantBusinessProvisioner`, PlatformAdmin tenants, FrontendSupport auth/businesses — do not reinvent CQRS layout.
10. **Cleanup Band 15 stubs** in the same Phase 3 DQ as the new band (no duplicate live queues).

---

## Domain Architecture Layer

| Concern | Home |
|---------|------|
| Iden HTTP / token client | `apps/api/Infrastructure/Iden/` (`IIdenClient`, service-token cache, enforce, tenants, identities, roles, catalog) |
| Options / secrets | `Iden__*` via existing Secrets Manager loader; `Auth:Mode=Iden` vs `DevBypass` (local only) |
| JWT middleware | Replace/augment DevBypass with JwtBearer validating Iden JWKS; map claims → `IBusinessContext` |
| Business context | Existing `IBusinessContext` / accessor — add `BuContextId`, `IdentityId`, `IdentityClass` |
| Tenancy mirror | Extend `TenantBusinessProvisioner` + new `ITenancyMirror` / reconcile Hangfire job |
| Sync metadata | Columns on `CorTenant` / `CorTenantBusiness`: `LastSyncedAtUtc`, `SyncStatus` (`ok` / `pending` / `divergent`) |
| Admin tenant/business create | `Modules/PlatformAdmin/Features/Tenants/` — Iden-first commands |
| Customer user + roles | `Modules/FrontendSupport/Features/Users/` + **web `/users` permissions UI** — Iden identity/member/role |
| Feature catalog | `apps/api/Iden/feature-catalog.json` + `FeatureKeys.cs` + sync CLI (modules in § Catalog) |
| Enforce gate | `IFeatureEnforce` + filter/attribute used by privileged handlers |
| External API keys | Existing `CorTenantApiKey` / F2 path — keep Documate-owned; bind to `IdenBusinessId` |
| Customer FE login | `apps/web` auth → Iden login + BuContext select + silent refresh |
| Admin FE | `apps/admin` tenant/business create forms calling new admin APIs |
| Architecture docs | `iden-constraints.md`, `auth-iden.md`, `patterns/iden-tenancy-users-features.md`, README + critical-rules |
| Tests | Auth claim mapping, mirror write-through, enforce deny, catalog sync dry-run, no local Guid create |

### Tenancy mirror fields (normative)

| Local | Source | Editable in Documate UI? |
|-------|--------|---------------------------|
| `CorTenant.IdenTenantId` | Iden | No (mirror only) |
| `CorTenant.Name` | Iden tenant name | No — mirror |
| `CorTenant.IsActive` | Iden | No — mirror |
| `CorTenantBusiness.IdenBusinessId` | Iden | No — mirror |
| `CorTenantBusiness.Name` / `TenantName` | Iden | No — mirror |
| `CorTenantBusiness.IsActive` | Iden | No — mirror |
| `CorTenantBusiness.IntakeEmailSlug` | Documate-only | Yes |
| `ProviderModeEnumId` | Documate product setting | Yes (Documate-owned) |
| `LastSyncedAtUtc` / `SyncStatus` | Mirror machinery | System |

### Catalog: Modules + FeatureKeys (**APPROVED** with Phase 2 — 2026-09-22)

> **ModuleKey** = stable Iden key (snake-ish). **Display name** = human-friendly (UI / Iden Web).  
> **FeatureKey** = `documate.{area}.{resource}.{operation}`. Types: `page` | `action` | `sensitive`.  
> Modules are **platform-seeded**; Documate CI syncs features only.  
> Data rule: FeatureKey → **all** business rows of that type (not creator-owned).

#### Customer app (`apps/web`) — 3 modules

| ModuleKey | Display name | Covers |
|-----------|--------------|--------|
| `customer.agents_and_channels` | **Agents & Channels** | Agents, templates, queues/channels, intake mailboxes |
| `customer.files_and_documents` | **Files & Documents** | Files, documents, search, home entry for workspace |
| `customer.system` | **System** | Users & permissions, business profile/switch, API keys, billing view, settings, audits (placeholder), in-app docs |

#### Admin / platform app (`apps/admin`) — 3 modules

| ModuleKey | Display name | Covers |
|-----------|--------------|--------|
| `admin.tenancy` | **Tenancy** | Tenants, businesses (create/list/detail) |
| `admin.platform` | **Platform** | Agent templates, cross-tenant agents, document types, providers, events, actions |
| `admin.operations` | **Operations** | Dashboard, ops files/docs, monitoring, support |

#### FeatureKeys — Customer: Agents & Channels

| FeatureKey | Type | UI / API |
|------------|------|----------|
| `documate.customer.agents.list` | page | `/agents` |
| `documate.customer.agents.view` | action | Agent detail / prompt preview |
| `documate.customer.agents.create` | action | Create agent |
| `documate.customer.agents.update` | action | Edit agent |
| `documate.customer.agents.delete` | sensitive | Delete agent |
| `documate.customer.agents.templates.view` | page | Templates |
| `documate.customer.channels.list` | page | `/queues` |
| `documate.customer.channels.view` | action | Channel detail |
| `documate.customer.channels.create` | action | Create channel |
| `documate.customer.channels.update` | action | Update channel |
| `documate.customer.channels.delete` | sensitive | Delete channel |
| `documate.customer.intake.view` | page | Intake mailboxes |
| `documate.customer.intake.manage` | action | Manage mailboxes |

#### FeatureKeys — Customer: Files & Documents

| FeatureKey | Type | UI / API |
|------------|------|----------|
| `documate.customer.home.view` | page | `/home` |
| `documate.customer.files.list` | page | `/files` |
| `documate.customer.files.view` | action | File detail / download |
| `documate.customer.files.upload` | action | Upload |
| `documate.customer.files.update` | action | Reprocess / non-delete mutations |
| `documate.customer.files.delete` | sensitive | Delete file |
| `documate.customer.documents.view` | action | Document view |
| `documate.customer.documents.edit` | action | Edit extract |
| `documate.customer.documents.publish` | sensitive | Publish |
| `documate.customer.search.view` | page | Schema search |

#### FeatureKeys — Customer: System (includes Users & permissions page)

| FeatureKey | Type | UI / API |
|------------|------|----------|
| `documate.customer.users.list` | page | **`/users`** — users list |
| `documate.customer.users.invite` | action | Invite / create member |
| `documate.customer.users.permissions.manage` | sensitive | **Assign roles / FeatureKey access** on user detail |
| `documate.customer.users.deactivate` | sensitive | Deactivate member |
| `documate.customer.business.profile.view` | page | Business profile |
| `documate.customer.business.profile.update` | action | Update Documate-owned profile fields |
| `documate.customer.business.switch` | action | Switch business / BuContext |
| `documate.customer.integrations.apikeys.list` | page | `/api-keys` |
| `documate.customer.integrations.apikeys.manage` | sensitive | Create/revoke keys |
| `documate.customer.billing.overview.view` | page | Usage / billing |
| `documate.customer.settings.view` | page | Business settings (as shipped) |
| `documate.customer.audits.view` | page | Audits (placeholder until feature ships) |
| `documate.customer.docs.view` | page | `/docs` |

#### FeatureKeys — Admin app

| Module | FeatureKey | Type | UI |
|--------|------------|------|-----|
| Tenancy | `documate.admin.tenancy.tenants.list` | page | `/tenants` |
| Tenancy | `documate.admin.tenancy.tenants.manage` | sensitive | Create/update tenant |
| Tenancy | `documate.admin.tenancy.businesses.list` | page | `/businesses` |
| Tenancy | `documate.admin.tenancy.businesses.manage` | sensitive | Create/update business (Iden-first) |
| Platform | `documate.admin.platform.templates.manage` | action | Agent templates |
| Platform | `documate.admin.platform.agents.view` | page | Cross-tenant agents |
| Platform | `documate.admin.platform.catalog.manage` | action | Document types / providers |
| Platform | `documate.admin.platform.events.manage` | action | Events / actions |
| Operations | `documate.admin.operations.dashboard.view` | page | Dashboard |
| Operations | `documate.admin.operations.ops.view` | page | Ops files/docs |
| Operations | `documate.admin.operations.monitoring.view` | page | Monitoring |
| Operations | `documate.admin.operations.support.view` | page | Support |

> Admin app auth today = AdminGate (out of Iden Ops redesign scope). FeatureKeys above are for **when** admin identities use Iden enforce; until then PlatformAdmin policy remains. Catalog still registered so platform can entitle later.

#### Role templates (customer)

| SysKey | Display name | FeatureKeys (summary) |
|--------|--------------|------------------------|
| `documate.admin` | Admin | All **customer.*** keys |
| `documate.editor` | Editor | Agents/channels/files (no deletes, no `users.permissions.manage`, no apikeys.manage) |
| `documate.files_operator` | Files operator | Files & Documents keys except `files.delete` + `documents.publish`; `home.view`; **no** Agents & Channels manage; **no** System users/apikeys |
| `documate.viewer` | Viewer | `*.list` / `*.view` / `home.view` / `docs.view` / `billing.overview.view` only |

#### Users & permissions page (customer app) — in scope

| Item | Detail |
|------|--------|
| Route | `/users` (list), `/users/:id` (detail / access) |
| Who | Callers with `users.list` / `users.permissions.manage` |
| Actions | List members; invite; **assign/change role** (and thus FeatureKeys); deactivate |
| UX | Show role + effective FeatureKey summary (from Iden role features / validate); no local permission matrix editor as SoR — pick **role template** (advanced: optional user overrides later, not MVP) |
| API | `FrontendSupport` Users feature → Iden identities/members/roles |
| **Mockup first** | **W6:** list + edit/permissions mockup approved → **W6b:** implement |

**Please Approve / amend this reduced catalog.** Then W5 uses it as manifest source of truth.

---

## Domain Validation Rules

**Finalized (from exploration — no open Decision Required):**

| ID | Rule |
|----|------|
| V1 | Reject create business/tenant that does not round-trip Iden Guids |
| V2 | Human privileged mutation requires `enforce.allowed == true` |
| V3 | Portal identity class rejected on customer staff APIs in MVP (internal only) |
| V4 | Queries for agents/files/queues filter `BusinessId` only — never `CreatedBy == me` as ACL |
| V5 | Integration API key must resolve to exactly one `IdenBusinessId`; missing/revoked → 401 |
| V6 | ServiceClient secret never in SPA or admin browser bundles |
| V7 | Fail-closed: Iden unreachable → deny writes; allowlisted public reads only |
| V8 | `SyncStatus=divergent` blocks new writes for that business until ops clears (or soft-quarantine — see Decision Required below) |

### Decision Required — DR-SYNC-1: divergent business behavior

| Option | Behavior |
|--------|----------|
| **A (LOCKED)** | Log + alert; allow reads; **block writes** until reconcile succeeds or ops marks resolved |
| B | Log + alert only; do not block writes |
| C | Soft-disable business (`IsActive=false` locally) until fixed |

**Choice:** **A** (2026-09-22).

### Decision Required — DR-KEY-1: integration (External) API key capabilities

**Yes — this is only about Documate-managed per-business integration keys** (today’s F2 / `CorTenantApiKey` used on `/api/v1`), **not** human Iden login and **not** the Documate ServiceClient.

When a partner calls Documate with a long-lived business key, how much of that business’s External API can the key use?

| Option | Meaning in practice |
|--------|---------------------|
| **A (LOCKED)** | One key for a business → access to the **same External API surface as today** (ingest/files/etc. already exposed to API keys). No per-key “can delete / cannot manage agents” matrix in MVP. Humans still use Iden FeatureKeys in the SPA. |
| B | When creating/editing a key in Documate, admin picks an **allowlist** (e.g. upload only, no delete). Each External route checks the key’s allowlist. More control, more product/UI work. |

Humans (User A admin vs User B files-only) are **already** handled by Iden roles/FeatureKeys — DR-KEY-1 does **not** change that.

**Choice:** **A** (2026-09-22).

---

## Process Flows

### F1 — Admin creates Tenant + first Business

```text
Admin SPA → POST /api/admin/tenants
  → Iden service token
  → POST Iden /tenants (+ firstBusiness product=documate_cloud)
  → Upsert CorTenant + CorTenantBusiness (Iden Guids)
  → Seed roles (documate.admin/editor/viewer) via Iden
  → Existing default Queue / workflow bootstrap
  → Return admin DTO
```

### F2 — Admin adds Business under Tenant

```text
→ Iden POST /tenants/{id}/businesses
→ Mirror + seed roles + defaults
```

### F3 — Customer login (Iden)

```text
SPA → Iden POST /auth/login
→ if multiple: GET/POST /bu-contexts/select
→ store access+refresh; silent refresh on 401
→ call Documate with Bearer
→ JWT validate → map bu_context_id → IBusinessContext
→ read-repair mirror → handlers
```

### F4 — Users & permissions (customer app)

```text
[Gate] Mockup for /users + /users/:id approved (W6)
Admin with users.permissions.manage opens /users or /users/:id
→ Documate POST/PATCH /api/app/users (invite, assign role, deactivate)
→ Iden: identity internal → member → role template FeatureKeys
→ UI shows role + effective access summary (not a local permission SoR)
```

### F5 — Privileged action

```text
Handler → IFeatureEnforce.Check(buContextId, FeatureKeys.X)
→ if deny: 403 + reason mapping
→ else domain logic BusinessId-scoped
```

### F6 — Shadow reconcile (Hangfire)

```text
List Documate-owned / known Iden businesses (service token)
→ for each: GET Iden; upsert local (auto-create if missing)
→ mark divergent orphans; update LastSyncedAtUtc
```

### F7 — FeatureKey CI sync

```text
CI → service token → GET modules by key → upsert features from manifest
→ fail build on cross_software_access_denied / assert software_key=documate
```

### F8 — External API key (Documate-managed)

```text
Client sends X-Api-Key → ApiKeyAuthenticationHandler
→ resolve CorTenantApiKey → BusinessId / IdenBusinessId
→ IBusinessContext without BuContext (machine)
→ no Iden human enforce; optional key capability allowlist (Phase 2 default: business full External surface as today, tighten later)
```

---

## Instruction and Control Set

| Control | Mechanism |
|---------|-----------|
| Auth mode | `Auth:Mode=Iden` (real) / `DevBypass` (local non-prod only) |
| Claim map | `sub`→IdentityId, `bu_context_id`→BuContextId, `tenant_id`, `tenant_business_id`, `identity_class` |
| Enforce cache | ≤30s memory cache keyed `(buContextId, capabilityKey)`; bypass on writes optional |
| Session | Silent refresh; `token_reuse_detected` → full logout |
| Catalog | Manifest is source for intended keys; Iden is runtime authority |
| Mirror | Write-through required on all Documate-initiated tenancy creates |

### Decision Required — DR-KEY-1: API key capability model

| Option | Behavior |
|--------|----------|
| **A (recommended)** | Keep current External surface per business key (no per-key FeatureKey matrix in MVP) |
| **B** | Store allowlisted FeatureKey/capability strings on `CorTenantApiKey` and gate External routes |

**Stop:** Choose A/B before External wave in Phase 3.

---

## Permissions and Security

| Actor | Auth | Authorization |
|-------|------|----------------|
| Customer SPA user | Iden JWT + BuContext | FeatureKeys via `/enforce/check` |
| Admin SPA (platform) | Existing AdminGate until separate ops plan (out of scope) | PlatformAdmin policy; tenancy create uses ServiceClient server-side |
| Documate API (server) | ServiceClient | catalog/tenant/roles scopes |
| External integration | Documate API key | Business isolation; DR-KEY-1 |
| Portal user | N/A in MVP | — |

**MUST NOT:** SPA catalog writes; local password SoR; authorize on `edition_key` alone; fail-open writes; portal roles for staff.

---

## Dispatch Index (preview — formal DQ in Phase 3)

| Wave | Theme | Depends |
|------|-------|---------|
| W1 | Iden client + secrets + JWT/BuContext → `IBusinessContext` | — |
| W2 | Tenancy mirror write-through + SyncStatus + reconcile auto-create | W1 |
| W3 | Admin API/UI: create tenant/business Iden-first; remove local Guid mint | W2 |
| W4 | Customer + (as needed) login via Iden; silent refresh; retire InterimFeGate/DevBypass for non-local | W1 |
| W5 | Feature catalog manifest + CI sync + `IFeatureEnforce` + starter keys/roles | W1 |
| W6 | **Users & permissions UI mockup** (approve before code) | W5 |
| W6b | Customer Users & permissions UI + APIs; enforce wiring | W6 |
| W7 | Wire enforce on privileged FrontendSupport handlers (agents/files/users/…) | W5, W6b |
| W8 | External API keys confirmed Documate-owned (3A); DR-KEY-1 | W2 |
| W9 | Architecture docs pack | W5+ |
| W10 | Cleanup Band 15 DQ-1501–1507 stubs; auth regression tests | W4–W8 |

Exact DQ ids assigned in Phase 3.

---

## Wave Sections (implementation intent)

### W1 — Iden client + JWT
- Typed client (NuGet `Iden.ApiClient` if available, else HttpClient)
- Service token acquire/cache (TTL &lt; 30m)
- JwtBearer + JWKS; claim mapping; `Auth:Mode`

### W2 — Shadow sync
- Extend provisioner; SyncStatus columns + migration
- Hangfire reconcile; auto-create; divergent handling per DR-SYNC-1

### W3 — Admin tenancy create
- Replace local Guid paths in PlatformAdmin / any FrontendSupport create-business that invents ids
- Admin FE forms

### W4 — Human login
- Web auth service → Iden; BuContext picker; refresh interceptor
- Disable InterimFeGate when `Auth:Mode=Iden`

### W5 — Feature catalog
- Manifest + constants + sync tool
- Seed role templates on business create
- `IFeatureEnforce`

### W6 — Users & permissions mockup (gate)

**Before any `/users` implementation code:** produce and get developer approval on a **UI mockup** for:

1. **Users list** (`/users`) — members, roles, status, invite CTA  
2. **User edit / permissions** (`/users/:id`) — profile summary, **role picker** (admin / editor / files_operator / viewer), effective FeatureKey summary (read-only from role), deactivate  

Deliverable: Figma or static HTML/Angular stub mock (team choice) checked into `docs/plans/` evidence or design link recorded in the DQ.  
**Do not** start W6b APIs/FE until mockup is **approved**.

### W6b — Users + enforce (after mockup approve)
- Implement approved mockup: `/users`, `/users/:id` — list, invite, **assign role / access**, deactivate (System module FeatureKeys)
- CRUD members/roles via Documate → Iden orchestration
- Wire `IFeatureEnforce` on agents, files, channels, users, api-keys, etc.

### W8 — Integration keys
- Document ownership; ensure keys store `IdenBusinessId`; **DR-KEY-1 A**
- **Harden:** `/api/app/*` must not accept ApiKey auth (today policy scheme selects ApiKey whenever `X-Api-Key` is present — harden so app routes require human/Iden JWT only; External stays `/api/v1` + ApiKey scheme)
- Document intended External surface: uploads, files, documents, sync extract — **not** agents/queues/channels admin APIs

### W9 — Docs
- `iden-constraints.md`, `auth-iden.md` (retire placeholder), `patterns/iden-tenancy-users-features.md`, critical-rules, README router

### W10 — Cleanup + tests
- Mark obsolete / remove Band 15 stub entries from plan 03 DQ index
- Regression suite: login, enforce deny, mirror, key isolation

---

## Phase-end contract

### Finalized decisions (carried from exploration)

- Customer app = internal + roles; business-wide data; no creator ACL
- Admin creates tenant/business via Documate → Iden
- Login = Iden (BuContext); silent refresh
- Shadow = write-through + reconcile auto-create; mirror Iden tenancy fields
- FeatureKeys = engineering starter + CI
- 3A: Documate owns business integration keys; ServiceClient = product only
- Band 15 rewrite under plan 25 + stub cleanup
- Portal MVP = no; AdminGate/Iden Ops out of scope
- **DR-SYNC-1 A:** divergent → block writes, allow reads, alert
- **DR-KEY-1 A:** integration keys keep today’s External surface; no per-key capability matrix in MVP

### Pending decisions (Phase 2)

- None — Phase 2 approved; catalog locked; see Band 20 DQ

### Readiness

**Ready for Phase 3 execution** — [dispatch queue](./25-iden-integration-dispatch-queue.md). Ask which DQ to run.
