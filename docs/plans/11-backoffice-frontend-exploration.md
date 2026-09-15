# Documate v3 — Back Office Frontend (Exploration)

> **Status:** Exploration — Phase 1 **complete** (2026-09-14) — DR-BO1…BO7 locked to recommendations  
> **Type:** Product + engineering exploration for **Internal SaaS control panel** (`apps/admin`)  
> **Audience:** Documate platform support, ops, and dev teams  
> **Upstream:** [06-frontend-app-exploration.md](./06-frontend-app-exploration.md) §8.1 (INT-xx inventory); [01-project-exploration-mental-design.md](./01-project-exploration-mental-design.md)  
> **Downstream:** [16-backoffice-frontend-implementation-plan.md](./16-backoffice-frontend-implementation-plan.md) (Phase 2) → Phase 3 dispatch queue  
> **Mockups:** [documate-backoffice-mvp.canvas.tsx](/C:/Users/fahee/.cursor/projects/d-Work-Documate-documate-v3/canvases/documate-backoffice-mvp.canvas.tsx)  
> **Created:** 2026-09-02  
> **Amended:** 2026-09-14 — all DR-BO recommendations accepted  

**Goal:** Define the **first slice** of the back office frontend — pages support and dev need to monitor the platform — produce **mockups**, then (Phase 2+) implementation plan and dispatch queue. **No production code in Phase 1.**

---

## Planning flow

| Phase | Document |
|-------|----------|
| 1 — Exploration | **This file** + Canvas mockups |
| 2 — Implementation plan | After Phase 1 verified |
| 3 — Dispatch queue | After Phase 2 approved |
| Execution | One DQ at a time per `00-governance/06-dispatch-queue-execution.md` |

---

## 1. Problem Framing

### 1.1 Why back office now

The **customer app** (`apps/web`, Band 16) is largely shipped. Support and dev teams still lack a dedicated surface to:

- See platform health at a glance (errors, queue backlog, provider failures)
- **Watch all files and documents cross-tenant** with status, webhook delivery, upload timestamps, tenant/business context, rich filters, and configurable columns
- **Dashboard charts** for file/document volume and **step timing KPIs** (avg intake, OCR, extract, webhook, etc.)
- Find a tenant or business quickly when a customer reports an issue
- Inspect a business’s recent files, agents, and queues **without** logging into the customer app
- Monitor API traffic and failure rates on `/api/v1` and `/api/app`
- Toggle safe operational flags (maintenance mode, feature flags)

Plan 06 already inventoried **18 internal screens** (`INT-01`…`INT-18`) but **deferred mockups and build** (P4-B′: customer only). This plan starts the **basic internal panel** promised in P1-D.

### 1.2 Audience and jobs-to-be-done

| Persona | Primary jobs |
|---------|----------------|
| **Support** | “Customer X says uploads fail” → filter files by tenant/business/status → inspect webhook + stage timing |
| **Ops / SRE** | Platform KPIs, file/doc volume charts, avg stage durations, alert triage |
| **Dev / on-call** | Drill into a business’s pipeline stage, reprocess guidance, config sanity checks |
| **Platform admin** (later) | Tenant suspend, billing, model catalog — **not MVP** |

### 1.3 Boundaries

| In scope (MVP) | Out of scope (later bands) |
|----------------|----------------------------|
| Read-only cross-tenant visibility | Billing, invoices, usage charges (INT-12/13) |
| **Files + documents ops monitor (INT-20)** | User CRUD — Iden is SoT (INT-06/07 link-out only) |
| **Dashboard volume charts + step KPIs (INT-01)** | Audited “act as customer” impersonation |
| Tenant + Business search/list/detail | Full APM replacement (Datadog remains primary) |
| Support workspace (client lookup) | Models catalog + tenant quotas (INT-14/15) |
| API monitoring (aggregates) | Full defaults catalog CRUD (INT-16/17/18) |
| System settings (safe toggles) | |

---

## 2. Scope — MVP page cut (build first)

Full inventory remains in [06-frontend-app-exploration.md](./06-frontend-app-exploration.md) §8.1. **Recommended Phase 1 build order:**

