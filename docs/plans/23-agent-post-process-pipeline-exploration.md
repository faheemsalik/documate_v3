# 23 — Agent post-process pipeline (Exploration)

> **Status:** Phase 1 — 🔄 draft  
> **Type:** Product exploration  
> **Track:** Product  
> **Related:** Plan 22 (PP1 locked post-process as prompt text; MCP runner unused on extract); Plan 01 §13 Agent-primary post-process (historical MCP workflow)  
> **Downstream:** Phase 2 / Phase 3 — **not started** (implementation deferred)

**Outcome (intent):** Stop relying on the extract LLM to honor free-text post-process rules. After extraction, Documate runs a **structured, allowlisted scheme** against `ResultJson`. Users keep simple natural-language instructions for editing; a **Generate scheme** action asks an LLM to turn that text into executable JSON; the scheme is stored on the agent and applied in code at runtime.

---

## Planning flow

| Phase | Document |
|-------|----------|
| 1 — Exploration | **This file** 🔄 |
| 2 — Implementation plan | — (deferred until Phase 1 verified + approved) |
| 3 — Dispatch queue | — (deferred; **no DQ in this file**) |

---

## 1. Problem Framing

Today (Plan 22 / PP1), post-process is **extra prompt text** appended into the extract user message. That is unreliable: the model may ignore rules, partially apply them, or invent transforms. Deterministic business rules (conditional maps, math, regex extract, multi-value transforms) belong in a **post-extract pipeline**, not in the extract prompt.

Legacy MCP workflow post-process (`IAgentPostProcessRunner` + `DefaultWorkflowId`) remains in the tree but is **not called** from `DocumentExtractStage`. Customer UI already describes the textarea as prompt-append, not a workflow.

---

## 2. Scope

### Goals

- User writes **simple NL** post-processing instructions; text is saved **as-is** for later editing.
- User can **Generate final scheme** (name TBD): NL → LLM → required JSON scheme Documate can execute in code.
- UX copy tells the user we will **refine** their text to be more precise/meaningful when generating.
- Persist generated scheme on agent (and optionally template defaults) as **text** for runtime.
- After extract (+ schema validate), apply scheme to `ResultJson`.
- Scheme design must **grow**: conditional ops, map/transform multi-values, divide/math, regex extract, etc.
- **Security:** allowlist ops only — no arbitrary code / script / eval.

### Non-goals (this exploration / likely v1)

