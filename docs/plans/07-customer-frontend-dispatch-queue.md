# Documate v3 — Customer Frontend MVP — Dispatch Queue

> **Document type:** Dispatch queue (Phase 3)  
> **Status:** ✅ **Band 16 complete** (2026-08-30)  
> **Source plan:** [07-customer-frontend-implementation-plan.md](./07-customer-frontend-implementation-plan.md) (Phase 2 **approved** 2026-08-30)  
> **Upstream:** [06-frontend-app-exploration.md](./06-frontend-app-exploration.md)  
> **Supersedes:** Plan 03 **DQ-1301**, **DQ-1302** (thin Wave 8 shell) — see `03-documate-v3-dispatch-queue.md`  
> **Scope:** Customer Angular app (Nav B, PrimeNG, AG Grid CE Files) + FrontendSupport file/document/summary/search APIs  
> **Out of scope:** Internal SaaS panel; tickets; usage/billing APIs; cancel/reprocess UI; IntakeRejection UI; monitor; customer workflow authoring  

**Status legend:** ✅ Complete · 🔄 In Progress · ⬜ Ready · ⏸ Parked · ❌ Cancelled  

**Band 16** = Customer frontend MVP (product plan 07 — not Plan 00 eng bands).

---

## Completion Summary

| Metric | Value |
|--------|--------|
| Total DQ items | 11 |
| ✅ Complete | 11 |
| 🔄 In Progress | 0 |
| ⬜ Ready | 0 |
| ⏸ Parked | 0 |
| ❌ Cancelled | 0 |

### Finalized decisions (from plan 07)

| ID | Decision |
|----|----------|
| Nav | **B** (Nanonets-style) |
| UI | **PrimeNG** + **AG Grid Community** (Files; theme to match PrimeNG) |
| Schema | Form builder **FB1** (custom lean) |
| Home KPIs | **H2** — `files/summary` API |
| Agent test | **T1** — default-queue upload + poll |
| Defer | Tickets, usage/billing APIs, cancel/reprocess UI |
| API | Build A1–A6 as needed on `/api/app` only |

### Pending decisions

None — Band 16 closed.

### Assumptions

- Auth bypass OK until Band 15 (Plan 03).  
- Agents / Queues / Catalogs / Me / ApiKeys / file upload+get+download already exist.  
- Default queue id from Me / bootstrap (**K1**).  
- Canvas mockups are guidance.

### Risks

| Risk | Mitigation |
|------|------------|
| Schema search on JSON | Start with bounded filters; document index follow-up if slow |
| AG Grid ≠ PrimeNG look | Dedicated theming pass in DQ-1601 + DQ-1604 |
| T1 needs route map | Test UX documents that agent must be mapped on default queue |

---

## Dispatch Index

| DQ | Band | Title | Status | Depends on |
|----|------|--------|--------|------------|
| DQ-1601 | 16 | Web shell: PrimeNG light/dark, Nav B, routes, AG Grid theme bridge | ✅ | — |
| DQ-1602 | 16 | FrontendSupport: file list + documents + file summary (A1–A3, A5) | ✅ | — |
| DQ-1603 | 16 | FrontendSupport: schema field search (A4) + business profile gap (A6 if needed) | ✅ | DQ-1602 |
| DQ-1604 | 16 | Files UI: AG Grid list + file detail + document detail | ✅ | DQ-1601, DQ-1602 |
| DQ-1605 | 16 | Schema search UI (CUS-16) | ✅ | DQ-1601, DQ-1603, DQ-1604 |
| DQ-1606 | 16 | Agents: list, templates, edit, test runner (T1) | ✅ | DQ-1601 |
| DQ-1607 | 16 | Schema form builder FB1 (CUS-10) | ✅ | DQ-1606 |
| DQ-1608 | 16 | Post-processing options UI (CUS-11) | ✅ | DQ-1606 |
| DQ-1609 | 16 | Channels & intake: overview, settings, routes (CUS-04…06) | ✅ | DQ-1601 |
| DQ-1610 | 16 | Business profile/switcher, API keys, docs, Home (H2) | ✅ | DQ-1601, DQ-1602 |
| DQ-1611 | 16 | OpenAPI client regen, polish, supersede notes / Postman | ✅ | DQ-1604…DQ-1610 |

