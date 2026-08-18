# Documate v3 — Project Exploration & Mental Design

> **Document type:** Exploration / mental design  
> **Status:** Mental design frozen (blocking opens closed; some topics deferred by choice)  
> **Scope:** Product intent, concepts, boundaries, open questions  
> **Out of scope:** Implementation details, tech stack choices, coding plans, infrastructure blueprints  

**Related documents (separate, not covered here):**
- **Product glossary (Batch / File / Document)** — [00-product-glossary.md](./00-product-glossary.md)
- Document Queue design — [02-document-queue-design.md](./02-document-queue-design.md)
- Implementation plan — [03-documate-v3-implementation-plan.md](./03-documate-v3-implementation-plan.md)

---

## 1. Purpose of this document

This document captures the **mental design** of Documate v3: what the product is, how its pieces relate, what a customer experiences, and which decisions still need discussion.

It exists so we can:
1. Align on product direction before any build work.
2. Surface ambiguities early (especially multi-document files, agents vs queues, post-processing, MCP).
3. Freeze a shared vocabulary for later queue and implementation docs — see [00-product-glossary.md](./00-product-glossary.md).

Nothing in this document should be read as an implementation commitment.

---

## 2. Background & direction change

We already have a working Documate product focused on business document extraction (invoices, credit notes, delivery notes, POs, and similar).

**v3 is not a polish of the old system.** It is a deliberate direction change:

| Old direction (mental) | New direction (mental) |
|---|---|
| Heavier reliance on a fixed / vendor-shaped extraction model | Multi-provider, multi-step extraction pipeline owned by us |
| Schema thinking closer to legacy extraction platforms | Customer-owned output schemas as the contract |
| Extraction as the primary “end” | Extraction + optional post-processing workflows as the end |
| Limited intake paths | API **multi-file** upload + email-forward; concurrent non-blocking intake |
| Limited identity story | First-class integration with Iden (identity management) |

The core promise remains: turn messy business documents into **reliable structured data** the customer’s systems can consume.

---

## 3. Product promise (one sentence)

**Documate v3** lets a customer define AI Agents (with schemas), route documents through Queues (departments / sub-clients), extract via a multi-step process, optionally post-process, and receive results via push (webhook) and/or poll — with tenant identity handled by Iden.

### 3.0 Product capability phases (roadmap — not scheduling)

| Product phase | Name | Scope posture |
|---|---|---|
| **Phase 1** | Core extraction platform | Queues, Agents, File→Document pipeline, async/sync APIs, email intake, Mode 1 — this exploration’s primary freeze |
| **Later — White-label SDK** | §3.1 | Separate exploration → own impl plan / DQ when prioritized |
| **Later — Statements reconciliation** | §3.2 | Separate product phase (not “just another Agent schema”) |
| **Later — MCM DN rebranding** | §3.3 | **MCM-only** customer feature; separate product phase |
| **Deferred brainstorm — Classification strategy** | §3.4 | Bring forward when implementing split/classify (e.g. DQ-0702); options not frozen here |

Phase numbers below are **product capability phases**, not planning-process Phase 1/2/3 (explore → plan → DQ).

### 3.1 Major goal (later phase) — White-label & zero-friction developer SDK

> **Status:** Captured as a **major product goal**. Not designed here. **Own later product phase.** Must be discussed as its own exploration before committing APIs, packaging, or branding.

**Intent:** Sell Documate as a **white-labeled** extraction platform that ISVs / product teams embed inside their own product so developers **do not rebuild** document extraction.

**Why this matters:** Extraction itself looks easy; the reliability comes from the small linked parts around it (intake, split/classify, schema contract, validation, async delivery, retries, webhooks, ops). The product wins if integrating **those** parts is faster than building a thin LLM wrapper.

