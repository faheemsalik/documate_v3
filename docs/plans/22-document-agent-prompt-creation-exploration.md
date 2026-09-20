# 22 — Document Agent prompt creation (Exploration)

> **Status:** Phase 1 — **complete** (verified 2026-09-20) → Phase 2 ✅ → Phase 3 filed  
> **Type:** Product exploration  
> **Track:** Product  
> **Downstream:** [22-document-agent-prompt-creation-implementation-plan.md](./22-document-agent-prompt-creation-implementation-plan.md) · [22-document-agent-prompt-creation-dispatch-queue.md](./22-document-agent-prompt-creation-dispatch-queue.md)

**Outcome:** Make extract prompts inspectable and steerable: compose from template system prompt + agent instructions + schema comments + post-process text; persist the last sent prompt for 7 days; show a **redacted** history on the customer files page; let admin manage templates and inspect any tenant/business agent’s **full** composed prompt. Customers never see the system prompt.

---

## Planning flow

| Phase | Document |
|-------|----------|
| 1 — Exploration | **This file** ✅ |
| 2 — Implementation plan | [22-document-agent-prompt-creation-implementation-plan.md](./22-document-agent-prompt-creation-implementation-plan.md) ✅ |
| 3 — Dispatch queue | [22-document-agent-prompt-creation-dispatch-queue.md](./22-document-agent-prompt-creation-dispatch-queue.md) ⬜ Band 19 |

---

## 1. Problem Framing

Operators cannot see or control the prompt that actually runs extraction. The assembled prompt is discarded after the LLM HTTP call. The customer Post-process tab is an MCP workflow checkbox, not extra prompt text. Schema fields have no comments. Admin cannot edit templates or inspect a tenant agent’s composed prompt.

---

## 2. Scope

**In:** extract-prompt composition, persistence (7-day child table), customer redacted files history, customer post-process textarea, schema `description` comments, compose preview, admin template CRUD + optional push to clones, admin cross-tenant agent list.

**Out:** deleting the MCP runner class; OCR/classify prompts; customer FE redesign; reprocess / N-attempt extract UX.

---

## 3. Current-State Findings

- Composition: [`LiveLlmDocumentExtractAdapter`](../../apps/api/Infrastructure/Extract/LiveLlmDocumentExtractAdapter.cs) — hardcoded system string + `OpsAgent.Instructions` + schema JSON + OCR text; prompt not stored.
- Post-process: [`AgentPostProcessRunner`](../../apps/api/Infrastructure/PostProcess/AgentPostProcessRunner.cs) mutates `ResultJson` via MCP after extract. Customer UI is a checkbox on [`agent-edit.page.html`](../../apps/web/src/app/features/agents/pages/agent-edit.page.html).
- Templates: [`CorAgentTemplate`](../../apps/api/Domain/CorAgentTemplate.cs) seeded; customer clone-only; no admin CRUD (Plan 11 INT-16 deferred).
- Schema builder: key / type / required only — no `description`.
- Files / admin ops document detail: `ResultJson` only.

---

## 4. Risks and Constraints

- Customer `/api/app` must never return `SystemPrompt` or stored `SystemPromptText`.
- Template push can surprise tenants — confirm with clone count.
- 7-day purge removes investigation data by design.
- `nvarchar(max)` on `OpsFiles` would hurt file-grid queries — child table only.

---

## 5. Open Questions

None. P1–P5 and PP1–PP7 locked below.

---

## 6. Recommended Direction

See implementation plan. Summary: snapshot system prompt onto `OpsAgent` at clone; optional push on template save; shared composer; child table last prompt per document; customer sees user-prompt only.

---

## 7. Exploration Exit Criteria

- [x] Current composition and storage mapped.
- [x] Post-process redefined (prompt text; MCP unused).
- [x] Prompt layers locked; customer never sees system prompt.
- [x] P1–P5 locked including child table + 7-day retention.
- [x] Phase 2 approved (“go ahead”, 2026-09-20).

---

## Locked decisions

| ID | Decision |
|----|----------|
| **PP1** | Post-process = extra prompt text appended to extract. MCP UI removed; runner unused in extract path. |
| **PP2** | System prompt lives on the **platform template** (admin view/edit). Customer edits Instructions + Post-process + schema comments. |
| **PP3** | Sent prompt stored for investigation; customer files surface is **redacted** (no system). |
| **PP4** | Compose preview on backend: admin = full; customer = redacted. |
| **PP5** | Admin template CRUD + list agents for any tenant/business with composed prompt. |
| **PP6** | Field and section comments included in the composed prompt. |
| **PP7** | Customer never sees system prompt (edit, preview, files history). |
| **P1** | **Snapshot at clone.** On template save, admin may **push** the new system prompt to all clones or leave them. New clones get the current template prompt. |
| **P2** | Exact last prompt in a **child SQL table** (`FileId` + `DocumentId`). Retention **7 days**. |
| **P3** | Last run only on the files UI. |
| **P4** | JSON Schema **`description`** for field, section, and column comments. |
| **P5** | No per-agent system override in v1. Existing agents change only via P1 push. |

### Assumptions

- Extract (not classify/OCR) is in scope.
- Files page = file detail (per child document) + document detail.
- Push updates **system prompt only** (not customer-owned instructions/schema/post-process).

### Risks

Covered in §4.

### Readiness

**Phase 1 complete.** Phase 2 approved. Phase 3 Band 19 filed.