### Wave A — Shell + navigation (required first)

| ID | Screen | Why first |
|----|--------|-----------|
| **INT-SHELL** | App shell, auth gate, sidebar, theme | Foundation for all pages |

### Wave B — Operations monitor (highest value — **developer priority 2026-09-02**)

| ID | Screen | Primary actions | MVP? |
|----|--------|-----------------|------|
| **INT-01** | Platform home / ops dashboard | **File + document volume charts**; **step timing KPIs**; alerts | **Yes** |
| **INT-20** | **Files & documents monitor** | Cross-tenant grid; status + webhook; column picker; rich filters | **Yes** |
| **INT-11** | Support workspace | Search tenant/business/email → snapshot | **Yes** |
| **INT-04** | Businesses list (cross-tenant) | Filter, sort, open business | **Yes** |
| **INT-05** | Business detail (support view) | Queues, agents, recent files, error summary | **Yes** |

### Wave C — Tenant admin (second priority)

| ID | Screen | Primary actions | MVP? |
|----|--------|-----------------|------|
| **INT-02** | Tenants list | Search, filter status; **+ Create tenant** | **Yes** |
| **INT-03** | Tenant detail | Businesses under tenant, flags, usage summary | **Yes** |

### Wave D — Platform ops (third priority)

| ID | Screen | Primary actions | MVP? |
|----|--------|-----------------|------|
| **INT-09** | API monitoring | Traffic, 4xx/5xx, latency, top routes | **Yes** |
| **INT-08** | System settings | Feature flags, maintenance banner, global limits | **Yes** (limited) |

### Explicitly deferred (Phase 2+ product bands)

| ID | Screen | Reason |
|----|--------|--------|
| INT-06 / INT-07 | Users | Iden SoT — link to Iden admin, no parallel user DB |
| INT-10 | Tenant visualization | Overlaps INT-01 + INT-03 charts |
| INT-12 / INT-13 | Billing | No billing domain yet |
| INT-14 / INT-15 | Models + quotas | Provider catalog product TBD |
| INT-16 / INT-17 / INT-18 | Platform templates / defaults | Existing seed APIs; admin UX later |

**MVP mockup set:** INT-SHELL + **9 screens** (INT-01, **INT-20**, 02, 03, 04, 05, 08, 09, 11) — see Canvas.

---

## 2.1 INT-20 — Files & documents monitor (detail)

Single page with **Files | Documents** tabs. **AG Grid Enterprise-style column picker** (Community: custom column-visibility dropdown persisted per user in `localStorage` until admin prefs API exists).

### UX rules

1. **Default columns** — smallest set support needs daily (tenant, business, name, status, upload time, webhook on docs).  
2. **Extended columns** — all domain fields available; **hidden by default**; user enables via **Columns ▾** multi-select dropdown.  
3. **Filters** — filter bar above grid + saved filter presets (later); all filters combinable (AND).  
4. **Sort + paginate** — server-side; default sort = upload datetime desc.  
5. **Row actions** — open file detail drawer, open business (INT-05), copy IDs.  
6. **Export** — CSV of visible columns (Phase 2+ nice-to-have).

### Composite column display (locked 2026-09-02)

| Composite column | Layout |
|------------------|--------|
| **Business** | **Single column.** Primary line = business name (semibold). Secondary line = tenant name (muted/subtitle). Sort/filter still available on both fields independently. |
| **Webhook** (documents) | **Single column.** Primary = status pill (`webhook_delivery_status`). Secondary line (one line, muted) = `{lastAt} · HTTP {code} · {n} attempt(s)`. Full `WebhookLastError` remains in hidden column picker. |

### Files tab — default visible columns

| Column | Source |
|--------|--------|
| Upload datetime | `OpsFile.CreatedAt` |
| **Business** | Composite: `CorTenantBusiness.Name` + `CorTenant.Name` (subline) |
| File name | `OpsFile.OriginalFileName` |
| Public status | `file_public_status` enum key |
| Internal stage | `file_internal_stage` enum key |
| Documents | `Documents.Count` |
| Queue | `OpsQueue.Name` |
| Source | `intake_source` enum key |
| Error | `OpsFile.ErrorMessage` (truncated) |

