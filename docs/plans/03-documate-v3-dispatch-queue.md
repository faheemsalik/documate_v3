# Documate v3 Phase 1 — Dispatch Queue

> **Document type:** Dispatch queue (Phase 3)  
> **Status:** ✅ Active bands complete; Band 15 follow-on parked  
> **Source plan:** [03-documate-v3-implementation-plan.md](./03-documate-v3-implementation-plan.md)  
> **Upstream:** Exploration [01](./01-project-exploration-mental-design.md) · Queue design [02](./02-document-queue-design.md) · Glossary [00-product-glossary.md](./00-product-glossary.md)  
> **Scope:** Phase 1 product build — domain, APIs, Core pipeline, delivery, web UI  
> **Out of scope:** Mode 2 BYOK UI, HITL, mapping catalogs, customer MCP UX, white-label SDK, statements reconciliation, MCM DN rebranding, real email D1/D2 (parked — stub only in Phase 1)

**In scope (Band 15, after Phase 1 — J3):** Real Iden (human + M2M); Documate as Iden integration harness; retire F2 / fixed tokens. Parked until Phase 1 product done-when.

**Status legend:** ✅ Complete · 🔄 In Progress · ⬜ Ready · ⏸ Parked · ❌ Cancelled

**Bands** (product plan — not Plan 00 eng bands):

| Band | Focus |
|------|--------|
| 00 | Foundation: scaffold, domain, migrations, seeds |
| 01 | Iden auth + Business context |
| 02 | Agents + templates + schemas |
| 03 | Queues + routes + webhook/email config |
| 04 | Storage + File/Batch/IntakeRejection/events |
| 05 | In-process dispatcher + pipeline stub |
| 06 | External async upload + poll + API keys |
| 07 | Core OCR / split-classify-route / extract |
| 08 | Per-Document webhooks |
| 09 | Sync-wait API |
| 10 | Cancel + reprocess |
| 11 | Agent post-processing + internal MCP |
| 12 | Email stub + intake agent |
| 13 | Angular configure + monitor |
| 14 | Hardening |
| 15 | **Iden Integration & Validation** — discover APIs, live test via Documate, fix Iden, retire F2 |

---

## Completion Summary

| Metric | Value |
|--------|--------|
| Total DQ items | 55 |
| ✅ Complete | 46 |
| 🔄 In Progress | 0 |
| ⬜ Ready | 0 |
| ⏸ Parked | 0 |
| ❌ Cancelled | 9 (DQ-1301–1302; DQ-1501–1507 superseded by Band 20 / plan 25) |

**Count note:** Phase 1 executable = bands 00–14 except DQ-1202. Band 15 ❌ cancelled — Iden work is Band 20 ([25-iden-integration-dispatch-queue.md](./25-iden-integration-dispatch-queue.md)).

### Finalized decisions (from plan 03)

| ID | Decision |
|----|----------|
| A | **A1 + Hangfire (SQL)** — in-process Hangfire Server; durable jobs; webhooks later same backbone |
| B | Sync-wait: **single Document only**; multi-doc → fail; wait terminal or timeout |
| C | **C2** — no webhooks on sync-wait calls |
| D | Support **D1 and D2** later (one active at a time); Phase 1 stub only |
| E | **E3 + P9/F6** (Plan 04, 2026-09-15) — real multi-doc split; DQ-0702 skeleton done; Wave 4b = DQ-0705…0710 + DQ-0802 |
| F | **F2** — Business API keys (Phase 1 bridge → retire Band 15 after Phase 1) |
| G | Sync-wait max **60 seconds** |
| H | **H1** — CorTenant + CorTenantBusiness (product extension; Iden = SoT) |
| I | Queue UUID PK + SequenceId |
| J | **J3** — late Iden validation; Band 15 follow-on after Phase 1 product |
| — | Entity catalog **approved** |
| — | Keep Queue + QueueRoute (Queue untyped; type→Agent routes) |
| K | **K1** — Default channel: Business create → default Queue (`IsDefault`); Agent create/clone auto-route when Business has exactly one Queue; Phase 1 UI shows Queue ID on Business; multi-queue UI later |
| — | **Iden Integration** — Band 15 ❌ → Band 20 (plan 25) |

### Pending decisions

None blocking Phase 1 execution. (J3 locked.)

### Assumptions

- Plan 00 architecture / CQRS conventions already in place.
- `apps/api` / `apps/web` may need scaffold in DQ-0001 if missing.
- At least one LLM provider + object storage available for Wave 4+.

### Risks

| Risk | Mitigation |
|------|------------|
| Split/classify quality | Clear Failed codes; PartialReady; reprocess (E3 honesty) |
| F2 keys become permanent | Band 20 DR-KEY-1 A: Documate-owned `/api/v1` keys; `/api/app` hardened |
| Iden API gaps / bugs | Band 20 + Iden defect loop; fix in Iden repo; explicit waivers only |
| Email sold before allowlist | Stub only until Wave 14 + real D1/D2 phase |

---

## Dispatch Index

