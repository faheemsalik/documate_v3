# Documate v3 — Customer Frontend MVP (Implementation Plan)

> **Status:** Phase 2 — **approved** (2026-08-30)  
> **Type:** Engineering implementation plan (`apps/web` + FrontendSupport APIs)  
> **Upstream:** [06-frontend-app-exploration.md](./06-frontend-app-exploration.md) (Phase 1 complete 2026-08-30)  
> **Also aligns:** Plan 03 Wave 8 (DQ-1301/1302 superseded)  
> **Downstream:** [07-customer-frontend-dispatch-queue.md](./07-customer-frontend-dispatch-queue.md) (Phase 3 Band 16)  
> **Created:** 2026-08-30  
> **Amended:** 2026-08-30 — DR1=FB1, DR2=H2, DR3=T1; Phase 2 approved  

**Outcome:** Ship a **customer Angular app** (Rossum-inspired, light/dark, **Nav B**) on **PrimeNG**, with **AG Grid Community** for Files, **schema form builder**, and the **FrontendSupport APIs** required for MVP (file list/detail/search; **no** cancel/reprocess, tickets, usage, or billing APIs yet).

---

## Planning flow

| Phase | Document |
|-------|----------|
| 1 — Exploration | `06-frontend-app-exploration.md` ✅ |
| 2 — Implementation plan | **This file** |
| 3 — Dispatch queue | Amend `03-documate-v3-dispatch-queue.md` after this plan is approved |

---

## Delivery Principles

1. **Customer app only** — Internal SaaS panel is out of this MVP.  
2. **Nav B** — Nanonets-style sidebar (pinned My agents / New agent; Overview; Documents; Workflow; Settings; help + team footer).  
3. **PrimeNG** for chrome, forms, dialogs, menus; **AG Grid CE** only for Files list (theme tokens mapped to PrimeNG).  
4. **Schema = form builder** — not Monaco-as-primary; JSON may remain an advanced escape hatch later.  
5. **Build APIs we need** on `/api/app` — do not call External `/api/v1` from the product UI.  
6. **Defer** tickets, usage reports, billing APIs, cancel, reprocess, IntakeRejection UI, monitor.  
7. **No pipeline logic in the browser** — UI orchestrates FrontendSupport only.  
8. **Standalone Angular + signals** — follow `docs/architecture/patterns/angular-*.md`; no NgRx unless a later DQ.  
9. **OpenAPI clients** for `/api/app` when practical (`openapi-and-clients.md`).  
10. **Amend Wave 13** — DQ-1301/1302 must not be executed as the old “thin configure shell.”

---

## MVP screen cut

| ID | Screen | MVP? | Notes |
|----|--------|------|-------|
| CUS-01 | Dashboard / Home | **Yes** | KPIs from file list aggregates or lightweight summary endpoint — **not** usage/billing APIs |
| CUS-02 | Business profile | **Yes** | May need profile read/update API if missing beyond Me |
| CUS-03 | Switch business | **Yes** | Me + business list/switch; Iden later |
| CUS-04 | Channels & intake (default queue) | **Yes** | Existing Queues APIs |
| CUS-05 | Intake settings | **Yes** | Webhook / email / allowlist |
| CUS-06 | Route mapping | **Yes** | Existing routes APIs |
| CUS-07 | My agents | **Yes** | Existing Agents APIs |
| CUS-08 | New agent / templates | **Yes** | Catalogs + clone |
| CUS-09 | Agent edit | **Yes** | |
| CUS-10 | Schema form builder | **Yes** | Persists agent schema JSON via Agents update |
| CUS-11 | Post-processing options | **Yes** | Configure via agent fields / workflow attach already seeded server-side |
| CUS-12 | Agent test runner | **Yes** | Upload + poll get-file (or sync extract if exposed on app) |
| CUS-13 | Extract data (Files list) | **Yes** | **New** list API + AG Grid |
| CUS-14 | File detail | **Yes** | Existing get + download; docs list if needed; **no** cancel/reprocess |
| CUS-15 | Document detail | **Yes** | Need document get on `/api/app` if missing |
| CUS-16 | Schema search | **Yes** | **New** search API (extracted fields) |
| CUS-17 | Usage stats | **Defer** | No usage API |
| CUS-18 | Documentation | **Yes** | Static / OpenAPI links |
| CUS-19 / CUS-20 | Support tickets | **Defer** | No tickets API |
| CUS-21 | API keys | **Yes** | Existing if present |
| CUS-22 | Workflows | **Defer** | Coming-soon stub optional |

Nav labels follow **Nav B** (Exploration lock).

---

## Domain Architecture Layer

### Web (`apps/web`)

```text
apps/web/src/app/
  core/           # auth interceptor, theme (light/dark), config, PrimeNG + AG Grid theme bridge
  shared/         # layout shell (Nav B), page chrome, form-builder primitives if shared
  features/
    home/
    business/
    queues/       # channel overview, intake, routes
    agents/       # list, templates, edit, schema builder, post-process, test
    files/        # list (AG Grid), detail, document detail, schema search
    docs/         # API knowledge
    api-keys/
```

