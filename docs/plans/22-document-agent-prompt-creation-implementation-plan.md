# 22 — Document Agent prompt creation (Implementation Plan)

> **Status:** Phase 2 — **complete** (approved → Phase 3 2026-09-20)  
> **Type:** Engineering implementation plan (product behavior locked in exploration)  
> **Upstream:** [22-document-agent-prompt-creation-exploration.md](./22-document-agent-prompt-creation-exploration.md) (PP1–PP7, P1–P5 locked)  
> **Related:** Plan 01 Agent-primary extract; Plan 07 CUS-09/10; Plan 11 INT-16 (unparked here); DQ-1101 MCP runner **left in tree, unused on extract path**  
> **Downstream:** [22-document-agent-prompt-creation-dispatch-queue.md](./22-document-agent-prompt-creation-dispatch-queue.md) (Band 19)

**Outcome:** One shared extract-prompt composer; template system prompt snapshotted onto agents; customer post-process textarea + schema `description` comments; last sent prompt stored 7 days on a child table; customer files show **user prompt only**; admin templates CRUD with optional push; admin can list any tenant/business agent and see the **full** composed prompt.

**Out of this plan:** Deleting `AgentPostProcessRunner`; OCR/classify prompts; N-attempt prompt history; per-agent system override; customer FE redesign.

---

## Planning flow

| Phase | Document |
|-------|----------|
| 1 — Exploration | [22-document-agent-prompt-creation-exploration.md](./22-document-agent-prompt-creation-exploration.md) ✅ |
| 2 — Implementation plan | **This file** |
| 3 — Dispatch queue | [22-document-agent-prompt-creation-dispatch-queue.md](./22-document-agent-prompt-creation-dispatch-queue.md) ⬜ Band 19 |

---

## Delivery Principles

1. **One composer.** Extract, customer preview, and admin preview call the same `IExtractPromptComposer`. Preview/runtime must not drift.
2. **System prompt is admin-only.** Never on `/api/app` DTOs. LLM `system` role uses the agent snapshot. Customer preview/files return **user prompt only**.
3. **Snapshot + optional push (P1).** Clone copies `CorAgentTemplate.SystemPrompt` → `OpsAgent.SystemPrompt`. Template save asks whether to update all clones. No live inherit.
4. **Post-process is prompt text (PP1).** Customer textarea → `OpsAgent.PostProcessPrompt`. Do not call `IAgentPostProcessRunner` from [`DocumentExtractStage`](../../apps/api/Infrastructure/Pipeline/Stages/DocumentExtractStage.cs). Leave the runner class registered but unused.
5. **Schema comments (P4).** JSON Schema `description` on header fields, table/section nodes, and columns. Composer inlines the schema JSON as-is.
6. **Last prompt, 7 days (P2/P3).** Child table keyed by `FileId` + `DocumentId`; upsert last run; Hangfire daily purge `CreatedAt < now-7d`. Not a column on `OpsFiles`.
7. **Copy existing slices.** Customer agents/files: extend [`AgentsController`](../../apps/api/Modules/FrontendSupport/Features/Agents/AgentsController.cs) and [`DocumentsController`](../../apps/api/Modules/FrontendSupport/Features/Files/DocumentsController.cs). Admin: new PlatformAdmin features (MediatR), not DbContext-in-controller like [`AdminProvidersController`](../../apps/api/Modules/PlatformAdmin/Features/Providers/AdminProvidersController.cs).
8. **Do not redefine CQRS/folder conventions.** Follow `docs/architecture/`.

---

## Domain Architecture Layer

| Concern | Home |
|---------|------|
| Composer | `apps/api/Infrastructure/Extract/ExtractPromptComposer.cs` (`IExtractPromptComposer`) |
| LLM send | Existing [`LiveLlmDocumentExtractAdapter`](../../apps/api/Infrastructure/Extract/LiveLlmDocumentExtractAdapter.cs) — system role + user role from composer |
| Persist last prompt | New `OpsDocumentExtractPrompt` + upsert from extract stage |
| 7-day purge | Hangfire recurring job next to [`EmailIntakeJobs`](../../apps/api/Program.cs) registration |
| Agent/template columns | [`OpsAgent`](../../apps/api/Domain/OpsAgent.cs), [`CorAgentTemplate`](../../apps/api/Domain/CorAgentTemplate.cs) + EF config/migration |
| Customer agent API | Extend FrontendSupport `Agents` — `PostProcessPrompt`; `GET/POST .../prompt-preview` (redacted) |
| Customer files API | Extend `DocumentDetailDto` with user prompt; list items get `hasExtractPrompt` only |
| Admin templates | `Modules/PlatformAdmin/Features/AgentTemplates/` — CRUD + `pushSystemPrompt` flag on update |
| Admin agents | `Modules/PlatformAdmin/Features/Agents/` — list filter tenant/business; detail + full preview |
| Admin ops | Extend [`AdminDocumentDetailDto`](../../apps/api/Modules/PlatformAdmin/Features/Ops/AdminOpsDetailController.cs) with system + user prompt texts |
| Customer FE | [`agent-edit`](../../apps/web/src/app/features/agents/pages/agent-edit.page.html), schema builder, file/document detail |
| Admin FE | `apps/admin` new routes: templates + agents; ops document detail prompt panel |
| Seed | [`PlatformCatalogSeeder.EnsureTemplate`](../../apps/api/Infrastructure/Persistence/Seeding/PlatformCatalogSeeder.cs) writes `SystemPrompt` |
| Tests | `tests/api` composer + redaction + upsert/purge |