| DQ | Band | Title | Status | Depends on |
|----|------|--------|--------|------------|
| DQ-0001 | 00 | Confirm/create `apps/api` + `apps/web` scaffold (Plan 00 layout) | ✅ | — |
| DQ-0002 | 00 | Domain entities + EF migrations (approved catalog) | ✅ | DQ-0001 |
| DQ-0003 | 00 | CorEnumType/CorEnum seeds + `*EnumId` conventions | ✅ | DQ-0002 |
| DQ-0004 | 00 | Health checks + config placeholders (storage/providers) | ✅ | DQ-0001 |
| DQ-0101 | 01 | Iden human auth wiring (JWT/OIDC as Iden dictates) | ✅ | DQ-0001 |
| DQ-0102 | 01 | Business context pipeline + CorTenant/CorTenantBusiness extension rows | ✅ | DQ-0101, DQ-0002 |
| DQ-0201 | 02 | Platform catalogs: Provider, DocumentType, AgentTemplate APIs | ✅ | DQ-0102, DQ-0003 |
| DQ-0202 | 02 | Agent CRUD (schema, instructions, WorkflowId, DocumentTypeId) | ✅ | DQ-0201 |
| DQ-0203 | 02 | Guided clone from AgentTemplate | ✅ | DQ-0202 |
| DQ-0204 | 02 | Agent create/clone auto QueueRoute when Business has one Queue | ✅ | DQ-0203, DQ-0304 |
| DQ-0301 | 03 | Queue CRUD (multi-queue per Business) | ✅ | DQ-0102 |
| DQ-0302 | 03 | QueueRoute + RoutingLocked behavior | ✅ | DQ-0301, DQ-0202 |
| DQ-0303 | 03 | Queue webhook settings + email address mint + allowlist entries | ✅ | DQ-0301 |
| DQ-0304 | 03 | Default Queue bootstrap on Business create (`IsDefault`) + expose default queue_id | ✅ | DQ-0102, DQ-0301 |
| DQ-0401 | 04 | Blob/object storage for File bytes (business-prefixed keys) | ✅ | DQ-0004 |
| DQ-0402 | 04 | Persist Batch, File, Document, IntakeRejection, WorkEvent | ✅ | DQ-0002, DQ-0301 |
| DQ-0501 | 05 | In-process dispatcher (A1) + File pipeline stub (non-blocking enqueue) | ✅ | DQ-0402 |
| DQ-0601 | 06 | External: multi-file async upload → 202 + ids | ✅ | DQ-0501, DQ-0401, DQ-0603 |
| DQ-0602 | 06 | External: poll/list/get File & Document filters | ✅ | DQ-0402 |
| DQ-0603 | 06 | F2 Business-scoped API keys (temporary) | ✅ | DQ-0102 |
| DQ-0701 | 07 | Normalize/OCR adapter(s) Mode 1 | ✅ | DQ-0501 |
| DQ-0702 | 07 | Split → classify → route (E3 multi-doc PDFs) | ✅ | DQ-0701, DQ-0302 |
| DQ-0703 | 07 | Extract via Documate meta-provider + schema validate → Ready/Failed | ✅ | DQ-0702, DQ-0202 |
| DQ-0704 | 07 | Real OCR (Textract→Google) + live LLM extract + sync gates + app priority + ops alert | ✅ | DQ-0703, DQ-0901, DQ-0304 |
| DQ-0705 | 07 | Wave 4b: real per-page OCR artifacts (`normalize.page.{n}.*`) | ✅ | DQ-0704 |
| DQ-0706 | 07 | Wave 4b: admin pipeline model settings (intelligence T1, fallback, extract) | ✅ | DQ-1411, DQ-0705 |
| DQ-0707 | 07 | Wave 4b: page intelligence (T1 + fallback) + persist page profiles | ✅ | DQ-0706 |
| DQ-0708 | 07 | Wave 4b: P9 boundary engine + classify-after-group + text/layout slices + observability | ✅ | DQ-0707, DQ-0702 |
| DQ-0709 | 07 | Wave 4b: per-Document PDF materialization + DownloadUrl cache (Document URL only when PDF exists) | ✅ | DQ-0708, DQ-0401 |
| DQ-0710 | 07 | Wave 4b: extract on grouped slices with separate extract model; Case A / hint rules alignment | ✅ | DQ-0709, DQ-0703 |
| DQ-0801 | 08 | Per-Document webhook dispatch + attempt metadata | ✅ | DQ-0703, DQ-0303 |
| DQ-0802 | 08 | Wave 4b: webhook + External File/Document detail URLs; File embeds full Documents | ✅ | DQ-0709, DQ-0801, DQ-0602 |
| DQ-0901 | 09 | Sync-wait API (single-doc, 60s, no webhook C2) | ✅ | DQ-0703, DQ-0603 |
| DQ-1001 | 10 | Cancel File and Cancel Document | ✅ | DQ-0703, DQ-0801 |
| DQ-1002 | 10 | Explicit reprocess → new File | ✅ | DQ-0601, DQ-0703 |
| DQ-1101 | 11 | Agent post-processing runner + internal MCP (1–2 platform tools) | ✅ | DQ-0703 |
| DQ-1201 | 12 | Email intake: IntakeMailbox + simulate + decision skeleton (Plan 14 EI-0…EI-4) | ✅ | DQ-0501, DQ-0402; Plan 14 |
| DQ-1202 | 12 | Real SES inbound (D1 catch-all) — Plan 14 EI-5 | ✅ | DQ-1201 (live MX smoke after ops verify) |
| DQ-1203 | 12 | Email intake harden + allowlist sell posture (Plan 14 EI-6) | ✅ | DQ-1201 |
| DQ-1204 | 12 | Email FO: EmailIntakeJson + sender chain + body excerpt (Plan 14 FO-0…FO-2) | ✅ | DQ-1203; [follow-on impl](./14-email-intake-followon-implementation-plan.md) |
| DQ-1205 | 12 | Email FO: F3 partial attach + Files/External/webhook surfaces (FO-3a…FO-3b) | ✅ | DQ-1204 |
| DQ-1206 | 12 | Email FO: daily S3 MIME retention job + tests (FO-4…FO-5) | ✅ | DQ-1205, DQ-1410 |
| DQ-1301 | 13 | Angular: Agents + Business default channel (Queue ID) + intake settings | ❌ | Superseded by Band 16 (`07-customer-frontend-dispatch-queue.md`) |
| DQ-1302 | 13 | Angular: Files/Documents monitor + cancel/reprocess actions | ❌ | Superseded by Band 16 (`07-customer-frontend-dispatch-queue.md`) |
| DQ-1401 | 14 | Hardening: rate/size limits, allowlist enforce path, metrics/logs | ✅ | DQ-0801, DQ-0303 |
| DQ-1402 | 14 | Upload intake performance (multi-file parallel, fewer DB round trips, intake timing metrics) | ✅ | DQ-0601, DQ-0401, DQ-1401 |
| DQ-1410 | 14 | System settings: CorSystemSetting + cache/seed + wire EmailIntake/Pipeline (Plan 15 SS-0…SS-2) | ✅ | DQ-1401; [settings impl](./15-system-settings-db-implementation-plan.md) |
| DQ-1411 | 14 | System settings: `/api/admin/system-settings` + separate admin creds + tests (SS-3…SS-4) | ✅ | DQ-1410 |
| DQ-1501 | 15 | Inventory Iden APIs + Documate-facing contract note | ❌ | Superseded by Band 20 (plan 25) |
| DQ-1502 | 15 | Live Iden human auth (Angular + API) — no fixed shipping tokens | ❌ | Superseded by DQ-2002/2008 |
| DQ-1503 | 15 | Integration harness: Tenant→Business through Documate | ❌ | Superseded by DQ-2003–2005 |
| DQ-1504 | 15 | Iden defect loop (reproduce via Documate; fix/track in Iden) | ❌ | Continue ad-hoc under Band 20 ops |
| DQ-1505 | 15 | Iden M2M / machine auth for External APIs | ❌ | Out of Band 20 scope (3A Documate keys remain; M2M later) |
| DQ-1506 | 15 | Retire F2 TenantApiKey (remove or kill-switch); docs Iden-only | ❌ | Superseded by DR-KEY-1 A (Documate-owned keys on `/api/v1`) |
| DQ-1507 | 15 | Auth + tenancy regression suite (CI-friendly) | ❌ | Superseded by DQ-2017 (`Band20IdenAuthTests`) |

**Count note:** Index includes DQ-1202 ⏸ and Band 15 ⏸ (J3). Phase 1 execution starts at DQ-0001.

---

## Wave Sections

### Wave 0 — Foundation
DQ-0001 → DQ-0002 → DQ-0003; DQ-0004 parallel after DQ-0001.

### Wave 1 — Identity & tenancy
DQ-0101 → DQ-0102.

### Wave 2 — Configuration (Agents)
DQ-0201 → DQ-0202 → DQ-0203; DQ-0204 after DQ-0304.

### Wave 3 — Configuration (Queues)
DQ-0301 → DQ-0302, DQ-0303, DQ-0304.

### Wave 4 — Storage & work records
DQ-0401, DQ-0402.

### Wave 5 — Dispatcher
DQ-0501.

### Wave 6 — External async + keys
DQ-0603 → DQ-0601, DQ-0602.

### Wave 7 — Core pipeline
DQ-0701 → DQ-0702 → DQ-0703 → **DQ-0704** (real OCR + live LLM).  
**Wave 4b / real split (Plan 04):** DQ-0705 → DQ-0706 → DQ-0707 → DQ-0708 → DQ-0709 → DQ-0710; then **DQ-0802**.

### Wave 8 — Delivery
DQ-0801, DQ-0901, DQ-1001, DQ-1002; **DQ-0802** (URL surfaces after DQ-0709).

### Wave 9 — Post-process + email + harden + settings/follow-on
DQ-1101, DQ-1201…1203 ✅; DQ-1401 ✅.  
**Recommended next:** Band 17 back office (**DQ-1701**), or Band 15 Iden when unparked.  
Band 13 Angular (**DQ-1301/1302**) ❌ superseded by Band 16.

### Wave 10 — Iden Integration & Validation (J3 — follow-on)
All DQ-1501–1507 ❌ Cancelled — superseded by Band 20 (plan 25).  

---

## DQ Entries

### DQ-0001 — Confirm/create API + Web scaffold
- **Status:** ✅ Complete  
- **Dependency:** —  
- **Source:** Plan 03 Wave 0; Plan 00 folder-structure  
- **Outcome:** `apps/api` and `apps/web` exist and match Plan 00 layout (or gaps documented and fixed).  
- **Required Documents:** `docs/architecture/patterns/folder-structure.md`  
- **Evidence:**  
  - `Documate.slnx` + `apps/api/Documate.Api.csproj` (.NET 10) with `Domain/`, `Infrastructure/`, `Modules/{Core,FrontendSupport,External}/Features/`  
  - MediatR registered; sample FrontendSupport feature `SystemInfo` (`GET /api/app/system/ping`)  
  - `apps/web` Angular 21 standalone app with `src/app/{core,shared,features}/`  
  - `tests/api` xUnit project referenced  
  - `dotnet build Documate.slnx` OK; `dotnet test` OK; `npm run build` (web) OK; no vulnerable NuGet packages (OpenApi 2.7.5)  
  - Root `.gitignore` added for .NET/Node artifacts  

### DQ-0002 — Domain entities + EF migrations
- **Status:** ✅ Complete  
- **Dependency:** DQ-0001  
- **Source:** Plan 03 Domain entity catalog (approved)  
- **Outcome:** All catalog entities persisted (CorTenant, CorTenantBusiness, CorEnum*, Provider, DocumentType, AgentTemplate, Agent, WorkflowDefinition, Queue, QueueRoute, allowlist, Batch, File, Document, IntakeRejection, WorkEvent, TenantApiKey). UUID+SequenceId / bigint+prefixed keys / BusinessId-only / soft delete / RowVersion per plan.  
- **Required Documents:** Plan 03 entity catalog; glossary  
- **Evidence:**  
  - Domain entities under `apps/api/Domain/` (bases + all catalog types; `FileEntity` → table `Files`)  
  - `DocumateDbContext` + fluent configs; soft-delete query filters; RowVersion; SequenceId identity on wire-facing  
  - Migration `Infrastructure/Persistence/Migrations/*InitialCatalog*` applied to LocalDB `Documate_Dev`  
  - Connection string `Documate` in appsettings (no secrets)  

