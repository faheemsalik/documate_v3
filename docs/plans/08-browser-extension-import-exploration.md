# Documate v3 — Browser Extension Import (Exploration)

> **Status:** Exploration — Phase 1 **amended** (2026-09-01 framing + Mode 1 lock; remaining opens still open)  
> **Type:** Product exploration (new client surface: browser extension + page-import definitions)  
> **Upstream:** [01-project-exploration-mental-design.md](./01-project-exploration-mental-design.md); [00-product-glossary.md](./00-product-glossary.md); External APIs + Customer app (Plans 03 / 07)  
> **Downstream:** Phase 2 implementation plan → Phase 3 dispatch queue (**only after this exploration is verified**)  
> **Created:** 2026-08-31  
> **Amended:** 2026-09-01 — zero-code host posture; three delivery modes; Mode 1 = product Phase 1; Autopilot deferred  
> **Host examples (non-exclusive):** Simplicity Cloud, HAPA, other browser ERPs — **no code changes in those apps**

**Goal:** Align on a **plug-and-play browser extension** so Documate users can get extracted data into host ERP pages/APIs **without any code or change in the underlying host app**.

---

## Planning flow

| Phase | This document |
|-------|----------------|
| 1 — Exploration | **This file** |
| 2 — Implementation plan | Only after remaining opens below are locked (or explicitly deferred) |
| 3 — Dispatch queue | New DQ wave(s) — decided in Phase 2 |

**Note:** Per governance, Phase 2 and Phase 3 are **separate** deliverables. This file does **not** contain implementation steps or DQ items.

**Naming:** “Product Phase 1” below = first **shipped capability** of this extension feature. It is not the planning-process Phase 1/2/3 (explore → plan → DQ).

---

## 1. Problem Framing

### 1.1 Core problem

Customers use Documate to extract structured data, but the **system of record** is still a host app (ERP / Simplicity Cloud / HAPA / similar) opened in the browser.

Asking every host vendor (or the customer’s IT) to **integrate via code, webhooks, or custom APIs** fails the product goal:

> **Zero host change.** Documate must work as **plugin-play**: install extension → register pages → import. No patches, embeds, or SDK installs inside Simplicity / HAPA / other ERPs.

### 1.2 Three delivery modes (product roadmap)

| Mode | Name | How it works | When |
|------|------|--------------|------|
| **Mode 1** | **Assisted page fill** | User has host page open (e.g. purchase invoice). Selects a Ready Documate Document. Extension pastes `ResultJson` into mapped page fields. User (or extension-triggered **page Save**) saves the invoice **in the host UI**. | **Product Phase 1 — LOCKED** |
| **Mode 2** | **Session Autopilot** | Browser open + user logged into host. Extension has **studied** how that page submits (which host API / payload). Extension calls that API to create the document without manual field paste. | **Deferred** (later product phase) |
| **Mode 3** | **Credential Autopilot** | User not required online. Documate acts with stored host login credentials on their behalf. | **Deferred — later stage** (highest trust / security bar) |

```text
Mode 1 (now):     [User on host page] → paste fields → Save in host UI
Mode 2 (later):   [Browser + host session] → replay studied host submit API
Mode 3 (later):   [No user online] → stored credentials → host API on behalf
```

### 1.3 Mode 1 — Product Phase 1 (locked intent)

1. User authenticates to Documate in the extension.  
2. User uploads files; extension shows processing + **ready** Documents.  
3. User opens host page (e.g. purchase invoice create/edit).  
4. User selects a Ready Document → **Import** pastes values into registered fields.  
5. Invoice is **saved via the host page** (user click Save, or extension clicks/triggers the same Save control — not a silent backend API until Mode 2).  
6. User can **study / register** pages: AI + tools propose field maps → user confirms → name the page → definition saved on Documate.

### 1.4 What this is not

| Not this | Why |
|----------|-----|
| Code changes / plugins inside host ERP | Violates zero-host-change promise |
| Replacement for External API / webhooks | Machine integrations stay first-class for customers who *want* code |
| White-label embed SDK (Plan 01 §3.1) | Different product |
| Plan 01 “mapping catalogs” | Name→id enrichment — different later concept |
| Mode 2 / Mode 3 in Product Phase 1 | Explicitly deferred |
| Unattended bot without browser session | That is Mode 3 |

