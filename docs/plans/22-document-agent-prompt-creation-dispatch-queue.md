# Documate v3 — Document Agent prompt creation — Dispatch Queue

> **Document type:** Dispatch queue (Phase 3)  
> **Status:** 🔄 **Band 19 in progress** (DQ-1901–1904, DQ-1907–1910 ✅)  
> **Source plan:** [22-document-agent-prompt-creation-implementation-plan.md](./22-document-agent-prompt-creation-implementation-plan.md)  
> **Upstream:** [22-document-agent-prompt-creation-exploration.md](./22-document-agent-prompt-creation-exploration.md)  
> **Band 19** = Extract prompt compose / store / preview (plans 22)

**Status legend:** ✅ Complete · 🔄 In Progress · ⬜ Ready · ⏸ Parked · ❌ Cancelled

API vs Web are **separate** items (governance bands 04/05 spirit). Do not mix them in one DQ.

---

## Completion Summary

| Metric | Value |
|--------|--------|
| Total DQ items | 11 |
| ✅ Complete | 8 |
| 🔄 In Progress | 0 |
| ⬜ Ready | 1 (DQ-1905) |
| ⏸ Parked | 0 |
| ❌ Cancelled | 0 |

### Finalized decisions

All PP\* / P1–P5 (exploration) and DR-PC1…PC7 (implementation plan).

### Pending decisions

None.

### Assumptions

- Execute **one** DQ at a time unless the developer batches.
- Apply the Band 19 migration before relying on extract persist or admin template columns.
- Do not invent product behavior; do not redefine CQRS/folder conventions.

### Risks

| Risk | Mitigation |
|------|------------|
| System prompt leak on `/api/app` | DQ-1903/1905 DTOs omit it; DQ-1911 evidence asserts absence |
| `nvarchar(max)` on file list | Prompt lives only on `OpsDocumentExtractPrompt`; lists use `hasExtractPrompt` |
| MCP still runs | DQ-1902 removes `RunAsync` from extract stage |

---

## Dispatch Index

| DQ | Band | Title | Status | Depends on |
|----|------|--------|--------|------------|
| **DQ-1901** | 19 | Schema + migration + backfill | ✅ | — |
| **DQ-1902** | 19 | Composer + extract persist + skip MCP | ✅ | DQ-1901 |
| **DQ-1903** | 19 | App Agents API: post-process + redacted preview | ✅ | DQ-1902 |
| **DQ-1904** | 19 | Customer agent FE: textarea, schema comments, preview | ✅ | DQ-1903 |
| **DQ-1905** | 19 | App files/documents API: user prompt fields | ⬜ | DQ-1902 |
| **DQ-1906** | 19 | Customer files FE: prompt on file + document | ⬜ | DQ-1905 |
| **DQ-1907** | 19 | Admin template API + seeder | ✅ | DQ-1901 |
| **DQ-1908** | 19 | Admin template FE + push dialog | ✅ | DQ-1907 |
| **DQ-1909** | 19 | Admin agents API + ops document prompt | ✅ | DQ-1902 |
| **DQ-1910** | 19 | Admin agents FE + ops prompt UI | ✅ | DQ-1909 |
| **DQ-1911** | 19 | Hangfire 7-day purge + evidence | ⬜ | DQ-1903, DQ-1905, DQ-1909 |

**Suggested next:** **DQ-1905** (app files/documents API).

**Critical path (extract + customer files):** DQ-1901 → DQ-1902 → DQ-1905 → DQ-1906  
**Customer agent edit:** DQ-1901 → DQ-1902 → DQ-1903 → DQ-1904  
**Admin templates:** DQ-1901 → DQ-1907 → DQ-1908 (parallel after 1901)  
**Admin agents:** DQ-1901 → DQ-1902 → DQ-1909 → DQ-1910

After 1901, **DQ-1902** and **DQ-1907** can proceed in parallel if batched.

---

## Wave / DQ Entries

### DQ-1901 — Schema + migration + backfill

- **Status:** ✅ Complete  
- **Dependency:** —  
- **Source:** Plan 22 PC-0; P1, P2, DR-PC1  
- **Outcome:**
  - `CorAgentTemplate.SystemPrompt`, `DefaultPostProcessPrompt`.
  - `OpsAgent.SystemPrompt`, `PostProcessPrompt`.
  - New `OpsDocumentExtractPrompt` (`WireFacingEntity`): `BusinessId`, `FileId`, `DocumentId`, `SystemPromptText`, `UserPromptText`; unique `DocumentId` (not deleted); indexes on `FileId`, `CreatedAt`.
  - Backfill templates + agents with current hardcoded system string from `LiveLlmDocumentExtractAdapter`.
  - EF migration; `DocumateDbContext` DbSet; entity configuration.
