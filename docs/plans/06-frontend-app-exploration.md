# Documate v3 — Frontend App (Exploration)

> **Status:** Exploration — Phase 1 **complete** (2026-08-30)  
> **Type:** Product + engineering exploration for **two UI surfaces** (basic Internal + Customer app)  
> **Upstream:** [01-project-exploration-mental-design.md](./01-project-exploration-mental-design.md); [02-document-queue-design.md](./02-document-queue-design.md); [03-documate-v3-implementation-plan.md](./03-documate-v3-implementation-plan.md) Wave 8; DQ-1301 / DQ-1302  
> **Downstream:** [07-customer-frontend-implementation-plan.md](./07-customer-frontend-implementation-plan.md) (Phase 2) → Phase 3 DQ amend Wave 13  
> **Created:** 2026-08-28  
> **Amended:** 2026-08-29 — dual purpose; P1–P5; 2026-08-30 — mockups accepted; MVP + eng locks  

**Goal:** Align on **what** the frontend is (two audiences, two IA trees), inventory every screen, produce **mockups first**, then decide packaging, engineering stack, and which slice ships against current APIs vs later product phases.

---

## Planning flow

| Phase | This document |
|-------|----------------|
| 1 — Exploration | **This file** + mockup set (see §8) |
| 2 — Implementation plan | Only after purpose, screen inventory, mockups, and phasing are approved |
| 3 — Dispatch queue | Likely **separate** DQs for Customer app vs Internal panel; amend Wave 13 as needed |

---

## 1. Problem Framing

### 1.1 Purpose (developer — 2026-08-29) — **LOCKED intent**

Documate frontend is **not** one thin “configure Agents” shell. It has **two sections**:

| Surface | Audience | Purpose |
|---------|----------|---------|
| **A — Internal (SaaS control panel)** | Documate platform / support / ops team | Manage the platform: tenants, businesses, users, system config, API monitoring, cross-tenant visibility, billing, models, platform agents/defaults |
| **B — Customer application** | Tenant/Business operators | Run their own extraction ops: business profile, default queue + routes, agents (from templates), schema form builder, post-process, testing, usage, API docs, support tickets, file management |

This **expands** beyond Plan 03 Wave 8 / DQ-1301–1302 (those covered only a **minimal customer** slice). Treat Wave 8 as a **possible first build slice**, not the full product vision.

### 1.2 Why mockups come first

| Concern | Why it hurts |
|---------|----------------|
| Scope inflation | Billing, tickets, models, support consoles need product design before engineering DQs |
| Dual IA | Internal vs customer nav, authz, and data isolation differ completely |
| Schema UX | Customer wants **form builder** for schema — larger than textarea/Monaco |
| API reality | Many internal/customer screens have **no** FrontendSupport APIs yet |
| Plan 03 mismatch | Shipping DQ-1301 “as written” would undershoot this vision without a conscious phase cut |

### 1.3 Engineering reality (unchanged facts)

- `apps/web` is a blank Angular 21 shell (auth stub + Me call).  
- Customer configure APIs exist (Agents, Queues, Catalogs, Me).  
- Customer **file list / cancel / reprocess / rejections** mostly missing on `/api/app`.  
- **Internal** control-panel APIs largely **do not exist** (platform-admin module TBD).  
- Auth: Iden humans (Band 15); Phase 1 may use bypass — **internal roles vs customer roles** must be designed with Iden.

---

## 2. Scope

### 2.1 Surface A — Internal (SaaS control panel)

| Area | Intent (developer) | Notes / explore |
|------|--------------------|-----------------|
| Tenant management | CRUD / status / suspend | Iden is SoT for org identity — Documate may show projections + Documate-specific flags |
| Business management | Cross-tenant Business list/detail | Isolation unit for ops data |
| Users management | Platform + tenant users | Prefer Iden memberships; Documate UI may be a console over Iden |
| System configuration / settings | Global knobs | OCR/LLM provider keys already server-side; UI for safe toggles only |
| API monitoring | Request health, errors, latency | New observability product surface |
| Tenant data visualization | Status dashboards per tenant | Files/docs volumes, failure rates |
| Support helper screens | “Current status of clients” | Deep-link into a Business’s queue/files without impersonation vs with — **open** |
| Billing management | Plans, invoices, usage charges | **New product** — not in Plan 03 Phase 1 |
| Models management + billing | Catalog, cost, quotas, enable/disable | Explore: provider rows, $/1k tokens, tenant caps, Mode 1 meta routing visibility |
| Agents management | Platform Agent **templates** / system agents | Distinct from customer Agents |
| Defaults management | Document types, default workflows, seed templates, default queue policies | Platform catalog admin |

### 2.2 Surface B — Customer application

