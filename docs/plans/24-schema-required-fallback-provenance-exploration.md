# 24 — Schema required fallback + field provenance (Exploration)

> **Status:** Phase 1 — 🔄 draft  
> **Type:** Product exploration  
> **Track:** Product  
> **Related:** Plan 22 (schema `description` / captions); Plan 23 (post-process pipeline — parallel exploration); extract path (`DocumentExtractStage` / `JsonSchemaLite`)  
> **Downstream:** Phase 2 / Phase 3 — **not started** (implementation deferred)

**Outcome (intent):** When a schema field is **Required** but the document does not supply usable data, operators can opt to **generate a fallback** (e.g. current date in output format, random serial with digit length + optional prefix) instead of failing the document. Extract output must carry **per-field provenance** so consumers can decide in code (`actual` | `normalized` | `resolved` | `random_generated` — exact enum TBD). Schema/UI configures the policy; runtime always returns provenance when the feature is enabled (scope TBD).

---

## Planning flow

| Phase | Document |
|-------|----------|
| 1 — Exploration | **This file** 🔄 |
| 2 — Implementation plan | — (deferred until Phase 1 verified + approved) |
| 3 — Dispatch queue | — (deferred; **no DQ in this file**) |

---

## 1. Problem Framing

Today, a missing required property fails the document with `schema_invalid` after LLM extract. That is correct for “must be on the page,” but some integrations need a **synthesized stand-in** when the source is silent (missing invoice date → “use today”; missing serial → “generate N-digit code”) so downstream systems keep flowing.

Operators also need **truthfulness**: consumers must know whether a value was read from the document, normalized from a messy extract, resolved by a rule, or randomly generated — without guessing from the value alone.

Plan 23 explores a general post-process pipeline. Fallback generation could live at extract-time, as a dedicated code step, or as post-process ops. This exploration frames those options without locking Plan 23.

---

## 2. Scope

### Goals

- Per-field (or per-agent) configuration: when Required and data is unavailable, **fail** (current) vs **generate fallback** according to type/format rules.
- Generate rules at least for:
  - **Date:** e.g. “now” / current UTC or business-local date, emitted in the field’s `x-documate-outputFormat`.
  - **Number / integer / string-as-serial:** e.g. random digits of length N + optional prefix (and possibly suffix / charset — Decision Required).
- Runtime extract output exposes **provenance per field** so SDK, webhooks, and host code can branch.
- Schema builder (customer + admin template) can configure the policy without raw JSON editing as the primary UX.
- Document interaction with Required validation, formats, and Plan 23 post-process.

### Non-goals (this exploration / likely v1)

- Inventing folder/CQRS / engineering conventions (stay under `docs/architecture/`).
- Full “Document Memory” / master-data resolution as provenance `resolved` (may reserve the enum value for later).
- Arbitrary scripting for generators.
- Changing OCR / classify stages.
- Implementation plan, migrations, or dispatch queue items in this document.
- Guaranteeing cryptographic-quality randomness for security tokens (product serials ≠ secrets).

### In / out

| In | Out |
|----|-----|
| Product options for schema extensions, UI, when generation runs, provenance shape | Engineering layout redesign |
| Grounding in current `JsonSchemaLite` / extract / ResultJson consumers | Production code changes |
| Open Decision Required for Phase 2 | DQ entries |
| Note Plan 23 interaction | Locking Plan 23 DSL |

---

## 3. Current-State Findings

### Required validation (`JsonSchemaLite`)

[`JsonSchemaLite`](../../apps/api/Infrastructure/Extract/JsonSchemaLite.cs) validates agent `OutputSchemaJson` against the LLM instance:

- **Required** = property **key must exist** on the object (`ContainsKey`). Message: `$: missing required property '{name}'`.
- If the key is present, type checks run only when the child node is **non-null**. A present key with JSON `null` **skips** type validation today — so `null` may pass Required even though type is e.g. `string` / `date`.
- No format enforcement for `x-documate-outputFormat` (formats are prompt/schema metadata for the LLM, not runtime validators).
- No notion of “unavailable,” “empty string,” or “generate.”

### Extract stage

[`DocumentExtractStage`](../../apps/api/Infrastructure/Pipeline/Stages/DocumentExtractStage.cs):

1. LLM extract → write `OpsDocument.ResultJson` + artifact.
2. Parse JSON; on failure → `schema_invalid`.
3. `JsonSchemaLite.Validate(agent.OutputSchemaJson, instance)`.
4. On invalid → fail document `schema_invalid` (errors truncated / capped in events).
5. On valid → mark Ready; **no mutation** of ResultJson after LLM (post-process prompt is prompt-append only per Plan 22 PP1; Plan 23 proposes a future code executor).

