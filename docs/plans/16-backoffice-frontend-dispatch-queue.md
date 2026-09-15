# Documate v3 — Back Office Frontend MVP — Dispatch Queue

> **Document type:** Dispatch queue (Phase 3)  
> **Status:** ✅ **Band 17 complete** (2026-09-15)  
> **Source plan:** [16-backoffice-frontend-implementation-plan.md](./16-backoffice-frontend-implementation-plan.md) (Phase 2 **approved** 2026-09-14)  
> **Upstream:** [11-backoffice-frontend-exploration.md](./11-backoffice-frontend-exploration.md)  
> **Also uses:** [15-system-settings-db-implementation-plan.md](./15-system-settings-db-implementation-plan.md) for INT-08  
> **Scope:** `apps/admin` + `Modules/PlatformAdmin` (`/api/admin`) — ops monitor, dashboard analytics, tenants/businesses, support, hybrid monitoring, settings  
> **Out of scope:** Act-as-business; billing; models; users CRUD; `OpsApiRequestLog`; cancel/reprocess from admin; customer `apps/web` changes  

**Status legend:** ✅ Complete · 🔄 In Progress · ⬜ Ready · ⏸ Parked · ❌ Cancelled  

**Band 17** = Back office / platform admin MVP (product plan 16).

---

## Completion Summary

| Metric | Value |
|--------|--------|
| Total DQ items | 9 |
| ✅ Complete | 9 |
| 🔄 In Progress | 0 |
| ⬜ Ready | 0 |
| ⏸ Parked | 0 |
| ❌ Cancelled | 0 |

### Finalized decisions (from plans 11 + 16)

| ID | Decision |
|----|----------|
| App | Separate **`apps/admin`** (port **4203**) |
| API | **`Modules/PlatformAdmin`** → `/api/admin` |
| UI | **PrimeNG** + **AG Grid Community**; Retool/Linear admin look |
| Charts | **PrimeNG Chart** (IMP1-A) |
| Access | Read-only ops + **tenant create** only (BO3-A) |
| Analytics TZ | **UTC** (BO7-A) |
| Stage KPIs | Compute on read (BO6-A) |
| Auth | DevBypass **platform-admin** (BO5-A) |
| Monitoring | Hybrid health/Hangfire + Datadog links (BO4-C) |
| Hangfire KPI | Link (+ optional count) (IMP2-A) |
| Settings | Plan 15 wrapper (IMP3-A) |
| Composite columns | Business = name + tenant subline; Webhook = status + one-line meta |

### Pending decisions

None — Band 17 executable.

### Assumptions

- Canvas mockups are guidance.  
- Plan 15 settings service available or stubbed for INT-08.  
- Work events sufficient for approximate stage timings.  
- Cross-tenant queries OK for PlatformAdmin only.

### Risks

| Risk | Mitigation |
|------|------------|
| INT-20 slow filters | Indexes + evidence in DQ-1702 |
| Stage timing accuracy | Label “approximate”; BO6-B later |
| Plan 15 timing | DQ-1708 stubs if settings not live |
| Dual-app CORS | Explicit 4203 origin in DQ-1701 |

---

## Dispatch Index

| DQ | Band | Title | Status | Depends on |
|----|------|--------|--------|------------|
| DQ-1701 | 17 | Foundation: `apps/admin` shell, PlatformAdmin module, PlatformAdmin auth, CORS 4203 | ✅ | — |
| DQ-1702 | 17 | Ops monitor APIs: cross-tenant files + documents list (B7, B8) + indexes | ✅ | DQ-1701 |
| DQ-1703 | 17 | INT-20 UI: AG Grid, filters, column picker, OrgCell + WebhookCell | ✅ | DQ-1701, DQ-1702 |
| DQ-1704 | 17 | Analytics APIs: summary, volume, by-business, hourly, stage-timings (B9–B13) | ✅ | DQ-1701 |
| DQ-1705 | 17 | INT-01 dashboard UI: charts, KPIs, business/hour/month views, drill-through | ✅ | DQ-1701, DQ-1704 |
| DQ-1706 | 17 | Tenants + Businesses APIs (B2–B6) including create tenant (B3) | ✅ | DQ-1701 |
| DQ-1707 | 17 | INT-02/03/04/05 UI + support lookup API (B14) + INT-11 | ✅ | DQ-1701, DQ-1706 |
| DQ-1708 | 17 | Monitoring snapshot (B15) + INT-09; Settings (B16) + INT-08 | ✅ | DQ-1701 |
| DQ-1709 | 17 | Polish: empty/error states, query-param drill-through, OpenAPI admin tag, ops README | ✅ | DQ-1703…DQ-1708 |

