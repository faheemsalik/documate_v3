# Plan 00 — Governance & Engineering Conventions — Implementation Plan

> **Document type:** Implementation plan (Phase 2)  
> **Status:** Promoted to Phase 3 — see dispatch queue  
> **Scope:** Coding conventions, folder structure, design patterns, plan-writing rules, Cursor rules, architecture governance docs  
> **Out of scope:** Product behavior, queue/agent schemas, Iden product UX, feature delivery  

**Upstream:** Exploration decisions locked in chat (Plan 00 Phase 1).  
**Downstream:** Phase 3 dispatch queue will create the files listed here.  
**Product design:** Separate plan (e.g. project design / queue design) — agents must not mix product decisions into this plan’s execution.

---

## Metadata

| Field | Value |
|-------|--------|
| Plan id | `00-governance-engineering` |
| Apps in scope | `apps/api` (.NET 10), `apps/web` (Angular) |
| Plans root | `docs/plans/` |
| Architecture root | `docs/architecture/` |
| Cursor rules | `.cursor/rules/` |

---

## Delivery Principles

1. **Engineering only** — no product rules in governance docs (no agent/queue/schema product policy).
2. **Docs are source of truth** — `.mdc` files stay short and link to `docs/`; do not duplicate long policy in Cursor rules.
3. **Adapt ERP, don’t clone** — reuse plan lifecycle / phase gates / evidence; rewrite API rules for Documate’s single-project feature CQRS.
4. **One concern per artifact** — one Cursor rule ≈ one concern; pattern shards under `docs/architecture/patterns/`.
5. **Preserve `old_code` until cutover** — reference-only; do not extend; deletion is a later DQ after cutover.
6. **No production app scaffolding in Plan 00** unless a DQ explicitly creates empty folder placeholders — prefer docs that describe the tree first.
7. **Three planning phases remain mandatory** for all future work — encoded in `docs/plans/00-governance/` + always-on Cursor rules.

---

## Domain Architecture Layer (engineering)

### Target repo layout

```text
documate_v3/
  apps/
    api/                          # single .NET 10 Web API project
      Domain/                     # all entities + base types (no feature logic)
      Modules/                    # A2: features live inside modules
        Core/
          Features/{FeatureName}/ # Commands, Queries, Dtos, handlers
        FrontendSupport/
          Features/{FeatureName}/
            {FeatureName}Controller.cs  # thin — MediatR only
            Commands/
            Queries/
            Dtos/
        External/
          Features/{FeatureName}/
      Infrastructure/             # DbContext, Iden client, storage, providers, email
      Program.cs
    web/                          # Angular app (B2: full conventions documented)
      src/app/
        core/                     # singleton services, auth, interceptors
        shared/                   # reusable UI primitives
        features/                 # feature modules/routes
  tests/
    api/                          # .NET test project(s)
  docs/
    plans/
      00-governance/              # plan-writing rules (ported from ERP)
      …                           # future exploration / impl / DQ docs
    architecture/
      README.md                   # entry router
      governance/                 # critical rules, quick refs, clarify, preserve
      patterns/                   # CQRS, folders, naming, MediatR, Angular
  .cursor/rules/                  # short always-on / glob rules
  old_code/                       # reference only until cutover
```

### Locked architecture rules (encode in critical-rules + patterns)

| Rule | Detail |
|------|--------|
| Single API project | All .NET code for the API host lives under `apps/api` (one csproj). |
| Domain placement | Entities only under `Domain/`; never returned as API contracts. |
| CQRS | MediatR commands/queries; handlers colocated in the feature folder. |
| Feature folders | One feature folder owns controller + commands + queries + DTOs + validators. |
| Controllers | Thin: bind → send MediatR → map result. No `DbContext`, no repositories. |
| Modules | Core / FrontendSupport / External are **code boundaries** (folders), not product docs. |
| Web | Angular; feature folders under `apps/web/src/app/features/`. |
| Shared NuGet/TS packages | None for now. Angular consumes API via HTTP/OpenAPI client generation later. |
| Auth | Integrate with Iden (external IdP). Plan 00 only states “do not invent a second identity island”; integration design is another plan. |
| Tests | .NET in `tests/api/`; Angular unit specs colocated (`*.spec.ts`); e2e later under `tests/web-e2e/` if needed. |