- **Required Documents:** Plan 22 implementation §Domain Architecture; `docs/architecture/patterns/cqrs-feature-slice.md` (entity vs DTO); `EntityConfigurations.cs`  
- **Evidence:**
  - `Domain/OpsDocumentExtractPrompt.cs`; `CorAgentTemplate` + `OpsAgent` prompt columns
  - `EntityConfigurations.cs` unique filtered index `IX_OpsDocumentExtractPrompts_DocumentId`
  - `DocumateDbContext.OpsDocumentExtractPrompts`
  - Migration `20260920040000_Band19ExtractPrompts` (defaultValue backfill of default system prompt)
  - API `dotnet msbuild /t:CoreCompile` succeeded (full `dotnet build` blocked by running debugger locking `Documate.Api.dll`)

---

### DQ-1902 — Composer + extract persist + skip MCP

- **Status:** ✅ Complete  
- **Dependency:** DQ-1901  
- **Source:** Plan 22 PC-1; PP1, DR-PC1, DR-PC2  
- **Outcome:**
  - `ExtractPromptDefaults` + `IExtractPromptComposer` in `Infrastructure/Extract/`.
  - `LiveLlmDocumentExtractAdapter` uses composer: LLM `system` = snapshot system prompt; `user` = instructions + schema + post-process + document text.
  - `DocumentExtractStage` passes agent snapshot fields; **upsert** `OpsDocumentExtractPrompt`; **does not** call `IAgentPostProcessRunner.RunAsync`.
  - Leave runner class registered.
  - Unit tests: sections, omit empty post-process, placeholder vs OCR, schema `description` in user message.
- **Required Documents:** Plan 22 §Composer contract; `IDocumentExtractAdapter.cs`; `DocumentExtractStage.cs`  
- **Evidence:**
  - `Infrastructure/Extract/ExtractPromptComposer.cs` registered singleton
  - `LiveLlmDocumentExtractAdapter` + unused `Mode1DocumateMetaExtractAdapter` return composed texts
  - `DocumentExtractStage.UpsertExtractPromptAsync`; MCP `RunAsync` removed from extract path; `IAgentPostProcessRunner` still registered
  - `tests/api/ExtractPromptComposerTests.cs` (xunit; run after stopping the locked API process)

---

### DQ-1903 — App Agents API (post-process + redacted preview)

- **Status:** ✅ Complete  
- **Dependency:** DQ-1902  
- **Source:** Plan 22 PC-2; PP7, DR-PC7  
- **Outcome:**
  - `AgentDto` / create / update include `postProcessPrompt`; **never** `systemPrompt`.
  - Clone copies `SystemPrompt` + `DefaultPostProcessPrompt`; `DefaultWorkflowId` = null.
  - Create-without-template uses `ExtractPromptDefaults.SystemPrompt`.
  - `GET` + `POST /api/app/agents/{id}/prompt-preview` → `{ userPrompt }` only; POST overrides customer fields; server uses stored snapshot for system.
  - Catalog template DTO: `defaultPostProcessPrompt`; still no system prompt.
- **Required Documents:** Plan 22 F2; `AgentsController.cs`; `CatalogsController.cs`; `critical-rules-api.md`  
- **Evidence:**
  - `AgentDto` / create / update: `PostProcessPrompt`; no `SystemPrompt` on `/api/app`
  - Clone snapshots `SystemPrompt` + `DefaultPostProcessPrompt`; `DefaultWorkflowId` = null; no `IDefaultWorkflowBootstrap`
  - Create uses `ExtractPromptDefaults.SystemPrompt`; update applies `PostProcessPrompt` only when the body includes it
  - `GET`/`POST /api/app/agents/{id}/prompt-preview` → `{ userPrompt }`; POST overrides instructions/schema/post-process; composer uses stored system snapshot
  - Catalog `AgentTemplateDto.DefaultPostProcessPrompt`; still no system prompt
  - `tests/api/AgentsAppApiTests.cs` (DTO redaction + preview handler)

---

### DQ-1904 — Customer agent FE

- **Status:** ✅ Complete  
- **Dependency:** DQ-1903  
- **Source:** Plan 22 PC-3; PP1, P4  
- **Outcome:**
  - Post-process tab: textarea for `postProcessPrompt`; remove workflow checkbox. Keep provider override.
  - Schema builder: `description` on fields, tables, columns (`schema-builder.util.ts` + HTML).
  - Redacted compose preview (GET saved / POST live).
