# Documate v3 — Back Office Frontend MVP (Implementation Plan)

> **Status:** Phase 2 — **approved** (2026-09-14)  
> **Type:** Engineering implementation plan (`apps/admin` + `Modules/PlatformAdmin`)  
> **Upstream:** [11-backoffice-frontend-exploration.md](./11-backoffice-frontend-exploration.md) (Phase 1 complete 2026-09-14; DR-BO1…BO7 locked)  
> **Also aligns:** Plan 06 §8.1 INT inventory; Plan 15 system settings (INT-08); customer `apps/web` stack patterns  
> **Downstream:** [16-backoffice-frontend-dispatch-queue.md](./16-backoffice-frontend-dispatch-queue.md) (Phase 3 Band 17)  
> **Mockups:** [documate-backoffice-mvp.canvas.tsx](/C:/Users/fahee/.cursor/projects/d-Work-Documate-documate-v3/canvases/documate-backoffice-mvp.canvas.tsx)  
> **Created:** 2026-09-14  
> **Amended:** 2026-09-14 — Phase 2 approved; Band 17 DQ filed  

**Outcome:** Ship a **platform admin Angular app** (`apps/admin`) for support/dev ops: **INT-20** cross-tenant files/documents monitor (column picker + rich filters), **INT-01** dashboard (volume charts, hourly/date/month, business-wise KPIs, stage timings), tenants/businesses/support workspace, hybrid API monitoring, and limited system settings — backed by new **`/api/admin/*`** APIs under **`Modules/PlatformAdmin`**.

---

## Planning flow

| Phase | Document |
|-------|----------|
| 1 — Exploration | `11-backoffice-frontend-exploration.md` ✅ |
| 2 — Implementation plan | **This file** |
| 3 — Dispatch queue | After this plan is approved |

---

## Delivery Principles

1. **Separate app** — `apps/admin` (DR-BO1=A); customer `apps/web` unchanged.  
2. **Retool/Linear admin aesthetic** — dense tables, monospace IDs (DR-BO2=A); not Rossum customer chrome.  
3. **PrimeNG + AG Grid Community** — same stack as customer web; AG Grid for INT-20 (and dense lists).  
4. **PlatformAdmin module** — all admin HTTP under `/api/admin`; never call `/api/app` or `/api/v1` from admin UI for product data.  
5. **Read-only ops for files/docs** — no act-as-business / impersonation (DR-BO3=A); tenant create is the only write in Wave C.  
6. **Analytics UTC** — all hour/day/month buckets in UTC (DR-BO7=A).  
7. **Stage KPIs compute-on-read** from work events / timestamps (DR-BO6=A).  
8. **API monitoring hybrid** — health + Hangfire link in-app; Datadog deep links (DR-BO4=C); no `OpsApiRequestLog` in MVP.  
9. **DevBypass platform-admin** interim auth (DR-BO5=A); Band 15 replaces with Iden role.  
10. **No pipeline logic in the browser** — admin UI orchestrates PlatformAdmin only.  
11. **Standalone Angular + signals** — copy patterns from `apps/web`; no NgRx.  
12. **Defer** billing, models, users admin, impersonation, platform template CRUD.

---

## MVP screen cut

| ID | Screen | MVP? | Notes |
|----|--------|------|-------|
| INT-SHELL | Admin shell, auth gate, nav, theme | **Yes** | Port 4203 |
| INT-01 | Ops dashboard | **Yes** | Volume + hourly + business KPIs + stage timings |
| INT-20 | Files & documents monitor | **Yes** | Tabs; composite Business + Webhook columns; column picker |
| INT-11 | Support workspace | **Yes** | Lookup → snapshot → drill to INT-05 / INT-20 |
| INT-04 | Businesses list | **Yes** | Cross-tenant |
| INT-05 | Business detail | **Yes** | Queues, agents, recent files |
| INT-02 | Tenants list + **create** | **Yes** | `POST /api/admin/tenants` |
| INT-03 | Tenant detail | **Yes** | Businesses under tenant |
| INT-09 | API monitoring | **Yes** | Health + Hangfire + Datadog links (thin) |
| INT-08 | System settings | **Yes** | Prefer Plan 15 settings service |

Nav (locked exploration):

```text
Overview
  Dashboard              → INT-01
  Files & documents      → INT-20
  Support workspace      → INT-11
Customers
  Tenants                → INT-02
  Businesses             → INT-04
Platform
  API monitoring         → INT-09
  System settings        → INT-08
```

---

## Domain Architecture Layer

### Admin web (`apps/admin`)