### Files tab — hidden columns (column picker)

| Column | Source |
|--------|--------|
| File ID | `OpsFile.Id` |
| Sequence ID | `OpsFile.SequenceId` |
| Business ID | `OpsFile.BusinessId` |
| Tenant ID | `CorTenant.IdenTenantId` |
| Queue ID | `OpsFile.QueueId` |
| Batch ID | `OpsFile.BatchId` |
| Content type | `OpsFile.ContentType` |
| Size (bytes) | `OpsFile.SizeBytes` |
| Storage key | `OpsFile.StorageKey` |
| Storage bucket | `OpsFile.StorageBucket` |
| Content hash | `OpsFile.ContentHash` |
| Email message ID | `OpsFile.EmailMessageId` |
| Email from | `OpsFile.EmailFrom` |
| Email subject | `OpsFile.EmailSubject` |
| Intake hints | `OpsFile.IntakeHintsJson` |
| Reprocess of file ID | `OpsFile.ReprocessOfFileId` |
| Error code | `OpsFile.ErrorCode` |
| Cancelled at / by | `OpsFile.CancelledAt`, `CancelledByUserId` |
| Completed at | `OpsFile.CompletedAt` |
| Updated at | `OpsFile.UpdatedAt` |
| Created by | `OpsFile.CreatedByUserId` |
| Duration (total) | `CompletedAt − CreatedAt` (computed) |

### Documents tab — default visible columns

| Column | Source |
|--------|--------|
| Upload datetime | `OpsFile.CreatedAt` (parent file) |
| **Business** | Composite: business name + tenant subline |
| File name | parent `OpsFile.OriginalFileName` |
| Document type | `CorDocumentType.Key` |
| Agent | `OpsAgent.Name` |
| Public status | `document_public_status` |
| Internal stage | `document_internal_stage` |
| **Webhook** | Composite: status pill + subline `lastAt · HTTP · attempts` |
| Error | `OpsDocument.ErrorMessage` (truncated) |

### Documents tab — hidden columns (column picker)

| Column | Source |
|--------|--------|
| Document ID / Sequence ID | `OpsDocument.Id`, `SequenceId` |
| File ID | `OpsDocument.FileId` |
| Queue / Batch ID | `OpsDocument.QueueId`, `BatchId` |
| Page range | `PageStart`–`PageEnd` |
| Provider | `CorProvider.Key` |
| Schema version | `OpsDocument.SchemaVersion` |
| Failed stage | `OpsDocument.FailedStage` |
| Retry count / next retry | `RetryCount`, `NextRetryAt` |
| Webhook last error | `WebhookLastError` (full) |
| Cancelled at / by | `CancelledAt`, `CancelledByUserId` |
| Completed at | `CompletedAt` |
| Updated at | `UpdatedAt` |
| Has result JSON | boolean (link to detail, not inline blob) |

### Filters (both tabs — combinable)

| Filter | Applies to |
|--------|------------|
| Upload date range | Files, Documents |
| Updated date range | Files, Documents |
| Completed date range | Files, Documents |
| Tenant (multi-select) | Both |
| Business (multi-select) | Both |
| Queue (multi-select) | Both |
| Public status (multi) | Both |
| Internal stage (multi) | Both |
| Source (api / email / api_sync) | Files |
| Has error (yes/no) | Both |
| Error code (text) | Both |
| File name contains | Both |
| Document type (multi) | Documents |
| Agent (multi) | Documents |
| **Webhook status (multi)** | Documents |
| Webhook HTTP status | Documents |
| Webhook attempts ≥ N | Documents |
| Batch ID | Both |
| Document count range | Files |
| File size range | Files |
| Reprocess lineage (is reprocess / has reprocess child) | Files |
| Cancelled only | Both |
| Email from contains | Files |
| Provider | Documents |

---

## 2.3 INT-02 — Tenants list: create tenant

**Primary action:** **+ Create tenant** button (top-right of list page). Toggles inline create panel (mockup) or dialog (implementation).