| Area | Intent (developer) | Notes |
|------|--------------------|-------|
| Business profile | Name, contact, display | Projection + Documate settings |
| Business management | Multi-business if membership allows | Align with Iden Business switcher |
| Default Queue settings | Webhook, email, allowlist + **route mapping** | K1: one default channel; routes still visible |
| Agents management | List + create from templates/defaults | Guided clone (Plan 01 B) |
| Schema form builder | Visual schema editing | Replaces “JSON only” assumption |
| Post-processing options | Attach/configure agent post-process | MCP tools exist Core-side; UX TBD |
| Customer workflow management | **Phase 2** | Explicitly deferred |
| Agent testing | Upload/sample run against an agent | Sync extract or app upload + poll |
| Usage dashboard & reports | Volume, cost, success rates | Needs metering APIs |
| API reference & knowledge | In-app docs / OpenAPI links | Content + nav |
| Support ticket system | Basic customer → Documate tickets | **New product** |
| File management | List/search uploads; search by schema fields | Needs list + search APIs |

### 2.3 Explicitly deferred (product)

| Item | When |
|------|------|
| Customer workflow authoring | Customer Phase 2 (developer) |
| Mode 2 BYOK UI | Later product |
| HITL / mapping catalogs | Later |
| White-label embed SDK | Plan 01 §3.1 — not this UI |

### 2.4 Non-goals (engineering)

- Pipeline logic in the browser.  
- Domain entities in Angular.  
- NgRx without a DQ.

---

## 3. Current-State Findings

| Area | Today |
|------|--------|
| `apps/web` | Empty routes; `AuthService` dev-bypass; hardcoded Me URL |
| Customer APIs | Agents, Queues (+ routes/webhook/email/allowlist), Catalogs, Me (`defaultQueueId`), ApiKeys, single-file get/upload |
| Customer gaps | File/Document list & search, cancel/reprocess on `/api/app`, IntakeRejections, usage/billing, tickets, schema form schema persistence beyond raw JSON |
| Internal APIs | **None** dedicated admin/platform module |
| Plan 03 Wave 8 | Minimal customer only — no internal panel, no billing/tickets/usage |
| Auth roles | No platform-admin vs customer-operator split in Documate yet |

---

## 4. Risks and Constraints

| Risk | Notes |
|------|--------|
| **Vision vs Phase 1** | Full dual surface is multi-quarter; must cut mockup → MVP slices |
| **Iden boundary** | Tenant/User/Business SoT is Iden — internal “management” UIs must not become a second identity DB |
| **Support impersonation** | Support screens may need audited “act as Business” — security-sensitive |
| **Billing / metering** | No domain yet — mockups invent product that needs a separate product exploration |
| **Schema form builder** | High effort; must map to `OutputSchemaJson` contract |
| **Two apps vs one** | Packaging affects auth, deploy, CORS, branding |
| **DQ-1301/1302** | Must be amended once mockup phasing is chosen — do not execute blindly |

---

## 5. Open Questions

### Product / packaging (answer before bulk mockups)

#### P1 — App packaging — **LOCKED (2026-08-29)**

| Choice | Meaning |
|--------|---------|
| **P1-D** | **Two apps.** Internal (`apps/admin` or similar) is a **basic** control panel only (not full INT catalog). Customer = `apps/web`. Build order for code can prioritize basic internal later; **mockups skip internal** (see P4). |

#### P2 — Mockup fidelity — **LOCKED**

| Choice | Meaning |
|--------|---------|
| **P2-C** | **Hi-fi branded** mockups (not wireframes) |

#### P3 — Mockup delivery format — **LOCKED**

| Choice | Meaning |
|--------|---------|
| **P3-E** | **Cursor Canvas** mockups (interactive `.canvas.tsx` beside chat) |

#### P4 — What to mock — **LOCKED**

| Choice | Meaning |
|--------|---------|
| **P4-B′** | **Customer app full** (`CUS-01`…`CUS-22`, skip Phase-2 workflow or mark coming-soon). **No internal mockups.** |

#### P5 — Visual direction — **LOCKED (2026-08-29)**

| Choice | Meaning |
|--------|---------|
| **P5-R** | **Rossum-inspired** document ops UI (queue sidebar, status tabs, dense file table, upload modal, statistics). **Both light and dark themes** required in mockups and eventual product. |

Reference images saved under `docs/plans/mockups/ref-rossum/`.

### Engineering — **LOCKED (2026-08-30)** for customer MVP