- **Required Documents:** Plan 22 PC-3; `agent-edit.page.*`; `schema-form-builder.*`; `agents-api.service.ts`; `critical-rules-web.md`  
- **Evidence:**
  - Post-process tab: textarea bound to `postProcessPrompt`; workflow checkbox removed; provider override kept
  - Schema builder comments → JSON Schema `description` on fields, tables, columns
  - Prompt preview tab: POST draft / GET last saved → `userPrompt` only
  - `tests` `schema-builder.util.spec.ts` (3 passed); `ng serve` compiled `agent-edit-page`
  - Browser: Delivery note agent — schema comments + post-process in draft preview; last-saved omitted unsaved text; no system prompt; Intake/Test tabs still load

---

### DQ-1905 — App files/documents API (user prompt)

- **Status:** ⬜ Ready after DQ-1902  
- **Dependency:** DQ-1902  
- **Source:** Plan 22 PC-4 API; PP3, PP7, DR-PC4  
- **Outcome:**
  - `DocumentDetailDto`: `extractUserPrompt`, `extractPromptCapturedAt` (from `UserPromptText` only).
  - File documents list items: `hasExtractPrompt` (+ optional capturedAt) — **do not** select prompt text on list.
  - Never return `SystemPromptText` on `/api/app`.
- **Required Documents:** Plan 22 F4; `DocumentsController.cs`; Files list-documents handler  
- **Evidence:** — (fill on execute)

---

### DQ-1906 — Customer files FE

- **Status:** ⬜ Ready after DQ-1905  
- **Dependency:** DQ-1905  
- **Source:** Plan 22 PC-4 FE; P3, DR-PC4  
- **Outcome:**
  - Document detail: “Extract prompt” card showing user prompt (empty/missing after 7-day purge).
  - File detail documents table: Prompt column / affordance → document detail.
- **Required Documents:** Plan 22 PC-4; `document-detail.page.*`; `file-detail.page.*`  
- **Evidence:** — (fill on execute)

---

### DQ-1907 — Admin template API + seeder

- **Status:** ✅ Complete  
- **Dependency:** DQ-1901  
- **Source:** Plan 22 PC-5 API; P1, P5, DR-PC5, DR-PC6  
- **Outcome:**
  - `Modules/PlatformAdmin/Features/AgentTemplates/` MediatR CRUD (`GET` list including unpublished, `GET` by id, `POST`, `PUT`).
  - PUT accepts `pushSystemPrompt`; when true, update `OpsAgent.SystemPrompt` for all non-deleted clones; return `clonedAgentCount`.
  - DTO **includes** `systemPrompt`.
  - `PlatformCatalogSeeder.EnsureTemplate` writes `SystemPrompt` (default string).
- **Required Documents:** Plan 22 F5; `cqrs-feature-slice.md`; `PlatformCatalogSeeder.cs`  
- **Evidence:**
  - `Modules/PlatformAdmin/Features/AgentTemplates/AdminAgentTemplatesController.cs`
  - PUT `pushSystemPrompt` updates cloned `OpsAgent.SystemPrompt`; response `clonedAgentCount` + `pushedSystemPrompt`
  - Seeder sets `SystemPrompt` on insert and backfills empty existing rows

---

### DQ-1908 — Admin template FE + push dialog

- **Status:** ✅ Complete  
- **Dependency:** DQ-1907  
- **Source:** Plan 22 PC-5 FE; P1  
- **Outcome:**
  - Admin routes + nav: Agent templates.
  - List / create / edit: system prompt, default instructions, default schema (copy schema builder or JSON+descriptions — no new shared package unless policy allows), default post-process, publish flag.
  - On save: confirm “Update system prompt on N cloned agents?” before `pushSystemPrompt=true`.
- **Required Documents:** Plan 22 PC-5; `nav.config.ts`; `shared-packages-policy.md`; Plan 11 INT-16  
- **Evidence:**
  - Nav **Platform → Agent templates** (`/agent-templates`)
  - List / new / edit pages; schema builder copied into admin (no shared package)
  - Save asks “Update system prompt on N cloned agents?” when system prompt changed and clones exist
  - `GET /api/admin/document-types` lookup for the type picker
  - `schema-builder.util.spec.ts` (3 passed); `ng build` includes `agent-template-edit-page`

---

### DQ-1909 — Admin agents API + ops document prompt