**Suggested first execution:** `DQ-1601` (shell) and/or `DQ-1602` (APIs) — independent; prefer **DQ-1602** if API-first, or **DQ-1601** if UI-first. Developer chooses.

**Critical path (UI-first):** DQ-1601 → DQ-1602 → DQ-1604 → …  
**Critical path (API-first):** DQ-1602 → DQ-1603; parallel DQ-1601 → merge at DQ-1604.

---

## Wave / DQ Entries

### DQ-1601 — Web shell (PrimeNG + Nav B + AG Grid theme)

- **Status:** ✅ Complete  
- **Dependency:** —  
- **Source:** Plan 07 W0; Nav B; Q3  
- **Outcome:**  
  - PrimeNG 21 + Aura (`@primeuix/themes`); dark via `html.app-dark` + `ThemeService`.  
  - AG Grid Community + `documateAgTheme` bridge (`core/ag-grid-theme.ts`) mapped to PrimeNG CSS vars.  
  - App shell: top bar (light/dark toggle) + **Nav B** sidebar + lazy feature routes with stubs.  
  - Deferred usage / support / workflows → coming-soon routes.  
- **Required Documents:** Plan 07; `docs/architecture/patterns/angular-*.md`; Canvas Nav B  
- **Evidence:**  
  - Packages: `primeng@21`, `@primeuix/themes`, `primeicons`, `ag-grid-community`, `ag-grid-angular`, `@angular/animations@21.2.19`  
  - Shell: `shared/layout/app-shell.*`, `nav.config.ts` (Nav B)  
  - Theme: `core/theme.service.ts`; AG bridge: `core/ag-grid-theme.ts`  
  - Routes: `app.routes.ts` + feature `*.routes.ts` stubs  
  - `npm run build` ✅; `ng test --watch=false` ✅ (3 tests)  

### DQ-1602 — File list, documents, summary APIs

- **Status:** ✅ Complete  
- **Dependency:** —  
- **Source:** Plan 07 A1–A3, A5 (H2)  
- **Outcome:**  
  - `GET /api/app/queues/{queueId}/files` — paged list, status/date filters, business-scoped.  
  - `GET /api/app/queues/{queueId}/files/{fileId}/documents` — documents under file.  
  - `GET /api/app/queues/{queueId}/documents/{documentId}` — detail + ResultJson + status.  
  - `GET /api/app/queues/{queueId}/files/summary` — counts for Home (to review / ready / failed / etc.).  
  - Public status uses existing file rollup semantics.  
  - Unit/API tests + Postman updates.  
  - **Not in scope:** cancel, reprocess.  
- **Required Documents:** Plan 07 §API; `critical-rules-api.md`; FilesController patterns  
- **Evidence:**  
  - `FilesController.cs` — A1 list (paged), A2 documents list, A5 summary; extended `FileDto`  
  - `DocumentsController.cs` — A3 document detail  
  - `AppFileQueryHelpers.cs` + `AppFileQueryHelpersTests.cs`  
  - Postman folder **05 — App files**: list, summary, documents, document detail  
  - `dotnet build` ✅; `dotnet test` ✅ (28 tests)

### DQ-1603 — Schema search API + business profile gap

- **Status:** ✅ Complete  
- **Dependency:** DQ-1602  
- **Source:** Plan 07 A4, A6  
- **Outcome:**  
  - `GET /api/app/queues/{queueId}/files/search` (or equivalent) — filter by extracted field key/value; business-scoped.  
  - Document performance limits / follow-up index need in evidence.  
  - Business profile get/update on `/api/app` **if** Me is insufficient for CUS-02.  