```text
apps/admin/src/app/
  core/           # platform-admin auth, interceptor, API base, theme
  shared/         # shell layout, OrgCell, WebhookCell, page chrome
  features/
    dashboard/    # INT-01
    ops-monitor/  # INT-20
    support/      # INT-11
    tenants/      # INT-02, INT-03
    businesses/   # INT-04, INT-05
    monitoring/   # INT-09
    settings/     # INT-08
```

- Scaffold from Angular CLI adjacent to `apps/web` (Angular 21, PrimeNG, AG Grid CE).  
- Dev port **4203**; CORS origin added on API for CustomerWebDev (or AdminWebDev) policy.  
- Lazy feature routes; shell wraps authenticated area.  
- Column visibility for INT-20: `localStorage` key per user until prefs API exists.

### API (`Modules/PlatformAdmin`)

New module (amend `docs/architecture/patterns/folder-structure.md` in W0):

```text
Modules/PlatformAdmin/Features/
  Me/
  Tenants/
  Businesses/
  OpsMonitor/      # files + documents list
  Analytics/       # volume, by-business, hourly, stage timings, summary KPIs
  Support/
  Monitoring/      # health snapshot + external links config
  Settings/        # thin wrapper over Plan 15 system settings
```

| Concern | Rule |
|---------|------|
| Authz | `[Authorize(Policy = "PlatformAdmin")]` on all `/api/admin` controllers |
| Isolation | Cross-tenant **by design**; never reuse `IBusinessContext` as sole scope for list queries |
| CQRS | Controllers → MediatR only; DTOs never Domain entities |
| Indexes | Plan indexes for `OpsFile.CreatedAt`, `BusinessId`, status enums; document search filters in DQ evidence |

### Auth (interim)

| Piece | Behavior |
|-------|----------|
| DevBypass | Config flag `Auth:DevBypass:IsPlatformAdmin = true` (or separate scheme claim) |
| Policy | `PlatformAdmin` requires claim `documate_platform_admin` / role |
| CORS | Allow `http://localhost:4203` |
| Band 15 | Replace with Iden platform-admin role; keep same policy name |

### UI stack

| Layer | Choice |
|-------|--------|
| Component library | **PrimeNG** (dense admin tokens; light/dark) |
| Ops grids | **AG Grid Community** + column picker + server-side sort/page |
| Charts | **PrimeNG Chart** (Chart.js) — **IMP1-A** |
| Forms | Reactive forms + PrimeNG |

---

## Domain Validation Rules

### Inherited locks (Plan 11)

| ID | Choice |
|----|--------|
| DR-BO1 | Separate `apps/admin` |
| DR-BO2 | Retool/Linear admin look |
| DR-BO3 | Read-only cross-tenant (except tenant create) |
| DR-BO4 | Hybrid monitoring |
| DR-BO5 | DevBypass platform-admin |
| DR-BO6 | Stage KPIs compute-on-read |
| DR-BO7 | UTC buckets |

### Composite column display (locked Plan 11)

| Column | Layout |
|--------|--------|
| **Business** | Primary = business name; secondary = tenant name |
| **Webhook** | Primary = status pill; secondary one line = `{lastAt} · HTTP {code} · {n} attempts` |

### Decision Required — IMP1 · Chart library — **LOCKED (IMP1-A)**

| Option | Meaning |
|--------|---------|
| **IMP1-A** | **PrimeNG Chart** (Chart.js) |
| **IMP1-B** | Standalone Chart.js / ng2-charts |
| **IMP1-C** | ApexCharts |

**Choice:** **IMP1-A** (2026-09-14) — already on PrimeNG; enough for line/bar/donut MVP.

### Decision Required — IMP2 · Hangfire pending metric — **LOCKED (IMP2-A)**

| Option | Meaning |
|--------|---------|
| **IMP2-A** | Show **link** to Hangfire dashboard; pending count optional if Monitoring API easy |
| **IMP2-B** | Always scrape Hangfire Monitoring API for counts |

**Choice:** **IMP2-A** (2026-09-14).

### Decision Required — IMP3 · System settings backend — **LOCKED (IMP3-A)**

| Option | Meaning |
|--------|---------|
| **IMP3-A** | Wrap **Plan 15** system settings service for INT-08 |
| **IMP3-B** | Admin-only appsettings editor (file) — avoid |
| **IMP3-C** | Defer INT-08 until Plan 15 ships |

**Choice:** **IMP3-A** (2026-09-14) — if Plan 15 not yet live, INT-08 ships read-only placeholders / feature-flag stubs in same band after settings seed.

### Locked validation