**Suggested first execution:** **DQ-1701** (foundation).  
After 1701, **DQ-1702** and **DQ-1704** / **DQ-1706** can proceed in parallel (API tracks).

**Critical path (ops-first):** DQ-1701 → DQ-1702 → DQ-1703  
**Critical path (dashboard):** DQ-1701 → DQ-1704 → DQ-1705  
**Customers track:** DQ-1701 → DQ-1706 → DQ-1707  

---

## Wave / DQ Entries

### DQ-1701 — Foundation (admin app + PlatformAdmin auth)

- **Status:** ✅ Complete  
- **Dependency:** —  
- **Source:** Plan 16 W0; DR-BO1/BO2/BO5  
- **Outcome:**  
  - Create `apps/admin` (Angular 21) with PrimeNG + AG Grid CE + light/dark theme (admin aesthetic, not Rossum customer chrome).  
  - Dev serve on **4203**; API CORS allows admin origins.  
  - Shell: sidebar nav (Dashboard, Files & documents, Support, Tenants, Businesses, API monitoring, System settings) + router-outlet + lazy feature stubs.  
  - Add `Modules/PlatformAdmin` (or equivalent) with `GET /api/admin/me` (B1).  
  - Auth policy **`PlatformAdmin`**; DevBypass sets platform-admin claim when configured.  
  - Amend `docs/architecture/patterns/folder-structure.md` for `apps/admin` + PlatformAdmin.  
  - Update `.vscode/launch.json` / solution docs if needed for dual frontend.  
- **Required Documents:** Plan 16 §Delivery + Architecture; Plan 11 mockups; `angular-*.md`; `critical-rules-api.md`  
- **Evidence:**  
  - API: `Modules/PlatformAdmin/Features/Me/AdminMeController.cs` — `GET /api/admin/me`  
  - Auth: `PlatformAdminAuth.PolicyName` + claim `documate_platform_admin` on `AdminGateAuthenticationHandler`  
  - System settings controller updated to `[Authorize(Policy = PlatformAdmin)]`  
  - CORS: localhost/127.0.0.1 **4203** on `CustomerWebDev`  
  - Admin app: `apps/admin` — shell nav, login via `POST /api/admin/auth/login`, stubs for INT screens  
  - Docs: `docs/architecture/patterns/folder-structure.md`; `docs/ops/backoffice-admin.md`  
  - VS Code: `Admin: Chrome` + `run-admin` task; compound `Full stack: API + Admin`  
  - `dotnet build` API ✅; `npm run build` (admin development) ✅ → `apps/admin/dist/admin`  

---

### DQ-1702 — Ops monitor APIs (B7, B8)

- **Status:** ✅ Complete  
- **Dependency:** DQ-1701  
- **Source:** Plan 16 W1; Plan 11 §2.1 filters/columns  
- **Outcome:**  
  - `GET /api/admin/ops/files` — paged, sorted, cross-tenant; filters per Plan 11; DTO includes tenant/business names for OrgCell.  
  - `GET /api/admin/ops/documents` — same; webhook fields for WebhookCell (status, lastAt, HTTP, attempts).  
  - PlatformAdmin-only; no business-scope restriction.  
  - Add/verify indexes for `CreatedAt`, `BusinessId`, status/stage enums as needed; record query notes in evidence.  
  - Tests + Postman (or equivalent) for list + filter combos.  
  - **Not in scope:** cancel/reprocess, ResultJson in list rows.  