### Schema builder (customer + admin)

Shared pattern in web/admin schema builder utils:

| Capability | Today |
|------------|--------|
| Types | `string` \| `number` \| `integer` \| `boolean` \| `date` |
| Formats | `x-documate-inputFormat` / `x-documate-outputFormat` (date & number) |
| Captions | `x-documate-captions` (+ description suffix) |
| Required | root `required: string[]` from field checkbox |
| Title / description | JSON Schema `title` / `description` |
| On-missing / generate | **None** |
| Provenance | **None** |

UI: Required is a checkbox beside type; formats/details in an expandable panel. Table **columns** are not in the root `required` array today (column `required` is always false when parsed from nested props).

### ResultJson consumers (flat schema assumption)

| Consumer | Behavior |
|----------|----------|
| External poll (`resultJson`) | Parses/stores flat extract object |
| Webhooks / public events | `data` = parsed `ResultJson` when Ready |
| Customer document detail | Parses flat field map for display |
| Admin ops document detail | Same |
| Files search (`JSON_VALUE` on paths) | Assumes flat property paths under ResultJson |
| SDK (Plan 10) | Hosts expect `resultJson` shaped like output schema |

Any provenance design that **breaks flat field → scalar** mapping is a compatibility risk for all of the above.

### Plan 23 interaction (note only)

Plan 23: after extract (+ validate), apply allowlisted scheme ops to `ResultJson`. Fallback generation could be:

- **Extract-time / pre-validate code** (this feature’s Option B) — fills missing required before validate; provenance `random_generated`.
- **Post-process op** (e.g. `generate_if_missing`) — same effect later; couples this feature to Plan 23 timeline and DSL.
- **Both** — risk of double-generation or unclear ownership.

Phase 2 for either plan should record an explicit handoff once both explorations lock.

---

## 4. Risks and Constraints

| Risk | Note |
|------|------|
| Silent fake data | Downstream treats generated date/serial as real without reading provenance |
| LLM invents values without marking provenance | If generation is “instruct the model,” audit trail is weak |
| Breaking flat ResultJson | Envelope / array shapes break webhooks, SDK, `JSON_VALUE` search |
| Required vs null / empty | Today null may pass; product must define “unavailable” |
| Format drift | Generated date must honor output format; serials must match type (string vs number) |
| Plan 23 overlap | Two places that mutate ResultJson → ordering and re-validate confusion |
| Table/array fields | Required + generate for line items is harder than header scalars |
| Crypto / PII | “Random” must not be marketed as secure IDs; avoid generating fake PII by default |
| Customer vs admin | Templates may ship aggressive generate defaults tenants inherit |

---

## 5. Options

### 5.1 Config in schema (UI + JSON Schema extensions)

#### Config shape options

| Option | Sketch | Pros | Cons |
|--------|--------|------|------|
| **C1 — `x-documate-onMissing`** | `"x-documate-onMissing": "fail" \| "generate"` (+ sibling generate rule props) | Matches existing `x-documate-*` pattern; per-field | New extensions to teach; round-trip in builder |
| **C2 — nested generate object** | `"x-documate-fallback": { "when": "missing", "strategy": "now" \| "serial", ... }` | Extensible; clear grouping | Heavier UI; more Decision Required knobs |
| **C3 — agent-level default + field override** | Agent: default onMissing; fields override | Less click-fatigue | Harder to reason; template inheritance surprises |
| **C4 — Required modes** | Replace boolean Required with `required_strict` / `required_or_generate` | Single control | Breaks mental model of JSON Schema `required`; migration |

**Generate rule sketches (per type):**

| Type | Example rule fields | Example behavior |
|------|---------------------|------------------|
| date | `strategy: now`, timezone TBD | Emit today in `x-documate-outputFormat` |
| number / integer / string | `strategy: serial`, `digits: N`, `prefix?` | e.g. `INV-` + 8 digits |
| string (non-serial) | `strategy: constant`? / disallow | Prefer explicit constant over random prose |
| boolean | usually no generate / fixed true\|false | Avoid random booleans in v1 |

**UI sketch (lean):** When Required is checked, show secondary control: “If missing: Fail document (default) | Generate fallback”. Expanding Details reveals type-specific generate params (digits, prefix; date = “Use current date”). Admin template builder mirrors customer.

**Default:** `fail` = today’s behavior (no schema change required for existing agents).

---

### 5.2 Where / when generation runs