- **Required Documents:** Plan 07 A4/A6  
- **Evidence:**  
  - A4: `GET .../files/search` — `fieldKey`, `value`, optional `matchMode` (`exact`|`contains`), paged  
  - SQL Server `JSON_VALUE` on `OpsDocuments.ResultJson`; top-level scalar fields only  
  - **Index follow-up:** persisted extracted-field projection or computed column + index for production scale  
  - `AppFileSchemaSearch.cs` + `AppFileSchemaSearchTests.cs` (fieldKey validation, match modes)  
  - A6: `GET/PUT /api/app/business/profile` — read/update `CorTenantBusiness.Name` (Me is read-only)  
  - Postman: **01b — Business profile**, **GET schema search files** in folder 05  
  - `dotnet build` ✅; `dotnet test` ✅ (41 tests)

### DQ-1604 — Files UI (AG Grid + detail)

- **Status:** ✅ Complete  
- **Dependency:** DQ-1601, DQ-1602  
- **Source:** Plan 07 W2; CUS-13…15  
- **Outcome:**  
  - Extract data page: AG Grid CE list themed like PrimeNG table; status pills; selection checkboxes (no cancel/reprocess actions).  
  - File detail: signed URL preview + metadata + documents list.  
  - Document detail: fields + line table from ResultJson.  
  - Wire upload entry where mockup expects it.  
- **Required Documents:** Plan 07 W2; Canvas Files/detail  
- **Evidence:**  
  - `features/files/pages/file-list.page.*` — AG Grid list, status filter, upload, row nav  
  - `features/files/pages/file-detail.page.*` — preview iframe/image, metadata, documents table  
  - `features/files/pages/document-detail.page.*` — scalar fields + dynamic line tables from ResultJson  
  - `features/files/data/files-api.service.ts`, `components/status-pill`, `utils/result-json.util`  
  - `core/app-context.service.ts`, `core/api-base.ts`  
  - `npm run build` ✅; `ng test --watch=false` ✅ (5 tests)

### DQ-1605 — Schema search UI

- **Status:** ✅ Complete  
- **Evidence:** `features/files/pages/schema-search.page.*`; Nav + Extract data link; `FilesApiService.searchFiles()`; `npm run build` ✅

### DQ-1606 — Agents list, templates, edit, test (T1)

- **Status:** ✅ Complete  
- **Evidence:** `features/agents/pages/*`; `agent-test-runner` (T1); `core/api/agents-api.service.ts`; `npm run build` ✅

### DQ-1607 — Schema form builder (FB1)

- **Status:** ✅ Complete  
- **Evidence:** `schema-form-builder.component.*`; `schema-builder.util.ts` + spec; Agent edit Schema tab

### DQ-1608 — Post-processing options UI

- **Status:** ✅ Complete  
- **Evidence:** Agent edit Post-process tab — workflow toggle + provider override; saves via Agents PUT

### DQ-1609 — Channels & intake UI

- **Status:** ✅ Complete  
- **Evidence:** `features/queues/pages/queue-overview.page.*`; webhook/email/allowlist/routes tabs; `queues-api.service.ts`

### DQ-1610 — Business, API keys, docs, Home

- **Status:** ✅ Complete  
- **Evidence:** `business-profile/switch`, `api-keys`, `docs`, `home` pages; shell team footer; H2 summary cards

### DQ-1611 — OpenAPI client, polish, queue hygiene

- **Status:** ✅ Complete  
- **Evidence:** `core/api/README.md`; typed wrappers; empty states; Postman 01b/05; `ng test` ✅ (6 tests); DQ-1301/1302 superseded

---

## Agent output contract (Phase 3)

### Finalized Decisions

Inherited from plan 07 (Nav B, PrimeNG+AG Grid, FB1, H2, T1, API package, deferrals). DQ-1301/1302 **superseded**.

### Pending Decisions

None for Band 16.

### Assumptions / Risks

See Completion Summary.

### Readiness

**Band 16 execution complete.** All 11 DQs delivered.