| Field | Required | Notes |
|-------|----------|-------|
| Tenant name | Yes | Display name; maps to `CorTenant.Name` |
| Iden tenant ID | No | Auto-generate UUID if blank; maps to `CorTenant.IdenTenantId` |
| Provider mode | Yes | Default Mode 1; maps to `ProviderModeEnumId` |
| Initial business name | No | If set, creates first `CorTenantBusiness` + default queue/workflow bootstrap |

**API (Phase 2 sketch):** `POST /api/admin/tenants`

**Iden boundary:** MVP creates Documate mirror rows only. Band 15 may add Iden org provisioning hook; until then platform-admin can onboard tenants for DevBypass / interim ops.

---

## 2.2 INT-01 — Dashboard charts & step KPIs (detail)

### Time controls (global dashboard filter)

All volume charts and business KPIs share one **time control bar**:

| Control | Purpose |
|---------|---------|
| **Granularity** | **Hour** · **Day** · **Month** — switches chart bucket size |
| **Range preset** | Last 24h · 7d · 30d · 12 months |
| **Specific date** | Date picker — drill into **one calendar day** (hourly chart + that day's KPIs) |
| **Month picker** | When granularity = Month — select month for business-wise table |
| **Business filter** | All businesses · multi-select · single business — scopes business KPIs + hourly chart |

**Behaviour:**
- **Hour granularity** → x-axis = 00–23 (or last 24 hourly buckets); used for specific-date drill-down and “today/yesterday” views.
- **Day granularity** → x-axis = days in selected range (default 7d/30d views).
- **Month granularity** → x-axis = months (rolling 12m); drives **files per business per month** table/chart.

### Volume charts (main dashboard)

| Chart | Granularity | Series |
|-------|-------------|--------|
| **Files over time** | **Hour / day / month** toggle | Total uploads; optional split by public status |
| **Documents over time** | Same | Total documents created; optional split by status |
| **Hourly files (selected date)** | **Hour only** | Files uploaded per hour for picked date; shown when specific date set |
| **Hourly documents (selected date)** | **Hour only** | Documents per hour for picked date |
| Files by status (snapshot) | Current window | Stacked bar or donut for selected date range |
| Documents by status | Current window | Same |
| **Files per business (monthly)** | **Month** | Bar chart or ranked table — file count by business for selected month |
| **Files per business (specific date)** | **Day** | KPI table — each business’s file + document count on selected date |
| Top tenants by volume | Bar | File count in selected window |
| Failure rate trend | Line | Failed ÷ total files |

Date range selector: **Last 24h | 7d | 30d | 12m | specific date | specific month** — drives all charts + KPIs.

### Business-wise volume KPIs

**Monthly view** (granularity = Month, or dedicated “Business volume” panel):

| KPI / widget | Definition |
|--------------|------------|
| Files per business (this month) | `COUNT(OpsFile)` grouped by `BusinessId` + `CorTenantBusiness.Name`, bucketed by calendar month |
| Documents per business (this month) | Same for `OpsDocument` |
| Month-over-month delta | Compare selected month vs prior month per business (% change) |
| Top 10 businesses | Ranked bar chart by file count in selected month |
| Business breakdown table | Columns: **Business** (name + tenant subline), Files, Documents, Failed %, Avg duration — **sortable** |

**Specific date view** (date picker set):

| KPI / widget | Definition |
|--------------|------------|
| Files on {date} (total) | Platform-wide count for UTC/local day boundary (configurable TZ — **DR-BO7**) |
| Files on {date} per business | Table: **Business** (name + tenant subline) · Files · Documents · Failed · Peak hour |
| Hourly distribution | **Hour-wise line/bar chart** for selected date (files + documents overlay) |
| vs same day last week | Optional comparison series on hourly chart |
| Drill-through | Click business row → INT-20 filtered to that business + date |

**API shape (Phase 2 sketch):**
- `GET /api/admin/analytics/files/volume?granularity=hour|day|month&from=&to=&businessIds=`
- `GET /api/admin/analytics/files/by-business?month=2026-09` or `?date=2026-09-01`
- `GET /api/admin/analytics/files/hourly?date=2026-09-01&businessIds=`

### KPI strip (headline metrics)

| KPI | Definition |
|-----|------------|
| Files (window) | Count of `OpsFile` created in range |
| Documents (window) | Count of `OpsDocument` created in range |
| Failed files % | Failed status ÷ total files |
| Failed documents % | Failed status ÷ total documents |
| Webhook success % | Succeeded webhook status ÷ configured webhooks |
| Avg end-to-end (file) | Mean `CompletedAt − CreatedAt` for completed files |
| Avg end-to-end (document) | Mean `CompletedAt − CreatedAt` for completed documents |
| Hangfire pending | Job queue depth (link to Hangfire) |

### Step timing KPIs (per pipeline stage)

Computed from **`OpsWorkEvent`** timestamps + stage transition events (and/or `UpdatedAt` when internal stage changes). Display as a **horizontal bar chart** or KPI grid:

**File stages** (`file_internal_stage`):

| KPI label | Stage transition |
|-----------|------------------|
| Avg intake | `received` duration |
| Avg normalize / OCR | `received` → `normalize` |
| Avg split | `normalize` → `split` |
| Avg classify | `split` → `classify` |
| Avg route | `classify` → `route` |
| Avg extract (file-level) | `route` → `extract` |
| Avg complete (file) | `extract` → `complete` |

**Document stages** (`document_internal_stage`):

| KPI label | Stage transition |
|-----------|------------------|
| Avg extract | `received` → `extract` |
| Avg validate | `extract` → `validate` |
| Avg post-process | `validate` → `post_process` |
| Avg deliver / webhook | `post_process` → `deliver` |
| Avg complete (doc) | `deliver` → `complete` |

**Engineering note:** Today `OpsWorkEvent` logs discrete events but **does not store precomputed stage durations**. Phase 2 must add either:
- **BO6-A:** Admin analytics SQL over work events + stage snapshots (compute on read), or  
- **BO6-B:** Materialized `OpsStageTiming` rollup table updated by pipeline (faster dashboards).

**Recommendation:** BO6-A for MVP admin; BO6-B if dashboard load exceeds ~2s on production data.

---

## 3. Current-State Findings

| Area | Today |
|------|--------|
| `apps/admin` | **Does not exist** |
| Internal / platform APIs | **None** — no `/api/admin` module |
| Customer APIs | `/api/app/*` — business-scoped; not suitable for cross-tenant reads |
| Auth roles | DevBypass only; no `platform-admin` vs `customer-operator` split |
| Observability | `/health`, Hangfire dashboard (dev); no aggregated API metrics in product DB |
| Plan 06 | INT inventory complete; internal mockups explicitly skipped until now |
| Customer web | `apps/web` Band 16 complete — patterns (PrimeNG, Nav shell) reusable |

### Data sources MVP can use (existing)

| Screen need | Likely source |
|-------------|---------------|
| Tenants / Businesses | `CorTenant`, `CorTenantBusiness` |
| Recent files / status | `OpsFile`, `OpsDocument`, `WorkEvent` |
| Webhook columns | `OpsDocument.Webhook*` + `webhook_delivery_status` enum |
| Tenant/Business on grid | Join `CorTenantBusiness` → `CorTenant` |
| Step timing KPIs | **`OpsWorkEvent.CreatedAt`** — compute stage deltas (new admin analytics query) |
| Agents / Queues | `OpsAgent`, `OpsQueue` |
| API keys (read-only) | `CorTenantApiKey` |
| Health | `/health`, Hangfire, optional `WorkEvent` aggregates |
| API monitoring | **New** — request log table or external metrics bridge (Decision Required) |

---

## 4. Risks and Constraints

| Risk | Mitigation |
|------|------------|
| **No admin APIs** | Phase 2 plan must size new `Modules/PlatformAdmin` (or `Internal`) module + `/api/admin` |
| **Cross-tenant data leak** | Strict platform-admin authz; integration tests per tenant boundary |
| **Support impersonation** | MVP = **read-only** support view; no customer-session impersonation |
| **Iden boundary** | Tenant/User identity stays in Iden; Documate shows projections + deep links |
| **Monitoring without APM** | MVP may use DB aggregates + Hangfire; full charts may need log pipeline (DR-BO4) |
| **Scope creep** | Lock MVP to 9 screens; INT-20 column catalog is large — ship default + picker first |
| **Stage KPI accuracy** | Work events may not capture every sub-step; document in UI as “approximate” until BO6-B |
| **Two-app deploy** | `apps/admin` separate port/build from `apps/web` (P1-D) |

---

## 5. Open Questions — Decision Required

### DR-BO1 — App packaging

| Option | Meaning |
|--------|---------|
| **BO1-A** | **Separate app** `apps/admin` (recommended — matches P1-D) |
| **BO1-B** | Route prefix inside `apps/web` (`/admin/*`) — shared deploy, risk of auth bleed |

**Choice:** **BO1-A** (2026-09-14) — separate Angular app, separate dev port (e.g. 4203), shared PrimeNG/theme tokens.

### DR-BO2 — Visual direction

| Option | Meaning |
|--------|---------|
| **BO2-A** | **Retool / Linear admin** — dense tables, monospace IDs, dark-friendly (distinct from customer Rossum) |
| **BO2-B** | **Stripe Dashboard admin** — calm, spacious, finance-trust aesthetic |
| **BO2-C** | **Match customer Rossum** — same brand; less distinction between surfaces |

**Choice:** **BO2-A** (2026-09-14) — internal tool clarity; customer app keeps Rossum identity.

### DR-BO3 — Support access model (MVP)

| Option | Meaning |
|--------|---------|
| **BO3-A** | **Read-only** cross-tenant view (recommended MVP) |
| **BO3-B** | **Act-as-business** — audited token exchange; higher security scope |

**Choice:** **BO3-A** (2026-09-14) for MVP; BO3-B as Band 15+ follow-on with audit log.

### DR-BO4 — API monitoring data source

| Option | Meaning |
|--------|---------|
| **BO4-A** | **New `OpsApiRequestLog`** table + middleware (product-native, queryable from admin UI) |
| **BO4-B** | **External only** — embed/link Datadog/Sentry; no in-app charts MVP |
| **BO4-C** | **Hybrid** — health + Hangfire in-app; deep links to Datadog for traces |

**Choice:** **BO4-C** (2026-09-14) for MVP; BO4-A later if product wants standalone monitoring.

### DR-BO5 — Auth (Phase 1 interim)

| Option | Meaning |
|--------|---------|
| **BO5-A** | **DevBypass platform-admin** flag in config (mirrors customer Phase 1) |
| **BO5-B** | **Wait for Band 15** Iden platform roles before any admin UI |

**Choice:** **BO5-A** (2026-09-14) — unblock support/dev; replace with Iden platform role in Band 15.

### DR-BO6 — Step timing KPI computation

| Option | Meaning |
|--------|---------|
| **BO6-A** | **Compute on read** — admin analytics SQL over `OpsWorkEvent` + entity timestamps |
| **BO6-B** | **Rollup table** — `OpsStageTiming` maintained by pipeline (faster, more work upfront) |

**Choice:** **BO6-A** (2026-09-14) for MVP; migrate to BO6-B if query latency unacceptable.

### DR-BO7 — Dashboard date bucket timezone

| Option | Meaning |
|--------|---------|
| **BO7-A** | **UTC** for all buckets (simplest; consistent across tenants) |
| **BO7-B** | **Operator-local** timezone (browser TZ) |
| **BO7-C** | **Per-tenant** timezone when business filter applied |

**Choice:** **BO7-A** (2026-09-14) for MVP; BO7-B as UI toggle later.

---

## 6. Recommended Direction

1. **New app:** `apps/admin` — Angular 21, PrimeNG, AG Grid for dense lists (same stack as customer).  
2. **New API module:** `Modules/PlatformAdmin` → `/api/admin/*` with `[Authorize(Policy = "PlatformAdmin")]`.  
3. **Build order:** INT-SHELL → **INT-20 + INT-01 (ops monitor + dashboard)** → INT-11 → INT-04/05 → INT-02/03 → INT-09 + INT-08.  
4. **Mockups:** Interactive Canvas for 9 MVP screens (see link in header).  
5. **Reuse:** Auth interceptor pattern, theme bridge, page chrome from `apps/web` — do not fork unnecessarily.  
6. **Defer:** Billing, models, users admin, impersonation, platform template CRUD.

### Proposed nav (admin)

```text
Overview
  Dashboard              → INT-01  (volume charts + step KPIs)
  Files & documents      → INT-20  (cross-tenant monitor)
  Support workspace      → INT-11
Customers
  Tenants                → INT-02
  Businesses             → INT-04
Platform
  API monitoring         → INT-09
  System settings        → INT-08
```

---

## 7. Exploration Exit Criteria

- [x] Developer confirms MVP page cut (§2)  
- [x] Canvas mockups reviewed (9 screens + shell)  
- [x] DR-BO1 … DR-BO7 answered (all recommendations, 2026-09-14)  
- [x] Agreement that billing/models/users admin stay deferred  
- [x] Ready for Phase 2 implementation plan  

---

## 8. Mockup index

| ID | Canvas screen | File |
|----|---------------|------|
| INT-SHELL | Admin app shell + nav | `canvases/documate-backoffice-mvp.canvas.tsx` |
| INT-01 | Ops dashboard (volume charts + step KPIs) | same |
| INT-20 | Files & documents monitor | same |
| INT-11 | Support workspace | same |
| INT-04 | Businesses list | same |
| INT-05 | Business detail | same |
| INT-02 | Tenants list | same |
| INT-03 | Tenant detail | same |
| INT-09 | API monitoring | same |
| INT-08 | System settings | same |

---

## Agent output contract (Phase 1)

### Finalized Decisions

| # | Decision | Status |
|---|----------|--------|
| Purpose | Back office for support/dev monitoring | **Locked** |
| MVP scope | 9 screens + shell; **INT-20 ops monitor prioritized** | **Locked** |
| INT-20 | AG Grid + column picker + rich filters; composite Business + Webhook columns | **Locked** |
| INT-01 | Volume charts + step KPIs + **business/month/date/hour analytics** | **Locked** |
| Build order | INT-SHELL → INT-20 + INT-01 → INT-11 → INT-04/05 → INT-02/03 → INT-09 + INT-08 | **Locked** |
| Customer app | Unchanged — admin is separate surface | **Locked** |
| DR-BO1 | **BO1-A** — separate `apps/admin` | **Locked** (2026-09-14) |
| DR-BO2 | **BO2-A** — Retool/Linear admin aesthetic | **Locked** (2026-09-14) |
| DR-BO3 | **BO3-A** — read-only cross-tenant (no act-as) | **Locked** (2026-09-14) |
| DR-BO4 | **BO4-C** — hybrid health/Hangfire + Datadog links | **Locked** (2026-09-14) |
| DR-BO5 | **BO5-A** — DevBypass platform-admin interim | **Locked** (2026-09-14) |
| DR-BO6 | **BO6-A** — compute stage KPIs on read | **Locked** (2026-09-14) |
| DR-BO7 | **BO7-A** — UTC date buckets | **Locked** (2026-09-14) |
| Defer | Billing, models, users admin, impersonation | **Locked** |

### Pending Decisions

None — Phase 1 complete.

### Assumptions

- Cross-tenant queries are acceptable for platform-admin role only.  
- MVP admin UI is **English-only**; i18n deferred.  
- Hangfire dashboard remains available to dev; admin UI surfaces summary links.  

### Risks

- Underestimating `/api/admin` endpoint count delays all screens.  
- INT-09 stays thin until external APM deep links are wired (BO4-C by design).  
- Stage KPI accuracy depends on work-event coverage (approximate until BO6-B).  

### Readiness

**Ready for Phase 2** — implementation plan: [16-backoffice-frontend-implementation-plan.md](./16-backoffice-frontend-implementation-plan.md).
