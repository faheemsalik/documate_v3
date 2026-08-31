# Documate v3 — Real OCR + Live LLM Extract (Exploration)

> **Status:** Exploration — Phase 1 **complete**; Phase 2 **approved** → see [05-ocr-llm-extract-implementation-plan.md](./05-ocr-llm-extract-implementation-plan.md)  
> **Type:** Engineering / Core pipeline (OCR normalize + Mode 1 extract)  
> **Upstream:** [03-documate-v3-implementation-plan.md](./03-documate-v3-implementation-plan.md) Wave 4; DQ-0701 / DQ-0703 stub evidence; [04-split-classify-strategy-exploration.md](./04-split-classify-strategy-exploration.md)  
> **Downstream:** After **all** opens locked → Phase 2 implementation plan → new DQ(s) (do **not** silently rewrite DQ-0701/0703 ✅ without amendment)  
> **Created:** 2026-08-27  
> **Amended:** 2026-08-27 — developer: live LLM extract is basic product capability; bring into this plan  

**Goal:** End-to-end **usable** Core path for invoices:

1. **Real OCR** (Textract → Google Document AI fallback) → object-storage text/layout artifacts  
2. **Live LLM extract** (Mode 1 meta / real model HTTP) → schema-valid `ResultJson` on Documents  

Without (2), real OCR alone still leaves heuristic field fill — product is not useful for customers.

---

## Planning flow

| Phase | This document |
|-------|----------------|
| 1 — Exploration | **This file** |
| 2 — Implementation plan | Only after OCR **and** LLM opens are locked |
| 3 — Dispatch queue | New DQ item(s) under Wave 4 / follow-on (e.g. DQ-0704 OCR + DQ-0705 live extract, or one combined DQ — Phase 2 decides split) |

---

## 1. Problem Framing

| Concern | Why it hurts |
|---------|----------------|
| Stub OCR | Placeholder text → useless extract input |
| Heuristic extract | Even with real OCR, `SchemaGuidedExtractor` is not reliable invoice extraction |
| Single OCR provider | Outages / format failures stop the File |
| Dual cloud cost/latency | OCR fallback + LLM + sync 60s budget |
| Artifact location | Bulk text in SQL contradicts Plan 03 / DQ-0701 |
| Mode 1 | Customers must not pick providers; Documate chooses |

**Developer intent:**

- Artifacts in object storage — **not** Files table (**locked**).  
- Textract + Google Document AI with fallback (**locked**).  
- **Live LLM extract is in scope for this plan** — basic product requirement (2026-08-27).  
- Storage **D1** (configured `IObjectStorage`, local Dev OK).

---

## 2. Scope

### In scope — OCR / normalize