- **Required Documents:** Plan 16 API B7/B8; Plan 11 §2.1; CQRS feature-slice  
- **Evidence:**  
  - `Modules/PlatformAdmin/Features/Ops/AdminOpsController.cs` — B7/B8 with join to CorTenantBusiness/CorTenant  
  - Filters: businessIds, tenantIds, queueIds, status/stage/source keys, name, emailFrom, hasError, isReprocess, isCancelled, createdFrom/To  
  - Query note: list uses `CreatedAt` desc + BusinessId equality; existing OpsFile/OpsDocument indexes sufficient for MVP; scale follow-up if needed  

---

### DQ-1703 — INT-20 Files & documents UI

- **Status:** ✅ Complete  
- **Dependency:** DQ-1701, DQ-1702  
- **Source:** Plan 16 W2; Plan 11 composite columns  
- **Outcome:**  
  - Files | Documents tabs; filter bar (as many Plan 11 filters as practical in MVP; rest behind “more filters” OK).  
  - AG Grid with server-side page/sort; **Columns ▾** picker; prefs in `localStorage`.  
  - **OrgCell:** business primary, tenant secondary.  
  - **WebhookCell** (documents): status pill + one-line `{lastAt} · HTTP · attempts`.  
  - Row navigate/drill to business or keep selection for later detail.  
- **Required Documents:** Plan 16 W2; Canvas INT-20; AG Grid theming from Band 16 patterns  
- **Evidence:**  
  - `apps/admin/.../ops-monitor/pages/ops-monitor.page.*`  
  - Column picker keys: `documate.admin.ops.files.columns` / `.docs.columns`  
  - Query params: `businessId`, `tab`, `date` for drill-through from INT-01 / support  

---

### DQ-1704 — Analytics APIs (B9–B13)

- **Status:** ✅ Complete  
- **Dependency:** DQ-1701  
- **Source:** Plan 16 W3; DR-BO6/BO7  
- **Outcome:**  
  - `GET /api/admin/analytics/summary` — KPI strip counts/rates + optional e2e averages.  
  - `GET /api/admin/analytics/volume?granularity=hour|day|month&from=&to=&businessIds=`  
  - `GET /api/admin/analytics/by-business?month=` or `?date=` — files/docs/failed%/MoM or peak hour.  
  - `GET /api/admin/analytics/hourly?date=&businessIds=` — files + documents per UTC hour.  
  - `GET /api/admin/analytics/stage-timings` — file + document stage averages (compute-on-read; approximate).  
  - All buckets **UTC**.  
  - Tests for date-boundary edge cases.  
- **Required Documents:** Plan 16 §Analytics; Plan 11 §2.2  
- **Evidence:**  
  - `Modules/PlatformAdmin/Features/Analytics/AdminAnalyticsController.cs`  
  - Stage timings from consecutive `status_changed` OpsWorkEvent payloads (`stage` field); labeled approximate  

---

### DQ-1705 — INT-01 Dashboard UI

- **Status:** ✅ Complete  
- **Dependency:** DQ-1701, DQ-1704  
- **Source:** Plan 16 W4; IMP1-A  
- **Outcome:**  
  - Time control bar: granularity, range presets, specific date, month, business filter.  
  - KPI strip; files/documents over time; hourly chart when date set; business monthly + specific-date tables (OrgCell).  
  - Stage timing bar charts (file + document).  
  - Drill-through: business row → INT-20 with query params (`businessId`, `date`).  
  - Label stage KPIs as approximate.  
- **Required Documents:** Plan 16 W4; Canvas INT-01  
- **Evidence:**  
  - `apps/admin/.../dashboard/pages/dashboard.page.*` — CSS bar charts (no Chart.js dep); drill to `/ops`  

---

### DQ-1706 — Tenants + Businesses APIs (B2–B6)

- **Status:** ✅ Complete  
- **Dependency:** DQ-1701  
- **Source:** Plan 16 W5; Plan 11 §2.3  
- **Outcome:**  
  - `GET /api/admin/tenants`, `POST /api/admin/tenants` (name, optional IdenTenantId, provider mode, optional initial business + queue/workflow bootstrap).  
  - `GET /api/admin/tenants/{id}` — detail + businesses summary.  
  - `GET /api/admin/businesses`, `GET /api/admin/businesses/{businessId}` — support detail (queues, agents, recent file counts/errors as available).  
  - Uniqueness + validation per Plan 16.  
  - Tests for create + list.  