- Tenant create: name required ≤256; IdenTenantId unique; optional initial business bootstraps default queue + workflow (reuse existing bootstraps).  
- Analytics date filters: interpret as **UTC** midnight boundaries.  
- Stage timings labeled **approximate** in UI when derived from work events.  
- INT-20: no cancel/reprocess/edit of customer data in MVP.

---

## Process Flows

### F1 — Ops monitor (INT-20)

1. Open **Files & documents**.  
2. Choose Files or Documents tab.  
3. Apply filters (tenant, business, status, stage, webhook, dates, …).  
4. Server-side page/sort; toggle columns via Columns ▾ (`localStorage`).  
5. Row → drawer/detail or navigate business (INT-05) / filter drill.

### F2 — Dashboard analytics (INT-01)

1. Set granularity (hour/day/month), range or specific date/month, optional business filter.  
2. Load summary KPIs + volume series + stage timing bars.  
3. Specific date → hourly files/documents chart + per-business table.  
4. Month → files-per-business bar + MoM table.  
5. Click business → INT-20 with business + date query params.

### F3 — Support path

1. Support workspace search (name / queue / file id).  
2. Snapshot card → Open business detail or open INT-20 filtered.

### F4 — Tenant create

1. Tenants → **+ Create tenant**.  
2. POST name + optional Iden id + provider mode + optional initial business.  
3. List refresh; open tenant detail.

### F5 — Platform ops

1. API monitoring → health payload + Hangfire URL + Datadog URL from config.  
2. System settings → get/put allowed keys via Plan 15.

---

## Instruction and Control Set

### Locked controls

- Theme toggle light/dark (admin tokens, not Rossum customer theme).  
- Auth: DevBypass platform-admin until Band 15.  
- No External `/api/v1` or customer `/api/app` from admin for domain data.  
- INT-20 column prefs client-local only (MVP).  
- Charts/KPIs respect global time control bar.

### Config keys (sketch)

```json
"Auth": {
  "DevBypass": {
    "IsPlatformAdmin": true
  }
},
"Admin": {
  "HangfireDashboardUrl": "/hangfire",
  "DatadogDashboardUrl": ""
}
```

---

## Permissions and Security

- All `/api/admin/*`: **PlatformAdmin** policy required.  
- Cross-tenant reads audited via normal request logging; no customer impersonation.  
- Tenant create is privileged write; validate uniqueness; do not invent Iden users.  
- Never return raw API key secrets or full ResultJson blobs in list endpoints (detail endpoints may return truncated/result presence flags).  
- Signed download URLs: only if support needs file preview later — **out of MVP** unless already cheap reuse.  
- CORS locked to admin origin(s).

---

## API work package (MVP)

| ID | Endpoint (sketch) | Screen |
|----|-------------------|--------|
| B1 | `GET /api/admin/me` | Shell |
| B2 | `GET /api/admin/tenants` | INT-02 |
| B3 | `POST /api/admin/tenants` | INT-02 create |
| B4 | `GET /api/admin/tenants/{id}` | INT-03 |
| B5 | `GET /api/admin/businesses` | INT-04 |
| B6 | `GET /api/admin/businesses/{businessId}` | INT-05 |
| B7 | `GET /api/admin/ops/files` | INT-20 Files tab |
| B8 | `GET /api/admin/ops/documents` | INT-20 Documents tab |
| B9 | `GET /api/admin/analytics/summary` | INT-01 KPI strip |
| B10 | `GET /api/admin/analytics/volume` | INT-01 charts (`granularity=hour\|day\|month`) |
| B11 | `GET /api/admin/analytics/by-business` | INT-01 business tables (`month=` or `date=`) |
| B12 | `GET /api/admin/analytics/hourly` | INT-01 hourly (`date=`) |
| B13 | `GET /api/admin/analytics/stage-timings` | INT-01 stage KPIs |
| B14 | `GET /api/admin/support/lookup` | INT-11 |
| B15 | `GET /api/admin/monitoring/snapshot` | INT-09 |
| B16 | `GET/PUT /api/admin/settings/...` | INT-08 via Plan 15 |

**List query conventions (B7/B8):** page, pageSize, sort, filters from Plan 11 §2.1; DTOs include composite fields for Business/Webhook display.

**Explicitly out of MVP:** act-as-business, billing, models, users CRUD, request-log table, cancel/reprocess from admin.

---

## Dispatch Index (preview — Phase 3 will number)