### DQ-0003 — CorEnum seeds + conventions
- **Status:** ✅ Complete  
- **Dependency:** DQ-0002  
- **Source:** Plan 03 CorEnum seeds; `docs/architecture/patterns/cor-enum.md`  
- **Outcome:** System EnumTypeKey/EnumKey seeds loaded; Id resolver for comparisons; no bare CLR enum columns on domain tables.  
- **Required Documents:** `patterns/cor-enum.md`, `critical-rules-api.md`  
- **Evidence:**  
  - `CorEnumSeedCatalog` + `CorEnumSeeder` (system scope); hosted service migrates + seeds on startup  
  - `ICorEnumIdResolver` / `CorEnumIdResolver` (Require by EnumTypeKey+EnumKey → Id)  
  - Includes Phase 1 types + `webhook_delivery_status`; unit tests pass; API ping OK after seed  

### DQ-0004 — Health + config placeholders
- **Status:** ✅ Complete  
- **Dependency:** DQ-0001  
- **Source:** Plan 03 Wave 0  
- **Outcome:** Health endpoint; config stubs for storage and provider credentials (secrets not in repo).  
- **Required Documents:** Plan 03  
- **Evidence:**  
  - `GET /api/app/health` + `MapHealthChecks("/health")` with DB check  
  - `Storage` + `Providers` options; `.env.example` for secrets (not in repo)  
  - Smoke: HEALTH=Healthy

### DQ-0101 — Iden human auth
- **Status:** ✅ Complete  
- **Dependency:** DQ-0001  
- **Source:** Plan 03 Wave 1; `iden-constraints.md`; Decision J3  
- **Outcome:** API authenticates humans via Iden wiring as available; Angular can obtain session/token. Under J3, interim/fixed tokens allowed for Phase 1; live Iden + remove shipping fixed tokens is Band 15 (DQ-1502).  
- **Required Documents:** `governance/iden-constraints.md`, `auth-wiring-placeholder.md`  
- **Evidence:**  
  - `DevBypass` auth scheme (J3 interim); `Auth:Mode=DevBypass` in Development  
  - `GET /api/app/me` [Authorize]; Angular `AuthService` + `authInterceptor`  
  - Live Iden deferred to Band 15

### DQ-0102 — Business context + CorTenant extension rows
- **Status:** ✅ Complete  
- **Dependency:** DQ-0101, DQ-0002  
- **Source:** Plan 03 Decision H1; Iden Tenant→Business  
- **Outcome:** MediatR/pipeline scopes by `business_id`; ensure/upsert CorTenant + CorTenantBusiness product-extension rows (not Iden master SoT); TenantName projection maintained.  
- **Required Documents:** Plan 03 CorTenant sections; iden-constraints  
- **Evidence:**  
  - `IBusinessContext` / `BusinessContextAccessor` from claims  
  - `TenantBusinessProvisioningMiddleware` → `TenantBusinessProvisioner` upserts CorTenant + CorTenantBusiness (TenantName projection) and bootstraps default Queue + `normalize_fields_v1` workflow  
  - `TenantBusinessEnsureCache` (singleton): ensure SQL runs **once per `tenantId|businessId` per API process**; later authenticated requests skip re-ensure (name sync from claims only on first in-process ensure)  
  - Smoke `/api/app/me` returns Dev Tenant/Business after provision

### DQ-0201 — Platform catalogs APIs
- **Status:** ✅ Complete  
- **Dependency:** DQ-0102, DQ-0003  
- **Source:** Plan 03 Provider / DocumentType / AgentTemplate  
- **Outcome:** FrontendSupport read (and admin seed) APIs for platform catalogs.  
- **Required Documents:** Plan 03  
- **Evidence:** 
  - Seeded Providers / DocumentTypes / AgentTemplates on startup (`PlatformCatalogSeeder`)  
  - `GET /api/app/catalogs/document-types|providers|agent-templates` (+ get by key)  
  - Smoke: 4 document types, 3 templates

### DQ-0202 — Agent CRUD
- **Status:** ✅ Complete  
- **Dependency:** DQ-0201  
- **Source:** Plan 03 Agent; Agent-primary WorkflowId  
- **Outcome:** Create/update/list Agents with schema JSON, instructions, DocumentTypeId, optional WorkflowId; Business-scoped.  
- **Required Documents:** Plan 03; glossary Agent  
- **Evidence:** 
  - `GET/POST/PUT/DELETE /api/app/agents` Business-scoped; schema/instructions/DocumentType/Workflow/Provider  
  - Soft-delete on DELETE

### DQ-0203 — Guided clone
- **Status:** ✅ Complete  
- **Dependency:** DQ-0202  
- **Source:** Exploration guided-clone decision  
- **Outcome:** Clone AgentTemplate → new Agent (schema/instructions/defaults copied; editable).  
- **Required Documents:** Plan 01 §8.5; Plan 03  
- **Evidence:** 
  - `POST /api/app/agents/clone-from-template` copies schema/instructions/provider; sets SourceTemplateId  
  - Smoke: cloned `invoice_generic_v1` → Agent with SourceTemplateId

### DQ-0204 — Agent auto QueueRoute (single-Queue Business)
- **Status:** ✅ Complete  
- **Dependency:** DQ-0203, DQ-0304  
- **Source:** Plan 02 §3.1; Plan 03 Agent auto-route; Decision **K1**  
- **Outcome:** On Agent **create** or **guided clone**, if the Business has **exactly one** non-deleted Queue, insert `QueueRoute(QueueId, DocumentTypeId, AgentId)`. If that DocumentType is already routed on that Queue → conflict (no overwrite). If **2+** Queues → create Agent only (no auto-route). If **0** Queues → fail (default Queue missing). Respect `RoutingLocked` (no auto-route when locked).  
- **Required Documents:** Plan 02 §3.1; Plan 03 Agent / QueueRoute  
- **Evidence:**
  - `IAgentQueueRouteAutoMapper` wired into CreateAgent + CloneFromTemplate (transactional)
  - 0 queues → 409; type already routed → 409; 2+ queues → Agent only; RoutingLocked → skip route
  - Migration/runtime default Queue makes 0-queue case exceptional after provisioning

### DQ-0301 — Queue CRUD (multi-queue)
- **Status:** ✅ Complete  
- **Dependency:** DQ-0102  
- **Source:** Plan 03 Queue; multi-queue in model  
- **Outcome:** Business can create multiple Queues; CRUD + list.  
- **Required Documents:** Plan 02; Plan 03  
- **Evidence:** 
  - `GET/POST/PUT/DELETE /api/app/queues` Business-scoped; multi-queue per Business  
  - Defaults: allowlist `open`, workflow `inherit_agent_default`

### DQ-0302 — QueueRoute + routing lock
- **Status:** ✅ Complete  
- **Dependency:** DQ-0301, DQ-0202  
- **Source:** Plan 02 routing lock; Plan 03 QueueRoute  
- **Outcome:** Type→Agent map CRUD until first File; then RoutingLocked; immutable map.  
- **Required Documents:** Plan 02 §12; Plan 03  
- **Evidence:** 
  - `PUT/GET .../routes` type→Agent map; rejects when `RoutingLocked` (409)  
  - `POST .../routing/lock` for smoke before File pipeline; first-File will call same lock later

### DQ-0303 — Webhook + email address + allowlist config
- **Status:** ✅ Complete  
- **Dependency:** DQ-0301  
- **Source:** Plan 02/03 Queue email & webhook  
- **Outcome:** Configure webhook URL/secret/enable; mint unguessable email local-part; manage allowlist entries + AllowlistModeEnumId. Inbound receive may still be stubbed.  
- **Required Documents:** Plan 01 §14.4; Plan 03  
- **Evidence:** 
  - `PUT .../webhook` (secret hashed); `POST .../email/mint`; `PUT .../email` allowlist mode  
  - Allowlist entry CRUD; inbound receive still stubbed  
  - Postman: `docs/postman/Documate-v3-Smoke-Waves-0-3.postman_collection.json`