- Lazy feature routes; shell layout wraps authenticated area.  
- Feature `data/*` → `/api/app/...` only.  
- View models in feature `models/` — not Domain entities.

### API (`apps/api` — FrontendSupport)

Extend **Files** (and Documents as needed) under existing CQRS feature slices:

| Capability | Today | MVP work |
|------------|-------|----------|
| Upload / get file / download URL | Exists | Keep |
| **List files** (queue-scoped, filter, page) | **Missing** | Add |
| **List documents for file** | Likely missing on app | Add if not present |
| **Get document** (result JSON, status) | Likely missing on app | Add |
| **Search files by extracted field** | **Missing** | Add |
| Cancel / reprocess | Exists Core-side elsewhere | **Out of MVP** (no app endpoints required yet) |
| Agents / Queues / Catalogs / Me / ApiKeys | Mostly exist | Wire UI; fill small gaps (e.g. business profile) |

List/search must respect **Business** isolation via `IBusinessContext`. Prefer indexed filters on status / received time; schema search may start as JSON-path / stored extracted projection — call out index strategy in DQ evidence.

### UI stack

| Layer | Choice |
|-------|--------|
| Component library | **PrimeNG** (Aura or equivalent preset; light/dark) |
| Files grid | **AG Grid Community** + Theming API `withParams` mapped to PrimeNG design tokens |
| Other tables | PrimeNG `Table` / DataView OK (Agents cards stay card layout) |
| Forms | Reactive forms + PrimeNG inputs |
| Icons | PrimeIcons (or one consistent set) |

**Why not Material-only:** denser ops UI + form/table surface area fits PrimeNG; AG Grid already chosen for Files — theming to PrimeNG is explicit work, not a second full design system.

---

## Domain Validation Rules

### Decision Required — DR1 · Schema form-builder toolkit — **LOCKED (FB1)**

| Option | Meaning |
|--------|---------|
| **FB1** | **Custom** lean builder (field list, type, required, nested table section) → emits agent schema JSON |
| **FB2** | **ngx-formly** (or similar) + custom types for table/line-items |
| **FB3** | Third-party visual form designer (heavier dependency) |

**Choice:** **FB1** (2026-08-30).

### Locked validation (product/eng)

- Agent schema remains the SoT stored on Agent; builder is an editor over that JSON.  
- Queue remains **K1** default channel; UI does not invent multi-queue product admin.  
- File public status for list columns follows existing rollup semantics server-side.  
- No cancel/reprocess actions in UI for MVP.

### Decision Required — DR2 · Home / Dashboard metrics without usage API — **LOCKED (H2)**

| Option | Meaning |
|--------|---------|
| **H1** | Derive simple counts from **file list** (to review / failed / ready) for default queue |
| **H2** | Add a thin `GET /api/app/.../summary` (counts only, no billing) |
| **H3** | Static placeholder cards until usage API exists |

**Choice:** **H2** (2026-08-30) — `GET` summary counts for Home (A5 required).

---

## Process Flows

### F1 — Extract data (Files)

1. User opens **Documents → Extract data**.  
2. UI loads AG Grid via `GET .../queues/{id}/files` (filters: status, date, schema facets).  
3. Row open → File detail (PDF/download URL + documents).  
4. Document open → extracted fields + line table from ResultJson.  
5. Schema search → dedicated query API → results grid.

### F2 — Agent configure

1. **My agents** / **New agent** (templates) → clone/create.  
2. Edit agent → schema form builder → save schema on agent.  
3. Post-process options → save agent/workflow settings already supported server-side.  
4. Test runner → upload sample file to default queue (or agent-bound path) → poll file/document until terminal → show PDF + JSON.

### F3 — Channel

1. Channels & intake → default queue overview.  
2. Intake settings → webhook / email / allowlist.  
3. Route mapping → document type → agent; lock when product rules require.

### F4 — Business context

1. Team footer / Switch business → select active business.  
2. Me + subsequent `/api/app` calls use business context.

---

## Instruction and Control Set

### Decision Required — DR3 · Agent test upload path — **LOCKED (T1)**

| Option | Meaning |
|--------|---------|
| **T1** | Reuse `POST /api/app/queues/{defaultQueueId}/files` + poll get-file |
| **T2** | Add dedicated `POST /api/app/agents/{id}/test` that forces route to that agent |

**Choice:** **T1** (2026-08-30) — test runner uses default-queue upload + poll; agent must be on route map.

### Locked controls

- Theme toggle light/dark (app-level; PrimeNG + AG Grid theme modes).  
- Auth: keep current bypass until Band 15; interceptor ready for bearer.  
- No External module calls from web.

---

## Permissions and Security

- All MVP APIs: authenticated human; **Business-scoped**.  
- API keys UI: existing FrontendSupport rules; never expose secrets in logs.  
- Signed download URLs: short-lived; UI does not persist permanently.  
- Schema search: only within current Business; prevent cross-tenant JSON probes.  
- No platform-admin routes in this app.

---

## API work package (MVP)