### 1.5 Product promise (Mode 1)

**Documate Import Extension (Phase 1)** lets a Documate user, with a host ERP page open, import a Ready Document into that page’s fields and save through the host UI — after registering the page once (AI-assisted, user-confirmed), with **no changes** to Simplicity, HAPA, or any other host app.

---

## 2. Scope

### 2.1 In scope — Product Phase 1 (Mode 1)

| Area | Intent |
|------|--------|
| **Extension shell** | Auth, upload, Document list, Import, Register page |
| **Auth** | Documate user login (see P2) |
| **Upload + progress** | Files into Queue; show processing / ready / failed |
| **Import (Mode 1)** | Select Ready Document → fill mapped DOM fields on **current** page → save via **host page Save** (not studied host API) |
| **Page study (fields)** | DOM + AI propose ResultJson key → control mappings; user confirms; recommended page name |
| **Server persistence** | Save page-import definition on Documate |
| **Reuse** | Later visits: match / select definition → Import without re-study |
| **Host posture** | Works against arbitrary browser hosts; examples Simplicity, HAPA — **no host code** |

### 2.2 Explicitly deferred

| Item | Notes |
|------|-------|
| **Mode 2 — Session Autopilot** | Study host **submit API** / network capture; create docs via API while browser session alive |
| **Mode 3 — Credential Autopilot** | Store host credentials; run without user online |
| Network/HAR capture as first-class study output | Belongs to Mode 2 study, not Mode 1 field maps |
| Native desktop / non-browser hosts | |
| Building ERP screens inside Documate | |
| Statements reconciliation (Plan 01 §3.2) | Page fill ≠ reconciliation product |
| White-label extension for ISVs | |
| Replacing Customer web app | |

### 2.3 Non-goals (engineering — Phase 2 awareness)

- Running Core OCR/LLM extract inside the browser  
- Inventing a second Result schema — map **from** Agent `ResultJson` **to** page fields  
- Implementing Mode 2/3 APIs or credential vaults in Product Phase 1 DQs  

### 2.4 Design implication for later Autopilot

Mode 1 definitions (field maps) are **not** enough for Mode 2. Mode 2 will need a separate (or extended) study artifact: **how the page submits** (endpoint, method, auth cookies/headers from session, payload shape). Product Phase 1 should **not** pretend field maps = Autopilot — but Phase 2 may note extension points so Mode 2 is not blocked later.

---

## 3. Current-State Findings

| Area | Today | Extension impact |
|------|-------|------------------|
| Browser extension | **None** | Greenfield client |
| Human auth (Iden JWT) | **Not wired** — DevBypass on `/api/app`; Band 15 planned | Production “Documate user login” needs interim or Iden |
| Machine auth | Business API keys on `/api/v1/*` | Interim path for upload/list/ResultJson |
| Upload / ready / ResultJson | External + app APIs exist | Reusable Mode 1 spine |
| Page / DOM mapping APIs | **None** | Required for Mode 1 |
| Host submit-API study | **None** | Mode 2 only — deferred |
| Host credential vault | **None** | Mode 3 only — deferred |
| Zero-host-change | N/A | Extension is the only host touchpoint |

**Reusable spine:** auth → upload → poll `status=ready` → `ResultJson` → DOM fill → host Save.  
**Missing:** extension client, page definition CRUD, field study + confirm, DOM fill + Save trigger.

---

## 4. Risks and Constraints

| Risk | Notes |
|------|-------|
| **Auth gap** | Documate login vs API-key interim (P2) |
| **Fragile selectors** | Host SPA updates break maps |
| **Wrong-page import** | Purchase invoice map on credit note page |
| **PII / DOM to LLM** | Field study may send page structure to AI |
| **CSP / injection** | Some hosts restrict content scripts |
| **Schema drift** | Agent schema change vs saved maps |
| **Line items** | Tables harder than header fields |
| **Save vs submit** | Triggering host Save may run validation; partial fills fail |
| **Mode creep** | Pressure to ship Mode 2 network study early — keep out of Phase 1 DQs |
| **Credential temptation** | Mode 3 must not leak into Phase 1 storage design without vault design |

