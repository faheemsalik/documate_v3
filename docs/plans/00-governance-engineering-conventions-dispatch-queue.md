# Plan 00 — Governance & Engineering Conventions — Dispatch Queue

> **Document type:** Dispatch queue (Phase 3)  
> **Status:** ✅ Complete (batched execution per developer request)  
> **Source plan:** `docs/plans/00-governance-engineering-conventions-implementation-plan.md`  
> **Scope:** Create eng governance docs, patterns, and Cursor rules only  
> **Out of scope:** Product design, app scaffolding code, Iden implementation  

**Status legend:** ✅ Complete · 🔄 In Progress · ⬜ Ready · ⏸ Parked · ❌ Cancelled

---

## Completion Summary

| Metric | Value |
|--------|--------|
| Total DQ items | 16 |
| ✅ Complete | 16 |
| 🔄 In Progress | 0 |
| ⬜ Ready | 0 |
| ⏸ Parked | 0 |
| ❌ Cancelled | 0 |

**Locked decisions:** A2, B2, C1, D1, E1.

**Angular B2 defaults applied in DQ-0501:** standalone components; signals + services (no NgRx by default); reactive forms.

---

## Dispatch Index

| DQ | Band | Title | Status | Depends on |
|----|------|--------|--------|------------|
| DQ-0001 | 00 | Plans README + eng vs product track router | ✅ | — |
| DQ-0301 | 03 | Port full `docs/plans/00-governance/` (D1) | ✅ | DQ-0001 |
| DQ-0101 | 01 | Architecture README + folder-structure (A2) | ✅ | DQ-0001 |
| DQ-0102 | 01 | Naming conventions | ✅ | DQ-0101 |
| DQ-0201 | 02 | Critical rules + quick-ref **API** (C1) | ✅ | DQ-0101 |
| DQ-0202 | 02 | Prompt clarification | ✅ | DQ-0001 |
| DQ-0701 | 07 | Preservation rules (`old_code`) | ✅ | DQ-0001 |
| DQ-0401 | 04 | CQRS feature-slice + MediatR patterns | ✅ | DQ-0101, DQ-0102 |
| DQ-0501 | 05 | Angular conventions suite (B2) | ✅ | DQ-0101 |
| DQ-0203 | 02 | Critical rules + quick-ref **Web** | ✅ | DQ-0501 |
| DQ-1101 | 11 | Shared packages policy | ✅ | DQ-0101 |
| DQ-0801 | 08 | OpenAPI / client-gen conventions | ✅ | DQ-0101 |
| DQ-0901 | 09 | Infrastructure conventions | ✅ | DQ-0101 |
| DQ-0601 | 06 | Auth/Iden **constraints** | ✅ | DQ-0201 |
| DQ-1001 | 10 | Auth wiring placeholders | ✅ | DQ-0601 |
| DQ-0002 | 00 | Install `.cursor/rules/*.mdc` (E1) | ✅ | docs above |
| DQ-0003 | 00 | Smoke check | ✅ | DQ-0002 |

---

## DQ evidence (batched)

### DQ-0001 ✅
- **Evidence:** Created `docs/plans/README.md` (eng vs product tracks).

### DQ-0301 ✅
- **Evidence:** Created `docs/plans/00-governance/` README + 01–09 (Documate DQ bands 00–11).

### DQ-0101 ✅
- **Evidence:** `docs/architecture/README.md`, `patterns/folder-structure.md` (A2 tree).

### DQ-0102 ✅
- **Evidence:** `patterns/naming-conventions.md`.

### DQ-0201 ✅
- **Evidence:** `governance/critical-rules-api.md`, `governance/quick-reference-api.md`.

### DQ-0202 ✅
- **Evidence:** `governance/prompt-clarification.md`.

### DQ-0701 ✅
- **Evidence:** `governance/preservation-rules.md`.

### DQ-0401 ✅
- **Evidence:** `patterns/cqrs-feature-slice.md`, `patterns/mediatr-conventions.md`.

### DQ-0501 ✅
- **Evidence:** `patterns/angular-conventions.md`, `angular-feature-structure.md`, `angular-state-and-forms.md`. Defaults: standalone, signals+services, reactive forms.

### DQ-0203 ✅
- **Evidence:** `governance/critical-rules-web.md`, `governance/quick-reference-web.md`.

### DQ-1101 ✅
- **Evidence:** `patterns/shared-packages-policy.md`.

### DQ-0801 ✅
- **Evidence:** `patterns/openapi-and-clients.md`.

### DQ-0901 ✅
- **Evidence:** `patterns/infrastructure-conventions.md`.

### DQ-0601 ✅
- **Evidence:** `governance/iden-constraints.md`.

### DQ-1001 ✅
- **Evidence:** `governance/auth-wiring-placeholder.md`.

### DQ-0002 ✅
- **Evidence:** `.cursor/rules/` — `00-domain-and-entry`, `05-clarify-before-assuming`, `06-plan-sequence-gates`, `planning-router`, `10-architecture-api-cqrs`, `20-critical-api-rules`, `21-critical-web-rules`, `90-memory-and-workflow`.

### DQ-0003 ✅ — Smoke check
| Check | Result |
|-------|--------|
| Architecture README routes to governance + patterns | Pass |
| API critical rules present; no product agent/queue schema policy | Pass |
| Web critical rules + angular patterns present | Pass |
| `00-governance` phase gates + Documate bands | Pass |
| Plans README separates eng vs product | Pass |
| Cursor rules use Documate paths only | Pass |
| No `apps/api` / `apps/web` scaffold (docs-only queue) | Pass (intentional) |

---

## Readiness

**Plan 00 engineering governance: complete.**  
Next work: product plans (e.g. document queue design) or scaffold DQs for `apps/api` / `apps/web` (separate plan).