### Suggested feature folder example (API)

```text
Modules/FrontendSupport/Features/Queues/
  QueuesController.cs
  Commands/
    CreateQueueCommand.cs
    CreateQueueHandler.cs
  Queries/
    GetQueueByIdQuery.cs
    GetQueueByIdHandler.cs
  Dtos/
    QueueDto.cs
    CreateQueueRequest.cs
```

### Module → folder mapping — DECIDED: A2

| Module | Responsibility (engineering) | Placement |
|--------|------------------------------|-----------|
| **Core** | Extraction pipeline, workers/orchestration, provider adapters | `Modules/Core/Features/{Name}/` |
| **FrontendSupport** | APIs for `apps/web` | `Modules/FrontendSupport/Features/{Name}/` |
| **External** | Partner/integration APIs | `Modules/External/Features/{Name}/` |

Route prefixes (to encode in naming conventions): e.g. `/api/app/...` vs `/api/v1/...`.

---

## Decision A — DECIDED: A2 (features inside modules)

`Modules/{Core|FrontendSupport|External}/Features/{Name}/`

---

## Decision B — DECIDED: B2 (full Angular conventions in Plan 00)

Full Angular style docs (structure, standalone policy, state, forms, HTTP, naming). Still no product UI rules.

---

## Domain Validation Rules (engineering — Decision C — DECIDED: C1)

Enforce V1–V7 via markdown + Cursor only. No custom analyzers/CI architecture tests in Plan 00.

| # | Validation |
|---|------------|
| V1 | Endpoint in feature folder with command/query + DTO |
| V2 | DTOs only — never domain entities on the wire |
| V3 | No DbContext/repos on controllers |
| V4 | Handlers orchestrate Infrastructure |
| V5 | Placement matches folder-structure doc |
| V6 | No unrelated feature edits without ask |
| V7 | Do not modify `old_code` unless DQ says so |

---

## Process Flows — Decision D — DECIDED: D1

Port the **full** ERP plan-governance pack into `docs/plans/00-governance/` (lifecycle, templates, dispatch queue docs, agent/evidence, phase gates), Documate-adapted.

Planning flow:

```text
Exploration → verify → Implementation plan → verify → Dispatch queue → execute items
```

---

## Instruction and Control Set — Decision E — DECIDED: E1

Short Cursor `.mdc` files that **link** to docs (do not embed full policy).

### Docs to create

| Path | Purpose |
|------|---------|
| `docs/plans/00-governance/README.md` | Governance index |
| `docs/plans/00-governance/01-plan-lifecycle.md` | Draft → Active → Promoted → Archived |
| `docs/plans/00-governance/02-exploration-plan-template.md` | Exploration sections |
| `docs/plans/00-governance/03-implementation-plan-template.md` | Implementation sections |
| `docs/plans/00-governance/04-dispatch-queue-template.md` | DQ entry shape |
| `docs/plans/00-governance/05-dispatch-queue-governance.md` | **Documate DQ bands 00–11** |
| `docs/plans/00-governance/06-dispatch-queue-execution.md` | Execute one item |
| `docs/plans/00-governance/07-agent-behavior-and-output-contract.md` | Phase-end output |
| `docs/plans/00-governance/08-evidence-and-completion-rules.md` | Evidence required |
| `docs/plans/00-governance/09-plan-sequence-and-step-gates.md` | Hard gates |
| `docs/architecture/README.md` | Architecture entry router |
| `docs/architecture/governance/critical-rules-api.md` | Authoritative API never-dos |
| `docs/architecture/governance/quick-reference-api.md` | One-page API orientation |
| `docs/architecture/governance/critical-rules-web.md` | Full Angular never-dos (B2) |
| `docs/architecture/governance/quick-reference-web.md` | Angular orientation (B2) |
| `docs/architecture/governance/prompt-clarification.md` | Ask before widening scope |
| `docs/architecture/governance/preservation-rules.md` | `old_code` + unrelated-module touch |
| `docs/architecture/patterns/folder-structure.md` | Authoritative tree |
| `docs/architecture/patterns/cqrs-feature-slice.md` | MediatR feature pattern |
| `docs/architecture/patterns/naming-conventions.md` | Namespaces, files, routes |
| `docs/architecture/patterns/mediatr-conventions.md` | Command/query naming, handlers |
| `docs/architecture/patterns/angular-conventions.md` | Full Angular conventions (B2) |
| `docs/architecture/patterns/angular-feature-structure.md` | Angular feature folder layout (B2) |
| `docs/architecture/patterns/angular-state-and-forms.md` | State + forms conventions (B2) |