- Real body behind `IOcrNormalizeAdapter` (composing primary → secondary)
- Credentials under `Ocr:` (F3)
- Artifacts: `normalize.text.txt` + `normalize.layout.json` (C1)
- Formats: PDF, PNG, JPG; text/* passthrough; Excel/Word **follow-on**
- Sync HTTP gates; worker sync/async OCR thresholds; app priority (locked below)
- Seed `google_document_ai`; record winning OCR `providerKey`

### In scope — live LLM extract (**added**)

- Real body behind `IDocumentExtractAdapter` (replace heuristic-only Mode 1 path)
- Call LLM with Agent **instructions + output schema + normalize text**
- Persist `OpsDocument.ResultJson` + existing extract artifact pattern
- Schema validate (already in `DocumentExtractStage`) → ready/failed
- Server-side LLM credentials (Mode 1; no customer picker)
- WorkEvent refs (providerKey, field counts / errors — not full prompts/completions dumps unless decided)

### Out of scope

- Real split/classify algorithms (Plan 04)
- Excel/Word normalize (follow-on DQ)
- Google Docs/Sheets
- Email intake enqueue
- Customer-facing provider picker (Mode 2 / BYOK)
- Post-processing MCP tools (DQ-1101)
- Migrating historical stub artifacts

### Non-goals

- Storing full OCR/LLM payloads on `OpsFiles` columns  
- Wrapping OCR/LLM as MCP tools (Plan 01)

---

## 3. Current-State Findings

| Area | Today |
|------|--------|
| OCR adapter | Stub binaries; PdfPig page count; text passthrough |
| Extract adapter | `Mode1DocumateMetaExtractAdapter` → `SchemaGuidedExtractor` only; `llmArmed` log if keys set — **no LLM HTTP** |
| Catalog | `aws_textract`, `documate_meta`, `gpt_5_6`, `claude_sonnet_6` seeded; no `google_document_ai` |
| DQ-0701 / 0703 | ✅ stub seams; Plan 03 Wave 4 still intended real OCR + real LLM |
| Dispatch index | Next open DQs are cancel/reprocess (1001/1002), not live LLM |
| Sync extract | `POST …/extract`, 60s wait, single-doc |

---

## 4. Risks and Constraints

| Risk | Notes |
|------|--------|
| Sync 60s | OCR (esp. dual) + LLM may exceed timeout even at 3 pages / 5 MB |
| Cost | OCR fallback + every-doc LLM |
| Prompt injection / PII | Full OCR text to third-party LLM — DPA / subprocessors |
| Schema adherence | Models may return invalid JSON → existing `schema_invalid` path |
| Heuristic fallback | If LLM fails, fall back to heuristic or fail hard? (open) |
| Priority + LLM | High-priority app jobs still share worker pool |

---

## 5. Open Questions

### Locked — OCR / delivery (2026-08-27)

| # | Choice | Meaning |
|---|--------|---------|
| **Q1** | **A3** | OCR fallback on primary API failure **or** empty/low-quality text |
| **Q2** | **B3** | Configurable OCR order; default Textract → Google Document AI |
| **Q3** | **C1** | File-level normalize text + layout only |
| **Q4** | **D1** | Same `IObjectStorage` as originals |
| **Q5a** | **3 pages + 5 MB** | HTTP sync extract rejects above either limit |
| **Q5b** | **P1** | App upload optional `priority` (`normal` \| `high`) |
| **Q5c** | **E1 @ 8 pages** | Worker: sync OCR ≤8 pages; async+poll if larger |
| **Q6** | **F3** | `Ocr:` options section |
| **Q7** | **G1** | Seed `google_document_ai` + existing `aws_textract` |
| **Q8** | **H1** | `text/*` passthrough |
| **Q8a** | **O2** | Excel/Word follow-on DQ |
| **Q8b** | **Out** | No Google Docs/Sheets |
| **QX** | **In plan** | Live LLM extract **is** in scope for this exploration (developer 2026-08-27) |

**HTTP sync vs worker OCR limits:**

| Layer | Limit |
|-------|--------|
| HTTP sync-wait extract | ≤ **3 pages** and ≤ **5 MB** |
| Cloud OCR inside Hangfire | Sync if ≤ **8 pages**; async+poll if larger |

---

### Still open — live LLM extract

#### L1 — Which model path (Mode 1) — elaborated

**What “extract” needs:** for each Document, call *some* LLM with:

- Agent `Instructions`
- Agent `OutputSchemaJson`
- Normalize text (from OCR artifacts)

**Who chooses the model?** That is L1. Today the code always pretends `documate_meta` and never calls a real model. Agents already have unused fields: `DefaultProviderId`, `ProviderStrategyJson` (catalog can hold `gpt_5_6`, `claude_sonnet_6`, etc.).

---

**L1a — Single platform default (simplest)**

- One model for the whole product, from config (e.g. `Llm:ProviderKey=gpt_5_6` + API key).
- Every Agent/Queue uses that same model.
- Ignore `Agent.DefaultProviderId` for now (or store `documate_meta` on the Document as today).

| Pros | Cons |
|------|------|
| Fastest to ship; one secret; one failure mode | Cannot A/B models per document type; no per-Agent override |

**Example:** All invoice agents → always GPT. Change model = change server config, redeploy/restart.

---

**L1b — Dual LLM fallback (like OCR A3/B3)**

- Config: primary model + secondary model (e.g. GPT → Claude).
- On primary fail / bad JSON / timeout → try secondary, then fail (or heuristic if L3 says so).
- Still **platform-chosen**, not customer-picked (Mode 1).

| Pros | Cons |
|------|------|
| Survives one vendor outage; mirrors OCR story | 2× cost on failures; more credentials; harder sync-60s budget |

**Example:** Primary OpenAI down → Claude still extracts.

---

**L1c — Honor Agent.DefaultProviderId when set**

- If the Agent has `DefaultProviderId` → `gpt_5_6` / `claude_sonnet_6` → use that model’s credentials.
- If null → fall back to platform default (L1a behavior).
- Fits existing schema; still Mode 1 if **UI doesn’t expose** provider picker (only ops/seed/templates set it).

| Pros | Cons |
|------|------|
| Different doc types can use different models later; uses fields you already have | Need credential map per provider key; more testing; easy to “accidentally” become Mode 2 if UI exposes it |

**Example:** Invoice agent → GPT; delivery-note agent → Claude; both still Documate-operated.

---

**How to choose**

| If you want… | Pick |
|--------------|------|
| Smallest first ship; one key | **L1a** |
| Vendor resilience like OCR | **L1b** (can add later on top of L1a) |
| Per-Agent model without customer UI | **L1c** |

**Recommendation:** **L1a** now; design config so **L1b/L1c** can be added without changing `IDocumentExtractAdapter`.  
**Hybrid often works:** ship **L1a**, keep Agent.DefaultProviderId unused until a later DQ.

**Related (not L1):** L5 decides what `providerKey` you *record* on Document/WorkEvent (`documate_meta` vs concrete model). L1 decides what you *call*.

**Locked (2026-08-27): L1c** — resolve model from `Agent.DefaultProviderId` when set; else platform default.

#### Locked — LLM path (2026-08-27 evening)

| # | Choice | Meaning |
|---|--------|---------|
| **L1** | **L1c** | Model from `Agent.DefaultProviderId` when set |
| **L1c-i** | **N1 + templates** | `CorAgentTemplate.DefaultProviderId` **must be seeded** (LLM catalog id, e.g. `gpt_5_6`); clone copies it onto Agent. If Agent still null → **platform default** from `Llm:DefaultProviderKey` |
| **L1c-ii** | **M3** | Only `provider_category=llm` counts; non-LLM / bad id → treat as null → N1 |
| **L2** | **L2a** | Dedicated `Llm:` section with per-`ProviderKey` credentials |
| **L3** | **L3c + alert** | Retry once on same model; then fail extract. **Also** notify fixed ops email `faheem@manticsoftware.com` (Phase 1 hard-coded; config later) |
| **L4** | **L4a** | Send **full** normalize text for Document extract. Truncation/page policy for **split** deferred (Plan 04) |

#### Still open

*None — Phase 1 LLM opens closed 2026-08-27.*

#### Newly locked (2026-08-27)

| # | Choice | Meaning |
|---|--------|---------|
| **L1c-iii** | **U1** (plain: 1-A) | Template/API set provider; no customer model picker in UI |
| **L5** | **L5c** (plain: 2-C) | Clients see Documate meta; WorkEvent logs real model |
| **L6** | **L6d** | LLM API keys required in every environment; no missing-key heuristic |
| **L7** | **L7a** (plain: 3-A) | One dispatch item: real OCR + live LLM together |
| **L8** | **T1** | Keep HTTP sync-wait 60s; rely on ≤3 pages / ≤5 MB gates |

#### L1c follow-ups (detail retained)

**L1c-i — When `DefaultProviderId` is null** — **LOCKED N1 + templates** (see table).

**L1c-ii** — **LOCKED M3**.

**L1c-iii — Who sets `DefaultProviderId` in Phase 1 (Mode 1 = no customer picker)** — still open

| Option | Meaning |
|--------|---------|
| **U1** | Seed/clone from AgentTemplate; App API can set it (ops/dev); Angular hides picker for now |
| **U2** | App UI exposes provider dropdown |
| **U3** | Platform always overwrites to default (defeats L1c) |

**Rec:** **U1** (matches "template has default provider").

---

#### L2 — **LOCKED L2a**

---

#### L3 — **LOCKED L3c + email**

On final failure after retry: Document/File failed as today **and** send notification email to `faheem@manticsoftware.com` (fixed for now). Open for Phase 2: SMTP vs third-party mailer; not blocking exploration choice.

---

#### L4 — **LOCKED L4a**

Full OCR text per Document extract. Split/segmentation token policy = later.

---

#### L5 — What we record as provider — still open (elaborated)

With L1c you may call `gpt_5_6` or `claude_sonnet_6`. What do External/App clients *see*?

| Option | Meaning |
|--------|---------|
| **L5a** | Document always `documate_meta` (hide vendor) |
| **L5b** | Document stores concrete key (`gpt_5_6`) — clients see engine |
| **L5c** | Document/`External` DTO show `documate_meta`; WorkEvent stores concrete key |
| **L5d** | DB stores concrete; External DTO still maps to `documate_meta` |

**Rec:** **L5c**.

---

#### L6 — "What API keys?" — elaborated (still open)

**Not** your Documate External `X-Api-Key` (partner auth).  
**Not** AWS/Google OCR keys (`Ocr:` section).

**LLM API keys** = secrets to call the **language model vendor** when extracting fields, e.g.:

| Config (under locked **L2a** `Llm:`) | Used for |
|--------------------------------------|----------|
| `Llm:Providers:gpt_5_6:ApiKey` | OpenAI (or compatible) chat/completions |
| `Llm:Providers:claude_sonnet_6:ApiKey` | Anthropic messages API |
| `Llm:DefaultProviderKey` | Which key/model when Agent has no provider |

Today `Providers:DocumateMetaApiKey` / `DefaultLlmApiKey` only set `llmArmed=true` and **do not call** anyone.

**L6 asks:** if those LLM keys are **missing** in config, what should the app do?

| Option | Meaning |
|--------|---------|
| **L6a** | Never use heuristic; extract fails (or refuse to process) without keys |
| **L6b** | **Development:** allow old heuristic extract so Postman works without paying LLM. **Production:** require keys |
| **L6c** | **Production/Staging:** process **fails to start** if default LLM key missing |
| **L6d** | Keys required in every environment (including local Dev) |

**Common combo:** **L6b + L6c** — local soft, deployed hard.

---

#### L7 — DQ packaging — elaborated (still open)

This is **how we ticket/ship** the work, not runtime behavior.

| Option | Meaning |
|--------|---------|
| **L7a** | **One dispatch item:** implement real OCR **and** live LLM together → one "useful invoice path" merge |
| **L7b** | **Two items:** first land OCR (text artifacts real); second land LLM (fields real). Between them, extract is still heuristic |
| **L7c** | Two ticket numbers for review size, but **both required** before calling the wave "done" |

| If you care about… | Prefer |
|--------------------|--------|
| "Product useless without LLM" as one outcome | **L7a** or **L7c** |
| Smaller PRs / safer rollback of OCR alone | **L7b** |

---

#### L8 — Sync HTTP timeout — elaborated (still open)

**Two clocks:**

1. **Partner** calls `POST /api/v1/queues/{id}/extract` and **waits on the HTTP connection** (today max **60 seconds**). Then pipeline must finish OCR (+ fallback) + LLM (+ retry) or we return `timedOut` + ids.
2. **Hangfire** may keep working after timeout; client polls.

**L8 asks only about clock (1):**

| Option | Meaning |
|--------|---------|
| **T1** | Keep **60s**; rely on sync gates (≤3 pages, ≤5 MB) |
| **T2** | Raise wait (e.g. **90–120s**) because OCR+LLM+retry needs more room |
| **T3** | Keep **60s**; on timeout still return ids (`timedOut`) — client polls (already true today) |

**T1 vs T3:** both keep 60s; T3 just emphasizes "timeout is OK, poll." T2 changes the limit.

**Rec:** start **T3** (or T1); move to T2 only if smoke shows frequent timeouts.


## 6. Recommended Direction

### OCR (unchanged locks)

1. Composing `IOcrNormalizeAdapter`: Textract → Google (B3/A3); artifacts C1; storage D1; `Ocr:` F3.  
2. Formats: PDF/PNG/JPG + text passthrough; Excel/Word later.  
3. Sync HTTP ≤3 pages / 5 MB; worker OCR sync ≤8 pages.  
4. App `priority` P1.  
5. Seed Google OCR provider G1.

### Live LLM (new)

6. Resolve model from Agent (from template) else platform default; only LLM catalog providers.  
7. Credentials in `Llm:` map; required in all environments.  
8. Retry once on LLM failure, then fail + email ops.  
9. Full OCR text to the model; clients see “Documate”; log real model internally.  
10. Ship as **one** work item: real OCR + live LLM.  
11. Amend Plan 03 / add DQ — do not pretend DQ-0703 already did live LLM.

---

## 7. Exploration Exit Criteria

Phase 1 is done when:

- [x] All OCR and LLM exploration opens locked
- [x] Developer approved Phase 2 (2026-08-28)

---

## Phase-end contract

### Finalized Decisions (plain language)

| Topic | Decision |
|-------|----------|
| OCR text storage | Object storage artifacts (not Files table) |
| OCR engines | Textract then Google Document AI (configurable); seed both |
| File types now | PDF, PNG, JPG (+ plain text passthrough); Excel/Word later |
| Sync API limits | Max 3 pages and 5 MB; wait 60 seconds |
| Worker OCR | Sync call up to 8 pages; poll for larger |
| App upload | Optional priority high/normal |
| AI extract | Live LLM — required for useful product |
| Which model | From Agent (copied from Agent Template); else platform default; only real LLM catalog rows |
| Who picks model | Template/API; hide picker from customers |
| LLM secrets | Dedicated `Llm:` config; keys required everywhere |
| LLM failure | Retry once, then fail; email faheem@manticsoftware.com |
| Prompt text | Full OCR text for extract |
| What clients see | "Documate"; real model name only in internal WorkEvents |
| How we ship | One work item: OCR + live LLM together |

### Pending Decisions

*None for Phase 1.* Phase 2 open: see implementation plan **DR1**.

### Readiness

**Phase 2 in progress** — [05-ocr-llm-extract-implementation-plan.md](./05-ocr-llm-extract-implementation-plan.md)