| # | Topic | Choice |
|---|-------|--------|
| MVP cut | Defer tickets / usage reports / billing APIs (and related screens as API-backed) | **Locked** |
| Nav | **Nav B** (Nanonets-style: pinned My agents / New agent; Overview; Documents; Workflow; Settings; help + team footer) | **Locked** |
| Q1 | Monitor APIs | **Defer** with usage/tickets (not MVP) |
| Q2 | IntakeRejection API | **Defer** (not MVP) |
| Q3 | Component library | **PrimeNG** (+ **AG Grid Community** for Files list, themed to match PrimeNG) |
| Q4 | Schema editor | **Form builder (D2)** |
| API gaps | Plan and build FrontendSupport APIs needed for MVP screens | **Yes** — cancel/reprocess **not** required yet |
| Cancel / reprocess | Out of MVP | **Locked** |

### Models / billing ideas (explore — not locked)

Possible Internal “Models & billing” capabilities to show in mockups:

1. Provider/model catalog (enable, region, Mode 1 routing weight)  
2. Per-model unit cost + tenant markup  
3. Tenant quotas (pages/day, $ cap)  
4. Usage drill-down (tenant → business → agent)  
5. Invoice export / Stripe-like plan tiers  
6. Alerting when OCR fallback or LLM error rate spikes  

---

### 5.1 Popular visual references (for P5)

Pick **one** primary reference (or mix: “nav like X, density like Y”).

| ID | Reference | Why teams copy it | Fit for Documate customer app | Watch-outs |
|----|-----------|-------------------|-------------------------------|------------|
| **R1** | **Stripe Dashboard** | Gold standard B2B ops UI: clear hierarchy, excellent tables, calm blue accent, trust-heavy finance look | Strong for usage, billing-adjacent, API keys, files lists | Can feel “payment product”; dense |
| **R2** | **Linear** | Modern product craft: sparse chrome, fast nav, excellent detail panels | Strong for Agents list, issue-like tickets, clean status | Less “enterprise admin”; dark-first brand |
| **R3** | **Vercel** | Developer-product polish: monospace accents, deploy/status clarity | Strong for API reference, agent test runner, technical operators | Less friendly for non-dev ops users |
| **R4** | **Notion** | Soft structure, nested pages, approachable | Strong for knowledge / API docs section | Weak for dense ops tables |
| **R5** | **HubSpot / Salesforce Lightning** | Classic SaaS CRM: left nav, object lists, record detail | Strong for Business profile, tickets, multi-object ops | Heavier, dated if copied literally |
| **R6** | **Retool / internal-tool aesthetic** | Dense forms, builders, admin productivity | Strong for schema form builder, route mapping | Feels “internal tool,” not polished product |
| **R7** | **AWS Console** | Service catalog + resource tables | Familiar to infra buyers | Cluttered; avoid as primary brand |
| **R8** | **Intercom / Zendesk** | Support inbox + customer context | Strong for tickets + knowledge | Narrow if used for whole app |

**Recommendation for Documate customer app:** **R1 Stripe** as primary (lists, settings, trust), with **R6-lite** patterns only on schema form builder / route mapping screens.

---

## 6. Recommended Direction (current)

1. Dual surface remains product truth; internal stays **basic** when built; **mockups = customer only**.  
2. Customer Canvas mockups accepted for now (iterate later).  
3. Schema = form builder (**D2**). Nav = **B**. UI = **PrimeNG** + AG Grid CE on Files.  
4. MVP defers tickets / usage / billing APIs; build remaining FrontendSupport APIs needed for Files list/search/detail.  
5. Amend DQ-1301/1302 via Phase 2 plan `07` → Phase 3 DQ.

---

## 7. Exploration Exit Criteria

- [x] Dual purpose accepted (Internal + Customer)  
- [x] **P1–P5** answered  
- [x] Screen inventory §8 accepted for mockups  
- [x] Customer Canvas reviewed “ok for now” (2026-08-30)  
- [x] MVP vs later labeled (see Phase 2 plan §MVP)  
- [x] Engineering Q1–Q4 + nav + API gap policy answered  
- [x] Decision on Plan 03 Wave 8 / DQ-1301–1302 amendment — **❌ superseded by Band 16** (`07-customer-frontend-dispatch-queue.md`)  

Then Phase 2 implementation plan: **`07-customer-frontend-implementation-plan.md`**.

---

## 8. Screen inventory (mockup checklist)

IDs are stable for mockup filenames: `INT-xx`, `CUS-xx`.

### 8.1 Internal — SaaS control panel

