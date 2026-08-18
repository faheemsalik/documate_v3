# Plan 03 — Documate v3 Product Delivery — Implementation Plan

> **Document type:** Implementation plan (Phase 2)  
> **Status:** Approved for Phase 3 — entity catalog verified; A–I closed; see dispatch queue  
> **Downstream:** [03-documate-v3-dispatch-queue.md](./03-documate-v3-dispatch-queue.md)  
> **Entity naming (locked 2026-08-03):** Infrastructure/catalogs = **`Cor*`**; operational work = **`Ops*`**. FK **columns** drop the prefix (`CorTenant` → `TenantId`, `OpsFile` → `FileId`).  
> **Track:** Product delivery (maps onto engineering modules from Plan 00)  
> **Upstream:** [00-product-glossary.md](./00-product-glossary.md), [01-project-exploration-mental-design.md](./01-project-exploration-mental-design.md), [02-document-queue-design.md](./02-document-queue-design.md)  
> **Downstream:** Phase 3 dispatch queue (not created until this plan is approved)  

**Scope:** Build Documate v3 Phase 1 product capability — queues, agents, multi-file intake, multi-doc extraction, async + sync delivery, email intake, **real Iden auth (human + machine)**, Mode 1 providers, internal MCP post-processing hooks. Includes an **Iden Integration & Validation** phase: discover Iden APIs, exercise them through Documate, fix Iden defects when found, and **retire fixed tokens / F2 temporary keys**.

**Out of scope:** Mode 2 BYOK UI, HITL, mapping catalogs, tenant MCP UX, general chatbots / non-document AI suite, replacing `old_code` deletion, **white-label SDK packaging (§3.1)**, **statements reconciliation (§3.2)**, **MCM DN rebranding (§3.3)**, **split/classify technique lock** (see [`04-split-classify-strategy-exploration.md`](./04-split-classify-strategy-exploration.md) — decide before DQ-0702).

---



## Metadata


| Field                    | Value                                              |
| ------------------------ | -------------------------------------------------- |
| Plan id                  | `03-documate-v3-product`                           |
| Apps                     | `apps/api` (.NET 10), `apps/web` (Angular)         |
| Modules                  | `Core`, `FrontendSupport`, `External` (A2 folders) |
| Product sources of truth | `01-…`, `02-…`                                     |


---



## Delivery Principles