| ID | Endpoint (sketch) | Purpose |
|----|-------------------|---------|
| A1 | `GET /api/app/queues/{queueId}/files` | Paged list + status/date filters; optional projected schema columns |
| A2 | `GET /api/app/queues/{queueId}/files/{fileId}/documents` | Documents under file |
| A3 | `GET /api/app/queues/{queueId}/documents/{documentId}` | Detail + ResultJson (+ public status) |
| A4 | `GET /api/app/queues/{queueId}/files/search` | Query by extracted field key/value (CUS-16) |
| A5 | `GET /api/app/queues/{queueId}/files/summary` | Home KPIs (**H2** — required) |
| A6 | Business profile get/update if not covered by Me | CUS-02 |

**Explicitly out of MVP:** cancel, reprocess, tickets, usage/billing, IntakeRejection list, monitor.

Reuse existing: Agents CRUD/clone, Queues + routes + webhook/email/allowlist, Catalogs, Me, file upload/get/download, ApiKeys.

---

## Dispatch Index (preview — Phase 3 will number)

| Wave | Focus | Depends on |
|------|-------|------------|
| W0 | Web shell: PrimeNG theme light/dark, Nav B layout, routing skeleton | — |
| W1 | FrontendSupport file list + documents get + **summary (H2/A5)** | — |
| W2 | Files UI: AG Grid list themed to PrimeNG; file/document detail | W0, W1 |
| W3 | Schema search API + UI | W1 |
| W4 | Agents / templates / edit / test (T1 or T2) | W0 |
| W5 | Schema form builder (FB*) | W4 |
| W6 | Post-process options UI | W4 |
| W7 | Queues: overview, intake, routes | W0 |
| W8 | Business profile + switcher; API keys; docs page; Home | W0, W1 |
| W9 | OpenAPI regen / client; amend retire DQ-1301/1302 | prior waves |
| W10 | Harden: empty states, errors, a11y pass on shell | prior |

Exact DQ-IDs assigned in Phase 3.

---

## Wave Sections (summary)

### W0 — Shell

- Install PrimeNG + preset; CSS variables for Rossum-like accent within PrimeNG.  
- AG Grid CE dependency; shared `agTheme` mapped to PrimeNG tokens.  
- App layout: top bar + Nav B + router-outlet.  
- Feature route stubs for MVP screens; deferred routes omit or “coming soon.”

### W1 — File/Document APIs

- Implement A1–A3 (+ A5 if H2) with MediatR handlers, tests, Postman.  
- Status filters align with public file status rollup.

### W2 — Files UI

- AG Grid: checkbox selection (bulk actions **except** cancel/reprocess — selection for future or export only), status pills, open row.  
- File detail: half-page document preview (iframe/object via signed URL) + metadata.  
- Document detail: field view + line table from ResultJson.

### W3 — Schema search

- A4 API + Filters UI → results in AG Grid or PrimeNG table.

### W4–W6 — Agents

- Cards list; templates gallery; edit form; test runner; form builder; post-process.

### W7–W8 — Channel, business, chrome pages

- Wire existing queue/agent/me/keys APIs; fill A6 if needed.

### W9–W10 — Clients + polish

- Regenerate OpenAPI client; documentation page; amend Plan 03 Wave 13 notes in DQ.

---

## Relationship to Plan 03 Wave 8

| Plan 03 Wave 8 item | This MVP |
|---------------------|----------|
| Agents clone/edit | **In** |
| Monitor | **Out** |
| Rejections | **Out** |
| Cancel/reprocess actions | **Out** (API may exist Core-side; not exposed in UI yet) |
| Default channel settings | **In** |

Phase 3 DQ must **replace** thin DQ-1301/1302 with waves above (or mark 1301/1302 superseded).

---

## Agent output contract (Phase 2)

### Finalized Decisions (inherited + this plan)

| # | Decision | Status |
|---|----------|--------|
| Surface | Customer app MVP only | **Locked** |
| Nav | **B** | **Locked** |
| UI kit | PrimeNG + AG Grid CE (Files) | **Locked** |
| Schema UX | Form builder | **Locked** |
| Defer | Tickets, usage, billing APIs; cancel/reprocess UI | **Locked** |
| API gaps | Build A1–A6 as needed | **Locked** |
| **DR1** | Form builder toolkit **FB1** (custom lean) | **Locked** |
| **DR2** | Home metrics **H2** (summary API A5) | **Locked** |
| **DR3** | Agent test **T1** (queue upload + poll) | **Locked** |

### Pending Decisions

None — Phase 2 approved; execution via Band 16 DQ.

### Assumptions

- Auth bypass acceptable until Band 15.  
- Default queue id available from Me / business bootstrap.  
- Canvas mockups are guidance, not pixel-perfect acceptance.  
- Agent under test is (or will be) on the default queue route map for T1.

### Risks

- Schema search performance on raw JSON.  
- AG Grid ↔ PrimeNG visual drift.  
- Form builder under-scoped vs real schemas.

### Readiness

**Phase 2 complete.** Execute [07-customer-frontend-dispatch-queue.md](./07-customer-frontend-dispatch-queue.md).