### Entity (normative)

`OpsDocumentExtractPrompt` : `WireFacingEntity` (UUID PK, like other ops rows)

| Column | Notes |
|--------|--------|
| `BusinessId` | Isolation |
| `FileId` | FK `OpsFiles` |
| `DocumentId` | FK `OpsDocuments`; **unique** among non-deleted (last run) |
| `SystemPromptText` | `nvarchar(max)` — **admin only** |
| `UserPromptText` | `nvarchar(max)` — customer-visible |
| `CreatedAt` | Purge key; indexed |

Do **not** add prompt text to [`OpsFile`](../../apps/api/Domain/OpsFile.cs).

`CorAgentTemplate`: add `SystemPrompt` (`nvarchar(max)`, required after backfill), `DefaultPostProcessPrompt` (`nvarchar(max)`, default empty).

`OpsAgent`: add `SystemPrompt` (`nvarchar(max)`, never in app DTO), `PostProcessPrompt` (`nvarchar(max)`, default empty).

Backfill: all templates and agents get today’s hardcoded system string from [`LiveLlmDocumentExtractAdapter`](../../apps/api/Infrastructure/Extract/LiveLlmDocumentExtractAdapter.cs) (“You extract structured data…”). Existing `DefaultWorkflowId` left as-is on rows; extract stage stops invoking the runner.

### Composer contract (normative)

```text
Compose(systemPrompt, instructions, outputSchemaJson, postProcessPrompt, documentText | null)
  → systemMessage = systemPrompt (trimmed; fallback ExtractPromptDefaults.SystemPrompt)
  → userMessage =
        Agent instructions:
        {instructions}

        Output JSON Schema:
        {outputSchemaJson}

        [Additional post-process instructions:]   # omit block if postProcess empty
        {postProcessPrompt}

        Document text (full OCR / normalize text):
        {documentText ?? "[Document text will be inserted at extract time]"}
```

LLM: `role=system` ← `systemMessage`; `role=user` ← `userMessage` (same split for Anthropic `system` field).

Preview uses `documentText: null`. Extract uses OCR/slice text.

---

## Domain Validation Rules

| Rule | Behavior |
|------|----------|
| Customer never reads system prompt | `AgentDto`, catalog template DTO, document detail, prompt-preview **omit** it |
| Admin reads system prompt | Template CRUD, admin agent detail, admin ops document, admin preview |
| Clone | Copy template `SystemPrompt`, `DefaultInstructions`, `DefaultSchemaJson`, `DefaultPostProcessPrompt`, provider. **Do not** set `DefaultWorkflowId` |
| Create agent (no template) | `SystemPrompt` = `ExtractPromptDefaults.SystemPrompt`; `PostProcessPrompt` empty |
| Template save + `pushSystemPrompt=true` | `UPDATE OpsAgents SET SystemPrompt=… WHERE SourceTemplateId=template.Id AND NOT IsDeleted` |
| Template save + push false | Existing agents unchanged |
| Schema | `buildOutputSchema` / `parseOutputSchema` round-trip `description` |
| Upsert prompt | One row per `DocumentId`; replace texts + `CreatedAt` on each extract |
| Purge | Delete prompt rows with `CreatedAt < UtcNow.AddDays(-7)` |
| Empty post-process | Omit that section from user message |
| MCP | Extract path does not call `postProcess.RunAsync` |

---

## Decision Required (locked this phase)

Exploration already closed product DRs. Engineering choices below follow existing slices and P1–P5; treated as **locked** so Phase 3 can number DQs.

| ID | Locked |
|----|--------|
| **DR-PC1** | Child table stores **`SystemPromptText` + `UserPromptText`** (not one concatenated blob, not redact-by-string). |
| **DR-PC2** | Skip MCP in extract stage; clone/create no longer attach `DefaultWorkflowId`. Runner class stays. |
| **DR-PC3** | Hangfire daily recurring purge (same registration style as `email-intake-mime-retention`). |
| **DR-PC4** | Customer UI: file-detail documents table gets a Prompt affordance; full user prompt on **document detail**. |
| **DR-PC5** | Admin nav: **Agent templates** + **Agents** (filters: tenant, business). Unpark INT-16. |
| **DR-PC6** | New admin APIs use MediatR feature folders under `PlatformAdmin`. |
| **DR-PC7** | Live editor preview = `POST` compose from current fields; saved-state = `GET`. Customer POST body cannot include system prompt (server uses snapshot). |