- Re-enabling MCP `AgentPostProcessRunner` as the primary path (may stay dormant or be retired later — Decision Required).
- Arbitrary scripting (JS, C#, Python, jq full language).
- Customer-authored raw JSON scheme editing as the primary UX (may be advanced/admin later).
- OCR / classify post-process (extract path only).
- Implementation, migrations, or dispatch queue items in this document.

### In / out

| In | Out |
|----|-----|
| Product options for scheme DSL, storage, generate UX, runtime, security | Engineering folder/CQRS redesign |
| Mapping current `PostProcessPrompt` / MCP / extract compose | Production code changes |
| Open decisions for Phase 2 | DQ entries |

---

## 3. Current-State Findings

### Prompt-append path (live)

- [`ExtractPromptComposer`](../../apps/api/Infrastructure/Extract/ExtractPromptComposer.cs) appends a block when `postProcessPrompt` is non-empty:

  ```
  Additional post-process instructions:
  {postProcessPrompt}
  ```

- [`DocumentExtractStage`](../../apps/api/Infrastructure/Pipeline/Stages/DocumentExtractStage.cs) passes `agent.PostProcessPrompt` into extract; stores raw LLM `ResultJson`; schema-validates; marks Ready. **No post-extract transform step.**
- Customer agent edit — Post-process tab: textarea bound to `postProcessPrompt`; hint: *“This text is added to the user prompt at extract time. It is not a post-extract workflow.”*

### MCP runner (unused on extract)

- [`AgentPostProcessRunner`](../../apps/api/Infrastructure/PostProcess/AgentPostProcessRunner.cs): if `OpsAgent.DefaultWorkflowId` set, loads `CorWorkflowDefinition.DefinitionJson`, invokes internal MCP tools that mutate a `JsonObject` payload.
- Still registered in DI; **not invoked** from extract stage (Plan 22 PP1 / DQ-190x).

### Tables / fields

| Entity | Field | Role today |
|--------|-------|------------|
| `OpsAgent` | `PostProcessPrompt` (`nvarchar(max)`) | NL / free text; appended to extract prompt |
| `OpsAgent` | `DefaultWorkflowId` | Legacy MCP workflow FK; clone sets null |
| `CorAgentTemplate` | `DefaultPostProcessPrompt` | Copied to agent on clone |
| `OpsDocument` | `ResultJson` | Extract output; validated against `OutputSchemaJson` |
| `CorWorkflowDefinition` | `DefinitionJson` | MCP step list (legacy) |

No column today for a generated executable scheme.

### Product tension vs Plan 22 PP1

PP1 redefined post-process as **prompt text**. This exploration **supersedes that product direction** for runtime behavior: NL remains the authoring surface; execution moves to a structured scheme. Phase 2 must explicitly revise compose behavior (omit post-process from extract prompt once scheme exists, or always — Decision Required).

---

## 4. Risks and Constraints

| Risk | Note |
|------|------|
| LLM generates invalid / unsafe scheme | Need validate-before-save + allowlist; reject unknown ops |
| Scheme diverges from NL text | User edits NL without regenerating → stale scheme |
| Post-process breaks schema validity | Transform may invalidate `OutputSchemaJson` |
| Overwrite of good scheme | Need confirm-before-overwrite / regen UX |
| Expressiveness creep | Unbounded DSL → hard to secure and support |
| Dual systems | Prompt-append + scheme + dormant MCP confuse operators |
| Reprocess / idempotency | Applying scheme twice to already-transformed JSON |

---

## 5. Options

### 5.1 JSON scheme design (DSL)

Compare approaches for an executable, versionable scheme.

#### Option A — Ordered op-array pipeline (recommended lean)

```json
{
  "schemeVersion": 1,
  "ops": [
    { "op": "set", "path": "$.total", "from": "$.amount", "expr": "divide", "by": 100 },
    { "op": "map", "path": "$.lines[*].taxCode", "when": { "eq": ["$.type", "invoice"] }, "mapping": { "S": "STANDARD", "Z": "ZERO" } },
    { "op": "regex_extract", "path": "$.invoiceNumber", "from": "$.rawRef", "pattern": "INV-(\\d+)", "group": 1 }
  ]
}
```

| Pros | Cons |
|------|------|
| Easy to allowlist (`op` enum); sequential mental model | Nested conditionals / reuse can get verbose |
| LLM often generates arrays of steps reliably | Cross-field graphs less elegant than AST |
| Simple versioning (`schemeVersion` + op catalog version) | Need careful path language (JSONPath subset) |
| Matches “pipeline” product story | |

#### Option B — JSONLogic-like tree

Rules as nested `{ "and": [ … ] }` / `{ "var": "total" }` style trees applied to the document.

| Pros | Cons |
|------|------|
| Compact conditionals; existing mental models | Harder for LLM to emit correctly at scale |
| Good for if/then without inventing syntax | Full JSONLogic is large; need a **strict subset** |
| | Debugging “which branch ran” is harder than step lists |

#### Option C — jq-subset (filter string or AST of jq)

Store a constrained jq program or jq-like AST.

| Pros | Cons |
|------|------|
| Extremely expressive for JSON transforms | Security / sandboxing harder; subset definition is work |
| Power users already know jq | LLM reliability mixed; opaque failure messages |
| | Versioning and allowlisting ops is indirect |

#### Option D — Expression AST (typed nodes)

Full tree of `Binary`, `Call`, `PathRef`, `Literal` nodes; optional separate “statements” list for assignments.

| Pros | Cons |
|------|------|
| Maximum control and type-checkability | Heaviest for LLM generation and schema docs |
| Strong validation / IDE later | Higher implementation cost for v1 |

**Direction hint (non-binding):** Prefer **Option A** for v1 (allowlist + LLM reliability + growth via new `op` values). Borrow conditional primitives from B inside individual ops (`when`). Defer C/D unless expressiveness gaps appear after a spike in Phase 2.

---

### 5.2 Where to store

Keep **two artifacts**: user NL (editable) + generated scheme (runtime).

| Option | Storage | Pros | Cons |
|--------|---------|------|------|
| **S1** | Add `PostProcessSchemeJson` (name TBD) on `OpsAgent` + `DefaultPostProcessSchemeJson` on `CorAgentTemplate`; keep `PostProcessPrompt` as NL | Clear split; clone can copy both | Two columns to sync on template push/clone |
| **S2** | Single JSON blob `{ "nl": "…", "scheme": {…}, "meta": … }` replacing prompt column | One column | Breaks current string APIs; harder partial updates |
| **S3** | Child table `OpsAgentPostProcessRevision` (history of NL + scheme) | Version history / audit | More product + eng surface |

**Versioning (product):**

- Embed `schemeVersion` inside the scheme JSON (DSL version).
- Optional `generatedAt` / `sourcePromptHash` so UI can warn “NL changed since last generate”.
- Agent `SchemaVersion` is **output schema**, not post-process — do not overload it.

**Invalid scheme handling:**

- Reject save if validate fails (unknown op, bad path, bad regex).
- Runtime: if scheme empty → skip; if scheme present but invalid → fail document with dedicated error code (e.g. `post_process_invalid`) vs soft-skip — Decision Required.
- Template defaults: clone copies NL + scheme; admin template may ship starter NL without scheme until generate.

**Direction hint:** **S1** for v1; history (**S3**) later if needed.

---

### 5.3 LLM generate UX

| Element | Options |
|---------|---------|
| Entry | Button on Post-process tab: **Generate final scheme** / **Generate scheme** (copy TBD) |
| Expectation | Inline notice: we refine NL into more precise instructions, then into an executable scheme |
| Preview | Show **refined NL** (optional) and/or **scheme JSON** (pretty) before commit |
| Confirm | Confirm dialog when overwriting an existing scheme |
| Regen | Same button; confirm overwrite; keep prior NL unless user edited |
| Advanced | Optional “view / edit scheme JSON” — v1 off by default? |

**Flows (product):**

1. Edit NL → Save (NL only; scheme unchanged; optionally badge “scheme out of date”).
2. Generate → LLM → preview → Confirm → persist scheme (+ optional refined NL — Decision Required: overwrite NL or store refined separately?).
3. Extract runtime uses **scheme only** (once product locks “omit from prompt”).

---

### 5.4 Runtime executor

**Placement (conceptual):** After extract JSON parses and **after** (or **before**) schema validate — Decision Required.

| Placement | Pros | Cons |
|-----------|------|------|
| After validate | Fail fast on bad extract; post-process only on schema-valid data | Transform may then break schema |
| Before validate | One validate of final payload | Post-process may “fix” invalid extract and hide model errors |
| Validate → post-process → re-validate | Safest product story | Slightly more work / failure modes |

**Failure modes:**

- Empty / missing scheme → no-op (document Ready as today).
- Op failure (path missing, divide by zero, regex no match) → policy: fail document vs leave field unchanged — Decision Required (per-op or global).
- Partial apply → prefer **transactional**: all ops succeed or none written (or write + mark failed — Decision Required).

**Idempotency:**

- Prefer ops that are safe if re-run on already-processed JSON, **or** store raw extract separately and always apply scheme to raw (stronger; needs artifact/storage decision).
- Reprocess should re-run extract then scheme, not scheme-only on final JSON, unless product adds “re-apply scheme” later.

**Relationship to MCP runner:** either leave dormant, delete in a later eng task, or adapt MCP tools as backends for allowlisted ops — Decision Required; product default = **new code executor**, not MCP.

---

### 5.5 Security

- **No** arbitrary code execution, `eval`, scripting hosts, or unrestricted jq.
- **Allowlist** of `op` names and argument shapes; reject unknown fields where practical.
- Regex: ReDoS limits (timeout / complexity / length caps) — eng detail in Phase 2.
- Paths: constrained JSONPath subset (no recursive wildcards that explode memory).
- Generate endpoint: authenticated customer/admin only; scheme validated server-side before persist.
- Do not send secrets into generate prompt; scheme must not call external HTTP.

---

## 6. Open Questions — Decision Required

| ID | Question | Options (short) |
|----|----------|-----------------|
| **PP-DSL1** | Scheme shape for v1? | A op-array · B JSONLogic-subset · C jq-subset · D expression AST |
| **PP-DSL2** | Path language? | Dot paths only · JSONPath subset · JSON Pointer |
| **PP-ST1** | Storage layout? | S1 dual columns · S2 single blob · S3 revision table |
| **PP-ST2** | Column / field names? | e.g. keep `PostProcessPrompt` + add `PostProcessSchemeJson` (names TBD) |
| **PP-UX1** | On generate, overwrite NL with refined text? | Overwrite · keep original + store refined aside · show refined only in preview |
| **PP-UX2** | Require confirm + preview before save? | Always · only when scheme exists · skip for empty |
| **PP-UX3** | Allow manual scheme JSON edit in v1? | No · admin only · customer advanced |
| **PP-RT1** | Validate vs post-process order? | After · before · validate → PP → re-validate |
| **PP-RT2** | Op / scheme failure policy? | Fail document · skip op · skip all remaining |
| **PP-RT3** | Keep raw extract for re-apply? | Only final `ResultJson` · also store pre-PP artifact |
| **PP-CMP1** | Omit `PostProcessPrompt` from extract compose once scheme exists? | Always omit NL from prompt · omit only if scheme present · keep both (discouraged) |
| **PP-LEG1** | Fate of MCP `IAgentPostProcessRunner` / `DefaultWorkflowId`? | Leave dormant · remove later · bridge ops to MCP |
| **PP-TMP1** | Template defaults: ship scheme on template or generate per clone? | Default scheme on template · NL only · both |

---

## 7. Recommended Direction (draft — not locked)

Non-binding until developer locks Decision Required rows:

1. **DSL:** Option **A** (op-array pipeline) with `schemeVersion`, allowlisted ops, optional `when` on steps.
2. **Storage:** **S1** — keep NL in `PostProcessPrompt`; add scheme text column on `OpsAgent` / template default.
3. **UX:** Generate button + refine notice + scheme preview + confirm overwrite; warn when NL hash ≠ last generate.
4. **Runtime:** validate → apply scheme → **re-validate**; fail document on scheme/op errors (strict v1).
5. **Compose:** once scheme present, **omit** post-process NL from extract prompt (**PP-CMP1** lean).
6. **MCP:** leave dormant; do not build on it for v1.
7. **Implementation deferred** — no Phase 2 / DQ until this exploration is verified.

---

## 8. Exploration Exit Criteria

- [ ] Current prompt-append + unused MCP path documented (done in §3).
- [ ] DSL options compared (A–D) with pros/cons.
- [ ] Storage / UX / runtime / security options listed.
- [ ] Decision Required table answered by developer.
- [ ] Explicit supersession of Plan 22 **PP1 runtime meaning** acknowledged.
- [ ] Developer verifies Phase 1 and approves start of Phase 2 (implementation plan only).

---

## Explicit deferrals

- **No implementation plan** in this file.
- **No dispatch queue** in this file.
- **No application code** changes as part of this exploration.

---

## Agent output contract (Phase 1 end)

### Finalized Decisions

_None yet — Phase 1 draft._

### Pending Decisions

All **PP-DSL1…PP-TMP1** in §6.

### Assumptions

- Product track only; eng conventions stay under `docs/architecture/`.
- Extract path only; customer agent Post-process tab is the primary authoring surface.
- LLM used for **scheme generation**, not for applying transforms at document runtime.
- Allowlisted executor runs in-process in the API pipeline after extract.

### Risks

- Stale scheme vs edited NL; schema invalidation after transforms; ReDoS / path blowups; confusion with Plan 22 PP1 and dormant MCP.

### Readiness

**Blocked** — awaiting developer review of options and answers to Decision Required; then verify Phase 1 and approve Phase 2.

---

**Next gate:** Please review this exploration, answer §6 decisions (or mark defer), then approve writing the Phase 2 implementation plan only.