---

## 5. Open Questions

### Locked (2026-09-01)

| # | Choice | Meaning |
|---|--------|---------|
| **M0** | **Zero host change** | No code/config required inside ERP / Simplicity / HAPA / other hosts — extension-only integration |
| **M1** | **Mode 1 only** for Product Phase 1 | Assisted page fill + host UI Save |
| **M2** | **Mode 2 deferred** | Session Autopilot (study host submit API) — later product phase |
| **M3** | **Mode 3 deferred** | Credential Autopilot — later stage |
| **P7** | **P7-D** (new) | **Fill fields + complete save via host page Save control** (user or extension triggers same UI Save). **Not** calling a studied host backend API (that is Mode 2) |
| **P11** | **Mode 1 product cut** | Product Phase 1 = Mode 1 end-to-end (upload, ready list, register fields, import + host Save). Autopilot out. *Whether AI study is wave 1 vs 1b still open under P11b* |

### Still open — answer before Phase 2

#### P1 — Primary host focus — **Decision Required**

| Option | Meaning |
|--------|---------|
| **P1-A** | **Simplicity-first** presets; other hosts later |
| **P1-B** | **Generic any-page** from day one |
| **P1-C** | **Hybrid** — generic engine + presets (Simplicity / HAPA as examples) |

#### P2 — Extension Documate auth (MVP) — **Decision Required**

| Option | Meaning |
|--------|---------|
| **P2-A** | API key only |
| **P2-B** | Block until Iden human login |
| **P2-C** | Interim API key → migrate to Iden |

#### P3 — Definition ownership — **Decision Required**

| Option | Meaning |
|--------|---------|
| **P3-A** | Per-user within Business |
| **P3-B** | Per-Business shared |
| **P3-C** | Per-user + optional publish-to-Business |

#### P4 — Page identity — **Decision Required**

| Option | Meaning |
|--------|---------|
| **P4-A** | URL pattern only |
| **P4-B** | URL + DOM fingerprint |
| **P4-C** | Manual select definition |
| **P4-D** | Auto-suggest + user confirm before fill |

#### P5 — AI field-study locus — **Decision Required**

| Option | Meaning |
|--------|---------|
| **P5-A** | Server-side Documate LLM |
| **P5-B** | Client-side only |
| **P5-C** | Hybrid heuristics + server AI |

#### P6 — Mapping contract — **Decision Required**

| Option | Meaning |
|--------|---------|
| **P6-A** | `ResultJson` key → selector + input kind |
| **P6-B** | Via canonical Documate field ids |
| **P6-C** | Also pin document type / schema version hash *(combinable with A/B)* |

#### P8 — List UX — **Decision Required**

| Option | Meaning |
|--------|---------|
| **P8-A** | Document-centric (`ready`) |
| **P8-B** | File-centric expand |
| **P8-C** | Both |

#### P9 — Packaging — **Decision Required**

| Option | Meaning |
|--------|---------|
| **P9-A** | `apps/extension` in monorepo |
| **P9-B** | Separate repo |
| **P9-C** | Monorepo MVP; extract later |

#### P10 — Browser MVP — **Decision Required**

| Option | Meaning |
|--------|---------|
| **P10-A** | Chrome MV3 only |
| **P10-B** | Chrome + Edge |
| **P10-C** | Chrome + Firefox |

#### P11b — AI study timing within Mode 1 — **Decision Required**

| Option | Meaning |
|--------|---------|
| **P11b-A** | Mode 1 Wave 1: **manual** field register only; AI study Wave 2 |
| **P11b-B** | Mode 1 ships **with** AI study + confirm from the start |
| **P11b-C** | Heuristics-only first; AI optional later |

#### P12 — Statement page — **Decision Required**

| Option | Meaning |
|--------|---------|
| **P12-A** | Statement = another importable page |
| **P12-B** | Blocked until §3.2 reconciliation exploration |
| **P12-C** | Page fill OK; reconciliation out of scope |

#### P13 — Manage definitions outside extension — **Decision Required**

| Option | Meaning |
|--------|---------|
| **P13-A** | Extension only |
| **P13-B** | Extension + Customer web |
| **P13-C** | Web-first; extension runtime only |