### DQ-0304 — Default Queue bootstrap (`IsDefault`)
- **Status:** ✅ Complete  
- **Dependency:** DQ-0102, DQ-0301  
- **Source:** Plan 02 §3.1; Plan 03 Queue / CorTenantBusiness; Decision **K1**  
- **Outcome:** Ensuring/creating `CorTenantBusiness` creates a **default Queue** (`IsDefault = true`, name e.g. `Default channel`) if the Business has none. Migration/backfill for existing Businesses missing a default. Unique filtered: one default per Business. App/Business read APIs expose `defaultQueueId`. Optional: Business-scoped intake settings endpoints that write through to the default Queue (or document that existing Queue webhook/email APIs are used with that id).  
- **Required Documents:** Plan 02 §3.1; Plan 03 Queue `IsDefault`  
- **Evidence:**
  - `OpsQueue.IsDefault` + filtered unique index `IX_OpsQueues_BusinessId_IsDefault`
  - Migration `20260828010000_OpsQueueIsDefault` (promote oldest / insert missing)
  - `TenantBusinessProvisioner` → `IDefaultQueueBootstrap.EnsureDefaultAsync` (gated by `TenantBusinessEnsureCache` after first in-process ensure — see DQ-0102)
  - `GET /api/app/me` → `defaultQueueId`; `QueueDto.isDefault`
  - Cannot delete default Queue (409)

### DQ-0401 — Blob storage
- **Status:** ✅ Complete  
- **Dependency:** DQ-0004  
- **Source:** Plan 03 storage; security §6; Permissions and Security (GDPR/US posture)  
- **Outcome:** Store/retrieve File bytes with business-prefixed keys; signed URL support for non-API webhook file refs.  
- **Required Documents:** Plan 03  
- **Evidence:**
  - `IObjectStorage` + `S3ObjectStorage` (old_code: TransferUtility / GetObject / GetPreSignedURL ~30m / Intelligent-Tiering) + `LocalObjectStorage` for Dev
  - Keys: `tenants/{tenantSequenceId}/businesses/{businessSequenceId}/queues/{queueSequenceId}/files/{fileSequenceId}/{safeName}` (SequenceIds, not UUIDs)
  - Config: `Storage:Provider` = `local` | `s3`; see `.env.example`
  - App smoke: `POST/GET /api/app/queues/{queueId}/files`, `GET .../download-url`

### DQ-0402 — Work persistence
- **Status:** ✅ Complete  
- **Dependency:** DQ-0002, DQ-0301  
- **Source:** Plan 03 Batch/File/Document/IntakeRejection/WorkEvent  
- **Outcome:** Repositories/handlers can create and update work entities + append WorkEvents.  
- **Required Documents:** Plan 03; glossary  
- **Evidence:**
  - `IWorkRecordService` / `WorkRecordService`: Batch (≥2), File+blob, Document, IntakeRejection, WorkEvent append; routing lock on first File
  - Upload path creates File + placeholder Document + status WorkEvent
  - Postman: Wave 4 folder in smoke collection

### DQ-0501 — In-process dispatcher + stub pipeline
- **Status:** ✅ Complete  
- **Dependency:** DQ-0402  
- **Source:** Decision A1; Plan 02 §2.1 non-blocking  
- **Outcome:** Enqueue File work without blocking HTTP; stub worker advances File/Document statuses enough to prove concurrency.  
- **Required Documents:** Plan 02 §2.1; Plan 03 Decision A  
- **Evidence:**
  - **Amended:** Hangfire + SQL Server storage (not Channel-only) — jobs survive restart
  - `IWorkDispatcher` → `HangfireWorkDispatcher` → `FilePipelineJobs.ProcessFileAsync`
  - `IWebhookDispatcher` + `WebhookJobs` shell for DQ-0801 retries
  - `FilePipelineStub`: File/Document status walk; idempotent skip when already `ready`
  - In-process `AddHangfireServer` (`Pipeline:MaxConcurrentFiles` = WorkerCount); `QueuePollInterval = 0`
  - Dev dashboard: `/hangfire`
  - Upload enqueues after persist; HTTP returns while Hangfire runs stub
  - `GET file` exposes `publicStatusKey` + `internalStageKey` for poll smoke
  - Config: `Pipeline:StubStageDelayMs` (0 = realtime stub)

### DQ-0601 — Multi-file async upload
- **Status:** ✅ Complete  
- **Dependency:** DQ-0501, DQ-0401, DQ-0603  
- **Source:** Plan 03 Flow 1; External module  
- **Outcome:** `POST` N files → 202 + file_ids (+ batch_id if N≥2); routing lock on first file; concurrent enqueue proven.  
- **Required Documents:** Plan 02; Plan 03 Flow 1  
- **Evidence:**
  - `POST /api/v1/queues/{queueId}/files` multipart `files` → **202** `{ queueId, batchId?, fileIds }`
  - Batch when N≥2; each File persisted + blob + placeholder Document + Hangfire enqueue
  - Auth: F2 `X-Api-Key` only
  - **Follow-on (plan):** optional intake hints on upload (`documentCount` + type keys) — implement with DQ-0702 skip path

### DQ-0602 — Poll APIs
- **Status:** ✅ Complete  
- **Dependency:** DQ-0402  
- **Source:** Plan 02 poll; Plan 03  
- **Outcome:** List/get File and Document with filters (ids, dates, status, queue, file, batch).  
- **Required Documents:** Plan 02; Plan 03  
- **Evidence:**
  - `GET /api/v1/queues/{queueId}/files?status&batchId&createdFrom&createdTo`
  - `GET /api/v1/files/{fileId}`
  - `GET /api/v1/queues/{queueId}/documents?fileId&batchId&status&createdFrom&createdTo`
  - `GET /api/v1/documents/{documentId}`

### DQ-0603 — F2 API keys
- **Status:** ✅ Complete  
- **Dependency:** DQ-0102  
- **Source:** Decision F2 (bridge only)  
- **Outcome:** Issue/validate Business-scoped API keys for External; documented as **temporary bridge**. Superseded by DQ-1505/1506 — do not treat as permanent.  
- **Required Documents:** Plan 03 Decision F; TenantApiKey entity  
- **Evidence:**
  - App: `POST/GET/DELETE /api/app/api-keys` (DevBypass); raw key returned **once** on create (`dm_{prefix}_{secret}`)
  - `ApiKeyAuthenticationHandler` via `X-Api-Key` (or `Authorization: ApiKey …`); SHA-256 hash + prefix lookup
  - Policy scheme routes `/api/v1` → ApiKey; app routes → DevBypass
  - Temporary — retire Band 15 / DQ-1506