**Target developer experience (mental only — TBD):**
- Ship **easily integrable SDKs** (starting mental example: **C# NuGet**).
- Developer shares their **output schema as a type they already own** (interface / class) — not a separate JSON-schema authoring chore if we can avoid it.
- **Agent instructions** can be supplied **from within their platform** (their product configures behavior; Documate executes).
- White-label: their brand / their surface; Documate is the engine underneath.

**What this implies we must discuss later (non-exhaustive):**
- White-label tenancy, branding, and who owns the UI vs API-only embed.
- Schema-from-types story (codegen, attributes, shared contracts) vs today’s Agent-bound schema in the app.
- How platform-supplied instructions map to Agents without fighting Queue routing.
- NuGet (and other language) packaging, versioning, and auth for machine clients.
- What “white-label” guarantees (SLAs, data residency, support) vs self-serve SaaS.

**Phase 1 posture:** Keep Agent + schema + External APIs clean and versionable so this goal is not blocked later. Do **not** build white-label packaging or schema-from-C#-types in Phase 1.

### 3.2 Major feature (separate later phase) — Statements reconciliation

> **Status:** Captured only. **Not Phase 1.** Requires its **own product phase** (exploration → plan → DQ when scheduled). Details TBD.

**Intent:** Reconcile **two statements / lists** (e.g. supplier statement vs internal ledger, or two extracts) using intelligence beyond single-document structured extraction.

**Why it is not “just another Agent”:** Extraction yields structured rows on each side. Reconciliation needs **matching logic** across two sets — fuzzy/partial matches, multi-parameter rules (amounts, dates, refs, parties, line groupings), unmatched / disputed / tentative matches, and an explainable outcome set. That is a different product surface than Document → schema JSON.

**Mental capabilities (TBD — discuss later):**
- Ingest / identify the two sides (Files/Documents or already-extracted lists).
- Compare on a **variety of parameters** (exact + tolerant matching).
- Produce reconciliation results (matched pairs, exceptions, confidence / reason codes).
- UX / API for reviewing and closing exceptions (may overlap HITL later).

**Phase 1 posture:** Do not model reconciliation entities or match engines. Phase 1 schemas may still extract statement-like documents as normal Documents if needed for a customer — but **no reconciliation product**.

### 3.3 Major feature (separate later phase) — MCM delivery-note rebranding (**MCM only**)

> **Status:** Captured only. **Not Phase 1.** **Customer-specific (MCM).** Own **separate product phase**. More detail later.

**Intent:** For **MCM only**, rebrand supplier **delivery notes**: replace/render **MCM** identity over the supplier’s on the document artifact (not merely extract fields).

**Known requirements (high level — expand later):**
- Overlay / replace **supplier logo** with **MCM logo**.
- Replace / render **company name**, **phone numbers**, and **address** with MCM equivalents.
- Output is a **rebranded delivery-note document** (render pipeline), in addition to any structured extraction the queue already does.

**Why separate phase / MCM-only:**
- Document **generation / PDF (or image) rewrite** is a different stack from Core extraction.
- Hard-coding MCM assets and rules into the multi-tenant platform without a deliberate tenant/feature boundary is a product smell — design as **MCM-scoped** capability (feature flag / tenant pack), not as a global “every tenant gets DN rebrand.”

**Phase 1 posture:** No DN rebranding, no MCM logo pipeline, no PDF rewrite product. Extraction of DNs as Documents remains in scope if configured via normal Agents.

### 3.4 Deferred brainstorm — Document classification strategy

> **Status:** **Superseded for working brainstorm** by dedicated exploration:  
> [`04-split-classify-strategy-exploration.md`](./04-split-classify-strategy-exploration.md) (split storage + split/classify techniques, cost/latency).  
> This §3.4 remains the original capture of the option space; prefer the dedicated doc for decisions.  
> **Bring to front when:** DQ-0702 (or sooner if classify quality blocks demos).  
> **Then:** Lock techniques in 04 → amend Plan 03 → implement. Do **not** invent a permanent classify architecture inside the DQ before that lock.

**Why this needs its own discussion:** Phase 1 already assumes a **classify step** exists (QueueRoute needs a `DocumentType`; multi-type Files are first-class — Decision E / mental model B). That does **not** freeze *how* classification should work: heuristics vs LLM vs hybrid, page-level vs pack-level, confidence/HITL, customer override, cost/latency, failure UX, etc.

**What “classification” means here (mental):** Assign each logical Document (after or during split) a **DocumentType** (and confidence/evidence) so the Queue can **route** to the matching Customer AI Agent. Distinct from extraction schema fill and from post-processing.

**Option space to brainstorm later (non-exhaustive — not a decision list):**

| Theme | Possible directions (examples only) |
|---|---|
| **Who decides type** | Platform System AI Agent only · rules/heuristics first · LLM classify · hybrid (rules → LLM fallback) · customer-supplied hint on upload/email · force single type per Queue |
| **When it runs** | Before split · after split per child · joint split+classify · re-classify on reprocess only |
| **Signal sources** | Filename / MIME · OCR text windows · layout/visual · first/last pages · barcodes/QR · email subject/body · prior Files on same Queue |
| **Label set** | Platform DocumentType catalog only · Queue-restricted subset (routable types) · open vocabulary then map · “unknown / other” bucket |
| **Confidence & failures** | Hard fail unroutable · PartialReady · quarantine / HITL (later) · default Agent · ask customer via API callback |
| **Cost / latency** | Cheap classifier always · expensive model only on ambiguity · cache by fingerprint · Mode 1 meta-provider vs dedicated classify model |
| **Customer control** | Read-only platform behavior · tunable prompts/thresholds · custom classify Agent (later) · upload `document_type` override |
| **Multi-doc packs** | Independent per Document · pack-consistent constraints · “primary type + attachments” |

**Phase 1 posture until brainstorm:** Keep the **pipeline slot** (split → classify → route) and **QueueRoute** model. Implement a **minimal workable** classifier under Decision E (clear Failed / PartialReady / reprocess) without pretending the option space above is closed. After the brainstorm, upgrade or replace classify deliberately (may spawn its own mini-exploration / DQ band).

**Out of this brainstorm’s scope (already owned elsewhere):** Queue routing map shape (§6.1 B — frozen), Agent schemas, HITL product UX (separate later), mapping catalogs (§16).

---

## 4. Vocabulary (shared mental model)

**Canonical definitions:** [00-product-glossary.md](./00-product-glossary.md). Summary:

| Term | Meaning |
|---|---|
| **Iden Tenant** | Top identity org in Iden. Parent of Businesses. Documate mirrors as **CorTenant** (e.g. ProviderMode). |
| **Iden Business** | Second Iden level under a Tenant. **Documate isolation unit** for Agents, Queues, Files, Documents (§15). |
| **CorTenant** | Documate mirror of an Iden Tenant (billing/account settings). Not the same as a Queue. |
| **CorTenantBusiness** | Documate mirror of an Iden Business — where ops config and work live. |
| **User** | Person via Iden; may access one or more Businesses under a Tenant. |
| **Batch** | Optional log/correlation when **≥2 Files** arrive in one intake. Not a delivery unit. |
| **File** | One stored upload/email artifact. Parent of Documents; rollup for UI/ops. |
| **Document** (**Doc**) | One logical business document after split/classify. **Result + webhook unit.** |
| **Result** | Schema JSON on a Document when Ready. |
| **Job** | **Not a product term** — do not use; say File or Document. |
| **Schema / Output structure** | Customer output shape. **Bound to an Agent.** |
| **Platform Agent template** | Pre-built agent; guided-clone starting point. |
| **Agent** | Customer **AI Agent** (guided clone): schema + extraction instructions + **post-processing** for its document type. |
| **System AI Agent** | Platform-owned AI steps (classify, intake decision, …). Not user-cloned; not QueueRoute targets in Phase 1. |
| **Queue** | Ops lane **inside a Business** (multi-queue first-class): routing map, webhook, email. **Not** post-processing owner. **Not** an Iden Business. |
| **IntakeRejection** | Intake refused with **no File** created. |
| **Intake source** | `Api` / `Email` / `ApiSync` — drives whether original File is included on Document webhook. |
| **Post-processing / Workflow** | After extract; **bound to Agent** (document-type wise). Platform tools / internal MCP in Phase 1; user-customizable from day one. |
| **Provider mode** | Mode 1 (hidden providers) \| Mode 2 (BYOK). Phase 1 = Mode 1. Set at **Tenant** level. |
| **Documate provider** | Meta-provider behind Mode 1. |
| **Mapping catalog** (later) | Supplier/customer/product name → id enrichment. |
| **Iden** | Identity product: **Tenant → Businesses** + users. Documate does not reinvent this hierarchy. |

---

## 5. Module map (conceptual only) — DECIDED: keep three separate

### 5.1 Core module — Extraction brain

Owns the meaning of “turn document → structured result.”

Responsibilities (conceptual):
- Accept a document already associated with a queue/agent context.
- Run a **multi-step extraction process**.
- Use multiple intelligence providers as capabilities inside that process.
- Produce output that conforms to the agent’s schema.
- Hand off to post-processing when configured, then mark Document Ready (or Failed).

Does **not** own: public customer API surface, UI-facing CRUD, identity itself.

### 5.2 Frontend support APIs

APIs whose primary consumer is our own product UI / admin experience.

Responsibilities (conceptual):
- Configure queues, agents, platform-template browsing, webhooks, email intake.
- Browse Files / Documents / results with rich filters.
- Manage post-processing workflow definitions.
- Surface mapping catalogs in later phases.

### 5.3 Externally exposed APIs module

APIs whose primary consumer is the customer’s systems (integrations) **and** a sync path for interactive clients.

Responsibilities (conceptual):
- Authenticated **multi-file** document upload (non-blocking enqueue) — **async / disconnected** style.
- Poll for file / Document status with multiple filter options.
- Webhook-oriented integration contracts (**per Document**).
- **One synchronous (wait) API** for client-side use that holds the HTTP call until final result(s) are ready or timeout — see §10.1.
- Stable, versioned partner-facing surface.

Email intake is an alternate **intake channel**, not a fourth module.

---

## 6. Primary actors & relationships — DECIDED (multi-doc aware)

```
Iden (identity)
   │
   ├── Tenant
   │     └── Business[]          ← Iden two-level tenancy
   └── Users (membership / access to Businesses — Iden-owned)
          │
          ▼
Documate CorTenant (Provider Mode 1 | Mode 2; mirrors Iden Tenant)
   └── CorTenantBusiness[] (mirrors Iden Business)   ← isolation boundary
         ├── Agents (each has one Schema)
         ├── Queues
         │     ├── routing: document-type → Agent (see §6.1)
         │     ├── postback / webhook URL
         │     ├── email intake address
         │     └── type → Customer AI Agent (Agent owns post-processing)
         ├── Platform Agent templates are global; clones land in Business
         └── Mapping catalogs (later phase)
```

**Frozen relationship rules:**
1. Iden: **Tenant → Businesses**. Documate does not invent a third identity level.
2. One **Business** → many agents, many queues. Operational data is **Business-scoped** (not Tenant-flat).
3. **Schema binds to Agent** (not to queue).
4. Queue = separation by department / operational stream **within a Business**; owns intake + postback. Queue ≠ Iden Business.
5. Phase 1 must support **multiple logical documents per file** and **mixed document types per file**.
6. Changing queue routing: **not allowed once the queue has files** (§9.5).
7. Provider Mode lives on **Tenant**; Agents/Queues/Files/Documents live under **Business**.

### 6.1 Critical redesign: Queue cannot mean “exactly one agent” anymore

Earlier rule “queue → one agent” **breaks** once one PDF can contain an invoice + credit note + delivery note.

| Option | Idea | Verdict |
|---|---|---|
| **A. One agent, mega-schema** | Single schema covers all types | Reject — brittle, unusable UX |
| **B. Queue → one Router + type→Agent map** | Classify/split, then run the matching agent per child | **Recommended** |
| **C. Queue → many agents, no explicit router** | Implicit classify against all bound agents | Works but weaker control |

**Recommended mental model (B):**
- Queue holds a **routing table**: `document_type → Agent` (and maybe a default/fallback).
- A **split + classify** step (platform capability; may be its own lightweight agent) produces N **Documents**.
- Each Document runs **one** agent → one schema → one result.

Customers still “assign agents to a queue,” but as a **set / map**, not a single pointer. UI can start simple (“Invoice Agent, Credit Note Agent, DN Agent on this queue”).

**Critical note:** Without this change, phase-1 multi-type is fiction. Do not keep “one agent per queue” in vocabulary.

**Deferred:** *How* classify works (models, heuristics, confidence, overrides, cost) — see **§3.4**. Do not freeze classify strategy here; brainstorm when we reach that implementation point.

---

## 7. End-to-end flow (mental, happy path)

### 7.1 API multi-file upload

1. Customer authenticates (Iden-backed).
2. Customer may upload **one or many files** in one request.
3. If **N≥2** files → create optional **Batch** (log/correlation only); each file → **File**.
4. If N=1 → File only (no Batch).
5. Each file is **enqueued independently** (must not block other processing — see Queue doc §2.1).
6. Per file: split → classify → route → **Documents**.
7. **Webhook fires per Document** when that Document is terminal; poll anytime.
8. File rollup (`Ready` / `PartialReady` / …) available via poll for pack views.

### 7.2 Email-forward intake

1. Queue inbound email (unguessable; allowlist before selling email hard).
2. Hard gates; refuse → **IntakeRejection**.
3. Intake decision agent → one File per target; **Batch** if ≥2 targets (log only).
4. Same File → Documents path; **webhook per Document**; original file included when source ≠ API.

---

## 8. Extraction mental model

### 8.1 Schema on Agent is the contract — DECIDED

Extraction success = schema-valid structured data per the **Agent’s schema**.

### 8.2 Multi-step process (capability view)

Conceptual steps may include:
- Intake decision (especially email)
- File normalization / type handling
- Text & layout extraction (OCR / document AI)
- Classification (when needed)
- Field reasoning via LLM providers
- Schema validation / repair
- Post-processing workflow
- Ready for customer

**Provider capabilities in the design space:**
- OCR / document AI engines (e.g. Textract-class)
- LLM models as **flat catalog entries** (e.g. `gpt_5_6`, `claude_sonnet_6`, DeepSeek / Kimi model keys) — see §8.4a
- **Documate provider** (meta-router over the above)

### 8.3 Supported input families

PDF, images (JPG/PNG), DOCX, Excel, plain text — format-tolerant intake; agent strategy may differ by format family.

### 8.4 Provider modes — DECIDED (phase 1 = Mode 1 only)

| Mode | Customer experience | Billing mental model |
|---|---|---|
| **Mode 1** | Providers hidden. Customer uses Agents / Documate provider only. | Documate-metered usage |
| **Mode 2** | Customer can select providers and bring own API keys (BYOK). | Often billed by provider directly; Documate still a product fee |

Mode is selected when the customer account is created. Phase 1 ships Mode 1 only; design should not paint us into a corner that blocks Mode 2 later (e.g. agent config should allow a “provider strategy” concept even if UI hides it).

**Critical note:** Mode 2 BYOK implies key custody, rotation, abuse of stolen keys, and “whose quota failed?” support burden. Keeping it out of phase 1 is correct.

### 8.4a Provider catalog shape — DECIDED: flat model keys (no company→model tree)

We do **not** maintain a hierarchy of “companies” with nested model lists.

| Approach | Verdict |
|---|---|
| Company (OpenAI) → models (GPT…) | **Reject** for catalog UX/data model |
| **One Provider row per selectable engine/model** (`gpt_5_6`, `claude_sonnet_6`, …) | **DECIDED** |

OCR/meta entries are the same flat list (`aws_textract`, `documate_meta`, …). Vendor/company is optional metadata on the row if useful for ops — not a parent entity customers navigate.

Phase 1: customers still don’t pick from this list (Mode 1); Core / Documate meta-provider uses it internally.

### 8.5 What an Agent means to a customer (clarifying former Q8)

The earlier question was not “are agents visible?” — you already answered yes. It was: **how much of the agent’s internals does the customer configure?**

Three product depths (examples):

| Depth | Example | Customer does |
|---|---|---|
| **A. Template-only** | Pick “Invoice Agent (EU)” from catalog, assign to queue | Almost no config; schema mostly fixed by template |
| **B. Guided clone** | Clone “Invoice Agent”, edit schema fields, add instructions (“always prefer tax ID from footer”), set language | Most common SaaS sweet spot |
| **C. Full builder** | Define every pipeline step, prompts, provider routing, validation rules | Power users / Mode 2 later |

**Your direction (templates + customer-defined)** is frozen as **B. Guided clone**.

**Minimal Agent (mental contents):**
- Name, description
- Schema (required)
- Instructions / extraction guidance (natural language)
- Document-type intent (invoice, DN, PO, credit note, …) — used by queue routing / classify
- Provider strategy (hidden in Mode 1; explicit in Mode 2)
- Post-processing workflow (user-customizable day one; platform tools)

---

## 9. Document multiplicity & intake — DECIDED

**Inside one file:** multiple logical documents + mixed types (split → classify → route).  
**Across intake:** multiple files in one API request or one email — **required**.  
**Batch:** optional, only when ≥2 files in that intake; **log/correlation only**.

### 9.1 Hierarchy

```
[Batch]?   ← only if ≥2 files together; log only
 └── File
      └── Document[]   ← result + webhook unit
```

### 9.2 Webhook — per Document

Fire on each document terminal (`Ready` / `Failed` / `Rejected` / `Cancelled`).  
Do not wait for sibling docs or file rollup.  
File bytes on webhook only for non-API sources.

### 9.3 Implementation goal

Multi-file receive must **not** stop other processes (prior product pain). Enqueue per file; concurrent processing.

### 9.4 Routing lock / cancel / reprocess

- Routing locked once queue has files.  
- Cancel **file** or **Document**.  
- Reprocess: explicit caller only → new File.

Authoritative detail: [02-document-queue-design.md](./02-document-queue-design.md).

---

## 10. Delivery model — DECIDED

### 10.0 Two API styles

| Style | How it works | Typical consumer |
|---|---|---|
| **Async / disconnected** (default, most APIs) | Accept work → return ids immediately → track via **webhook** and/or **poll** | Backend integrations, email intake, bulk upload |
| **Sync / wait** (one dedicated API) | Same underlying pipeline, but the **HTTP request waits** until final result(s) are ready (or timeout) and returns them **in that response** | Client-side / interactive UI flows |

Most External APIs are disconnected (accept-and-track via webhook/poll). **Exactly one** wait-style extract API is in scope for clients that need an in-call result.

### 10.1 Sync wait API — design rules (mental) — DECIDED with plan 03 B/C/G

1. **Same Core pipeline** as async (File → Documents, agents, schemas) — not a second extraction engine.
2. **Single Document only:** if the File yields **more than one** Document → **fail** the sync-wait call (use async for multi-doc). If exactly one Document → hold HTTP until Ready/Failed (terminal) or timeout.
3. **Timeout:** **60 seconds** max. On timeout return timeout + ids for poll. Never hang forever.
4. Prefer **one file** per sync-wait call; multi-file packs stay on async.
5. **Webhook:** **suppressed** for sync-wait submissions (**C2**). Async/email still fire per Document.
6. Sync must **not** block other tenants’/files’ processing (§9.3) — waiting is per request, not a global lock.

### 10.2 Async delivery

- **Push:** webhook **per Document**.  
- **Poll:** documents, files, optional batch id filter.  
- No primary “Delivered” status; webhook metadata on Document.

---

## 11. Status design (summary)

| Object | Public statuses |
|---|---|
| Document | Received, Processing, Ready, Failed, Rejected, Cancelled |
| File | + PartialReady; Cancelled on whole-file cancel |
| Batch | Log record only — no customer delivery state machine |

See Queue design for rollup rules and stages.

---

## 12. Agents vs queues (frozen split)

### What “Agent” means (DECIDED vocabulary)

**Agent = AI Agent** in product language. Not every pipeline step is a product Agent.

| Kind | Examples | User-cloned? | On QueueRoute? |
|---|---|---|---|
| **Customer AI Agent** (`Agent` entity) | Invoice / DN extraction + that agent’s post-processing | Yes (templates) | **Yes** |
| **System AI Agent** | Classify, email intake decision | No (platform) | **No** |
| **Capability (not an Agent)** | OCR adapters, deterministic split, storage | n/a | **No** |

### Customer AI Agent
- Schema + extraction instructions
- **Post-processing workflow** for this agent / document type (user-customizable day one)
- Created from platform templates (guided clone)
- Reusable across queues via QueueRoute

### Queue
- Department / stream separation — **multiple Queues are first-class day one** under a Business
- Postback URL, email address, allowlist
- Holds **type → Customer AI Agent** routing map (locked once queue has files)
- Does **not** own post-processing

---

## 13. Post-processing & workflow — DECIDED (**Agent-primary**)

### Attach point: **Agent** (document-type wise via routed Agent)

After extract + schema validate, run the **Customer AI Agent’s** workflow (the Agent selected by QueueRoute for that Document’s type).

| Attach on | Phase 1 |
|---|---|
| **Agent** | **Primary** — customize per cloned agent / document type from day one |
| **Queue** | **Not** the owner; no queue-level workflow override in Phase 1 (revisit later only if a whole lane must force one workflow) |

**Phase 1 shape:** attach/enable platform steps or simple config on the Agent (full NL workflow authoring still later).  
**Tools:** platform-only via **internal MCP**.  
**MCP:** Internal tool bus for post-processing; customer MCP registrations later. **Do not** wrap OCR/LLM providers as MCP.

---

## 14. Email intake — DEEP DESIGN

Email is not “upload with a different transport.” It is an **unauthenticated intake channel** with messy human behavior. Design it as: **gates → intake decision → normal Documents**.

### 14.1 Why “one customer owns the queue” does not equal security

The inbound address is a **capability secret**, like an unauthenticated webhook URL.

| Myth | Reality |
|---|---|
| “Only our company knows the address” | It leaks via forwards, auto-CC, screenshots, onboarding docs, departed employees |
| “We can trust From:” | SMTP From is trivial to spoof; without SPF/DKIM alignment checks + allowlist policy, From is advisory |
| “Worst case is a failed extract” | Worst case is **paid OCR/LLM**, **poisoned ERP data**, or **malware handling** |
| “Spam filters will save us” | Business PDFs look like legitimate mail; content filters won’t know fake invoices |

### 14.2 Threat / failure scenarios (product-relevant)

| Scenario | Mechanism | Impact | Primary control |
|---|---|---|---|
| Address leak | Forward chain includes queue address | Outsider injects docs | Rotate address; allowlist |
| Cost attack | Burst of large PDFs/images | Bill shock / capacity soak | Rate/size limits; allowlist; disable |
| Data poison | Plausible fake invoice PDF | Wrong payments if webhook trusted blindly | Allowlist; customer-side validation; audit `source=email` |
| Reply-all accidents | User replies to thread copying queue address | Duplicate / noise Documents | Idempotency / duplicate detection later |
| Signature-only mail | Empty “please see attached” with no attach | Junk Files / confused intake | Intake agent → `Rejected` |
| Body-is-the-invoice | Supplier pastes text invoice in body | Real work hiding in body | Intake agent must consider body |
| Embedded image invoice | Invoice is inline image, not attachment | Missed doc if attachments-only | Intake agent + inline extraction policy |
| Encrypted / password PDF | Common in finance | Cannot OCR | `Rejected`/`Failed` with clear code |
| Malware / macro docs | Hostile office files | Security incident | Type allowlist + scanning before AI |
| Predictable address | `acme-ap@documate…` | Easy discovery | Random token local-part |
| Employee leaves | Still knows address | Insider/ex-insider inject | Allowlist + rotate |

### 14.3 Layered defense (recommended)

Think in layers. Do not rely on the intake AI alone — it costs money and can be wrong.

```
Email received
  → Layer 0: infrastructure accept (MX, size ceiling)
  → Layer 1: hard gates (cheap, deterministic)
  → Layer 2: intake decision agent (smarter, still bounded)
  → Layer 3: normal Documents
  → Layer 4: customer webhook/poll (they still own business trust)
```

#### Layer 1 — Hard gates (before AI spend)

| Gate | Phase 1? | Notes |
|---|---|---|
| Email intake enabled on queue | Yes | Kill switch |
| Unguessable address | Yes | Rotate without changing queue identity |
| Attachment count / total size caps | Yes | Hard ceilings |
| Allowed file types | Yes | pdf/jpg/png/docx/xlsx/txt… |
| Basic malware / content scan | Yes if feasible | At least block executables |
| Sender allowlist | **Design yes; enforce optional** | Strongest control — see §14.4 |
| Rate limit per queue / per sender | Yes light | Soft then hard reject |
| Subject shared secret | Later / premium | High-security tenants |

Failed hard gate → public `Rejected` (+ gate event). **No OCR/LLM.**

#### Layer 2 — Intake decision agent

Inputs (conceptual): From/To/Cc, subject, text body, HTML→text, attachment list + types, optional inline images, allowlist match result.

Outputs (conceptual decision record):
- `action`: `process` | `reject` | `process_partial`
- `targets[]`: each `{ kind: body|attachment|inline_image, ref, reason }`
- `context_links`: body notes that should travel with an attachment Document (not necessarily a separate extract)
- `confidence` / `reasons` for audit

**Decision policy — DECIDED:**

| Situation | Decision |
|---|---|
| Clear business attachments | Process attachments; body may also become a Document if it looks like a document |
| Body as document | **Allowed** even when attachments exist |
| No attachments, body looks like invoice/DN | Process body as document |
| Ambiguous / noise / unclear | **`Rejected`** (bias to reject) |
| Inline image is the only document | Process inline image |

#### Layer 3+ — Same as API path

Each accepted target becomes its own **File** → split/classify → Documents. Optional shared `email_message_id`. File webhook includes original file (non-API source).

### 14.4 Allowlist design (most important control)

Unguessable addresses reduce casual abuse; **allowlists reduce intentional abuse**.

| Mode (`allowlist_mode` CorEnum on Queue) | Behavior |
|---|---|
| `open` | Anyone who knows address can submit (phase 1 possible default) |
| `allowlist_preferred` | Unknown senders accepted but flagged / quarantined (later) |
| `allowlist_enforced` | Unknown senders → `Rejected` at Layer 1 |

Persisted on Queue as `AllowlistModeEnumId` → CorEnum (not a CLR enum column).

Allowlist entries: exact emails and/or entire domains (`@supplier.com`).  
Also decide: check **envelope sender**, **header From**, or **DKIM-aligned From** (best). Product language can say “trusted senders”; strength depends on alignment checks.

**Rotation:** Regenerating the queue address invalidates leaks without deleting the queue. Customers must update forward rules — treat as a deliberate security action.

### 14.5 What one inbound email becomes

| Email content | Files / Documents | Webhook |
|---|---|---|
| 3 PDF attachments | Batch (log) + 3 files | **per Document** (includes file) |
| 1 PDF + body-as-document | Batch (log) + 2 files | per Document |
| 1 PDF + noise body | 1 file (no batch) | per Document |
| Body-only invoice | 1 file | per Document |
| Ambiguous / noise | **IntakeRejection** (no file) | none |

### 14.6 Duplicate & thread hazards (design awareness)

- Forwarding the same invoice twice → two Files unless we add fingerprinting later.
- “Reply with thanks” including quoted history → intake agent must not reprocess quoted old invoices blindly.
- Calendar invites / winmail.dat → type gates.

Phase 1 can accept duplicates; document the limitation.

### 14.7 Customer responsibilities

Documate gates reduce risk; they do **not** make email a trusted payment instruction channel by themselves. Customers should keep allowlists tight when enforced, treat webhook data as unverified until their own AP rules run, and rotate addresses on staff changes / leaks.

### 14.8 Phase posture — DECIDED

| Phase | Email posture |
|---|---|
| **Phase 1** | Unguessable address + enable/disable + size/type/rate limits; intake decision agent; design allowlist model; do not sell email hard until allowlist UX is ready |
| **Before selling email hard** | Sender allowlist (enforceable) + rotate address UX |
| **Later** | Quarantine, subject tokens, duplicate fingerprinting, DKIM-aligned trust tiers |

---

## 15. Identity: Iden — Tenant + Business; machine clients DEFERRED

**Iden hierarchy (given):** two levels of tenant management —

| Level | Role |
|---|---|
| **Tenant** | Top org (customer account). |
| **Business** | Child under a Tenant. Working unit users operate in. |

Users, membership, and access rules across Businesses are **Iden’s job**. Documate consumes claims / context; it does not store a parallel org tree beyond thin mirrors for Documate-specific settings.

**Documate mapping (DECIDED for Phase 1 posture):**
- Mirror **Tenant** → `CorTenant` (e.g. ProviderMode).
- Mirror **Business** → `CorTenantBusiness`.
- **Isolation unit** for Agents, Queues, Files, Documents, IntakeRejections = **Business** (`BusinessId` only on those rows). Tenant name for UI = `CorTenantBusiness.TenantName` projection.
- UI/API always act in a **selected Business** context (from Iden token / session).
- Queue “dept / sub-client” remains a **Queue** under a Business — do not collapse Iden Business into Queue.

Human users: Iden. Users can create agents, queues, schemas, webhooks **inside Businesses they can access**.

Machine clients / API M2M credentials: **deferred**. When designed, bind to Iden **Tenant and/or Business** (not a Documate-only org invent). Temporary External API keys (Decision F) must still resolve to a Business scope.

---

## 16. Mapping catalogs (later phase)

Later-phase enrichment; conceptually post-extract / workflow. Reserve schema field intent for IDs where templates need them.

---

## 17. Non-goals for this exploration

- Choosing databases, queues tech, languages, frameworks.
- Final API routes / webhook JSON schemas.
- Full Document Queue state machine detail (dedicated doc — uses §9–§11).
- Implementation plan / estimates.
- Pricing / GTM.
- HITL / confidence UX (later).
- NL workflow authoring deep rules (later).
- Machine client identity model (deferred).
- White-label packaging / schema-from-types SDKs (major later phase §3.1 — not designed here).
- Statements reconciliation (separate later phase §3.2 — not designed here).
- MCM delivery-note rebranding (MCM-only later phase §3.3 — not designed here).
- Multi-file upload that globally blocks other processing (forbidden — Queue §2.1).

---

## 18. Decision log

| # | Topic | Decision |
|---|---|---|
| 1 | Modules | Keep three separate |
| 2 | Agents | Guided clone from templates |
| 3 | Schema | Binds to Agent |
| 3b | Queue ↔ Agent | Type → Agent routing map |
| 4 | Multiplicity | Multi-doc/multi-type inside file; multi-file intake required |
| 5 | Email | Body + attachments; Batch if ≥2 targets (log); ambiguity → IntakeRejection |
| 5b | Email ambiguity | Bias to **reject** |
| 6 | Email abuse | Unguessable + limits; allowlist before selling hard |
| 7 | Batch | **Optional log-only** when ≥2 files in one intake |
| 7b | Multi-file upload | **Required**; non-blocking concurrent enqueue |
| 8 | Agent depth | Guided clone |
| 9 | Providers | Mode 1 only in phase 1 |
| 9b | Provider catalog | **Flat model/engine keys** — no company→model hierarchy |
| 10 | HITL | Later |
| 11 | File in webhook | Non-API sources only (on **document** webhook) |
| 12 | Poll | Documents / files / optional batch_id |
| 13 | Statuses | Doc + File (+ PartialReady); Batch not a delivery SM |
| 13b | Unroutable type | Document **Failed** |
| 13c | Webhook | **Per Document** (async path) |
| 13d | API styles | Most APIs **async** (webhook + poll); **one sync wait API** returns final result in-call |
| 14 | Workflow | **Agent-primary** (per cloned agent / doc type); Queue not owner in Phase 1 |
| 15 | NL workflow | Later |
| 16 | Tools | Platform-only v1 |
| 17–18 | MCP | Internal post-processing phase 1 |
| 19 | Machine identity | Deferred |
| 19b | Iden tenancy | **Tenant → Business**; Documate isolation = Business |
| 20 | Who configures | Users |
| 21 | Routing lock | Once queue has files |
| 21b | Multi-queue | **First-class day one** under Business |
| 22 | Cancel | File and Document |
| 23 | Reprocess | Explicit only; new File |
| 24 | Refuse with no file | IntakeRejection |

---

## 19. Remaining open items

None blocking. Deferred by choice:
- Machine clients / Iden M2M
- HITL / confidence UX
- NL workflow deep rules
- Mapping catalogs phase
- **§3.1 White-label + developer SDKs** — own later product phase
- **§3.2 Statements reconciliation** — own later product phase (match engine ≠ extraction)
- **§3.3 MCM DN rebranding** — MCM-only; own later product phase (render/replace pipeline)
- **§3.4 Document classification strategy** — deferred brainstorm (options/ways); bring forward at split/classify (e.g. DQ-0702)

---

## 20. Mental design freeze (current)

1. Three modules; guided-clone **Customer AI Agents** (schema + post-process); System AI Agents platform-only; routing map locked once files exist; **multi-queue day one**.
2. Hierarchy: Iden **Tenant → Business** → optional **Batch (log)** → **File** → **Document**; multi-doc/multi-type inside a File.  
   (**Job** is not a product term. Queue ≠ Iden Business.)
3. Multi-file API/email intake **required**; must **not** block other processes.
4. Most APIs **async** (webhook + poll); **one sync wait API** (single-doc only, 60s, no webhook); timeout → ids + poll.
5. Webhooks **per Document** on async path; file bytes only for non-API sources.
6. Cancel file + Document; reprocess explicit only.
7. Email intake agent; IntakeRejection; Mode 1; MCP internal for post-processing.
8. Later / deferred captured only: white-label SDK (§3.1), statements reconciliation (§3.2), MCM DN rebranding (§3.3), **classification strategy brainstorm (§3.4)**.
9. Next: Implementation plan / DQ execution (Phase 1); bring §3.4 forward when hitting classify.

---

## 21. Next documents

1. **Document Queue design** — [02-document-queue-design.md](./02-document-queue-design.md).  
2. **Implementation plan** — [03-documate-v3-implementation-plan.md](./03-documate-v3-implementation-plan.md) (draft — pending A–G).

---

## 22. Revision log

| Date | Change |
|---|---|
| 2026-07-31 | Initial exploration through cancel/reprocess decisions. |
| 2026-07-31 | Temporarily removed Batch / single-file-only API. |
| 2026-07-31 | Optional log-only Batch; multi-file non-blocking intake; webhook per Document. |
| 2026-07-31 | **Dual API styles: async default + one sync wait API for client-side results.** |
| 2026-08-01 | **§3.1 major goal (later):** white-label + zero-friction SDKs (C# NuGet / schema-from-types / platform agent instructions). |
| 2026-08-01 | **§3.0–3.3:** product phase roadmap; §3.2 statements reconciliation; §3.3 MCM-only DN rebranding — each own later phase. |
| 2026-08-01 | **Iden two-level tenancy:** Tenant → Business; Documate isolation = Business; Queue ≠ Business. |
| 2026-08-02 | **Provider catalog:** flat keys (`gpt_5_6`, `claude_sonnet_6`, …) — no company→model tree. |
| 2026-08-02 | Rename mirrors: `CorTenant`, `CorTenantBusiness` (was TenantAccount / BusinessAccount). |
| 2026-08-02 | Lean tenancy: BusinessId-only on ops; TenantName projection on CorTenantBusiness. |
| 2026-08-02 | **Agent = AI Agent** vocab; System AI vs Customer AI vs capabilities; **workflow Agent-primary**; multi-queue day one. |
| 2026-08-02 | Sync-wait DECIDED: single-doc fail-if-multi, 60s, C2 no webhook; E3 async multi-doc kept. |
| 2026-08-02 | **§3.4 deferred brainstorm:** document classification strategy / options — bring forward at split/classify (e.g. DQ-0702). |
