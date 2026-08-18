# Documate v3 Phase 1 — Dispatch Queue

> **Document type:** Dispatch queue (Phase 3)  
> **Status:** ⬜ Ready for execution (no item started)  
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
| Total DQ items | 38 |
| ✅ Complete | 21 |
| 🔄 In Progress | 0 |
| ⬜ Ready | 9 |
| ⏸ Parked | 8 (DQ-1202 email; DQ-1501–1507 Iden follow-on J3) |
| ❌ Cancelled | 0 |

**Count note:** Phase 1 executable = bands 00–14 except DQ-1202. Band 15 parked until Phase 1 product accepted (Decision **J3**).

### Finalized decisions (from plan 03)

| ID | Decision |
|----|----------|
| A | **A1 + Hangfire (SQL)** — in-process Hangfire Server; durable jobs; webhooks later same backbone |
| B | Sync-wait: **single Document only**; multi-doc → fail; wait terminal or timeout |
| C | **C2** — no webhooks on sync-wait calls |
| D | Support **D1 and D2** later (one active at a time); Phase 1 stub only |
| E | **E3** + optional intake hints — full split/classify by default; skip when caller supplies complete documentCount + type(s) |
| F | **F2** — Business API keys (Phase 1 bridge → retire Band 15 after Phase 1) |
| G | Sync-wait max **60 seconds** |
| H | **H1** — CorTenant + CorTenantBusiness (product extension; Iden = SoT) |
| I | Queue UUID PK + SequenceId |
| J | **J3** — late Iden validation; Band 15 follow-on after Phase 1 product |
| — | Entity catalog **approved** |
| — | Multi-queue + QueueRoute day one; Agent-primary post-processing |
| — | **Iden Integration & Validation** (Band 15) — parked until Phase 1 done |

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
| F2 keys become permanent | J3: activate Band 15 right after Phase 1 product acceptance; keep F2 labeled temporary |
| Iden API gaps / bugs | Defect loop in Band 15; fix in Iden repo; explicit waivers only |
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
| DQ-0301 | 03 | Queue CRUD (multi-queue per Business) | ✅ | DQ-0102 |
| DQ-0302 | 03 | QueueRoute + RoutingLocked behavior | ✅ | DQ-0301, DQ-0202 |
| DQ-0303 | 03 | Queue webhook settings + email address mint + allowlist entries | ✅ | DQ-0301 |
| DQ-0401 | 04 | Blob/object storage for File bytes (business-prefixed keys) | ✅ | DQ-0004 |
| DQ-0402 | 04 | Persist Batch, File, Document, IntakeRejection, WorkEvent | ✅ | DQ-0002, DQ-0301 |
| DQ-0501 | 05 | In-process dispatcher (A1) + File pipeline stub (non-blocking enqueue) | ✅ | DQ-0402 |
| DQ-0601 | 06 | External: multi-file async upload → 202 + ids | ✅ | DQ-0501, DQ-0401, DQ-0603 |
| DQ-0602 | 06 | External: poll/list/get File & Document filters | ✅ | DQ-0402 |
| DQ-0603 | 06 | F2 Business-scoped API keys (temporary) | ✅ | DQ-0102 |
| DQ-0701 | 07 | Normalize/OCR adapter(s) Mode 1 | ✅ | DQ-0501 |
| DQ-0702 | 07 | Split → classify → route (E3 multi-doc PDFs) | ✅ | DQ-0701, DQ-0302 |
| DQ-0703 | 07 | Extract via Documate meta-provider + schema validate → Ready/Failed | ✅ | DQ-0702, DQ-0202 |
| DQ-0801 | 08 | Per-Document webhook dispatch + attempt metadata | ⬜ | DQ-0703, DQ-0303 |
| DQ-0901 | 09 | Sync-wait API (single-doc, 60s, no webhook C2) | ⬜ | DQ-0703, DQ-0603 |
| DQ-1001 | 10 | Cancel File and Cancel Document | ⬜ | DQ-0703, DQ-0801 |
| DQ-1002 | 10 | Explicit reprocess → new File | ⬜ | DQ-0601, DQ-0703 |
| DQ-1101 | 11 | Agent post-processing runner + internal MCP (1–2 platform tools) | ⬜ | DQ-0703 |
| DQ-1201 | 12 | Email intake stub + intake-decision agent skeleton | ⬜ | DQ-0501, DQ-0402 |
| DQ-1202 | 12 | Real email inbound D1 and/or D2 (one active at a time) | ⏸ | Activation: later email phase after stub proven |
| DQ-1301 | 13 | Angular: Agents + Queues configuration UI | ⬜ | DQ-0203, DQ-0303, DQ-0101 |
| DQ-1302 | 13 | Angular: Files/Documents monitor + cancel/reprocess actions | ⬜ | DQ-0602, DQ-1001, DQ-1301 |
| DQ-1401 | 14 | Hardening: rate/size limits, allowlist enforce path, metrics/logs | ⬜ | DQ-0801, DQ-0303 |
| DQ-1501 | 15 | Inventory Iden APIs + Documate-facing contract note | ⏸ | Activation: after Phase 1 product done-when (J3) |
| DQ-1502 | 15 | Live Iden human auth (Angular + API) — no fixed shipping tokens | ⏸ | DQ-1501, DQ-0101 |
| DQ-1503 | 15 | Integration harness: Tenant→Business through Documate | ⏸ | DQ-1502, DQ-0102 |
| DQ-1504 | 15 | Iden defect loop (reproduce via Documate; fix/track in Iden) | ⏸ | DQ-1503 |
| DQ-1505 | 15 | Iden M2M / machine auth for External APIs | ⏸ | DQ-1501, DQ-0601 |
| DQ-1506 | 15 | Retire F2 TenantApiKey (remove or kill-switch); docs Iden-only | ⏸ | DQ-1505, DQ-0603 |
| DQ-1507 | 15 | Auth + tenancy regression suite (CI-friendly) | ⏸ | DQ-1503, DQ-1505 |