1. **Product decisions live in 01/02** — this plan only sequences *how to build* them; do not reopen frozen product policy here without an explicit change request.
2. **Engineering conventions live in Plan 00 /** `docs/architecture/` — CQRS, thin controllers, **API responses use DTOs only** (never return Domain/EF entities as JSON). That is an API hygiene rule, not a product constraint.
3. `old_code/` **is reference-only history** — do **not** port, copy, or extend it into v3. Domain, APIs, and pipelines are **greenfield**. Leave the folder untouched unless a later explicit DQ archives/deletes it.
4. **Non-blocking intake is a hard goal** — multi-file accept must enqueue per file; never a global upload lock (Queue §2.1).
5. **Same pipeline for async and sync** — sync wait API is a delivery mode, not a second Core.
6. **Document is the result + webhook atom**; Batch is optional log-only; File owns split/rollup. (**Job** is not a product term — see glossary.)
7. **Mode 1 only in Phase 1** — hide providers; Documate meta-provider behind the scenes.
8. **Ship vertical slices** — each wave leaves something demonstrable (upload → process → poll/webhook or sync return).
9. **Iden is the identity SoT** — no second identity island. **Phase 1 (J3):** F2 / interim auth allowed to ship product. **Band 15 (follow-on):** live Iden (human + M2M), defect loop, retire F2 — do not leave temporary auth as the permanent story (Decisions F + J3).
10. **No general AI suite** — no chatbots / ERP MCP product surface beyond extraction post-processing tools.
11. **Phase 3 only after this plan is verified** — no production coding from this doc until DQ exists and items are selected (unless developer explicitly overrides).

---



## Domain Architecture Layer



### Product → engineering module map


| Product concern                                                                           | Module folder              | Notes                                             |
| ----------------------------------------------------------------------------------------- | -------------------------- | ------------------------------------------------- |
| Extraction, split/classify/route orchestration, provider adapters, workers                | `Modules/Core/`            | Owns pipeline stages; no public HTTP for partners |
| Queues, agents, templates, schemas UI APIs, Document/File browse, email config, workflows | `Modules/FrontendSupport/` | `/api/app/...`                                    |
| Async upload, poll, webhooks outbound contract, **sync wait** extract, cancel/reprocess   | `Modules/External/`        | `/api/v1/...`                                     |
| Persistence, blob/S3, Iden client, email receive, OCR/LLM clients, MCP tool host          | `Infrastructure/`          | Called from handlers/workers only                 |
| Entities (full catalog below)                                                             | `Domain/`                  | Never returned as API contracts — DTOs only       |




### Core pipeline (logical components)

```text
Intake (External upload | Email receiver)
  → File (+ optional Batch log)
  → normalize / OCR
  → split → classify → route (type→Agent)
  → Document[] 
  → extract (Mode 1 / Documate provider) → schema validate
  → post-process (platform tools via internal MCP)
  → terminal status
  → Delivery: webhook per doc (async) | wait response (sync) | poll always
```



### Web (`apps/web`) — Phase 1 surface (minimal)


| Area            | Purpose                                                            |
| --------------- | ------------------------------------------------------------------ |
| Auth via Iden   | Login / token                                                      |
| Agents          | Browse templates, guided clone, edit schema/instructions           |
| Queues          | Create queue, routing map, webhook, email address, workflow attach |
| Work monitor    | Files / Documents list + detail + cancel/reprocess                 |
| Rejected intake | IntakeRejection list                                               |


Rich NL workflow authoring deferred; Phase 1 workflow = attach/enable platform steps or simple config.

### Persistence / storage (conceptual — not vendor lock in this plan)


| Need             | Intent                                                              |
| ---------------- | ------------------------------------------------------------------- |
| Relational store | Entities in § Domain entity catalog                                 |
| Object storage   | Original files / OCR artifacts                                      |
| Work queue / bus | Non-blocking file & Document execution                              |
| Secrets config   | Webhook secrets, provider credentials (platform), optional API keys |


---



## Domain entity catalog (greenfield — for verification)

> **Purpose:** Phase 1 domain model for approval before coding. Not from `old_code`.  
> **Status:** Pending verification (updated for sequence ids, soft delete, Provider, DocumentType, Key, id strategy).



### Analyst notes (read before the tables)

**RowVersion — what it is:**  
Optimistic concurrency token (EF `rowversion` / SQL Server timestamp). When two users/processes load the same row, edit, and save, the second save fails instead of silently overwriting the first. It is **not** an audit trail and **not** a business version.  
**Verdict:** Keep on **mutable config** entities (Queue, Agent, Workflow, CorTenant). Skip on append-only `WorkEvent`. Optional on high-churn File/Document if workers use stage machines carefully — recommended on File/Document anyway to avoid lost status updates under concurrency.

**Soft delete — required (your call):**  
Every tenant-owned row gets soft-delete columns. Queries default to `IsDeleted = false`. Unique indexes on catalog `*Key` columns / email local-part must account for soft-deleted rows (filtered unique indexes).  
**Cost:** every list/query must filter; FKs to soft-deleted Agents/Queues need rules (block delete if routing locked / in use, or cascade soft-delete). Do not pretend soft delete is free.

**UUID vs sequence — DECIDED (your clarification):**


| Rule                                                                     | Implementation                                                                                             |
| ------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------- |
| Anything returned to users (External **or** app UI APIs, webhooks, poll) | `Id` **= UUID** (primary key **and** public wire id)                                                       |
| Maintenance / SQL quick lookup                                           | `SequenceId` **= bigint identity** (unique, not the wire contract)                                         |
| Reference/catalog rows never sent as resource ids to end users           | `Id` **= bigint identity** + entity-prefixed `*Key` (e.g. `ProviderKey`, `EnumTypeKey` — avoid bare `Key`) |


**Wire-facing (UUID PK + SequenceId):** CorTenant, CorTenantBusiness (if exposed), OpsAgent, OpsQueue, OpsBatch, OpsFile, OpsDocument, OpsIntakeRejection, CorTenantApiKey.

**Internal/reference (bigint PK + Key):** CorEnumType, CorEnum, Provider, DocumentType, AgentTemplate, WorkflowDefinition, QueueRoute, QueueEmailAllowlistEntry, WorkEvent.

**Iden tenancy:** **Isolation unit = Business** (`BusinessId` only on operational rows). Tenant lives on `CorTenant` / link on `CorTenantBusiness` — **do not** repeat `TenantId` on Queue/File/Document/…. `CorTenantBusiness.TenantName` is a **projection** (cached display) of the parent tenant name.

**Queue:** UUID `Id` on the wire + `SequenceId` for ops. **Decision I → I1 (closed).**

---



### Relationship overview

```text
CorTenant (Iden Tenant; ProviderModeEnumId)
 └── CorTenantBusiness[] (Iden Business)     ← isolation boundary
      ├── Agent (UUID Id; DocumentTypeId; SourceTemplateId)
      ├── WorkflowDefinition
      ├── Queue (UUID Id)
      │     ├── QueueRoute (DocumentTypeId → AgentId) LOCKABLE
      │     ├── QueueEmailAllowlistEntry
      │     └── WorkflowId?
      ├── Batch? (UUID Id)
      ├── File[] (UUID Id) → Document[] (UUID Id)
      ├── IntakeRejection[] (UUID Id)
      └── ApiKey? (F2 — Business-scoped preferred)

CorEnumType (EnumTypeKey) → CorEnum[] (EnumKey)          ← ERP-inspired lookup catalog (Scope: System / Tenant / Business)
Provider / DocumentType / AgentTemplate = global catalogs (ProviderKey / DocumentTypeKey / AgentTemplateKey)

WorkEvent → subject by type + subject's UUID Id (for wire-facing subjects)
```

---



### Entity purpose (logical why)


| Entity                       | Why it exists                                                                                                        |
| ---------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| **CorTenant**                | Documate mirror of an Iden Tenant — account-level settings (e.g. provider mode), not day-to-day work.                |
| **CorTenantBusiness**        | Documate mirror of an Iden Business — **isolation boundary** for ops config and work; holds `TenantName` projection. |
| **CorEnumType**              | Defines a named enum family (e.g. allowlist mode, file status) so values stay typed and seedable.                    |
| **CorEnum**                  | One selectable value in a family; rows reference it by Id (not CLR enums).                                           |
| **Provider**                 | Flat catalog of OCR/LLM/meta engines Core can call (`ProviderKey` like `gpt_5_6`).                                   |
| **DocumentType**             | Platform vocabulary of business doc kinds (invoice, DN, …) used for classify + routing.                              |
| **AgentTemplate**            | Platform starter Agent customers clone (guided setup).                                                               |
| **Agent**                    | Customer extraction brain: instructions + output schema + document-type intent.                                      |
| **WorkflowDefinition**       | Internal post-extract step definition (not a public wire resource).                                                  |
| **Queue**                    | Ops lane inside a Business: intake, webhook, email, workflow attach; not an Agent.                                   |
| **QueueRoute**               | Many type→Agent mappings for one Queue (multi-doc / multi-type files).                                               |
| **QueueEmailAllowlistEntry** | Trusted sender email/domain for a Queue’s email intake gate.                                                         |
| **Batch**                    | Optional log/correlation when ≥2 Files arrive together — not a delivery state machine.                               |
| **File**                     | One stored upload/email artifact; owns split/classify/rollup.                                                        |
| **Document**                 | One logical business document after split; result + async webhook unit.                                              |
| **IntakeRejection**          | Intake refused with **no File** created (audit + clear failure).                                                     |
| **WorkEvent**                | Append-only ops/audit trail for pipeline and delivery events.                                                        |
| **CorTenantApiKey** (F2)     | Temporary machine auth bound to a Business until Iden M2M exists.                                                    |




### Shared conventions (all persisted entities)

**Entity name prefixes (locked):**

| Prefix | Applies to | Examples |
|--------|------------|----------|
| **`Cor`** | Infrastructure + platform catalogs | `CorTenant`, `CorEnum`, `CorProvider`, `CorDocumentType`, `CorAgentTemplate`, `CorWorkflowDefinition`, `CorTenantApiKey` |
| **`Ops`** | Operational work (agents, queues, files, documents) | `OpsAgent`, `OpsQueue`, `OpsFile`, `OpsDocument`, `OpsBatch`, `OpsWorkEvent` |

**FK / column rule:** columns do **not** carry the entity prefix. `CorTenant` → FK column **`TenantId`**; `OpsFile` → **`FileId`**; `OpsAgent` → **`AgentId`**. (`CorTenantApiKey` is an **entity**, not a column.)

**Product glossary terms** (File, Document, Agent, Queue) stay unprefixed in product language; CLR/table names use `Ops*` / `Cor*`.


| Field                                 | Type                        | Who gets it                                            | Notes                                                                                      |
| ------------------------------------- | --------------------------- | ------------------------------------------------------ | ------------------------------------------------------------------------------------------ |
| `Id`                                  | **UUID**                    | Wire-facing entities                                   | **PK** and public id on the wire                                                           |
| `Id`                                  | **bigint identity**         | Reference/internal entities only                       | PK when no public UUID                                                                     |
| `SequenceId`                          | **bigint identity**         | **All wire-facing entities**                           | Extra unique sequence for SQL/dev (`WHERE SequenceId = 1042`). **Not** the API resource id |
| `BusinessId`                          | string/UUID (Iden Business) | **Operational rows** (Agent, Queue, File, Document, …) | **Sole isolation / authz scope**. Null on `CorTenant` + global catalogs                    |
| `CreatedAt` / `UpdatedAt`             | datetimeoffset              | All                                                    |                                                                                            |
| `CreatedByUserId` / `UpdatedByUserId` | string?                     | Most                                                   |                                                                                            |
| `IsDeleted`                           | bool                        | All                                                    | Soft delete **required**                                                                   |
| `DeletedAt` / `DeletedByUserId`       |                             | All                                                    |                                                                                            |
| `RowVersion`                          | rowversion                  | Mutable config + File/Document                         | Optimistic concurrency; omit on WorkEvent                                                  |


**API DTOs:** expose UUID resource `Id` only. Do **not** expose `SequenceId` on External APIs (optional on internal support UI). Auth context carries Iden `business_id` (and `tenant_id` for UI); **queries filter by** `BusinessId`.

**Lean tenancy:** no `TenantId` on operational tables. Need tenant display? Join/project from `CorTenantBusiness` (`TenantName` / `CorTenantId`).

---



### 1. `CorTenant`

**Role:** Thin Documate record for an Iden **Tenant**. Holds tenant-wide product settings (e.g. Mode 1 vs Mode 2). Does **not** own Queues/Files — those hang under Business.


| Field                                       | Type   | Notes                                   |
| ------------------------------------------- | ------ | --------------------------------------- |
| `Id`                                        | UUID   | PK                                      |
| `SequenceId`                                | bigint | Maintenance                             |
| `IdenTenantId`                              | string | Unique — Iden Tenant                    |
| `Name`                                      | string |                                         |
| `ProviderModeEnumId`                        | bigint | FK → CorEnum (`provider_mode`: `mode_1` |
| `IsActive`                                  | bool   |                                         |
| *(soft-delete + RowVersion; no BusinessId)* |        |                                         |


---



### 1b. `CorTenantBusiness`

**Role:** Thin Documate record for an Iden **Business**. This is the **data isolation unit**: Agents, Queues, and work belong here. `TenantName` is a cached projection of the parent tenant name for UI/lists.


| Field                        | Type   | Notes                                                                                     |
| ---------------------------- | ------ | ----------------------------------------------------------------------------------------- |
| `Id`                         | UUID   | PK                                                                                        |
| `SequenceId`                 | bigint | Maintenance                                                                               |
| `CorTenantId`                | UUID   | FK → CorTenant (parent tenant — **only** place operational path links tenant)             |
| `IdenBusinessId`             | string | Unique — Iden Business                                                                    |
| `Name`                       | string | Business display name                                                                     |
| `TenantName`                 | string | **Projection** of parent `CorTenant.Name` (cached for lists/UI; refresh on tenant rename) |
| `IsActive`                   | bool   |                                                                                           |
| *(soft-delete + RowVersion)* |        |                                                                                           |


---



### 2. `CorEnumType` + `CorEnum` (lookup catalog — inspired by ERP30)

**Role:** Replace persisted CLR enums. **CorEnumType** = the family (`EnumTypeKey`); **CorEnum** = each value (`EnumKey`). Domain columns store `*EnumId` and **compare Ids** in logic. No hierarchy (`ParentId`).

**Source inspiration:** ERP CorEnumType / CorEnum — adapted leaner for Documate.

**Not public wire resources** — bigint PKs. App may expose lookup lists; External partners still get domain UUIDs / result JSON, not CorEnum ids as first-class resource ids.

**No hierarchy:** no `ParentId` / multilevel tree.

#### 2a. `CorEnumType`


| Field           | Type   | Notes                                                              |
| --------------- | ------ | ------------------------------------------------------------------ |
| `Id`            | bigint | PK                                                                 |
| `EnumTypeKey`   | string | Unique stable type key e.g. `allowlist_mode`, `file_public_status` |
| `Name`          | string |                                                                    |
| `Scope`         | string | Bootstrap only: `system` or `business` — **not** a CorEnum FK      |
| `IsActive`      | bool   |                                                                    |
| *(soft-delete)* |        |                                                                    |




#### 2b. `CorEnum`


| Field           | Type    | Notes                                              |
| --------------- | ------- | -------------------------------------------------- |
| `Id`            | bigint  | PK                                                 |
| `TypeId`        | bigint  | FK → CorEnumType                                   |
| `Name`          | string  | Display                                            |
| `EnumKey`       | string  | Stable code within type (was ERP `SysKey`)         |
| `ShortName`     | string? |                                                    |
| `Narration`     | string? |                                                    |
| `DisplayStyle`  | string? | Optional UI hint                                   |
| `BusinessId`    | string? | Set when Scope = `business`; null for system seeds |
| *(soft-delete)* |         |                                                    |


**Unique (filtered):** `(TypeId, EnumKey, BusinessId)` respecting null BusinessId for system rows.

**Documate adaptations vs ERP:**

- No `ParentId` / multilevel; no DisplayOrder, IsSelectable, IsDefaultSelected, IsSystem, hierarchy helpers.
- `SysKey` → `EnumKey`.
- No `TenantId` on CorEnum — business-scoped values use `BusinessId` only; system values have null `BusinessId`.
- No `FeatureId` — use `CorEnumType.EnumTypeKey`.
- Dedicated catalogs stay: `DocumentType`, `Provider`. All former static enum columns → `*EnumId` FK → `CorEnum`.

**Usage rule (see** `docs/architecture/patterns/cor-enum.md`**):** persist `*EnumId` FKs; **compare CorEnum Ids** in domain logic (not `EnumKey`). Resolve seed `EnumKey` → Id via shared lookup/cache. Validate FK belongs to expected `CorEnumType.EnumTypeKey` on write. DTOs may expose `EnumKey` for UI/External.

**Convention:** `XxxEnumId` → `CorEnum.Id` for EnumTypeKey `xxx`.

#### Seed `CorEnumType` keys (Phase 1 — System scope)


| EnumTypeKey               | Purpose                               | Example `EnumKey` values                                                              |
| ------------------------- | ------------------------------------- | ------------------------------------------------------------------------------------- |
| `provider_mode`           | Tenant billing/provider UX mode       | `mode_1`, `mode_2`                                                                    |
| `provider_category`       | Provider engine kind                  | `ocr`, `llm`, `meta`, `other`                                                         |
| `allowlist_mode`          | Queue email sender policy (see Queue) | `open`, `allowlist_preferred`, `allowlist_enforced`                                   |
| `workflow_mode`           | How queue attaches post-processing    | `inherit_agent_default`, `override`, `disabled`                                       |
| `allowlist_match_type`    | Allowlist entry match shape           | `email`, `domain`                                                                     |
| `intake_source`           | How work entered the system           | `api`, `email`, `api_sync`                                                            |
| `file_public_status`      | File public status                    | `received`, `processing`, `ready`, `partial_ready`, `failed`, `rejected`, `cancelled` |
| `file_internal_stage`     | File pipeline stage (ops)             | TBD with Core stages                                                                  |
| `document_public_status`  | Document public status                | `received`, `processing`, `ready`, `failed`, `rejected`, `cancelled`                  |
| `document_internal_stage` | Document pipeline stage (ops)         | TBD with Core stages                                                                  |
| `work_subject_type`       | WorkEvent subject discriminator       | `file`, `document`, `batch`, `intake_rejection`, `queue`                              |
| `work_event_type`         | WorkEvent kind (optional typed)       | e.g. `status_changed`, `webhook_attempted`, …                                         |




### 3. `Provider` (platform catalog)

**Role:** Platform catalog of extraction engines/models (flat list — one row per model/engine key). Used by Core under Mode 1 (often via Documate meta-provider); customers don’t pick these in Phase 1 UI.

**Flat catalog — no Company entity / no company→model tree.** One row per selectable engine or model key (e.g. `gpt_5_6`, `claude_sonnet_6`, `aws_textract`, `documate_meta`).


| Field                                   | Type    | Notes                                                                                |
| --------------------------------------- | ------- | ------------------------------------------------------------------------------------ |
| `Id`                                    | bigint  | PK                                                                                   |
| `ProviderKey`                           | string  | Unique stable key e.g. `gpt_5_6`, `claude_sonnet_6`, `aws_textract`, `documate_meta` |
| `Name`                                  | string  | Display name                                                                         |
| `CategoryEnumId`                        | bigint  | FK → CorEnum (`provider_category`)                                                   |
| `VendorHint`                            | string? | Optional ops label only (e.g. OpenAI) — **not** a parent table                       |
| `IsPlatformManaged`                     | bool    |                                                                                      |
| `IsActive`                              | bool    |                                                                                      |
| *(soft-delete; global — no BusinessId)* |         |                                                                                      |


---



### 4. `DocumentType` (platform catalog)

**Role:** Stable platform list of business document kinds. Classify labels a Document with a type; QueueRoute uses that type to pick an Agent. Dedicated catalog (not CorEnum).


| Field                                   | Type    | Notes                                |
| --------------------------------------- | ------- | ------------------------------------ |
| `Id`                                    | bigint  | PK                                   |
| `DocumentTypeKey`                       | string  | Unique e.g. `invoice`, `credit_note` |
| `Name`                                  | string  |                                      |
| `Description`                           | string? |                                      |
| `IsActive`                              | bool    |                                      |
| *(soft-delete; global — no BusinessId)* |         |                                      |


---



### 5. `AgentTemplate` (platform)

**Role:** Pre-built Agent starter (schema/instructions defaults) that customers **guided-clone** into their own Agent. Platform-owned, not Business-owned.


| Field                                   | Type    | Notes                       |
| --------------------------------------- | ------- | --------------------------- |
| `Id`                                    | bigint  | PK                          |
| `AgentTemplateKey`                      | string  | Unique e.g. `invoice_eu_v1` |
| `Name` / `Description`                  |         |                             |
| `DocumentTypeId`                        | bigint  | FK → DocumentType           |
| `DefaultSchemaJson`                     | JSON    |                             |
| `DefaultInstructions`                   | text    |                             |
| `DefaultProviderId`                     | bigint? | FK → Provider               |
| `IsPublished`                           | bool    |                             |
| `Version`                               | int     |                             |
| *(soft-delete; global — no BusinessId)* |         |                             |


---



### 6. `Agent`

**Role:** Customer **AI Agent**: what to extract (**schema**), how to behave (**instructions**), document-type intent, and **post-processing** for that type. Schema + workflow bind here — not on the Queue.


| Field                        | Type    | Notes                     |
| ---------------------------- | ------- | ------------------------- |
| `Id`                         | UUID    | PK + wire `agent_id`      |
| `SequenceId`                 | bigint  | Maintenance               |
| `BusinessId`                 | string  | Iden Business — isolation |
| `Name` / `Description`       |         |                           |
| `DocumentTypeId`             | bigint  | FK → DocumentType         |
| `OutputSchemaJson`           | JSON    |                           |
| `SchemaVersion`              | int     |                           |
| `Instructions`               | text    |                           |
| `SourceTemplateId`           | bigint? | FK → AgentTemplate        |
| `DefaultWorkflowId`          | bigint? | FK → WorkflowDefinition   |
| `DefaultProviderId`          | bigint? | FK → Provider             |
| `ProviderStrategyJson`       | JSON?   | Mode 2 later              |
| `IsActive`                   | bool    |                           |
| *(soft-delete + RowVersion)* |         |                           |


---



### 7. `WorkflowDefinition`

**Role:** Definition of optional **post-processing** steps after extract (platform tools / internal MCP). Internal bigint id — not an External `workflow_id`. Attached primarily via Queue (Agent may supply a default).

**Not a public wire resource** (no External `workflow_id`; not a partner/poll identity). App may attach by internal id when configuring Queue/Agent. **bigint PK** — no UUID / SequenceId.


| Field                        | Type    | Notes                               |
| ---------------------------- | ------- | ----------------------------------- |
| `Id`                         | bigint  | PK                                  |
| `BusinessId`                 | string  | Iden Business — isolation           |
| `WorkflowKey`                | string? | Optional business-scoped stable key |
| `Name`                       | string  |                                     |
| `DefinitionJson`             | JSON    |                                     |
| `IsActive`                   | bool    |                                     |
| *(soft-delete + RowVersion)* |         |                                     |


---



### 8. `Queue`

**Role:** Operational lane inside a Business (department / stream): webhook, email intake, allowlist mode, workflow attach, and the routing map. Partners submit work to a Queue — not directly to an Agent.


| Field                                                 | Type    | Notes                                                                                                                                                                                                                                                                  |
| ----------------------------------------------------- | ------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Id`                                                  | UUID    | PK + wire `queue_id`                                                                                                                                                                                                                                                   |
| `SequenceId`                                          | bigint  | Maintenance                                                                                                                                                                                                                                                            |
| `BusinessId`                                          | string  | Iden Business — isolation                                                                                                                                                                                                                                              |
| `Name` / `Description`                                |         |                                                                                                                                                                                                                                                                        |
| `RoutingLocked` / `RoutingLockedAt`                   |         |                                                                                                                                                                                                                                                                        |
| `WebhookUrl` / `WebhookSecretHash` / `WebhookEnabled` |         |                                                                                                                                                                                                                                                                        |
| `EmailIntakeEnabled` / local-part / domain / version  |         |                                                                                                                                                                                                                                                                        |
| `AllowlistModeEnumId`                                 | bigint  | FK → CorEnum (`allowlist_mode`). **What it does:** controls Layer-1 email sender gate — `open` (anyone with address), `allowlist_preferred` (unknown accepted but flagged; later), `allowlist_enforced` (unknown → IntakeRejection / Rejected). See exploration §14.4. |
| `WorkflowModeEnumId`                                  | bigint  | FK → CorEnum (`workflow_mode`: inherit / override / disabled)                                                                                                                                                                                                          |
| `WorkflowId`                                          | bigint? | FK → WorkflowDefinition when mode = override (or always optional)                                                                                                                                                                                                      |
| `IsActive`                                            | bool    |                                                                                                                                                                                                                                                                        |
| *(soft-delete + RowVersion)*                          |         |                                                                                                                                                                                                                                                                        |


---



### 9. `QueueRoute`

**Role:** One row = “on this Queue, this **DocumentType** is handled by this **Agent**.” Needed because one Queue must support many types (mixed files). Frozen when `RoutingLocked` after first File. Not collapsed onto Queue as single DocumentTypeId/AgentId columns.


| Field            | Type   | Notes             |
| ---------------- | ------ | ----------------- |
| `Id`             | bigint | PK                |
| `BusinessId`     | string | Iden Business     |
| `QueueId`        | UUID   | FK → Queue        |
| `DocumentTypeId` | bigint | FK → DocumentType |
| `AgentId`        | UUID   | FK → Agent        |
| *(soft-delete)*  |        |                   |


**Unique filtered:** `(QueueId, DocumentTypeId)` where not deleted.

---



### 10. `QueueEmailAllowlistEntry`

**Role:** Allowlisted sender identity (exact email or domain) used when Queue allowlist mode is not fully open. Cheap Layer-1 gate before AI spend on email intake.


| Field             | Type   | Notes                                         |
| ----------------- | ------ | --------------------------------------------- |
| `Id`              | bigint | PK                                            |
| `BusinessId`      | string | Iden Business                                 |
| `QueueId`         | UUID   |                                               |
| `MatchTypeEnumId` | bigint | FK → CorEnum (`allowlist_match_type`: `email` |
| `Value`           | string |                                               |
| *(soft-delete)*   |        |                                               |


---



### 11. `Batch`

**Role:** Optional **correlation log** when two or more Files arrive in one intake (API multi-upload or one email). For support/filters (`batch_id`) only — **not** a delivery/status state machine and not the webhook unit.


| Field            | Type    | Notes                                          |
| ---------------- | ------- | ---------------------------------------------- |
| `Id`             | UUID    | PK + wire `batch_id`                           |
| `SequenceId`     | bigint  | Maintenance                                    |
| `BusinessId`     | string  | Iden Business                                  |
| `QueueId`        | UUID    |                                                |
| `SourceEnumId`   | bigint  | FK → CorEnum (`intake_source`: typically `api` |
| `EmailMessageId` | string? |                                                |
| `FileCount`      | int     | ≥ 2                                            |
| *(soft-delete)*  |         |                                                |


---



### 12. `File`

**Role:** One stored binary/artifact in a Queue. Parent of Documents; runs split/classify/route; exposes **rollup status** for UI/ops. Cancel-file aborts the whole pack.


| Field                                               | Type    | Notes                                |
| --------------------------------------------------- | ------- | ------------------------------------ |
| `Id`                                                | UUID    | PK + wire `file_id`                  |
| `SequenceId`                                        | bigint  | Maintenance                          |
| `BusinessId`                                        | string  | Iden Business                        |
| `QueueId`                                           | UUID    |                                      |
| `BatchId`                                           | UUID?   |                                      |
| `SourceEnumId`                                      | bigint  | FK → CorEnum (`intake_source`: `api` |
| `IntakeHintsJson` (planned)                         | json?   | Optional caller hints: documentCount + documentTypeKey(s); used to skip split/classify when complete |
| `PublicStatusEnumId`                                | bigint  | FK → CorEnum (`file_public_status`)  |
| `InternalStageEnumId`                               | bigint? | FK → CorEnum (`file_internal_stage`) |
| Storage / email / reprocess / error / cancel fields |         | As previously specified              |
| *(soft-delete + RowVersion)*                        |         |                                      |


---



### 13. `Document`

**Role:** One logical business document produced from a File (e.g. one invoice inside a multi-doc PDF). Runs one Agent → one schema result. **Async webhook unit** and primary poll result object.


| Field                                             | Type    | Notes                                    |
| ------------------------------------------------- | ------- | ---------------------------------------- |
| `Id`                                              | UUID    | PK + wire `document_id`                  |
| `SequenceId`                                      | bigint  | Maintenance                              |
| `BusinessId`                                      | string  | Iden Business                            |
| `QueueId` / `FileId` / `BatchId?`                 | UUID    |                                          |
| `DocumentTypeId`                                  | bigint? |                                          |
| `AgentId`                                         | UUID?   |                                          |
| `ProviderId`                                      | bigint? |                                          |
| `PublicStatusEnumId`                              | bigint  | FK → CorEnum (`document_public_status`)  |
| `InternalStageEnumId`                             | bigint? | FK → CorEnum (`document_internal_stage`) |
| SchemaVersion / slice / ResultJson / webhook meta |         | As previously specified                  |
| *(soft-delete + RowVersion)*                      |         |                                          |


---



### 14. `IntakeRejection`

**Role:** Record that intake was refused **without creating a File** (ambiguous email, allowlist fail, hard gate). Preserves audit/support trail when there is no work entity to attach the failure to.


| Field                    | Type   | Notes                          |
| ------------------------ | ------ | ------------------------------ |
| `Id`                     | UUID   | PK + wire                      |
| `SequenceId`             | bigint | Maintenance                    |
| `BusinessId` / `QueueId` |        |                                |
| `SourceEnumId`           | bigint | FK → CorEnum (`intake_source`) |
| error / email meta       |        |                                |
| *(soft-delete)*          |        |                                |


---



### 15. `WorkEvent`

**Role:** Append-only timeline of significant pipeline/delivery events (status changes, webhook attempts, etc.) for ops, debugging, and support. Not a customer-facing resource id.


| Field                                                | Type    | Notes                              |
| ---------------------------------------------------- | ------- | ---------------------------------- |
| `Id`                                                 | bigint  | PK (internal)                      |
| `BusinessId`                                         | string  | Iden Business                      |
| `SubjectTypeEnumId`                                  | bigint  | FK → CorEnum (`work_subject_type`) |
| `SubjectId`                                          | UUID    | Subject's PK                       |
| `EventTypeEnumId`                                    | bigint  | FK → CorEnum (`work_event_type`)   |
| `ProviderId`                                         | bigint? |                                    |
| `PayloadJson`                                        | JSON?   |                                    |
| *(soft-delete columns if required; normally retain)* |         |                                    |
| **No RowVersion**                                    |         | Append-only                        |


---



### 16. `CorTenantApiKey` / Business-scoped API key (if F2)

**Role (Decision F2):** Temporary Documate-issued API credential scoped to a **Business** for External calls until Iden machine-client auth exists. Prefer replacing later — do not treat as permanent identity design. **Entity** name is `CorTenantApiKey` (not a column prefix).


| Field                               | Type   | Notes                           |
| ----------------------------------- | ------ | ------------------------------- |
| `Id`                                | UUID   | PK                              |
| `SequenceId`                        | bigint | Maintenance                     |
| `BusinessId`                        |        | Prefer **Business**-scoped keys |
| Name / KeyPrefix / KeyHash          |        |                                 |
| `IsActive` / ExpiresAt / LastUsedAt |        |                                 |
| *(soft-delete + RowVersion)*        |        |                                 |


---



### Static C# enums — DECIDED: do not persist

No persisted “CLR enum columns.” Persist `XxxEnumId` **→ CorEnum** only. C# may still define **key mirrors** (`nameof`) for comparisons — not as EF enum mappings.

**Bootstrap exception:** `CorEnumType.Scope` remains a constrained string (`system`/`tenant`/`business`), not a CorEnum FK.

---



### What is intentionally **not** an entity in Phase 1


| Omitted                     | Why                       |
| --------------------------- | ------------------------- |
| Mapping catalogs            | Later                     |
| HITL tasks                  | Later                     |
| Tenant Provider credentials | Mode 2 later              |
| Customer MCP registrations  | Later                     |
| Separate Schema table       | On Agent.OutputSchemaJson |


---



### Entity verification checklist

- [x] Wire-facing: UUID `Id` (PK) + `SequenceId` — Queue, Agent, Batch, File, Document, IntakeRejection, …
- [x] Catalogs: bigint `Id` + prefixed key — `EnumTypeKey`/`EnumKey`, `ProviderKey`, `DocumentTypeKey`, `AgentTemplateKey`
- [x] All former static enum columns → `*EnumId` FK → CorEnum (seed types listed); Scope string bootstrap only
- [x] Soft delete on all; RowVersion on mutable config + File/Document
- [x] QueueRoute uses DocumentTypeId
- [x] Soft-delete-in-use rules accepted
- [x] Iden Tenant → Business mirrored; operational rows **BusinessId only**; `CorTenantBusiness.TenantName` projection

**Developer verification:** Entity catalog **approved** (2026-08-02).

**Decision H — Tenant / Business persistence:** **DECIDED H1** — persist `CorTenant` + `CorTenantBusiness` (Iden-aligned: product **settings/extension + display projections** keyed by Iden Guids — not a second master org registry; Iden remains SoT for Tenant/TenantBusiness).

**Decision I — Queue wire id:** **DECIDED I1** — Queue.Id is UUID (public PK) + SequenceId.

**Iden tenancy:** **LOCKED** — Tenant → Business; Documate isolation = Business.

### Decision Required — A: Worker hosting


| Option | Idea                          | Tradeoff      |
| ------ | ----------------------------- | ------------- |
| **A1** | In-process background workers | Fastest ship  |
| **A2** | Separate worker host          | Cleaner scale |
| **A3** | Serverless                    | Ops-heavy     |


**Recommendation:** **A1**. **Status:** **DECIDED A1 + Hangfire (SQL storage)**.

**Amendment (2026-08-04):** Pure in-memory enqueue (Channel-only) is **rejected** — work must survive process restart. Phase 1 keeps **A1 hosting** (Hangfire Server in-process with the API) but uses **Hangfire + SQL Server storage** as the durable job backbone for:

- File / Document pipeline jobs (non-blocking enqueue after File commit)
- Future **webhook delivery** retries/backoff (DQ-0801) via the same `IWebhookDispatcher` wrapper

Handlers enqueue through thin abstractions (`IWorkDispatcher`, later `IWebhookDispatcher`) — not raw `BackgroundJob` calls scattered in features. Job methods must be **idempotent**. Scale-out path remains **A2** (Hangfire Server in a separate worker host) without changing job contracts.

---



## Domain Validation Rules

Product/runtime invariants to enforce in handlers + workers (tests preferred).


| #   | Rule                                                                                              |
| --- | ------------------------------------------------------------------------------------------------- |
| V1  | Every Document has queue/file FKs; DocumentTypeId+AgentId or Failed `unroutable_type`             |
| V2  | Schema from Agent only                                                                            |
| V3  | Routing immutable when RoutingLocked                                                              |
| V4  | Batch only when file count ≥ 2                                                                    |
| V5  | Multi-file enqueue non-blocking                                                                   |
| V6  | Async accept returns ids before extract completes                                                 |
| V7  | Webhook per Document terminal; metadata on Document                                               |
| V8  | Sync wait timeout → ids + timed_out                                                               |
| V9  | Cancel File vs Cancel Document per Queue design                                                   |
| V10 | Reprocess → new File                                                                              |
| V11 | IntakeRejection when no File                                                                      |
| V12 | Mode 1: no customer provider picker                                                               |
| V13 | Non-API Document webhook may include original file                                                |
| V14 | DTOs only on HTTP                                                                                 |
| V15 | **Business** isolation only on work rows; tenant via CorTenantBusiness                            |
| V16 | Default queries exclude `IsDeleted`                                                               |
| V17 | Classify/route only against active DocumentType / Provider / Agent rows                           |
| V18 | Wire resource ids are UUID `Id` (Queue/Agent/Batch/File/Document/…); `SequenceId` is support-only |




### Decision Required — B: Sync wait timeout & multi-doc policy


| Option | Wait budget                   | Multi-doc in one file                          |
| ------ | ----------------------------- | ---------------------------------------------- |
| **B1** | Fixed e.g. 60s                | Wait for all children; timeout → partial + ids |
| **B2** | Fixed e.g. 120s               | Wait for all children; timeout → partial + ids |
| **B3** | Client-supplied cap (clamped) | Wait for all children                          |


**Recommendation:** **B1** (60s) + wait-for-all children.  
**Status:** Pending.

### Decision Required — C: Sync path webhooks


| Option | Behavior                                                     |
| ------ | ------------------------------------------------------------ |
| **C1** | Still fire per-Document webhooks if queue webhook configured |
| **C2** | Suppress webhooks for sync submissions                       |
| **C3** | Flag on request (`notify_webhook=true/false`), default true  |


**Recommendation:** **C1**.  
**Status:** Pending.

---



## Process Flows



### Flow 1 — Async multi-file upload (happy path)

```text
Client POST /api/v1/.../files (N files) + queue_id
  [optional per-file intake hints: documentCount, documentTypeKey(s)]
  → Auth (F2 API key / later Iden M2M)
  → If N≥2: create Batch (log)
  → Store each blob; create Files; lock routing if first
  → Enqueue each File (return 202 + batch_id? + file_ids)
  → Worker: normalize/OCR
       → if valid caller hints: skip split+classify → create Document(s) from hints → route
       → else: split → classify → route → Documents
  → Per Document: extract → validate → post-process → Ready
  → Per Document: webhook attempt
  → Client poll GetDocument / list filters
```

### Intake hints (optional — skip split/classify)

Partners often already know what they uploaded. External upload may supply **optional per-File hints**:

| Hint | Meaning |
|------|---------|
| `documentCount` | How many logical Documents are in this File (`≥ 1`) |
| `documentTypeKey` / `documentTypeKeys` | Platform DocumentType key(s) for those Documents |

**Rules (locked for plan):**

1. **No hints (or incomplete hints)** → full pipeline after normalize: **split → classify → route** (E3 default for PDFs; images/text usually 1 doc via classifier/heuristics).
2. **Complete hints** that allow skip:
   - `documentCount = 1` **and** one `documentTypeKey` → **skip split and classify**; create **one** Document of that type; **still route** via QueueRoute → extract…
   - `documentCount = N` (`N > 1`) **and** `documentTypeKeys` length `N` (or one key applied to all N only if explicitly allowed later) → **skip auto-split/classify**; create **N** Documents with given types (page ranges optional follow-on); **still route** each → extract…
3. **Partial hints** (e.g. only count, or only type without count) → **do not skip**; run full split/classify (safer than guessing).
4. Unknown `documentTypeKey` → Document **Failed** `unroutable_type` / invalid hint (same as classify miss), not silent ignore.
5. Hints are **caller assertions**, not OCR truth — persist on File for audit (`IntakeHintsJson` or dedicated columns — implement with DQ-0601 extension / DQ-0702).
6. Sync-wait (Decision B) still **single Document only**; multi-doc hints on sync-wait → fail.

This does **not** remove E3 as the default when callers omit hints.



### Flow 2 — Sync wait extract (client)

```text
Client POST sync extract (1 file recommended)
  → Same create File + enqueue
  → HTTP waits until all Documents terminal OR timeout
  → 200 { file_status, documents[], timed_out, ids }
  → (C1) webhooks may also fire
```



### Flow 3 — Email intake

```text
SMTP/inbound → gates → intake decision agent
  → reject: IntakeRejection
  → accept targets: Files (+ Batch if ≥2)
  → same worker pipeline
  → document webhooks include original_file
```



### Flow 4 — Cancel / reprocess

```text
Cancel File → Cancelled file + cancel active docs → doc webhooks for newly Cancelled
Cancel Document → that doc Cancelled → webhook; file rollup updates
Reprocess → new File from same bytes → new Documents → new webhooks
```



### Decision Required — D: Email inbound mechanism


| Option | Idea                                                                                    |
| ------ | --------------------------------------------------------------------------------------- |
| **D1** | Provider inbound parse webhook (e.g. SES/SendGrid inbound) into External/Infrastructure |
| **D2** | IMAP poller worker                                                                      |
| **D3** | Phase 1 stub: manual “simulate email” Admin API only; real inbound in later wave        |


**Recommendation:** **D3** stub in early waves, then **D1** before “sell email hard”.  
**Status:** Pending.

### Decision Required — E: Split/classify Phase 1 quality bar


| Option | Idea                                                                       |
| ------ | -------------------------------------------------------------------------- |
| **E1** | Heuristic + LLM classify; accept imperfect splits; expose failures clearly |
| **E2** | Single-doc assumption unless explicit “pack” flag on upload                |
| **E3** | Always run full multi-doc split for PDFs; images/text always 1 doc         |


**Recommendation:** **E3** for PDFs/Office; **1 doc** for single images/plain text unless classifier says otherwise.  
**Status:** **DECIDED E3**, amended **2026-08-04** with **optional caller intake hints**.  
**Amendment:** E3 remains the **default** when the API caller does **not** supply complete document nature/count. When the caller supplies complete intake hints (`documentCount` + matching `documentTypeKey`(s)), Core **skips split and classify** and creates Document(s) from those hints, then **routes** via QueueRoute. See Flow 1 § Intake hints. Deeper classify/split strategy when hints are absent: **[`04-split-classify-strategy-exploration.md`](./04-split-classify-strategy-exploration.md)** (lock before DQ-0702).

---



## Instruction and Control Set



### Build order constraints (agents & humans)

1. Do not implement Mode 2, HITL, mapping catalogs, or customer MCP UX in Phase 1 DQs.
2. Do not block the API host on multi-file upload handling.
3. Prefer feature slices: External upload → Core worker stub → poll → real OCR/LLM → webhook → sync wait → email → web UI.
4. Platform post-processing tools via **internal MCP** only after extract path works end-to-end.
5. Guided-clone Agent templates: ship ≥1 invoice template before general schema builder polish.



### Decision Required — F: External API auth until machine-client model exists


| Option | Idea                                                                                                 |
| ------ | ---------------------------------------------------------------------------------------------------- |
| **F1** | User bearer tokens from Iden only (same as app); integrations use a logged-in user token temporarily |
| **F2** | Documate-issued API keys bound to Iden **Business** (and Tenant) (thin layer; replace later)         |
| **F3** | Block External until Iden M2M ships                                                                  |


**Recommendation:** **F2** for partner testing speed, with explicit “temporary” label in docs. **Must be retired by Band 15 (DQ-1505/1506)** — not a permanent identity design.  
**Status:** **DECIDED F2** (bridge only).

---

### Decision Required — J: When to run Iden Integration & Validation

| Option | Idea | Tradeoff |
| ------ | ---- | -------- |
| **J1** | Iden-first — finish Band 15 (at least inventory + human live auth) before most product waves | Slowest product features; strongest identity footing |
| **J2** | Interleaved — inventory early; real human Iden with Wave 1; M2M + retire F2 after External exists; Phase 1 done-when includes Band 15 | Best balance (recommended) |
| **J3** | Late — ship product on F2/fixed tokens, then replace Iden in a follow-on phase | Fastest demo; risk temporary auth becomes permanent |

**Recommendation:** **J2**.  
**Status:** **DECIDED J3** — late: ship Phase 1 product on F2 / interim auth; Band 15 is a **mandatory follow-on phase** (not optional forever).  
**Status:** Pending.

### Decision Required — G: Sync wait timeout seconds

Numeric choice after B (e.g. 60 vs 120). Record with B.  
**Status:** Pending (tied to B).

---



## Permissions and Security

1. **Iden** is identity source for human users; no parallel user directory. Iden tenancy = **Tenant → Business**.
2. Every command/query scoped by `business_id` from auth context — **Business is the isolation unit** (no `TenantId` on work rows).
3. Queue/agent/File/Document access: users with access to that **Business** (Phase 1: any authenticated user for that Business can configure — finer RBAC later).
4. Email address = capability secret; unguessable; rotate supported; allowlist model present, enforce before hard sell.
5. Webhook HTTPS + shared secret (HMAC) required for production queues.
6. Original files in object storage with **business-prefixed** keys; signed download URLs for non-API webhook file refs.
7. Provider credentials (Mode 1) only in server secret store — never to browser.
8. Sync wait: same authz as async upload on that queue.
9. Do not log full document PII in events by default; store refs + error codes.

### Object storage (Phase 1 continuity with `old_code`)

**Locked for Phase 1:** Keep the **AWS S3** approach from `old_code` (`IAmazonS3` + `TransferUtility` upload, `GetObject` download, `GetPreSignedURL` ~30 minutes, Intelligent-Tiering).  
**v3 key improvement:** object keys are **tenant- and business-prefixed** and queue/file scoped using **SequenceIds** (not wire UUIDs) for shorter, human-readable paths — e.g. `tenants/1/businesses/2/queues/5/files/12/invoice.pdf`. Supports isolation and later residency.  
Local filesystem provider remains for Development when AWS is unavailable.

### Privacy & regulatory compliance (must plan for — not fully implemented in Phase 1)

Documate processes business documents that commonly contain **personal data** and financial identifiers. Product and engineering **must remain compliant** with:

| Regime | Intent (product obligations) |
|--------|------------------------------|
| **GDPR** (EU/EEA and similar) | Lawful basis, purpose limitation, data minimization, retention/deletion, DPIA where needed, subprocessors, DPA with customers, data-subject rights support (access/erasure/export where applicable), breach notification process |
| **US-side** | Applicable federal/state privacy and sector rules as sold (e.g. **CCPA/CPRA** and other state privacy acts where we have CA/US residents; customer contracts may add **HIPAA** / **GLBA** / etc. only if we explicitly offer that mode) |

**Engineering implications (track in later DQs / compliance phase — do not ignore):**

- Object keys and DB rows scoped by **Business**; no cross-tenant key layouts.
- Retention / purge paths for Files, Documents, blobs, and logs (align with customer DPA).
- Region/residency awareness for S3 buckets (EU vs US) as customer/config requires — Phase 1 may start single-region; multi-region is a compliance follow-on.
- Minimize PII in logs/events; encrypt secrets; signed URLs short-lived.
- Document subprocessors (AWS, LLM/OCR providers) for customer DPAs.

**Phase 1 posture:** Ship S3-compatible storage + Business isolation; **do not claim** full GDPR/CCPA certification until retention, DSR tooling, and residency options are explicitly delivered and reviewed.

---



## Dispatch Index (preview — Phase 3 expands to full DQ)

Bands for **this product plan** (distinct from Plan 00 eng bands; Phase 3 DQ doc will number items):


| Band | Focus                                                                                    |
| ---- | ---------------------------------------------------------------------------------------- |
| 00   | Scaffold alignment: Domain entities skeleton, module feature shells, migrations baseline |
| 01   | Iden auth wiring (human) + tenant/**business** context                                   |
| 02   | Agents + schemas + platform templates (FrontendSupport)                                  |
| 03   | Queues + routing lock + webhook config + email address mint                              |
| 04   | Storage + File/Batch/IntakeRejection persistence + events                                |
| 05   | Work dispatcher (non-blocking) + File pipeline stub                                      |
| 06   | External async upload + poll APIs                                                        |
| 07   | Core OCR + split/classify/route + extract (Mode 1)                                       |
| 08   | Per-document webhook delivery + retries metadata                                         |
| 09   | Sync wait API                                                                            |
| 10   | Cancel + reprocess                                                                       |
| 11   | Post-process platform tools (internal MCP)                                               |
| 12   | Email inbound (per Decision D) + intake agent                                            |
| 13   | Web UI: agents, queues, monitor                                                          |
| 14   | Hardening: allowlist enforce path, limits, observability                                 |
| 15   | **Iden Integration & Validation** — discover APIs, live test via Documate, fix Iden, retire F2 |


**Illustrative DQ outcomes (not executable until Phase 3):**


| Illust. | Outcome                                                          |
| ------- | ---------------------------------------------------------------- |
| DQ-00xx | Domain + Db migrations for Queue/Agent/File/Document/Batch/Event |
| DQ-01xx | Iden login + `[Authorize]` + tenant filter abstraction           |
| DQ-06xx | `POST` multi-file async → 202 + ids; list/get Documents          |
| DQ-07xx | Worker processes file → docs Ready with real provider calls      |
| DQ-08xx | Webhook POST on document terminal                                |
| DQ-09xx | Sync wait endpoint with timeout                                  |
| DQ-13xx | Angular features for configure + monitor                         |
| DQ-15xx | Live Iden contract + M2M; F2 retired; no fixed tokens            |


---



## Wave Sections (implementation order)



### Wave 0 — Foundation

- Confirm `apps/api` / `apps/web` skeleton matches Plan 00 (create if missing).
- Domain entities + repositories/DbContext for Queue, Agent, Schema, File, Document, Batch, IntakeRejection, WorkEvent.
- Health checks; config for storage/provider placeholders.



### Wave 1 — Identity & tenancy

- Iden human auth integration (Tenant + Business claims).
- Business (+ Tenant) context in MediatR pipeline behaviors; mirrors for CorTenant / CorTenantBusiness.



### Wave 2 — Configuration APIs (FrontendSupport)

- Agent templates + guided clone + schema CRUD.
- Queue CRUD, routing map, lock behavior, webhook settings, email address generation (even if inbound stubbed).



### Wave 3 — Intake + async External API

- Blob store upload.
- Multi-file async accept + optional Batch log.
- Poll list/get file & Document.
- **Prove** concurrent uploads do not serialize the whole host.



### Wave 4 — Core pipeline (Mode 1)

- Normalize/OCR adapter(s).
- Split/classify/route (Decision E).
- Extract via Documate meta-provider (wrap ≥1 real LLM + optional Textract/Google as available).
- Schema validation → Ready/Failed.



### Wave 5 — Delivery

- Per-document webhook dispatcher + metadata.
- Sync wait API (Decisions B/C/G).
- Cancel file/document; reprocess → new File.



### Wave 6 — Post-processing

- Agent post-processing attach (minimal; `Agent.WorkflowId`); multi-queue CRUD day one.
- Internal MCP tool host + 1–2 platform tools (e.g. normalize date/currency stub).



### Wave 7 — Email

- Intake decision agent + gates.
- Real or stub inbound per Decision D.
- Document webhooks include `original_file`.



### Wave 8 — Web UI

- Agents, queues, monitor, rejections, cancel/reprocess actions.



### Wave 9 — Harden Phase 1

- Rate/size limits; allowlist enforcement path; metrics/logs; sync timeout tuning.
- Do **not** market email hard until allowlist UX done.



### Wave 10 — Iden Integration & Validation (Band 15) — **follow-on after Phase 1 (J3)**

**Goal:** After Phase 1 product capability works, replace interim auth with **real Iden**. Documate is also the integration harness that surfaces Iden defects.

1. **Discover** Iden APIs (OIDC/JWT, Tenant, Business, memberships, machine/M2M clients) from Iden docs/repo; write a short Documate-facing contract note.
2. **Wire** humans end-to-end (Angular + API) with live Iden — remove fixed/dev bearer tokens from shipping configs.
3. **Exercise** Tenant → Business through Documate (login, business switch/context, CorTenant/CorTenantBusiness upsert, scoped APIs).
4. **Defect loop** — reproduce Iden bugs via Documate; open/fix in Iden (issues/PRs are evidence); unblock Documate only with real fixes or explicit waivers.
5. **Machine auth** — Iden M2M / client credentials for External APIs (replaces F2).
6. **Retire F2** `CorTenantApiKey` path (remove or kill-switch off); docs state Iden-only.
7. **Regression** suite (CI-friendly) covering auth + tenancy claims.

**Sequencing (Decision J3):** Band 15 is **⏸ parked** until Phase 1 product done-when (waves 0–9) is accepted. Then activate DQ-1501…. F2 remains labeled temporary during Phase 1.

---



## Phase 1 done-when (acceptance)

- [ ] Multi-file async upload returns immediately; files process concurrently.
- [ ] Multi-doc PDF can yield multiple Documents with different agents via routing map.
- [ ] Poll filters work (ids, dates, status, queue, file, batch).
- [ ] Webhook fires per document terminal; retries recorded; poll still works if webhook fails.
- [ ] Sync wait returns in-call results or timeout+ids.
- [ ] Cancel file and cancel document behave per Queue design.
- [ ] Explicit reprocess creates new File.
- [ ] Mode 1 only in UI/API.
- [ ] Routing lock after first file.
- [ ] IntakeRejection for refused email with no files (when email wave done).

### Iden follow-on done-when (Band 15 — after Phase 1; Decision J3)

- [ ] Humans authenticate via live Iden (no fixed tokens in shipping path).
- [ ] External machine auth via Iden M2M; F2 keys retired or kill-switched.
- [ ] Tenant→Business exercised through Documate; known Iden defects tracked/fixed or waived.

---



## Finalized Decisions (from product plans 01/02)

- Three modules: Core / FrontendSupport / External  
- Guided-clone agents; schema on agent; queue type→agent map + lock  
- Optional log-only Batch when ≥2 files  
- Multi-file intake required; non-blocking  
- Webhook per Document  
- Sync wait API + async majority  
- Cancel file + document; explicit reprocess  
- Email: body+attachments; ambiguity reject; IntakeRejection  
- Mode 1 Phase 1; MCP internal for post-processing only  
- No general chatbot/ERP-MCP product suite

---



## Pending Decisions (block Phase 3 numbering detail / some waves)


| ID  | Topic                   | Options                                                                        | Rec    |
| --- | ----------------------- | ------------------------------------------------------------------------------ | ------ |
| A   | Worker hosting          | A1 in-process + Hangfire SQL / A2 separate Hangfire host / A3 serverless       | **A1 + Hangfire** |

| B   | Sync timeout policy     | B1 60s / B2 120s / B3 client cap                                               | B1     |
| C   | Sync webhooks           | C1 fire / C2 suppress / C3 flag                                                | C1     |
| D   | Email inbound           | D1 provider webhook / D2 IMAP / D3 stub first                                  | D3→D1  |
| E   | Split/classify bar      | E3 default PDF multi-doc; **optional intake hints skip split+classify**        | **E3 + hints** |
| F   | External auth interim   | F1 user token / F2 tenant API keys / F3 wait M2M                               | **F2 bridge** → retire Band 15 (after Phase 1) |
| G   | Exact timeout seconds   | with B                                                                         | 60     |
| H   | Tenant persistence      | **DECIDED H1** — CorTenant + CorTenantBusiness (product extension; Iden = SoT) | H1     |
| I   | Queue wire id           | **DECIDED I1** — UUID PK + SequenceId                                          | I1     |
| J   | Iden validation timing  | J1 Iden-first / J2 interleaved / **J3 late**                                   | **J3** |
| —   | **Id strategy**         | User-facing: UUID PK + SequenceId; catalogs: bigint + prefixed `*Key`          | locked |
| —   | **Iden tenancy**        | Tenant → Business; isolation = Business                                        | locked |
| —   | **Queues + QueueRoute** | Multi-queue day one; type→Agent routes                                         | locked |
| —   | **Iden phase**          | Discover APIs · live test via Documate · fix Iden · retire F2                  | Band 15 **after** Phase 1 (J3) |


---



## Assumptions

- Plan 00 engineering layout is the code home for this work.
- Developer treats exploration + queue design as approved for Phase 2 (explicit ask to write this plan).
- At least one LLM provider and object storage will be available in the first Core wave.
- Angular web is required for Phase 1 configuration (not API-only).
- Domain entity catalog is greenfield and subject to checklist approval before Wave 0 coding.

---



## Risks


| Risk                                   | Mitigation                                                       |
| -------------------------------------- | ---------------------------------------------------------------- |
| Split/classify quality poor            | Clear Failed codes; PartialReady; reprocess; E decision honesty  |
| Sync timeouts frustrate UI             | Short docs; always return poll ids; keep async primary for packs |
| Provider cost from email abuse         | Gates + unguessable address; allowlist before hard sell          |
| Scope creep into Mode 2 / chatbots     | Explicit out of scope; refuse in DQs                             |
| Auth interim keys become permanent     | J3: Band 15 is mandatory follow-on after Phase 1; label F2 temporary; schedule activate DQ-1501 |
| Iden API gaps / bugs block Documate    | Defect loop in Band 15; fix in Iden; waivers only if explicit  |
| Entity model churn after coding starts | Verify catalog (+ H) before Phase 3 / Wave 0                     |


---



## Readiness

**Ready for next phase:** **Ready for execution** — entity catalog approved; A–I closed; DQ = [03-documate-v3-dispatch-queue.md](./03-documate-v3-dispatch-queue.md).  

Select a DQ item to implement (do not code until selected).

---



## Revision log


| Date       | Change                                                                                                                                       |
| ---------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| 2026-07-31 | Initial product implementation plan from frozen 01/02 designs.                                                                               |
| 2026-07-31 | Clarified DTO vs old_code principles (greenfield).                                                                                           |
| 2026-07-31 | **Added full Domain entity catalog for verification** (+ Decision H).                                                                        |
| 2026-07-31 | Canonical glossary: Batch / File / Document; retired Job as product term.                                                                    |
| 2026-08-01 | Entity catalog: bigint Id + soft delete; Provider & DocumentType; Key not Code; PublicId on work entities; Decision I; RowVersion explained. |
| 2026-08-01 | **Id strategy locked:** user-facing UUID as PK + SequenceId; Queue included; catalogs stay bigint+Key.                                       |
| 2026-08-01 | Out of scope note: white-label SDK, statements reconciliation, MCM DN rebranding (exploration §3.1–3.3).                                     |
| 2026-08-01 | **Iden Tenant → Business;** CorTenantBusiness; isolation = Business; authz/security updated.                                                 |
| 2026-08-02 | Provider catalog: flat model/engine keys; optional VendorHint only — no Company entity.                                                      |
| 2026-08-02 | Rename: `CorTenant`, `CorTenantBusiness` (was TenantAccount / BusinessAccount).                                                              |
| 2026-08-02 | WorkflowDefinition: not public wire id → bigint PK (no UUID/SequenceId).                                                                     |
| 2026-08-02 | Added CorEnumType + CorEnum (ERP30-inspired; Scope System/Tenant/Business).                                                                  |
| 2026-08-02 | All persisted enums → CorEnum FKs; AllowlistModeEnumId documented; seed type keys listed.                                                    |
| 2026-08-02 | Architecture: CorEnum rules — no static enum columns; **compare Ids** (not EnumKey) in domain logic.                                         |
| 2026-08-02 | Lean tenancy: BusinessId-only on ops rows; CorTenantBusiness.TenantName projection; CorEnum: EnumKey, no ParentId.                           |
| 2026-08-02 | CorEnum thinned: dropped DisplayOrder, IsSelectable, IsDefaultSelected, IsSystem, hierarchy fields.                                          |
| 2026-08-02 | Catalog keys renamed: bare `Key` → EnumTypeKey, ProviderKey, DocumentTypeKey, AgentTemplateKey, WorkflowKey.                                 |
| 2026-08-02 | Entity catalog: added logical purpose table + per-entity Role blurbs.                                                                        |
| 2026-08-02 | Multi-queue day one; Agent-primary post-processing; Queues + QueueRoute kept.                                                                |
| 2026-08-02 | **Decision H1 locked** (CorTenant + CorTenantBusiness; Iden remains SoT for org master).                                                     |
| 2026-08-02 | Entity catalog approved; A–I closed; Phase 3 DQ created (`03-documate-v3-dispatch-queue.md`).                                                |
| 2026-08-02 | Added **Wave 10 / Band 15 — Iden Integration & Validation**; F2 bridge-only; Decision J (rec J2); Phase 1 done-when includes real Iden.   |
| 2026-08-02 | **Decision J3 locked** — Band 15 parked until Phase 1 product done; Iden follow-on done-when separate.                                 |
| 2026-08-02 | Pointer to exploration **§3.4** classification strategy brainstorm (deferred until DQ-0702).                                          |
| 2026-08-03 | **Entity prefixes locked:** `Cor*` infra/catalogs; `Ops*` operational; FK columns drop prefix (`TenantId`, `FileId`).                 |
| 2026-08-04 | **Decision A amended:** A1 hosting + **Hangfire (SQL storage)**; Channel-only enqueue rejected; webhooks share Hangfire. |
| 2026-08-04 | **Intake hints:** optional `documentCount` + DocumentType key(s) on External upload; complete hints → skip split+classify (E3 default otherwise). |
| 2026-08-18 | **DQ-0702 Phase 1:** predetermined `documentTypeKey` skips split/classify; real split deferred; QueueRoute still runs. |
| 2026-08-18 | **DQ-0703:** Mode 1 `documate_meta` extract + schema validate; External poll `resultJson`. |