#### P14 — After successful host Save — **Decision Required**

| Option | Meaning |
|--------|---------|
| **P14-A** | No Documate status change (list still shows ready) |
| **P14-B** | Mark Document **imported** / hide from default ready list |
| **P14-C** | Soft flag + filter; reversible |

---

## 6. Recommended Direction

| Topic | Recommendation | Why |
|-------|----------------|-----|
| Host posture | **M0 locked** | Product differentiator vs code integrations |
| Capability cut | **Mode 1 only** | Autopilot deferred per developer |
| Target | **P1-C** | Generic engine; Simplicity/HAPA as early hosts |
| Auth | **P2-C** | Unblock before Band 15 |
| Ownership | **P3-B** or **P3-C** | Shared ops definitions |
| Page match | **P4-D** | Safer before fill+Save |
| AI locus | **P5-C** / **P5-A** | Keys & audit on Documate |
| Mapping | **P6-A + schema pin** | Simple + breakage detection |
| Import | **P7-D locked** | Fill + host UI Save; not Mode 2 API |
| List | **P8-A** | Document = Result atom |
| Package | **P9-A** / **P9-C** | Shared API contracts |
| Browser | **P10-B** | Chrome + Edge |
| AI timing | **P11b-B** or **P11b-A** | Prefer B if study is core to “register page”; A if schedule risk |
| Statement | **P12-C** | |
| Manage defs | **P13-B** | |
| Post-import | **P14-B** or **P14-C** | Avoid re-import duplicates |

**Mode 1 architecture sketch (conceptual — Phase 2 owns detail):**

```text
[Extension]
  Documate auth → upload → list ready Documents
  On host page: Register (field study) → confirm → save definition to Documate
  Import: ResultJson → DOM fill → trigger host page Save

[Documate API]
  existing: upload + documents + ResultJson
  new: PageImportDefinition CRUD (Mode 1 field maps only)
  new (if AI): study endpoint for field proposals
  later (not Phase 1): SubmitApiDefinition (Mode 2), CredentialVault (Mode 3)
```

---

## 7. Exploration Exit Criteria

Phase 1 exploration is **complete** when:

1. **M0–M3, P7, P11 (Mode 1 cut)** remain accepted as locked.  
2. Remaining **P1–P6, P8–P10, P11b, P12–P14** answered or explicitly deferred with defaults.  
3. Agreement: page-import definitions (Mode 1) ≠ Mode 2 submit-API study ≠ Plan 01 mapping catalogs.  
4. Developer approves starting **Phase 2 — Implementation plan** (Mode 1 only).

---

## Phase-end contract

### Finalized Decisions

| # | Decision | Notes |
|---|----------|-------|
| **M0** | Zero host-app code/change | Plug-and-play via browser extension only |
| **M1** | Product Phase 1 = Mode 1 Assisted page fill | Open page → paste fields → host UI Save |
| **M2** | Mode 2 Session Autopilot deferred | Study host submit API later |
| **M3** | Mode 3 Credential Autopilot deferred | Later stage; highest trust bar |
| **P7-D** | Fill + host page Save | Not silent host API create |
| **P11** | Autopilot out of Product Phase 1 | Mode 1 end-to-end only |

### Pending Decisions

P1–P6, P8–P10, P11b, P12–P14.

### Assumptions

- “Ready to import” = Document status **`ready`** + `ResultJson`.  
- Host Save = same control the user would click on the page (Mode 1), not Mode 2 API replay.  
- Extension complements External API; it does not replace customers who want code integrations.  
- Simplicity / HAPA are **example** hosts, not exclusive forever (pending P1).

### Risks

See §4 — highest for Mode 1: fragile DOM, wrong-page fill+Save, auth gap, AI/DOM privacy.

### Readiness

**Blocked** — Mode cut locked; still need remaining opens (esp. P1, P2, P11b) then developer approval for Phase 2 (Mode 1 implementation plan only).

---

## Changelog

| Date | Change |
|------|--------|
| 2026-08-31 | Initial Phase 1 exploration draft. |
| 2026-09-01 | Reframed: zero host change; Modes 1–3; Product Phase 1 = Mode 1 only; Autopilot deferred; P7-D fill+host Save; P11b for AI timing. |