### DQ-0701 — OCR / normalize
- **Status:** ✅ Complete  
- **Dependency:** DQ-0501  
- **Source:** Plan 03 Wave 4 Mode 1  
- **Outcome:** Adapter(s) produce text/layout usable by split/classify; Mode 1 credentials server-side only.  
- **Required Documents:** Plan 03  
- **Evidence:**
  - `IOcrNormalizeAdapter` / `Mode1OcrNormalizeAdapter` — text passthrough for text/*; stub layout for binaries
  - Artifacts in object storage: `…/artifacts/normalize.text.txt` + `normalize.layout.json` (sibling of File key)
  - Hangfire pipeline calls normalize at `file_internal_stage=normalize`; WorkEvent refs only (no full OCR text)
  - Placeholder Document gets `SliceRefJson` + page range for 0702
  - `Providers:DefaultOcrApiKey` arms providerKey `aws_textract` (real Textract body later); else `stub_normalize`
  - On failure: File/Document → `failed` / `normalize_failed`

### DQ-0702 — Split / classify / route (E3)
- **Status:** ✅ Complete (Phase 1 slice)  
- **Dependency:** DQ-0701, DQ-0302  
- **Source:** Decision E3 (+ intake-hints); exploration 04 P0/C0 locked  
- **Outcome:** Pipeline structure is normalize → **split → classify → route**. Skip split+classify **only** when caller `documentTypeKey` **and** normalize `pageCount==1`. Type-only multi-page Files still split (Phase 1: placeholder Document; stamp type after split). Real page split **deferred**. Unroutable type → Failed `unroutable_type`.  
- **Required Documents:** Plan 03 Flow 1 hints; [`04-split-classify-strategy-exploration.md`](./04-split-classify-strategy-exploration.md)  
- **Evidence:**
  - `IFileSplitStage` / `IFileClassifyStage` / `IDocumentRouteStage` in the Hangfire File worker
  - `OpsFile.IntakeHintsJson`; upload form fields `documentTypeKey`, optional `documentCount`
  - Skip WorkEvents: `skipped:true, reason:predetermined_type_single_page` (type + pageCount==1 only)
  - Typed multi-page: split not skipped; classify stamps caller type (`type_hint_after_split`)
  - Without type: split/classify log `deferred:true` and one placeholder Document
  - Typed + QueueRoute → Document.AgentId set; missing route → `unroutable_type`
  - Real split/classify algorithms: later phase (exploration 04 remainder)
  - Smoke 2026-08-18: unknown `documentTypeKey` → 400; typed+route → File ready, Document `invoice` + AgentId; no type → deferred placeholder (untyped); typed without QueueRoute → File/Document failed


### DQ-0703 — Extract + schema validate
- **Status:** ✅ Complete  
- **Dependency:** DQ-0702, DQ-0202  
- **Source:** Plan 03 Core extract Mode 1  
- **Outcome:** Documate meta-provider extract into Agent schema; validate → Ready/Failed; WorkEvents recorded.  
- **Required Documents:** Plan 03  
- **Evidence:**
  - `IDocumentExtractStage` / `Mode1DocumateMetaExtractAdapter` (`providerKey=documate_meta`) after route
  - Schema-guided fill from normalize text (label:value or JSON); live LLM when `Providers:DocumateMetaApiKey` / `DefaultLlmApiKey` is later wired (`llmArmed` log)
  - `JsonSchemaLite` validate → Ready or Failed `schema_invalid`; extract exceptions → `extract_failed`
  - No routed Agent → Failed `no_agent`; unroutable still `unroutable_type`
  - `OpsDocument.ResultJson` + artifact `extract.{sequenceId}.result.json`; External poll returns `resultJson` / `errorCode`
  - Unit tests: `tests/api/ExtractTests.cs`
  - Smoke 2026-08-18: typed invoice labels → ready `resultJson.invoice_number=INV-0703`; no type → `no_agent`; no QueueRoute → `unroutable_type`

### DQ-0704 — Real OCR + live LLM extract
- **Status:** ✅ Complete
- **Dependency:** DQ-0703, DQ-0901, DQ-0304
- **Source:** [05-ocr-normalize-real-providers-exploration.md](./05-ocr-normalize-real-providers-exploration.md); [05-ocr-llm-extract-implementation-plan.md](./05-ocr-llm-extract-implementation-plan.md); Queue K1
- **Outcome:** End-to-end useful path on **default Queue**: real OCR text/layout artifacts (Textract → Google Document AI fallback) + live LLM field extract (Agent/template provider) + schema validate; sync gates (≤3 pages, ≤5 MB, 60s); App upload optional priority; LLM fail after retry → ops email (best-effort when notifications enabled).
- **Required Documents:** Exploration 05; Implementation plan 05; Plan 02 §3.1; Plan 03 Wave 4
- **Evidence:**
  - `Ocr:` / `Llm:` / `Notifications:` options in appsettings + `.env.example`; `LlmStartupGate` requires default provider ApiKey+Model (skip only `Testing`)
  - Catalog: `google_document_ai` seeded; AgentTemplate `DefaultProviderId` → `gpt_5_6`
  - Normalize: `TextractOcrEngine` + `GoogleDocumentAiOcrEngine` compose primary→secondary; artifacts `normalize.text.txt` + `normalize.layout.json`; Textract sync ≤8 pages / single-page PDF bytes; multipage/large → S3 async Textract when `Storage:Provider=s3`, else Google fallback
  - Extract: `LiveLlmDocumentExtractAdapter` HTTP OpenAI-compatible / Anthropic; full normalize text; retry once; Document `ProviderId` = `documate_meta`; WorkEvent payload carries concrete `providerKey`
  - Sync extract: `SyncExtractGates` reject >3 pages or >5 MB before persist; wait 60s unchanged
  - App `priority=high` → Hangfire queue `priority` (server listens `priority`, `default`, `webhooks`)
  - LLM final fail → `IOpsAlertSender` log; SMTP when `Notifications:Enabled` (mail never changes status)
  - Packages: AWSSDK.Textract, Google.Cloud.DocumentAI.V1, MailKit; `dotnet build` OK; unit tests 18 passed
  - Smoke: set user-secrets for `Llm:Providers:gpt_5_6:ApiKey` (+ OCR keys); restart API; upload on **defaultQueueId** with QueueRoute → expect real OCR artifacts + LLM `resultJson` (not heuristic stub)

### DQ-0705 — Real per-page OCR artifacts (Wave 4b)
- **Status:** ✅ Complete  
- **Dependency:** DQ-0704  
- **Source:** Plan 03 Wave 4b; Plan 04 §15.5  
- **Outcome:** Normalize writes genuine per-page text/layout artifacts (`normalize.page.{n}.*`) usable by P9 intelligence; measured `pageCount` remains authoritative.  
- **Required Documents:** Plan 04; Plan 03 Wave 4b; Exploration 05  
- **Evidence:**
  - `OcrPageSplitter` — Textract LINE blocks grouped by `Block.Page`; Google page text via `TextAnchor` segments (paragraphs fallback); blank pages flagged (`IsBlank`)
  - Textract sync/async + Google engines return real `OcrPageText[]` (no whole-doc duplication)
  - `Mode1OcrNormalizeAdapter` writes file-level `normalize.text.txt` / `normalize.layout.json` **plus** `normalize.page.{n}.text.txt` + `normalize.page.{n}.layout.json`
  - `NormalizeResult.PageArtifacts` + `SliceRefJson.pageArtifacts[]`; WorkEvent normalize payload includes `pageArtifactCount` / `blankPageCount`
  - Unit tests: `tests/api/OcrPageSplitterTests.cs` (5); full suite **74** passed (2026-09-15)

### DQ-0706 — Admin pipeline model settings (Wave 4b)
- **Status:** ✅ Complete  
- **Dependency:** DQ-1411, DQ-0705  
- **Source:** Plan 03 Decision E (dual models + fallback); Plan 04 §10.3  
- **Outcome:** Backoffice/system settings bind provider/model for intelligence T1, intelligence fallback, and extract (Mode 1; not partner-facing). Caps for intelligence calls per File configurable.  
- **Required Documents:** Plan 03 Decision E; Plan 15 settings; Plan 04  
- **Evidence:** `SystemSettingKeys`, `SystemSettingsSeeder`, and `PipelineModelSettings` expose DB-backed intelligence T1/fallback/extract providers plus the 40-call default; provider defaults bootstrap from `Llm:DefaultProviderKey`; registered in `Program.cs`.

### DQ-0707 — Page intelligence T1 + fallback (Wave 4b)
- **Status:** ✅ Complete  
- **Dependency:** DQ-0706  
- **Source:** Plan 04 P9/F6; Plan 03 Decision E  
- **Outcome:** Per-page intelligence call with T1 cheap model; unrecognized/ambiguous → fallback model (page or local 3-page window); persist `intelligence.page.{n}.json`; never combine with extract `documentData`.  
- **Required Documents:** Plan 04 §§14–15; explained guide  
- **Evidence:** `PageIntelligenceService` reads per-page text, calls configured T1 then fallback through `documate-llm`, caps calls, persists `intelligence.page.{n}.json`, and falls back to blank/INV/DN/CN heuristics.

### DQ-0708 — P9 boundary engine + classify + slices + observability (Wave 4b)
- **Status:** ✅ Complete  
- **Dependency:** DQ-0707, DQ-0702  
- **Source:** Plan 04 P9/F6 locks; Plan 03 Flow 1  
- **Outcome:** Pure-function grouping with anchors + reset rules (blank, new identity, sequence restart, N=2); classify after group (C1 / type hint / no-hint QueueRoute); Failed on unresolved (no forced cut); persist text+layout slices; full signal/anchor audit artifacts + WorkEvent summaries. Replaces deferred no-ops from DQ-0702.  
- **Required Documents:** Plan 04 Finalized Decisions; Plan 03 Decision E / Flow 1  
- **Evidence:** `DocumentBoundaryEngine` implements blank/new-identity/restart/N=2 boundaries; split creates page-range Documents and `documents/{seq}/slice.*`; classify applies hint, C1, or route-constrained intelligence type; grouping WorkEvent and boundary tests added.

### DQ-0709 — Per-Document PDF + DownloadUrl cache (Wave 4b)
- **Status:** ✅ Complete  
- **Dependency:** DQ-0708, DQ-0401  
- **Source:** Plan 03 security §6 (amended); Plan 04 Document URL rule  
- **Outcome:** After grouping, materialize each Document’s PDF into object storage. Persist `DownloadUrl` + `DownloadUrlExpiresAt` on Document **only after** that PDF exists; refresh when expired. **Never** use parent File URL as Document URL (omit/null until PDF ready). File keeps original-upload URL cache separately.  
- **Required Documents:** Plan 03 entity catalog Document/File; explained guide URL section  
- **Evidence:** `DocumentPdfMaterializer` copies PDF ranges or a single image to `document.{seq}.pdf`; File/Document URL cache columns and migration `Wave4bDownloadUrlAndDocumentPdf` added; `SignedDownloadUrlService` refreshes by `Storage:SignedUrlMinutes`.

### DQ-0710 — Extract on grouped slices (Wave 4b)
- **Status:** ✅ Complete  
- **Dependency:** DQ-0709, DQ-0703  
- **Source:** Plan 03 Decision E; Plan 04 (identification ≠ extract)  
- **Outcome:** Extract uses configured extract model on grouped Document slices (separate from intelligence); Case A and hint rules unchanged; multi-doc packs produce N Ready/Failed Documents.  
- **Required Documents:** Plan 03 Flow 1; Plan 04  
- **Evidence:** `DocumentExtractStage` reads each Document slice and uses `PipelineModelSettings.ExtractProviderKey`; identification stays separate and never returns extracted document data.

### DQ-0801 — Per-Document webhooks
- **Status:** ✅ Complete  
- **Dependency:** DQ-0703, DQ-0303  
- **Source:** Plan 02 §9.2; Plan 03 Flow 1  
- **Outcome:** On Document terminal, HTTPS webhook + HMAC; attempts/metadata on Document; poll still works if webhook fails.  
- **Required Documents:** Plan 02 webhook payload fields  
- **Evidence:**
  - Enqueue on Document terminal (`ready`/`failed`/…); `api_sync` → `skipped` (C2 ready for DQ-0901)
  - POST `document.terminal` snake_case payload; `X-Documate-Signature: sha256=…` from Data-Protected queue secret (`WebhookSecretProtected`)
  - Status: `not_configured` | `pending` | `succeeded` | `exhausted` | `skipped`; attempts + last HTTP on Document
  - Self-scheduled retries (30s…10m, max 5); Hangfire `webhooks` queue; poll still returns `resultJson` if delivery fails
  - External poll: `webhookStatusKey`, `webhookAttempts`, `webhookLastHttpStatus`
  - Unit tests: HMAC + payload shape; smoke 2026-08-18: http://127.0.0.1 listener → succeeded HTTP 200 + signature; no URL → `not_configured` and Document still `ready`

### DQ-0802 — File/Document download URLs on webhook + External detail (Wave 4b)
- **Status:** ✅ Complete  
- **Dependency:** DQ-0709, DQ-0801, DQ-0602  
- **Source:** Plan 03 security §6 (amended); explained guide  
- **Outcome:** Webhook includes Document PDF URL **only when** Document PDF exists (omit otherwise — never parent-File URL). External `GET` Document returns URL under same rule. External `GET` File returns File original URL + **embedded full Document objects** (each with URL only if Document PDF ready). Shared refresh-after-expiry helper.  
- **Required Documents:** Plan 03 Permissions §6; Plan 04 Document URL rule  
- **Evidence:** Document detail refreshes its PDF URL; File detail refreshes the original File URL and embeds full Documents; sync DTOs carry URLs; webhook emits only the Document PDF URL/expiry and never substitutes the parent File URL.

### DQ-0901 — Sync-wait API
- **Status:** ✅ Complete  
- **Dependency:** DQ-0703, DQ-0603  
- **Source:** Decisions B, C2, G; Plan 03 Flow 2  
- **Outcome:** Sync extract: fail if >1 Document; else wait Ready/Failed or 60s; timeout returns ids; **no webhook**.  
- **Required Documents:** Plan 03 Decisions B/C/G; Plan 02 §9.1  
- **Evidence:**
  - `POST /api/v1/queues/{queueId}/extract` (API key); one `file`; intake source `api_sync`
  - HTTP waits until File+Document terminal or `Pipeline:SyncWaitTimeoutSeconds` (default 60); `timedOut` + `fileIds`/`documentIds` always returned
  - `documentCount > 1` → 400; `>1` Document after pipeline → 409; multi-file belongs on async `/files`
  - C2: webhook status `skipped` (no POST) even if Queue webhook is enabled
  - 200 body includes `fileStatus` + `documents[]` (same poll DTO, including `resultJson`)
  - Smoke 2026-08-18: typed invoice → 200 `timedOut=false` ready + `INV-0901` + `webhookStatusKey=skipped`; `documentCount=2` → 400

### DQ-1001 — Cancel File / Document
- **Status:** ✅ Complete
- **Dependency:** DQ-0703, DQ-0801
- **Source:** Plan 02 cancel rules
- **Outcome:** Cancel File aborts pack → Cancelled + webhooks for newly cancelled docs; Cancel Document single-doc; file rollup updates.
- **Required Documents:** Plan 02
- **Evidence:**
  - `ICancelWorkService` / `CancelWorkService` — Plan 02 §11; sets `CancelledAt` / `CancelledByUserId` / `ErrorCode=cancelled`
  - `POST /api/v1/files/{fileId}/cancel` — file → `cancelled` (overrides rollup); non-terminal docs → `cancelled` + `ScheduleIfTerminalAsync`; Ready/Failed/Rejected docs unchanged; idempotent if already cancelled
  - `POST /api/v1/documents/{documentId}/cancel` — doc → `cancelled` + webhook; file rollup via `FilePublicStatusRollup` (Plan 02 §6.2); 409 if doc already Ready/Failed/Rejected
  - Pipeline: `FilePipelineStub` aborts on cancelled (reload between stages); extract skips cancelled docs / cancelled file; does not overwrite whole-file cancel
  - WorkEvents: `work_event_type=cancelled` on cancel; file rollup `status_changed` after cancel-doc
  - Postman: cancel file + cancel document requests; unit tests for rollup (21 total passed)

### DQ-1002 — Reprocess
- **Status:** ✅ Complete
- **Dependency:** DQ-0601, DQ-0703
- **Source:** Plan 01/02 reprocess explicit → new File
- **Outcome:** Explicit reprocess creates new File (link ReprocessOfFileId) and new Documents; new webhooks.
- **Required Documents:** Plan 02
- **Evidence:**
  - `POST /api/v1/files/{fileId}/reprocess` → **202 Accepted** + new `ExternalFileDto` (`reprocessOfFileId` set); source File unchanged
  - `IReprocessWorkService`: download source blob → `CreateFileWithBlobAsync` (new storage key) with `ReprocessOfFileId` + copied hints/hash → Hangfire `EnqueueFileAsync`
  - Single-file reprocess → `BatchId=null` (Plan 02 §11.3); Documents/webhooks from normal pipeline
  - `CreateFileWithBlobRequest` extended with optional `ReprocessOfFileId` / `ContentHash`
  - Postman: POST reprocess file; unit tests 21 passed

### DQ-1101 — Agent post-processing + internal MCP
- **Status:** ✅ Complete
- **Dependency:** DQ-0703
- **Source:** Agent-primary workflow; Plan 03 Wave 6 (corrected: Agent not Queue)
- **Outcome:** After extract, run Agent.WorkflowId steps via internal MCP host; ≥1–2 platform tools (e.g. date/currency normalize stub).
- **Required Documents:** Plan 01 §13; Plan 03 Agent WorkflowId
- **Evidence:**
  - Internal MCP: `IInternalMcpHost` + platform tools `normalize_date`, `normalize_currency` (not OCR/LLM as MCP)
  - `IAgentPostProcessRunner` runs `Agent.DefaultWorkflowId` → `CorWorkflowDefinition.DefinitionJson` steps after schema validate, before Ready
  - Fail → `post_process_failed`; stage `document_internal_stage=post_process`; webhooks still on terminal
  - Bootstrap: Business provision creates `normalize_fields_v1` workflow; Agent create/clone auto-attaches when `DefaultWorkflowId` unset
  - DefinitionJson: `{"steps":[{"tool":"normalize_date","fields":["*"]},{"tool":"normalize_currency","fields":["*"]}]}`
  - Unit tests for date/currency tools; 23 tests passed

### DQ-1201 — Email IntakeMailbox + simulate + decision skeleton
- **Status:** ✅ Complete  
- **Dependency:** DQ-0501, DQ-0402  
- **Source:** [14-email-intake-ses-implementation-plan.md](./14-email-intake-ses-implementation-plan.md) EI-0…EI-4; Plan 01 §14; Decision D3+D1  
- **Outcome:** `OpsIntakeMailbox` (typed_agent + multi_type) on `docsintake.com`; portal CRUD/rotate; simulate API; Layer-1 gates; heuristic intake decision; Files or IntakeRejection on default Queue.  
- **Required Documents:** Plan 14 exploration + implementation; Plan 01 §14; Plan 03 Flow 3  
- **Evidence:**
  - Migration `20260911140000_OpsIntakeMailbox`; enum `intake_mailbox_kind`; `CorTenantBusiness.IntakeEmailSlug`
  - APIs under `/api/app/intake-mailboxes` (+ simulate); Core `IEmailIntakeProcessor` + `HeuristicEmailIntakeDecisionAgent`
  - UI: Agent “Intake email” tab; Channels multi-type mailboxes + allowlist
  - Unit tests: `EmailIntakeTests` (7) passed 2026-09-11

### DQ-1202 — Real SES inbound (D1)
- **Status:** ✅ Complete (code + runbook; live MX smoke after ops domain verify)  
- **Dependency:** DQ-1201  
- **Activation trigger:** Domain verified + receipt rule → S3/SNS; IMAP **not** in scope.  
- **Source:** Plan 14 EI-5; Decision D1  
- **Outcome:** SES catch-all consumer → same Core processor as simulate.  
- **Required Documents:** Plan 14; [docs/ops/ses-email-intake-runbook.md](../ops/ses-email-intake-runbook.md)  
- **Evidence:**
  - `POST /api/internal/email-intake/sns` → Hangfire `EmailIntakeJobs` → S3 MIME parse → processor
  - Unknown To: drop + log (DR-EI1)

### DQ-1203 — Email intake harden (EI-6)
- **Status:** ✅ Complete (baseline)  
- **Dependency:** DQ-1201  
- **Source:** Plan 14 EI-6  
- **Outcome:** Metrics/logging for unknown recipient + gate rejects; rate guidance; allowlist_enforced sell checklist.  
- **Required Documents:** Plan 14  
- **Evidence:** Structured logs on accept/reject/unknown; size/count gates in `EmailIntake` options; sell posture in exploration E7 + runbook  


### DQ-1301 — Angular Agents + Business default channel
- **Status:** ❌ Cancelled  
- **Cancellation note:** Superseded 2026-08-30 by customer frontend MVP Band 16 — see [07-customer-frontend-dispatch-queue.md](./07-customer-frontend-dispatch-queue.md) (DQ-1606, DQ-1609, etc.). Do not execute this thin Wave 8 shell.  
- **Dependency:** DQ-0204, DQ-0304, DQ-0303, DQ-0101  
- **Source:** Plan 03 Wave 8; Decision **K1**; superseded by Plan 07  
- **Outcome:** (cancelled)  
- **Required Documents:** —  
- **Evidence:** Cancelled in favor of DQ-1601+  

### DQ-1302 — Angular monitor UI
- **Status:** ❌ Cancelled  
- **Cancellation note:** Superseded 2026-08-30 by Band 16 Files UI (DQ-1604…) without cancel/reprocess in MVP. See [07-customer-frontend-dispatch-queue.md](./07-customer-frontend-dispatch-queue.md).  
- **Dependency:** DQ-0602, DQ-1001, DQ-1301  
- **Source:** Plan 03 Wave 8; superseded by Plan 07  
- **Outcome:** (cancelled)  
- **Required Documents:** —  
- **Evidence:** Cancelled in favor of DQ-1602/1604+  

### DQ-1401 — Hardening
- **Status:** ✅ Complete
- **Dependency:** DQ-0801, DQ-0303
- **Source:** Plan 03 Wave 9; Plan 14 EI-6
- **Outcome:** Rate/size limits; allowlist enforcement path; basic metrics/logs; do not market email hard until allowlist UX ready.
- **Required Documents:** Plan 03; Plan 01 email gates
- **Evidence:**
  - Size: `MaxAttachmentBytes`, `MaxTotalAttachmentBytes`, `MaxAttachments` enforced in `EmailIntakeProcessor` (reject codes `attachment_too_large`, `attachments_total_too_large`, `too_many_attachments`)
  - Rate: `MemoryEmailIntakeRateLimiter` + `RateLimitPerMailboxPerMinute`/`Hour` → reject `rate_limited`
  - Allowlist: `EmailAllowlistMatcher.Evaluate` (display-name From normalize; preferred miss metric; enforced empty list rejects); Agent + Channels UI mode + entries; runbook §8 checklist before sell hard
  - Metrics: `Documate.EmailIntake` — `unknown_recipient`, `rate_limited`, `rejected{code}`, `accepted`, `files_accepted`, `allowlist_preferred_miss`, `process_duration_ms`
  - Catalog: `GET /api/app/catalogs/enums/{typeKey}` for allowlist mode picker
  - Tests: allowlist display-name / preferred / empty enforced; rate limiter unit test (`tests/api/EmailIntakeTests.cs`)
  - Sync gates already config-tunable (`Pipeline:SyncWaitTimeoutSeconds` / `SyncMaxPages` / `SyncMaxBytes`) — no code change
  - Note: still do **not** market email as hard until partners use `allowlist_enforced`

### DQ-1402 — Upload intake performance
- **Status:** ✅ Complete
- **Dependency:** DQ-0601, DQ-0401, DQ-1401 (metrics baseline)
- **Source:** Dev smoke — ~16s to accept 3×87 KB PDFs; intake path not sized for partner batch upload
- **Outcome:** Faster **upload accept** path (202 / Created) — not full OCR/LLM pipeline. Target: multi-file batch intake dominated by blob I/O, not sequential SQL.
- **Likely work:**
  - Parallelize per-file blob writes in `ExternalUploadFilesHandler` (and app upload batch if added)
  - Reduce `CreateFileWithBlobAsync` round trips (cache queue + tenant scope per request; coalesce SaveChanges / defer WorkEvents to batch flush)
  - Optional: presigned direct-to-storage upload (design spike only if needed)
  - Intake timing metrics: `upload.accept_ms`, `upload.blob_ms`, `upload.db_ms` per file/batch
  - Document: async upload returns immediately; sync-wait / poll is separate from intake latency
- **Acceptance:** 3×100 KB files → HTTP accept **&lt; 2s** p95 local dev (excluding pipeline); evidence in DQ entry
- **Required Documents:** Plan 03 Wave 4 intake; DQ-0601
- **Evidence:**
  - `CreateFilesWithBlobsBatchAsync`: one queue/scope lookup, one insert `SaveChanges`, parallel blob uploads (max 8), one finalize `SaveChanges` + batched WorkEvents
  - `ExternalUploadFilesHandler` buffers multipart → batch create → parallel Hangfire enqueue
  - Metrics meter `Documate.UploadIntake`: `upload.accept_ms` / `upload.blob_ms` / `upload.db_ms` / `upload.files_accepted`
  - Single-file `CreateFileWithBlobAsync` delegates to batch path (same reduced round trips)
  - Tests: `UploadIntakeTimerTests`; full suite green
  - Note: re-smoke 3×~100KB via `POST /api/v1/queues/{id}/files` after deploy; accept latency should be blob-dominated (&lt;2s local)

### DQ-1410 — System settings table + wire consumers
- **Status:** ✅ Complete  
- **Dependency:** DQ-1401  
- **Source:** [15-system-settings-db-implementation-plan.md](./15-system-settings-db-implementation-plan.md) SS-0…SS-2; exploration S1–S6  
- **Outcome:** `CorSystemSetting` (`SettingKey` + `ValueJson`); seed missing keys from appsettings (EmailIntake ops + Pipeline sync gates + `MimeRetentionDays`=30 + `BodyExcerptMaxChars`); `ISystemSettings` memory cache + invalidate-on-write; thin adapters so EmailIntake/Pipeline read DB (secrets stay env).  
- **Required Documents:** Plan 15 exploration + implementation  
- **Evidence:**
  - Entity `CorSystemSetting` + migration `20260914182443_CorSystemSetting`
  - `ISystemSettings` / `MemoryCachedSystemSettings`; `SystemSettingKeys` allowlist; `SystemSettingsSeeder` on boot (after Migrate)
  - `IEmailIntakeSettings` / `IPipelineSyncSettings` adapters; wired processor, rate limiter, SES handler, sync extract, SNS secret read
  - `MimeRetentionDays` / `BodyExcerptMaxChars` added to `EmailIntakeOptions` bootstrap
  - `dotnet build` OK (alt output path)

### DQ-1411 — Admin system-settings API
- **Status:** ✅ Complete  
- **Dependency:** DQ-1410  
- **Source:** Plan 15 SS-3…SS-4; DR-SS1 amended  
- **Outcome:** `GET/PUT /api/admin/system-settings` (+ list/get by key) with **separate admin credentials** (not customer `/api/app` auth); allowlisted keys only; tests for seed idempotency, cache invalidate, unknown key reject.  
- **Required Documents:** Plan 15 implementation; Auth options pattern  
- **Evidence:** `Auth:AdminGate` + `AdminGateAuthenticationHandler`; `POST /api/admin/auth/login`; `GET/PUT /api/admin/system-settings`; policy scheme routes `/api/admin` → AdminGate; `SystemSettingsTests` (allowlist/seed/cache).

### DQ-1204 — Email follow-on: provenance JSON + excerpt
- **Status:** ✅ Complete  
- **Dependency:** DQ-1203  
- **Source:** [14-email-intake-followon-implementation-plan.md](./14-email-intake-followon-implementation-plan.md) FO-0…FO-2; F4/F2 locks  
- **Outcome:** `OpsFile.EmailIntakeJson`; keep `EmailSubject` / `EmailMessageId` / `EmailFrom` (denormalized); sender-chain resolver; body excerpt in JSON only; wire File create.  
- **Required Documents:** Email follow-on impl (DR-FO4…FO5, FO9)  
- **Evidence:** Migration `20260914183215_OpsFileEmailIntakeJson`; `EmailSenderChainResolver` + `EmailBodyExcerpt` + `EmailIntakeJsonBuilder`; MIME FromName/Reply-To/Resent-From; wired in `EmailIntakeProcessor` / `WorkRecordService`; `EmailSenderChainResolverTests`.

### DQ-1205 — Email follow-on: F3 partial + surfaces
- **Status:** ✅ Complete  
- **Dependency:** DQ-1204  
- **Source:** Follow-on FO-3a…FO-3b; DR-FO6…FO8  
- **Outcome:** Skip bad/oversize attachments with `skippedAttachments` in JSON; rejection `no_processable_attachments` when none left; expose intake JSON on app Files, External DTOs, webhooks.  
- **Required Documents:** Email follow-on impl  
- **Evidence:** Decision agent skip list + ProcessPartial; processor oversize skip (DR-FO8); FileDto/ExternalFileDto/webhook email fields; Files detail UI email card; `HeuristicEmailIntakeDecisionAgentTests`.

### DQ-1206 — Email follow-on: S3 MIME retention
- **Status:** ✅ Complete  
- **Dependency:** DQ-1205, DQ-1410  
- **Source:** Follow-on FO-4…FO-5; DR-FO1=B, FO10=A  
- **Outcome:** Daily Hangfire job deletes intake-prefix S3 objects older than `MimeRetentionDays` (default 30 from settings); metrics/tests/runbook.  
- **Required Documents:** Email follow-on impl; Plan 15 settings key  
- **Evidence:** `EmailIntakeMimeRetentionService`; recurring `email-intake-mime-retention`; metric `email_intake.mime_deleted`; runbook §9; `EmailIntakeMimeRetentionTests`.

### DQ-1501 — Inventory Iden APIs + contract note
- **Status:** ❌ Cancelled — superseded by Band 20 (plan 25 / DQ-2001+)  
- **Dependency:** —  
- **Activation trigger:** ~~Start after Phase 1 product done-when accepted (Decision J3).~~  
- **Source:** Plan 03 Wave 10; `iden-constraints.md`; Decision J3  
- **Outcome:** Documented inventory of Iden endpoints/flows Documate needs (OIDC/JWT, Tenant, Business, memberships, M2M/clients). Short Documate-facing contract note checked into `docs/` (or linked from auth-wiring). Gaps listed explicitly.  
- **Required Documents:** Iden docs/repo; `iden-constraints.md`  
- **Evidence:** See plan 25 exploration + `docs/architecture/governance/auth-iden.md`

### DQ-1502 — Live Iden human auth (no fixed shipping tokens)
- **Status:** ❌ Cancelled — superseded by DQ-2002 / DQ-2008  
- **Dependency:** DQ-1501, DQ-0101  
- **Source:** Plan 03 Wave 10; Decision J3  
- **Outcome:** Angular + API use live Iden for humans. Fixed/dev bearer tokens absent from shipping configs (local-only bypasses documented and non-default).  
- **Required Documents:** DQ-1501 contract note; `auth-wiring-placeholder.md`  
- **Evidence:** Band 20 `Auth:Mode=Iden` + SPA `idenBaseUrl`

### DQ-1503 — Tenant→Business harness through Documate
- **Status:** ❌ Cancelled — superseded by DQ-2003–2005  
- **Dependency:** DQ-1502, DQ-0102  
- **Source:** Plan 03 H1; Iden tenancy  
- **Outcome:** Repeatable path: login → Tenant/Business context → CorTenant/CorTenantBusiness upsert → Business-scoped API call succeeds/fails correctly.  
- **Required Documents:** Plan 03 CorTenant sections  
- **Evidence:** Band 20 provisioner + admin Iden-first create

### DQ-1504 — Iden defect loop
- **Status:** ❌ Cancelled — continue ad-hoc under Band 20  
- **Dependency:** DQ-1503  
- **Source:** Plan 03 Wave 10  
- **Outcome:** Iden issues found via Documate are reproduced, filed against Iden, and fixed or explicitly waived. Documate does not paper over Iden bugs with permanent local hacks.  
- **Required Documents:** Iden issue tracker / PRs  
- **Evidence:** (ops process; not a Band 15 DQ)

### DQ-1505 — Iden M2M for External
- **Status:** ❌ Cancelled — out of Band 20 (3A Documate keys remain)  
- **Dependency:** DQ-1501, DQ-0601  
- **Source:** Plan 03 Wave 10; Decision F retirement path  
- **Outcome:** External APIs accept Iden machine credentials (client credentials / M2M as Iden provides) resolving to Business scope.  
- **Required Documents:** DQ-1501 contract note  
- **Evidence:** DR-KEY-1 A keeps Documate-owned `/api/v1` keys; Iden M2M deferred

### DQ-1506 — Retire F2 API keys
- **Status:** ❌ Cancelled — superseded by DR-KEY-1 A  
- **Dependency:** DQ-1505, DQ-0603  
- **Source:** Decision F2 bridge end  
- **Outcome:** TenantApiKey path removed or kill-switched off by default; docs state Iden-only machine auth. Migration note for any bridge keys.  
- **Required Documents:** Plan 03 TenantApiKey; DQ-1505  
- **Evidence:** Keys remain Documate-owned on `/api/v1`; hardened away from `/api/app` (DQ-2015)

### DQ-1507 — Auth + tenancy regression suite
- **Status:** ❌ Cancelled — superseded by DQ-2017  
- **Dependency:** DQ-1503, DQ-1505  
- **Source:** Plan 03 Wave 10  
- **Outcome:** Automated (CI-friendly) checks for human auth, Business scoping, and M2M External auth against Iden (or recorded Iden test env).  
- **Required Documents:** DQ-1501–1506  
- **Evidence:** `tests/api/Band20IdenAuthTests.cs`

---

## Readiness

**Decision J3 locked.** Decision **K1** locked (default channel). Band 15 ❌ cancelled → Band 20.  
**Waves 0–6 complete; DQ-0701–0704 ✅; DQ-0801 ✅; DQ-0901 ✅; DQ-1001–1002 ✅; DQ-1101 ✅; DQ-0304 ✅; DQ-0204 ✅.**  
**Wave 4b (Plan 04 real split) complete:** DQ-0705…0710 + DQ-0802 ✅ (API suite: 78 passed, 2026-09-15).  
**Also:** Band 17 back office — [16-backoffice-frontend-dispatch-queue.md](./16-backoffice-frontend-dispatch-queue.md). Upload perf `DQ-1402` ✅.  
Email intake Plan 14 / DQ-1201–1203 ✅; hardening DQ-1401 ✅. Customer web UI: [07-customer-frontend-dispatch-queue.md](./07-customer-frontend-dispatch-queue.md) (Band 16).  
**Postman:** [`docs/postman/Documate-v3-API.postman_collection.json`](../postman/Documate-v3-API.postman_collection.json).  
**OCR/LLM secrets:** `Llm:Providers:…:ApiKey` required at startup; `Ocr:Textract` / `Ocr:GoogleDocumentAi` for real OCR.  
**Jobs:** Hangfire dashboard (Dev) at `/hangfire` (queues: `priority`, `default`, `webhooks`).  
**External auth:** `X-Api-Key` (F2 temporary). Optional upload field: `documentTypeKey`. App upload: optional `priority=high|normal`.  
**Default channel:** `GET /api/app/me` returns `defaultQueueId` after provisioning.

Do **not** start coding until a DQ is selected (per `00-governance/06-dispatch-queue-execution.md`).