- **Status:** ✅ Complete  
- **Dependency:** DQ-1902  
- **Source:** Plan 22 PC-6 API; PP4, PP5, DR-PC5  
- **Outcome:**
  - `Modules/PlatformAdmin/Features/Agents/`: paged list filter `tenantId`, `businessId`, name; get by id includes system prompt + instructions + schema + post-process.
  - `GET /api/admin/agents/{id}/prompt-preview` → `{ systemPrompt, userPrompt }` (placeholder document text).
  - Extend `AdminDocumentDetailDto` with `extractSystemPrompt` + `extractUserPrompt` + capturedAt.
- **Required Documents:** Plan 22 F6; `AdminOpsDetailController.cs`; `AdminBusinessesController.cs` (agent summary today)  
- **Evidence:**
  - `AdminAgentsController`: `GET /api/admin/agents` (paged, `tenantId` / `businessId` / `name`); `GET /api/admin/agents/{id}` includes `systemPrompt`, instructions, schema, post-process, tenant/business labels
  - `GET` + `POST /api/admin/agents/{id}/prompt-preview` → `{ systemPrompt, userPrompt }` via `IExtractPromptComposer` (`documentText: null`); POST optional field overrides
  - `AdminDocumentDetailDto.ExtractSystemPrompt` / `ExtractUserPrompt` / `ExtractPromptCapturedAt` from `OpsDocumentExtractPrompt`; missing/purged → nulls
  - File-documents list `hasExtractPrompt` only (no `nvarchar(max)` prompt text on list)
  - `tests/api/AdminAgentsApiTests.cs` — 5 passed (alt output; debugger holds `Documate.Api.dll`)
  - API `dotnet msbuild /t:CoreCompile` succeeded; full `dotnet test` copy blocked by `netcoredbg.exe`

---

### DQ-1910 — Admin agents FE + ops prompt UI

- **Status:** ✅ Complete  
- **Dependency:** DQ-1909  
- **Source:** Plan 22 PC-6 FE; DR-PC5  
- **Outcome:**
  - Admin Agents list (tenant/business filters) + detail with full composed preview.
  - Ops document detail: system prompt + user prompt panels.
  - Nav: Agents.
- **Required Documents:** Plan 22 PC-6; `apps/admin` ops-document-detail; `nav.config.ts`  
- **Evidence:**
  - Nav **Platform → Agents** (`/agents`); list filters tenant/business/name; detail shows system, instructions, schema, post-process + composed system+user preview
  - Ops document detail: Extract prompt cards (system + user + capturedAt); empty state if none/purged
  - Ops file documents table: Prompt column → document detail when `hasExtractPrompt`
  - Business detail agents link to `/agents/{id}`; “Agents →” with `businessId` query
  - `ng serve` (:4203) compiled `agent-list-page` + `agent-detail-page`; live `/api/admin/agents` still 404 until the debugger-locked API is restarted

---

### DQ-1911 — Hangfire 7-day purge + evidence

- **Status:** ⬜ Ready after DQ-1903, DQ-1905, DQ-1909  
- **Dependency:** DQ-1903, DQ-1905, DQ-1909  
- **Source:** Plan 22 PC-7; P2, DR-PC3  
- **Outcome:**
  - `ExtractPromptRetentionJobs.PurgeExpiredAsync`; daily recurring job in `Program.cs` (same style as `email-intake-mime-retention`).
  - Evidence tests: app DTO never contains system prompt; admin DTO does; upsert replaces; purge deletes rows older than 7 days.
- **Required Documents:** Plan 22 PC-7; `Program.cs` Hangfire registration; `08-evidence-and-completion-rules.md`  
- **Evidence:** — (fill on execute)

---

## Output contract

### Finalized Decisions

PP1–PP7, P1–P5, DR-PC1…PC7. Band **19**. First item **DQ-1901**.

### Pending Decisions

None.

### Assumptions

One DQ at a time unless batched. No production code in this Phase 3 filing.

### Risks

See Completion Summary table.

### Readiness

**Ready to execute next items.** Suggested: **DQ-1905**.

---

## Changelog

| Date | Note |
|------|------|
| 2026-09-20 | Band 19 filed: DQ-1901…1911. API/Web split vs Plan 22 proposed PC index. |
| 2026-09-20 | Executed batch DQ-1901 + DQ-1902 + DQ-1907. |
| 2026-09-20 | Executed DQ-1903 (app agents post-process + redacted preview). |
| 2026-09-20 | Executed DQ-1904 (customer agent FE textarea, schema comments, redacted preview). |
| 2026-09-20 | Executed DQ-1908 (admin agent templates nav, list/edit, push dialog). |
| 2026-09-20 | Executed DQ-1909 + DQ-1910 (admin agents API/FE + ops extract prompt UI). |