| Option | Mechanism | Pros | Cons | Provenance truthfulness |
|--------|-----------|------|------|-------------------------|
| **A — Instruct LLM to invent** | Prompt + schema description / composer hint when onMissing=generate | No new pipeline step | Model may invent when data *exists*; may omit key; may ignore format; hard to prove “random” vs hallucinated | Weak — often cannot honestly say `random_generated` |
| **B — Code after LLM, before validate** | Detect missing/unavailable required fields with generate policy; fill deterministically; then validate | Auditable; format-correct; independent of Plan 23 | New extract-stage concern; must define “unavailable” | Strong — code owns the write |
| **C — Post-process pipeline ops** | Plan 23 op `generate_if_missing` / similar | Reuses future executor; composable with other transforms | Blocked on Plan 23; validate order (PP-RT1); may “fix” bad extracts if before validate | Strong if code op; timing vs raw extract matters |

**Hybrid note:** A+B (prompt says “leave null if missing” + code fills) can reduce LLM guessing while keeping truthful provenance — Decision Required.

---

### 5.3 Provenance in output JSON

Exact enum TBD; illustrative set:

| Value | Meaning (draft) |
|-------|------------------|
| `actual` | Taken as extracted from document (model output trusted as present) |
| `normalized` | Present but reformatted (e.g. date/number format) — may be future / Plan 23 |
| `resolved` | Filled from non-document resolution (lookup / rule) — likely later |
| `random_generated` | Synthesized by Documate fallback generator |
| `missing_omitted`? | Optional if non-required absent — probably out of v1 |

#### Shape options

| Option | Shape | Pros | Cons | Consumer impact |
|--------|-------|------|------|-----------------|
| **P1 — Sibling `field__source`** | `invoice_date` + `invoice_date__source` | Flat-ish; easy grep | Pollutes property namespace; schema validate must ignore `__source` or strip before validate | Medium — search/UI may show extra keys |
| **P2 — Envelope per field** | `{ "invoice_date": { "value": "...", "provenance": "..." } }` | Explicit | **Breaks** flat schema & all consumers; schema type no longer matches | High — avoid for v1 unless versioned contract |
| **P3 — Parallel `_provenance` map** | Keep flat values; sibling object `"_provenance": { "invoice_date": "random_generated", ... }` | Preserves flat values for SDK/webhooks; one reserved key | Reserved key collision if customer names `_provenance`; need omit from schema required | Low–medium — best compatibility |
| **P4 — Array of field results** | `[{ "key", "value", "provenance" }]` | Uniform | Breaks object schema entirely | High |
| **P5 — Side channel only** | Provenance in artifact / event / separate column; ResultJson unchanged | Zero breaking change | Easy to ignore; poll/webhook consumers may never see it | Low break, high product miss |

**Validate interaction:** Provenance metadata must **not** fail `JsonSchemaLite` (strip before validate, or allow additionalProperties / exclude reserved keys from schema properties).

**Delivery:** Prefer provenance on the same payload as `data` / `resultJson` so External poll, webhooks, and public events stay consistent (vs P5-only).

---

### 5.4 Interaction matrix (Required, formats, UX)

| Topic | Options / notes |
|-------|-----------------|
| **When is data “unavailable”?** | Key absent · key null · empty string · whitespace · invalid type · LLM sentinel (`"N/A"`) — Decision Required (lean: absent + null + empty string) |
| **Required + fail** | Current: missing key → `schema_invalid` |
| **Required + generate** | Fill then validate; still fail if generate cannot satisfy type/format |
| **Not required + missing** | No generate (unless product later adds optional fill) |
| **Date formats** | Generator must emit `x-documate-outputFormat` (default `YYYY-MM-DD`) |
| **Number vs string serial** | Serial with prefix ⇒ usually **string** type; pure digits may be integer/number — UI should guide |
| **Timezone for “now”** | UTC vs business/tenant TZ — Decision Required |
| **Customer UX** | Schema field Details: onMissing + generate params; clear warning that generated ≠ document evidence |
| **Admin / templates** | Same controls; avoid shipping generate-on by default on platform templates unless intentional |
| **Tables / columns** | v1 header fields only? vs column-level — Decision Required (lean: header scalars only) |

---

## 6. Open Questions — Decision Required