**Count note:** Index includes DQ-1202 ⏸ and Band 15 ⏸ (J3). Phase 1 execution starts at DQ-0001.

---

## Wave Sections

### Wave 0 — Foundation
DQ-0001 → DQ-0002 → DQ-0003; DQ-0004 parallel after DQ-0001.

### Wave 1 — Identity & tenancy
DQ-0101 → DQ-0102.

### Wave 2 — Configuration (Agents)
DQ-0201 → DQ-0202 → DQ-0203.

### Wave 3 — Configuration (Queues)
DQ-0301 → DQ-0302, DQ-0303.

### Wave 4 — Storage & work records
DQ-0401, DQ-0402.

### Wave 5 — Dispatcher
DQ-0501.

### Wave 6 — External async + keys
DQ-0603 → DQ-0601, DQ-0602.

### Wave 7 — Core pipeline
DQ-0701 → DQ-0702 → DQ-0703.

### Wave 8 — Delivery
DQ-0801, DQ-0901, DQ-1001, DQ-1002.

### Wave 9 — Post-process + email stub + web + harden
DQ-1101, DQ-1201, DQ-1301 → DQ-1302, DQ-1401.  
DQ-1202 remains ⏸.

### Wave 10 — Iden Integration & Validation (J3 — follow-on)
All DQ-1501–1507 ⏸ until Phase 1 product done-when (waves 0–9) accepted.  
Then activate DQ-1501 → … → DQ-1507. F2 remains Phase 1 bridge only.

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
  - `TenantBusinessProvisioner` + middleware upserts CorTenant + CorTenantBusiness (TenantName projection)  
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

### DQ-0301 — Queue CRUD (multi-queue)
- **Status:** ✅ Complete  
- **Dependency:** DQ-0102  
- **Source:** Plan 03 Queue; multi-queue day one  
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
- **Outcome:** Pipeline structure is normalize → **split → classify → route**. Caller `documentTypeKey` **skips split+classify**, materializes Document(s), routes via QueueRoute. Real page split/classify **deferred**. Unroutable type → Failed `unroutable_type`.  
- **Required Documents:** Plan 03 Flow 1 hints; [`04-split-classify-strategy-exploration.md`](./04-split-classify-strategy-exploration.md)  
- **Evidence:**
  - `IFileSplitStage` / `IFileClassifyStage` / `IDocumentRouteStage` in the Hangfire File worker
  - `OpsFile.IntakeHintsJson`; upload form fields `documentTypeKey`, optional `documentCount`
  - Skip WorkEvents: `skipped:true, reason:predetermined_document_type`
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