If you override any DR-PC* before Phase 3, say so; otherwise they stand.

---

## Process Flows

### F1 — Clone from template

```text
POST /api/app/agents/clone-from-template
  → copy SystemPrompt, DefaultPostProcessPrompt, schema, instructions
  → DefaultWorkflowId = null
  → return AgentDto (no SystemPrompt)
```

### F2 — Customer edits agent + preview

```text
PUT /api/app/agents/{id}  (instructions, schema, postProcessPrompt, …)
POST /api/app/agents/{id}/prompt-preview
  body: instructions, outputSchemaJson, postProcessPrompt (optional overrides)
  → composer with agent.SystemPrompt (not returned)
  → { userPrompt }
```

### F3 — Extract

```text
DocumentExtractStage
  → composer(agent.SystemPrompt, instructions, schema, postProcess, ocrText)
  → LLM system+user
  → upsert OpsDocumentExtractPrompt
  → write ResultJson
  → do not RunAsync MCP
```

### F4 — Customer files history

```text
GET document detail (app)
  → UserPromptText if row exists and CreatedAt within 7d
  → never SystemPromptText
File detail documents table
  → hasExtractPrompt + capturedAt; Open → document detail
```

### F5 — Admin template save + push

```text
PUT /api/admin/agent-templates/{id}
  body includes systemPrompt, defaults, pushSystemPrompt?
  → save template
  → if push: update all cloned OpsAgent.SystemPrompt
  → response includes clonedAgentCount
```

### F6 — Admin inspect tenant agent

```text
GET /api/admin/agents?tenantId=&businessId=
GET /api/admin/agents/{id}
  → includes systemPrompt, instructions, schema, postProcessPrompt
GET /api/admin/agents/{id}/prompt-preview
  → { systemPrompt, userPrompt }  (placeholder document text)
```

---

## Instruction and Control Set

| Control | Rule |
|---------|------|
| App vs admin | `/api/app/*` redacted; `/api/admin/*` full |
| Business isolation | App queries filter `IBusinessContext.BusinessId` |
| Admin | `PlatformAdmin` policy; may read any tenant/business |
| List queries | File/document **lists** must not select prompt `nvarchar(max)` |
| Composer fallback | Empty agent/template system prompt → `ExtractPromptDefaults.SystemPrompt` |
| Push copy | Confirm clone count in admin UI before `pushSystemPrompt=true` |

---

## Permissions and Security

- Customer cannot read or write `OpsAgent.SystemPrompt` or `OpsDocumentExtractPrompt.SystemPromptText`.
- Catalog `GET /api/app/catalogs/agent-templates` stays clone-gallery: **no** `systemPrompt` field (keep `defaultInstructions` / `defaultSchemaJson` / add `defaultPostProcessPrompt` only).
- Admin template DTO includes `systemPrompt`.
- Stored user prompt includes OCR text — treat as tenant data; 7-day cap.

---

## Dispatch Index (proposed — Phase 3)

Exact DQ ids: Band 19 — see [dispatch queue](./22-document-agent-prompt-creation-dispatch-queue.md) (API/Web split vs this index).

| Proposed | Wave | Outcome |
|----------|------|---------|
| DQ-PC-01 | PC-0 | Migration: template/agent prompt columns + `OpsDocumentExtractPrompt`; backfill system prompt; unique DocumentId |
| DQ-PC-02 | PC-1 | Composer + adapter split + persist upsert + skip MCP; unit tests |
| DQ-PC-03 | PC-2 | App Agents: `postProcessPrompt`, clone/create, `GET/POST prompt-preview` (redacted) |
| DQ-PC-04 | PC-3 | Customer agent edit: textarea, schema `description`, redacted preview |
| DQ-PC-05 | PC-4 | App document/file DTOs + customer files UI (user prompt only) |
| DQ-PC-06 | PC-5 | Admin template CRUD + push dialog; seeder `SystemPrompt` |
| DQ-PC-07 | PC-6 | Admin agent list/detail/preview + ops document full prompt |
| DQ-PC-08 | PC-7 | Hangfire 7-day purge + evidence tests (redaction, upsert, purge) |

---

## Wave Sections

### PC-0 — Schema