| Wave | Focus | Depends on |
|------|-------|------------|
| **W0** | `apps/admin` scaffold; PlatformAdmin module stub; PlatformAdmin auth policy; CORS 4203; amend folder-structure | — |
| **W1** | Ops monitor APIs B7/B8 + indexes | W0 |
| **W2** | INT-20 UI (AG Grid, filters, column picker, composite cells) | W0, W1 |
| **W3** | Analytics APIs B9–B13 (UTC, compute-on-read timings) | W0 |
| **W4** | INT-01 dashboard UI (charts + business/hour/month views) | W0, W3 |
| **W5** | Tenants/Businesses APIs B2–B6 + create B3 | W0 |
| **W6** | INT-02/03/04/05 UI + support lookup B14 + INT-11 | W0, W5 |
| **W7** | Monitoring B15 + INT-09; Settings B16 + INT-08 (Plan 15) | W0 |
| **W8** | Polish: empty states, drill-through query params, OpenAPI, docs | prior |

Exact DQ-IDs assigned in Phase 3 (suggested Band **17**).

---

## Wave Sections (summary)

### W0 — Foundation

- Create `apps/admin` (Angular 21) mirroring web tooling.  
- Register `Modules/PlatformAdmin`; policy + DevBypass claim.  
- Shell nav + theme + auth interceptor.  
- Amend architecture folder-structure for `admin` + PlatformAdmin.

### W1 — Ops list APIs

- Paged cross-tenant file/document queries with joins to tenant/business/queue/agent/webhook enums.  
- Filter surface per Plan 11; performance evidence (indexes / query plans).

### W2 — INT-20 UI

- Files | Documents tabs; filter bar; Columns ▾; OrgCell + WebhookCell.  
- Server-side pagination/sort.

### W3 — Analytics APIs

- Summary, volume, by-business, hourly, stage-timings — all UTC.  
- Stage timings approximate from `OpsWorkEvent` + entity timestamps; document formula in handler comments.

### W4 — INT-01 UI

- Time control bar; PrimeNG charts; KPI strip; business tables; hourly overlay; drill to INT-20.

### W5–W6 — Customers + support

- Tenant CRUD-lite (create + list + detail); businesses list/detail; support lookup.

### W7 — Platform

- Monitoring snapshot + config URLs.  
- Settings UI bound to Plan 15 keys (maintenance, email intake, LLM toggle, upload limits as available).

### W8 — Harden

- Error/empty states; deep-link query params; OpenAPI tag `admin`; brief ops README.

---

## Relationship to other plans

| Plan | Relationship |
|------|----------------|
| 06 / 11 | Screen inventory + mockups + DR-BO locks |
| 07 / Band 16 | Customer app — do not mix routes |
| 15 | System settings SoT for INT-08 |
| 03 DQ | Phase 3 adds Band 17; does not reopen Wave 8 customer scope |

---

## Agent output contract (Phase 2)

### Finalized Decisions

| # | Decision | Status |
|---|----------|--------|
| App | `apps/admin` port 4203 | **Locked** (BO1-A) |
| API | `Modules/PlatformAdmin` → `/api/admin` | **Locked** |
| UI kit | PrimeNG + AG Grid CE | **Locked** |
| Charts | PrimeNG Chart (**IMP1-A**) | **Locked** (2026-09-14) |
| Hangfire KPI | Link (+ optional count) (**IMP2-A**) | **Locked** (2026-09-14) |
| Settings | Plan 15 wrapper (**IMP3-A**) | **Locked** (2026-09-14) |
| Access | Read-only ops + tenant create | **Locked** (BO3-A) |
| Analytics TZ | UTC | **Locked** (BO7-A) |
| Stage KPIs | Compute on read | **Locked** (BO6-A) |
| Auth | DevBypass platform-admin | **Locked** (BO5-A) |
| Monitoring | Hybrid | **Locked** (BO4-C) |
| APIs | B1–B16 | **Locked** |
| Waves | W0–W8 | **Locked** |

### Pending Decisions

None — Phase 2 **approved** 2026-09-14. Execute [16-backoffice-frontend-dispatch-queue.md](./16-backoffice-frontend-dispatch-queue.md) (Band 17).

### Assumptions

- Plan 15 lands in time for INT-08; otherwise settings UI stubs until settings service exists.  
- Canvas mockups guide layout, not pixel QA.  
- Work-event coverage is sufficient for approximate stage timings.  
- Cross-tenant admin queries are acceptable for PlatformAdmin only.

### Risks

- INT-20 filter cardinality → slow SQL without indexes.  
- Stage timing accuracy gaps.  
- Plan 15 timing vs W7.  
- Dual-app deploy/CORS drift.

### Readiness

**Phase 2 complete.** Execute [16-backoffice-frontend-dispatch-queue.md](./16-backoffice-frontend-dispatch-queue.md).