| ID | Question | Options (short) |
|----|----------|-----------------|
| **RF-CFG1** | Schema config shape? | C1 `x-documate-onMissing` · C2 nested `x-documate-fallback` · C3 agent default + override · C4 required modes |
| **RF-CFG2** | Generate strategies in v1? | date=`now` only · + serial(digits,prefix) · + constant · more |
| **RF-CFG3** | Definition of “unavailable”? | absent only · +null · +empty/whitespace · +invalid type · +sentinel strings |
| **RF-RUN1** | Where generation runs? | A LLM invent · **B** code pre-validate · C Plan 23 op · A+B hybrid |
| **RF-RUN2** | Order vs Plan 23 post-process? | generate before PP · after PP · generate only in PP · independent until Plan 23 locks |
| **RF-RUN3** | Re-validate after generate? | Yes (recommended) · no |
| **RF-PROV1** | Provenance shape? | P1 sibling · P2 envelope · **P3** `_provenance` map · P4 array · P5 side channel only |
| **RF-PROV2** | Provenance enum values? | `actual` \| `normalized` \| `resolved` \| `random_generated` (+/- others) |
| **RF-PROV3** | Always emit provenance object, or only when any non-actual? | Always (when feature used / agent flag) · only keys that differ · always for all agents |
| **RF-PROV4** | Reserved key name? | `_provenance` · `x-documate-provenance` · `__meta.provenance` |
| **RF-UX1** | Customer warning copy / confirm? | Inline help only · badge on Ready docs with generated fields · both |
| **RF-UX2** | Table/array columns in v1? | Header only · columns too |
| **RF-UX3** | Admin template default for onMissing? | Always fail · allow generate on templates |
| **RF-TZ1** | “Current date” timezone? | UTC · tenant/business setting · queue setting |
| **RF-SEC1** | Randomness quality statement? | Non-crypto PRNG OK for serials · document “not for secrets” |

---

## 7. Recommended Direction (draft — not locked)

Non-binding lean draft until developer locks Decision Required rows:

1. **Config:** **C1** — `x-documate-onMissing`: `fail` (default) \| `generate`, plus compact generate params (`x-documate-generate`: `{ "strategy": "now" }` or `{ "strategy": "serial", "digits": 8, "prefix": "INV-" }`). Builder: Required → “If missing” control + Details params.
2. **Runtime:** **B** — deterministic code **after** LLM parse, **before** schema validate; optional hybrid later (“leave empty if missing” in prompt) without trusting the model for provenance.
3. **Unavailable:** treat **absent, JSON null, and empty/whitespace string** as missing for generate-eligible fields (RF-CFG3 lean).
4. **Provenance:** **P3** — keep flat ResultJson values; add reserved `"_provenance": { "<field>": "<enum>" }` for fields that were generated (and optionally all touched fields). Enum lean: start with `actual` \| `random_generated`; reserve `normalized` / `resolved` for later without requiring them in v1.
5. **Validate:** strip or ignore `_provenance` during `JsonSchemaLite` validate; **re-validate** after fill.
6. **Scope v1:** header scalar fields only (not table columns); date `now` + serial; no random booleans/PII strings.
7. **Plan 23:** do **not** block this feature on PP DSL; document that a future PP op must not double-fill fields already marked `random_generated` (RF-RUN2 follow-up when Plan 23 locks).
8. **Implementation deferred** — no Phase 2 / DQ until this exploration is verified.

---

## 8. Exploration Exit Criteria

- [ ] Goals / non-goals agreed.
- [ ] Current Required / null / extract / consumer behavior documented (done in §3).
- [ ] Config, runtime placement, and provenance options compared with consumer impact.
- [ ] Plan 23 interaction noted (not blocked unless developer chooses C).
- [ ] Decision Required table answered by developer (or explicitly deferred).
- [ ] Developer verifies Phase 1 and approves start of Phase 2 (implementation plan only).

---

## Explicit deferrals

- **No implementation plan** in this file.
- **No dispatch queue** in this file.
- **No application code** changes as part of this exploration.
- **No** invention of folder / CQRS conventions.

---

## Agent output contract (Phase 1 end)

### Finalized Decisions

_None yet — Phase 1 draft._

### Pending Decisions

All **RF-CFG1…RF-SEC1** in §6.

### Assumptions

- Product track only; eng conventions stay under `docs/architecture/`.
- Extract path / `ResultJson` is the delivery surface; classify/OCR out of scope.
- Existing agents remain fail-on-missing unless schema opts into generate.
- Consumers that ignore provenance will see generated values as ordinary fields — product must warn in UI.

### Risks

- Fake-data misuse; LLM-only generation without honest provenance; breaking flat ResultJson; overlap with Plan 23; null-vs-missing ambiguity in current validator.

### Readiness

**Blocked** — awaiting developer review of options and answers to Decision Required; then verify Phase 1 and approve Phase 2.

---

**Next gate:** Please review this exploration, answer §6 decisions (or mark defer), then approve writing the Phase 2 implementation plan only.