- EF: `CorAgentTemplate.SystemPrompt`, `DefaultPostProcessPrompt`; `OpsAgent.SystemPrompt`, `PostProcessPrompt`; `OpsDocumentExtractPrompt`.
- Backfill system prompt from current hardcoded string.
- Indexes: `(DocumentId)` unique filtered not-deleted; `(CreatedAt)` for purge; `(FileId)`.

### PC-1 — Composer + extract

- `ExtractPromptDefaults` + `IExtractPromptComposer`.
- [`LiveLlmDocumentExtractAdapter`](../../apps/api/Infrastructure/Extract/LiveLlmDocumentExtractAdapter.cs) / [`ExtractAdapterRequest`](../../apps/api/Infrastructure/Extract/IDocumentExtractAdapter.cs) take system + post-process (or compose inside adapter from agent fields).
- [`DocumentExtractStage`](../../apps/api/Infrastructure/Pipeline/Stages/DocumentExtractStage.cs): pass snapshot fields; upsert prompt row; **remove** `postProcess.RunAsync` call.
- Tests: labeled sections, omit empty post-process, placeholder vs OCR, schema `description` present in user message.

### PC-2 — Customer agent API

- `AgentDto` / create / update: `postProcessPrompt`; **no** `systemPrompt`.
- Clone copies snapshot fields; `DefaultWorkflowId` null.
- `GET/POST /api/app/agents/{id}/prompt-preview` → `{ userPrompt }`.
- Catalog template DTO: `defaultPostProcessPrompt`; still no system prompt.

### PC-3 — Customer agent FE

- Post-process tab: textarea bound to `postProcessPrompt`; remove workflow checkbox. Keep provider override on General or Post-process as it is today (provider is not MCP).
- Schema builder: comment box per field, table, column; [`schema-builder.util.ts`](../../apps/web/src/app/features/agents/utils/schema-builder.util.ts) `description`.
- Preview panel (new tab or side panel) calling POST preview.

### PC-4 — Customer files

- `DocumentDetailDto.extractUserPrompt` + `extractPromptCapturedAt`.
- File documents list: `hasExtractPrompt` (no text).
- [`document-detail.page.html`](../../apps/web/src/app/features/files/pages/document-detail.page.html): “Extract prompt” card (user text, empty after purge).
- [`file-detail.page.html`](../../apps/web/src/app/features/files/pages/file-detail.page.html) documents table: Prompt column → Open document.

### PC-5 — Admin templates

- `GET/POST/PUT /api/admin/agent-templates` (list may include unpublished).
- PUT `pushSystemPrompt` + return `clonedAgentCount`.
- Admin FE: list/edit system prompt, default instructions, default schema (reuse builder or textarea JSON for v1 — **prefer reuse** of schema builder if it can be shared; otherwise JSON + descriptions in a simple form copied from customer builder).
- Confirm dialog: “Update system prompt on N cloned agents?”
- Seeder writes `SystemPrompt`.

### PC-6 — Admin agents + ops

- `GET /api/admin/agents` paged, filter `tenantId`, `businessId`, name.
- Detail: ingredients + full preview.
- Ops document detail: system + user prompt panels.
- Nav: Agent templates, Agents ([`nav.config.ts`](../../apps/admin/src/app/shared/layout/nav.config.ts)).

### PC-7 — Retention + evidence

- `ExtractPromptRetentionJobs.PurgeExpiredAsync`; `Cron.Daily` in [`Program.cs`](../../apps/api/Program.cs).
- Tests: app DTO never contains system prompt string; admin DTO does; upsert replaces; purge deletes old rows.

---

## Output contract

### Finalized from exploration

PP1–PP7, P1–P5 as in the exploration file.

### Locked (this Phase 2)

DR-PC1…PC7 as above.

### Pending

None — unless you override a DR-PC* before Phase 3.

### Assumptions

- Sharing the schema builder between `apps/web` and `apps/admin` may be a copy in v1 if there is no existing shared Angular library; do not create a new shared package unless architecture already allows it ([`shared-packages-policy.md`](../../docs/architecture/patterns/shared-packages-policy.md)).
- `DefaultWorkflowId` columns remain; they are inert on extract.
- OpenAPI client regen only if this repo’s current slice already regenerates for FrontendSupport (do not invent a new client pipeline).

### Risks

- Accidental inclusion of `SystemPrompt` in `AgentDto` or file list JSON.
- Large `UserPromptText` (OCR) — mitigated by child table + 7-day purge + not selecting it on lists.
- Admin push without reading the count dialog.

### Readiness

**Phase 2 complete.** Downstream: Band 19 dispatch queue. Execute via DQ selection (start **DQ-1901**).

---

## Changelog

| Date | Note |
|------|------|
| 2026-09-20 | Phase 2 draft from “go ahead” after P1–P5 lock. DR-PC1…PC7 recommended locked. |
| 2026-09-20 | Phase 3 approved (“go ahead”) → Band 19 DQ-1901…1911 filed. |