| ID | Screen | Primary actions |
|----|--------|-----------------|
| INT-01 | Platform home / ops dashboard | Cross-tenant KPIs, alerts |
| INT-02 | Tenants list | Search, filter status, open tenant |
| INT-03 | Tenant detail | Businesses, usage summary, suspend/flags |
| INT-04 | Businesses list (cross-tenant) | Filter by tenant, open business |
| INT-05 | Business detail (support view) | Queue id, recent files, agent list, health |
| INT-06 | Users list | Search; link to Iden where applicable |
| INT-07 | User detail / memberships | Roles, business access |
| INT-08 | System settings | Feature flags, global limits, maintenance |
| INT-09 | API monitoring | Traffic, errors, latency, top routes |
| INT-10 | Tenant status / visualization | Charts by tenant; drill-down |
| INT-11 | Support workspace | “Client status” search → business snapshot |
| INT-12 | Billing — plans & subscriptions | Assign plan, see status |
| INT-13 | Billing — invoices / charges | List, export |
| INT-14 | Models catalog | Enable/disable, cost, Mode 1 notes |
| INT-15 | Models — tenant quotas / billing rules | Caps, markup |
| INT-16 | Platform agent templates | CRUD templates/defaults |
| INT-17 | Defaults management | Document types, workflows, seed policies |
| INT-18 | Platform agents (system) | If distinct from templates |

### 8.2 Customer application

| ID | Screen | Primary actions |
|----|--------|-----------------|
| CUS-01 | Home / usage dashboard | Volume, success, cost summary |
| CUS-02 | Business profile | Edit profile fields |
| CUS-03 | Business management / switcher | List businesses; select active |
| CUS-04 | Default Queue — overview | Queue ID copy; status |
| CUS-05 | Default Queue — intake settings | Webhook, email, allowlist |
| CUS-06 | Default Queue — route mapping | DocumentType → Agent map |
| CUS-07 | Agents list | Filter; clone from template |
| CUS-08 | Agent templates gallery | Browse defaults; start clone |
| CUS-09 | Agent create / edit | Name, instructions, post-process |
| CUS-10 | Schema form builder | Visual fields → schema |
| CUS-11 | Post-processing options | Enable tools / workflow attach (simple) |
| CUS-12 | Agent test runner | Upload sample; show result JSON |
| CUS-13 | Files list | Search, status, open file |
| CUS-14 | File detail | Docs, cancel/reprocess, download |
| CUS-15 | Document detail | Result JSON, schema field view |
| CUS-16 | File search by schema | Query extracted fields |
| CUS-17 | Usage reports | Date range, export |
| CUS-18 | API reference / knowledge | Docs nav, code samples |
| CUS-19 | Support tickets list | Create / view status |
| CUS-20 | Support ticket detail | Thread |
| CUS-21 | API keys (ops) | Create/revoke (if in customer app) |
| CUS-22 | Workflow management | **Phase 2** — mock as “coming soon” or skip |

**Mockup set (locked P4):** customer `CUS-01`…`CUS-21` + optional `CUS-22` stub ≈ **21–22 canvases**. Internal screens listed for later build scope only — **not mocked**.

---

## Agent output contract (Phase 1 — complete)

### Finalized Decisions

| # | Decision | Status |
|---|----------|--------|
| Purpose | Frontend = **Internal (basic)** + **Customer app** | **Locked** |
| Mockups before code | Yes; mockups OK for now (2026-08-30) | **Locked** |
| P1 | Two apps; internal = **basic** subset only | **Locked** (P1-D) |
| P2 | Hi-fi branded (not wireframes) | **Locked** (P2-C) |
| P3 | Cursor Canvas mockups | **Locked** (P3-E) |
| P4 | Customer full mockups; **no** internal mockups | **Locked** (P4-B′) |
| P5 | **Rossum-inspired** + **light & dark** themes | **Locked** (P5-R) |
| Customer workflows | Later product phase | **Locked** |
| Nav | **Nav B** (Nanonets-style) | **Locked** |
| MVP defer | Tickets, usage reports, billing APIs | **Locked** |
| Q3 | **PrimeNG** + AG Grid CE (Files; theme to match PrimeNG) | **Locked** |
| Q4 | Schema **form builder** | **Locked** |
| API gaps | Build FrontendSupport APIs for MVP; **no** cancel/reprocess yet | **Locked** |

### Pending Decisions

| # | Topic |
|---|--------|
| Form-builder toolkit | **FB1** custom lean (Phase 2) |
| Home KPIs without usage API | **H2** summary API (Phase 2) |
| DQ-1301/1302 amend shape | Phase 3 |
| Agent test path | **T1** queue upload + poll (Phase 2) |

### Assumptions

- Plan 03 Wave 8 / DQ-1301–1302 are **superseded in scope** by this customer MVP plan (amend in Phase 3).  
- Iden remains identity SoT; Phase 1 web may keep auth bypass until Band 15.  
- Internal app is **out of** this MVP build.

### Risks

- File list/search APIs are net-new; underestimating query/index work delays UI.  
- Schema form builder quality vs ship date.  
- AG Grid + PrimeNG visual parity needs a dedicated theming pass.

### Readiness

**Ready for Phase 2** — implementation plan: `07-customer-frontend-implementation-plan.md`.