### Cursor rules to create

| File | alwaysApply / globs | Role |
|------|---------------------|------|
| `00-domain-and-entry.mdc` | always | Classify API vs Web; entry → `docs/architecture/README.md` |
| `05-clarify-before-assuming.mdc` | always | Ambiguous scope → ask |
| `06-plan-sequence-gates.mdc` | always | 3 phases; stop at gates |
| `planning-router.mdc` | always | Load `docs/plans/00-governance` by phase |
| `10-architecture-api-cqrs.mdc` | `apps/api/**/*.cs` | Feature CQRS placement |
| `20-critical-api-rules.mdc` | `apps/api/**/*.cs` | Link critical-rules-api |
| `21-critical-web-rules.mdc` | `apps/web/**/*.{ts,html,scss}` | Link critical-rules-web |
| `90-memory-and-workflow.mdc` | always | Min scope; no product inventing in eng DQs |

---

## Permissions and Security (engineering scope only)

Plan 00 states **constraints**, not Iden product design:

1. Authentication/authorization for humans and machines is **via Iden** — do not add a parallel user store in Documate.
2. API code must obtain tenant/user context from the validated token / Iden integration abstraction (name TBD in Iden plan) — handlers must not invent ad-hoc tenant filters that bypass that abstraction once it exists.
3. Secrets (provider keys later, webhook secrets) live in configuration/secret store patterns — never hard-coded.
4. Detailed claim names, service accounts vs API keys, and RBAC matrices belong in the **Iden / security project-design plan**, not here.

---

## Dispatch Index (preview — populate fully in Phase 3)

Bands (locked):

| Band | Focus |
|------|--------|
| 00 | Baseline & scope guard |
| 01 | Domain / engineering architecture docs |
| 02 | Validation / critical rules |
| 03 | Process flows (plan governance port) |
| 04 | API patterns (`apps/api`) |
| 05 | Web patterns (`apps/web`) |
| 06 | Permissions & auth **constraints** (Iden pointers only) |
| 07 | Migration / `old_code` preservation |
| 08 | Contracts (eng: OpenAPI/client gen conventions — light) |
| 09 | Infrastructure conventions (light; no cloud blueprint) |
| 10 | Authentication wiring **placeholders** (link out to Iden plan) |
| 11 | Shared packages policy (explicit “none for now”) |

**Phase 3 will create numbered DQ items** such as (illustrative — not executable yet):

| DQ (illustrative) | Outcome |
|-------------------|---------|
| DQ-0001 | Repo scope guard + README pointers |
| DQ-0301 | Port `docs/plans/00-governance/*` |
| DQ-0101 | `docs/architecture/README.md` + folder-structure |
| DQ-0201 | critical-rules + quick-ref API/Web |
| DQ-0401 | CQRS / MediatR pattern shards |
| DQ-0501 | Full Angular conventions set (B2) |
| DQ-0002 | `.cursor/rules/*` install |
| DQ-0701 | preservation-rules for `old_code` |

---

## Wave Sections (implementation order — for Phase 3)

### Wave 0 — Scope guard
- Confirm engineering-only boundary in a short `docs/plans/README.md`.
- Note that `01-project-exploration-mental-design.md` is **product** exploration (separate track).