### DQ-0801 — Per-Document webhooks
- **Status:** ⬜ Ready  
- **Dependency:** DQ-0703, DQ-0303  
- **Source:** Plan 02 §9.2; Plan 03 Flow 1  
- **Outcome:** On Document terminal, HTTPS webhook + HMAC; attempts/metadata on Document; poll still works if webhook fails.  
- **Required Documents:** Plan 02 webhook payload fields  
- **Evidence:** (fill on completion)

### DQ-0901 — Sync-wait API
- **Status:** ⬜ Ready  
- **Dependency:** DQ-0703, DQ-0603  
- **Source:** Decisions B, C2, G; Plan 03 Flow 2  
- **Outcome:** Sync extract: fail if >1 Document; else wait Ready/Failed or 60s; timeout returns ids; **no webhook**.  
- **Required Documents:** Plan 03 Decisions B/C/G; Plan 02 §9.1  
- **Evidence:** (fill on completion)

### DQ-1001 — Cancel File / Document
- **Status:** ⬜ Ready  
- **Dependency:** DQ-0703, DQ-0801  
- **Source:** Plan 02 cancel rules  
- **Outcome:** Cancel File aborts pack → Cancelled + webhooks for newly cancelled docs; Cancel Document single-doc; file rollup updates.  
- **Required Documents:** Plan 02  
- **Evidence:** (fill on completion)

### DQ-1002 — Reprocess
- **Status:** ⬜ Ready  
- **Dependency:** DQ-0601, DQ-0703  
- **Source:** Plan 01/02 reprocess explicit → new File  
- **Outcome:** Explicit reprocess creates new File (link ReprocessOfFileId) and new Documents; new webhooks.  
- **Required Documents:** Plan 02  
- **Evidence:** (fill on completion)

### DQ-1101 — Agent post-processing + internal MCP
- **Status:** ⬜ Ready  
- **Dependency:** DQ-0703  
- **Source:** Agent-primary workflow; Plan 03 Wave 6 (corrected: Agent not Queue)  
- **Outcome:** After extract, run Agent.WorkflowId steps via internal MCP host; ≥1–2 platform tools (e.g. date/currency normalize stub).  
- **Required Documents:** Plan 01 §13; Plan 03 Agent WorkflowId  
- **Evidence:** (fill on completion)

### DQ-1201 — Email stub + intake agent skeleton
- **Status:** ⬜ Ready  
- **Dependency:** DQ-0501, DQ-0402  
- **Source:** Decision D (later real); Plan 01 §14  
- **Outcome:** Admin/simulate email intake path; intake-decision agent skeleton; IntakeRejection when no File; ambiguity → reject.  
- **Required Documents:** Plan 01 §14; Plan 03 Flow 3  
- **Evidence:** (fill on completion)

### DQ-1202 — Real email D1 and D2
- **Status:** ⏸ Parked  
- **Dependency:** DQ-1201  
- **Activation trigger:** Start separate email inbound phase when ready to implement provider webhook **and** IMAP, with config selecting **one active** mechanism.  
- **Source:** Decision D  
- **Outcome:** (later) Production inbound email via D1 and/or D2.  
- **Required Documents:** Plan 03 Decision D  
- **Evidence:** —  

### DQ-1301 — Angular Agents + Queues UI
- **Status:** ⬜ Ready  
- **Dependency:** DQ-0203, DQ-0303, DQ-0101  
- **Source:** Plan 03 Wave 8  
- **Outcome:** Configure Agents (clone/edit), Queues, routes, webhook/email/allowlist in Angular (Plan 00 web conventions).  
- **Required Documents:** `angular-conventions.md`; Plan 03  
- **Evidence:** (fill on completion)

### DQ-1302 — Angular monitor UI
- **Status:** ⬜ Ready  
- **Dependency:** DQ-0602, DQ-1001, DQ-1301  
- **Source:** Plan 03 Wave 8  
- **Outcome:** Browse Files/Documents/rejections; cancel/reprocess actions.  
- **Required Documents:** Plan 03  
- **Evidence:** (fill on completion)

### DQ-1401 — Hardening
- **Status:** ⬜ Ready  
- **Dependency:** DQ-0801, DQ-0303  
- **Source:** Plan 03 Wave 9  
- **Outcome:** Rate/size limits; allowlist enforcement path; basic metrics/logs; do not market email hard until allowlist UX ready.  
- **Required Documents:** Plan 03; Plan 01 email gates  
- **Evidence:** (fill on completion)