- **Required Documents:** Plan 16 B2–B6; TenantBusinessProvisioner / DefaultQueueBootstrap patterns  
- **Evidence:**  
  - `Modules/PlatformAdmin/Features/Tenants/AdminTenantsController.cs`  
  - `Modules/PlatformAdmin/Features/Businesses/AdminBusinessesController.cs`  
  - Create uses `IDefaultQueueBootstrap` + `IDefaultWorkflowBootstrap` when initial business provided  

---

### DQ-1707 — Customers UI + Support workspace

- **Status:** ✅ Complete  
- **Dependency:** DQ-1701, DQ-1706  
- **Source:** Plan 16 W6  
- **Outcome:**  
  - INT-02 tenants list + **Create tenant** form/dialog.  
  - INT-03 tenant detail.  
  - INT-04 businesses list (OrgCell).  
  - INT-05 business detail (config, agents, recent files).  
  - `GET /api/admin/support/lookup` (B14) + INT-11 Support workspace UI.  
  - Links into INT-20 / INT-05.  
- **Required Documents:** Plan 16 W6; Canvas INT-02…05, INT-11  
- **Evidence:**  
  - Tenants/businesses pages under `apps/admin/src/app/features/{tenants,businesses,support}`  
  - Support: `Modules/PlatformAdmin/Features/Support/AdminSupportController.cs`  

---

### DQ-1708 — Monitoring + System settings

- **Status:** ✅ Complete  
- **Dependency:** DQ-1701  
- **Source:** Plan 16 W7; BO4-C; IMP2-A; IMP3-A; Plan 15  
- **Outcome:**  
  - `GET /api/admin/monitoring/snapshot` (B15) — health summary + Hangfire/Datadog URLs from config.  
  - INT-09 UI: health cards + deep links (no request-log table).  
  - INT-08 + B16: wrap Plan 15 system settings for allowed keys (maintenance, email intake, LLM toggle, limits as available).  
  - If Plan 15 not ready: read-only stubs + clear “coming soon” for writes.  
- **Required Documents:** Plan 16 W7; Plan 15 admin settings API; `Admin` config section  
- **Evidence:**  
  - `Modules/PlatformAdmin/Features/Monitoring/AdminMonitoringController.cs`  
  - `AdminOptions` (`Admin:HangfireDashboardUrl`, `Admin:DatadogDashboardUrl`) in appsettings  
  - Settings UI wraps existing `GET/PUT /api/admin/system-settings` (Plan 15 keys)  

---

### DQ-1709 — Polish + OpenAPI

- **Status:** ✅ Complete  
- **Dependency:** DQ-1703, DQ-1705, DQ-1707, DQ-1708  
- **Source:** Plan 16 W8  
- **Outcome:**  
  - Empty/error states across admin features.  
  - Consistent drill-through query params (INT-01 → INT-20; support → business).  
  - OpenAPI: tag/group admin endpoints; regen client if project uses OpenAPI clients for admin.  
  - Short `docs/ops/backoffice-admin.md` (how to run admin on 4203, DevBypass platform-admin).  
  - `npm run build` (admin) + API build green.  
- **Required Documents:** Plan 16 W8; OpenAPI conventions  
- **Evidence:**  
  - Empty/error messaging on dashboard, ops, tenants, businesses, support, monitoring, settings  
  - `docs/ops/backoffice-admin.md` updated with API surface  
  - `dotnet build` API ✅; `ng build` admin (development) ✅  
  - OpenAPI: controllers under `/api/admin` discovered via existing `AddOpenApi()`; no separate typed admin client in repo  

---

## Relationship to Plan 03

Band 17 is a **separate** customer/ops UI band (like Band 16). Do not revive cancelled DQ-1301/1302. Optional: add index pointer in `03-documate-v3-dispatch-queue.md` to this file.

---

## Agent output contract (Phase 3)

### Finalized Decisions

Inherited from Plans 11 + 16 — see Completion Summary. Band **17**, DQ-**1701…1709**.

### Pending Decisions

None.

### Assumptions

One DQ at a time unless developer batches. Prefer DQ-1701 first.

### Risks

See Completion Summary.

### Readiness

**Ready for execution.** Ask which DQ to run (suggested: **DQ-1701**).