### Wave 1 — Plan governance port
- Create `docs/plans/00-governance/` from ERP sources; rename ERP → Documate; replace DQ bands with 00–11 table; remove Nx/Angular ERP module maps.

### Wave 2 — Architecture governance + patterns
- Create architecture README, governance docs, pattern shards per Instruction set.
- Apply Decisions A–E.

### Wave 3 — Cursor rules
- Add `.cursor/rules/*.mdc` (E1).
- Verify alwaysApply rules mention Documate paths only.

### Wave 4 — Smoke check
- Agent self-check: open architecture README → critical rules → one pattern; confirm no product policy leaked into eng docs.

---

## Critical rules content outline (API) — to write in Phase 3

Authoritative list (draft; refine when writing file):

1. Never expose domain entities as DTOs.
2. Never inject `DbContext` or repositories into controllers.
3. Controllers only send MediatR messages and return DTOs/results.
4. New endpoints go in feature folders (Commands/Queries/Dtos colocated).
5. Do not create shared packages unless a DQ explicitly adds them.
6. Do not modify `old_code` unless the DQ says so.
7. Do not invent product behavior in engineering DQs — point to product plans.
8. Do not bypass Iden for authn/authz once integration exists.
9. Do not touch unrelated features without explicit request.
10. Prefer matching existing feature slice patterns over new folder styles.

Quick-ref tables: entity vs DTO, feature folder checklist, route prefix map (after Decision A), test locations.

---

## Finalized Decisions (Phase 2 so far)

| # | Topic | Decision |
|---|--------|----------|
| 1 | Plans path | `docs/plans/` |
| 2 | CQRS | MediatR |
| 3 | API shape | One project `apps/api` |
| 4 | Web | Angular `apps/web` |
| 5 | Domain | `Domain/` inside API project |
| 6 | Feature folders | Feature-wise vertical slices **inside modules (A2)** |
| 7 | Modules | Core / FrontendSupport / External; each has its own `Features/` |
| 8 | Shared packages | None for now |
| 9 | Runtime | .NET 10 |
| 10 | Tests | `tests/api/` + Angular colocated specs |
| 11 | Auth | Iden (constraints only in Plan 00) |
| 12 | `old_code` | Reference now; delete after cutover |
| 13 | DQ bands | 00–11 multi-app table |
| 14 | Product scope | Excluded from Plan 00 |
| 15 | Angular docs depth | **B2** — full Angular conventions in Plan 00 |
| 16 | Cursor rules style | **E1** — short `.mdc` + link to docs |
| 17 | Validation enforcement | **C1** — docs + Cursor only |
| 18 | Plan governance port | **D1** — full `00-governance` pack |

### Locked tree for A2

```text
apps/api/
  Domain/
  Modules/
    Core/
      Features/{Feature}/     # Commands, Queries, Dtos, handlers (pipeline-facing)
    FrontendSupport/
      Features/{Feature}/     # UI-facing HTTP features
    External/
      Features/{Feature}/     # partner/integration HTTP features
  Infrastructure/
```

---

## Pending Decisions (must close before Phase 3)

None. A2, B2, C1, D1, E1 are locked.

---

## Assumptions

- Solution file / csproj naming (`Documate.Api`, etc.) can be fixed in a later scaffold DQ.
- OpenAPI client generation for Angular is convention-later, not blocking Plan 00 docs.
- Iden integration details live in a separate plan; Plan 00 only forbids a second identity system.

## Risks

- A2 deeper paths; cross-module DTOs need a clear rule (prefer module-local DTOs; share only via explicit contracts later).
- B2 needs a few Angular defaults (standalone, state library) — surface as mini-decisions when writing those docs if unset.
- Product exploration doc (`01-…`) sits beside eng plans — agents may conflate tracks without a clear `docs/plans/README.md` router.

## Readiness

**Phase 3 created:** `docs/plans/00-governance-engineering-conventions-dispatch-queue.md`

---

## Phase 2 gate

Closed. Continue in the dispatch queue document.