### DQ-1501 — Inventory Iden APIs + contract note
- **Status:** ⏸ Parked  
- **Dependency:** —  
- **Activation trigger:** Start after Phase 1 product done-when accepted (Decision J3).  
- **Source:** Plan 03 Wave 10; `iden-constraints.md`; Decision J3  
- **Outcome:** Documented inventory of Iden endpoints/flows Documate needs (OIDC/JWT, Tenant, Business, memberships, M2M/clients). Short Documate-facing contract note checked into `docs/` (or linked from auth-wiring). Gaps listed explicitly.  
- **Required Documents:** Iden docs/repo; `iden-constraints.md`  
- **Evidence:** (fill on completion)

### DQ-1502 — Live Iden human auth (no fixed shipping tokens)
- **Status:** ⏸ Parked  
- **Dependency:** DQ-1501, DQ-0101  
- **Source:** Plan 03 Wave 10; Decision J3  
- **Outcome:** Angular + API use live Iden for humans. Fixed/dev bearer tokens absent from shipping configs (local-only bypasses documented and non-default).  
- **Required Documents:** DQ-1501 contract note; `auth-wiring-placeholder.md`  
- **Evidence:** (fill on completion)

### DQ-1503 — Tenant→Business harness through Documate
- **Status:** ⏸ Parked  
- **Dependency:** DQ-1502, DQ-0102  
- **Source:** Plan 03 H1; Iden tenancy  
- **Outcome:** Repeatable path: login → Tenant/Business context → CorTenant/CorTenantBusiness upsert → Business-scoped API call succeeds/fails correctly.  
- **Required Documents:** Plan 03 CorTenant sections  
- **Evidence:** (fill on completion)

### DQ-1504 — Iden defect loop
- **Status:** ⏸ Parked  
- **Dependency:** DQ-1503  
- **Source:** Plan 03 Wave 10  
- **Outcome:** Iden issues found via Documate are reproduced, filed against Iden, and fixed or explicitly waived. Documate does not paper over Iden bugs with permanent local hacks.  
- **Required Documents:** Iden issue tracker / PRs  
- **Evidence:** (fill on completion — issue/PR links)

### DQ-1505 — Iden M2M for External
- **Status:** ⏸ Parked  
- **Dependency:** DQ-1501, DQ-0601  
- **Source:** Plan 03 Wave 10; Decision F retirement path  
- **Outcome:** External APIs accept Iden machine credentials (client credentials / M2M as Iden provides) resolving to Business scope.  
- **Required Documents:** DQ-1501 contract note  
- **Evidence:** (fill on completion)

### DQ-1506 — Retire F2 API keys
- **Status:** ⏸ Parked  
- **Dependency:** DQ-1505, DQ-0603  
- **Source:** Decision F2 bridge end  
- **Outcome:** TenantApiKey path removed or kill-switched off by default; docs state Iden-only machine auth. Migration note for any bridge keys.  
- **Required Documents:** Plan 03 TenantApiKey; DQ-1505  
- **Evidence:** (fill on completion)

### DQ-1507 — Auth + tenancy regression suite
- **Status:** ⏸ Parked  
- **Dependency:** DQ-1503, DQ-1505  
- **Source:** Plan 03 Wave 10  
- **Outcome:** Automated (CI-friendly) checks for human auth, Business scoping, and M2M External auth against Iden (or recorded Iden test env).  
- **Required Documents:** DQ-1501–1506  
- **Evidence:** (fill on completion)

---

## Readiness

**Decision J3 locked.** Band 15 parked.  
**Waves 0–6 complete; DQ-0701 ✅; DQ-0702 Phase 1 slice ✅; DQ-0703 ✅.** Real split/classify later. Live LLM extract later.  
**Postman:** [`docs/postman/Documate-v3-Smoke-Waves-0-3.postman_collection.json`](../postman/Documate-v3-Smoke-Waves-0-3.postman_collection.json).  
**Next:** `DQ-0801` (per-Document webhooks).  
**Jobs:** Hangfire dashboard (Dev) at `/hangfire`.  
**External auth:** `X-Api-Key` (F2 temporary). Optional upload field: `documentTypeKey`.

Do **not** start coding until a DQ is selected (per `00-governance/06-dispatch-queue-execution.md`).
