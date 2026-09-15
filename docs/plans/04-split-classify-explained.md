# Split & Classify — Plain-Language Guide

> **Audience:** Developers and product owners who want the locked design without reading the full exploration.  
> **Status:** Explains decisions locked **2026-09-15** (Plan 04 / Plan 03 Decision E).  
> **Canonical detail:** [04-split-classify-strategy-exploration.md](./04-split-classify-strategy-exploration.md) · Plan 03 Decision E  
> **Not yet in production code:** DQ-0702 only shipped the skeleton. Real P9 behavior is Wave 4b (follow-on DQ).

---

## 1. What problem this solves

A partner uploads **one PDF** (or email attachment). That file may contain:

- one invoice, or  
- several invoices, or  
- a mix (invoice + delivery note + credit note), or  
- one invoice that spans many pages.

Documate must:

1. **Split** — decide which pages belong to which logical business document.  
2. **Classify** — assign each logical document a **DocumentType** (invoice, DN, …).  
3. **Route** — pick the Agent for that type via **QueueRoute**.  
4. **Extract** — fill the Agent schema (separate step / separate model).

**Important vocabulary**

| Term | Meaning |
|------|---------|
| **File** | The stored upload (one PDF/blob). Owns the pipeline and rollup status. |
| **Document** | One logical business doc after split (e.g. INV-1001). Webhook unit. |
| **Split** | Page grouping — *where does each document start and end?* |
| **Classify** | Type assignment — *is this an invoice or a DN?* |
| **Intelligence (identification)** | Early LLM pass that reads a page for evidence (numbers, start/continue/complete). **Not** full extraction. |
| **Extract** | Later LLM pass that fills the Agent’s business schema. **Always separate** from intelligence. |

**Type is not the split key.** Two consecutive invoices share type `invoice` but are **two Documents** (INV-1001 vs INV-1002). We group by **document identity** (invoice number, etc.), not by type.

---

## 2. End-to-end pipeline (big picture)

```mermaid
flowchart TD
  A[File accepted<br/>API or email] --> B[Store original bytes<br/>object storage]
  B --> C[Normalize / OCR<br/>measure pageCount<br/>per-page text+layout]
  C --> D{documentTypeKey<br/>AND pageCount == 1?}
  D -->|Yes Case A| E[One Document<br/>stamp type]
  E --> R[Route via QueueRoute]
  R --> X[Extract with extract model]
  X --> W[Webhook + poll]

  D -->|No| F[Page intelligence<br/>T1 cheap model]
  F --> G{Page recognized<br/>enough evidence?}
  G -->|No| H[Fallback model<br/>same page or 3-page window]
  G -->|Yes| I[Page profile stored]
  H --> I
  I --> J[App grouping engine<br/>anchors + signals]
  J --> K{Grouping OK?}
  K -->|Unresolved| FAIL1[Failed<br/>no forced cut]
  K -->|OK groups| L[Classify after group]
  L --> R2[Route]
  R2 --> S[Persist text+layout slices]
  S --> X2[Extract per Document<br/>extract model]
  X2 --> W
```

Stages always exist in order: **normalize → split → classify → route → extract**.  
Case A only **skips** the work inside split+classify; route and extract still run.

---

## 3. When do we skip work? (intake hints)

The caller may send `documentTypeKey`. They do **not** send page count — **OCR measures** it.

```mermaid
flowchart TD
  H{Caller sent<br/>documentTypeKey?}
  H -->|No| FULL[Full split + classify]
  H -->|Yes| P{Measured pageCount}
  P -->|1| SKIP[Skip split AND classify<br/>one Document of that type]
  P -->|greater than 1| SPLIT[Always run split<br/>Skip classify<br/>stamp type on every group]
```

| Situation | Split? | Classify? |
|-----------|--------|-----------|
| Type + **1 page** (Case A) | No | No |
| Type + **multi-page** | **Yes** | No (stamp caller type on each group) |
| No type | **Yes** | **Yes** (after grouping) |
| Queue has **exactly one** QueueRoute type (C1) | Yes (unless Case A) | No — stamp that single type after grouping |

`documentCount` from the caller, if ever present, is **audit only** — it never skips split.

---

## 4. Which models are used, and when?

There are **always two model roles**. Admins configure them in backoffice / system settings (partners never pick models).

```mermaid
flowchart LR
  subgraph Intelligence["Role A — Identification"]
    T1[T1 cheap model<br/>default for each page]
    FB[Fallback model<br/>stronger / more expensive]
    T1 -->|page not recognized<br/>or ambiguous| FB
  end

  subgraph ExtractRole["Role B — Extraction"]
    EX[Extract model<br/>Agent schema fill]
  end

  Intelligence -.->|never the same call| ExtractRole
```

| Step | Model | When it runs | What it returns |
|------|--------|--------------|-----------------|
| **Intelligence T1** | Cheap (admin-configured) | Every page that needs split (not Case A) | Page evidence: primary number, type guess, start / continue / complete, evidence list |
| **Intelligence fallback** | Expensive (admin-configured) | T1 insufficient / unrecognized / conflicting signals | Same evidence shape, higher quality; may use a **local 3-page window** (prev, current, next) |
| **Extract** | Separate extract model (admin-configured) | After grouping + route, **per Document** | Full Agent `documentData` / schema |

### What “fallback” means in practice

```mermaid
flowchart TD
  P[Page N needs intelligence] --> T1[Call T1 cheap model]
  T1 --> OK{Enough evidence?<br/>identity / boundary clear enough}
  OK -->|Yes| STORE[Store page profile<br/>log model_tier = T1]
  OK -->|No| FB[Call fallback model<br/>page or N-1,N,N+1 window]
  FB --> OK2{Enough now?}
  OK2 -->|Yes| STORE2[Store page profile<br/>log model_tier = fallback]
  OK2 -->|No| UNRES[Mark ambiguous / unresolved<br/>do not invent a cut]
```

**Hard rules**

- Identification and extract are **never** one combined LLM call.  
- Fallback is for **intelligence only** — it does not run extract.  
- Escalation must **not** invent DocumentTypes outside the Queue’s QueueRoute list.  
- Per-File **call caps** stop poison files from burning unlimited $ (fail fast when exceeded).

---

## 5. How splitting works (app owns the cut)

The LLM does **not** say “split the PDF at page 3.”  
It produces **evidence per page**. The **application** groups pages.

### 5.1 What the app looks at (simple)

| Strong signals (prefer) | Weak / supporting only |
|-------------------------|-------------------------|
| Primary document number (INV-1001) | Document **type** alone |
| Continuation language / line items continue | Logos / visual similarity |
| Blank page as separator | Filename heuristics |
| “Page x of y” when printed | — |

### 5.2 Active anchor

While reading pages in order, the app keeps an **active document** (anchor) keyed by primary identity.

**Close the current document when:**

1. **Blank page** — close before the blank; skip the blank.  
2. **New primary identity** — different invoice/DN number → start a new group.  
3. **Printed sequence restart** — e.g. a new “page 1 of …” after a finished sequence.  
4. **N = 2** consecutive pages with **no continuation evidence** while an anchor is open → stop attaching; treat as **unresolved / Failed** (do not keep swallowing pages into the wrong doc).

```mermaid
flowchart TD
  START[Start: no open document] --> PAGE[Next page profile]
  PAGE --> BLANK{Blank page?}
  BLANK -->|Yes| CLOSE1[Close open group if any]
  CLOSE1 --> PAGE
  BLANK -->|No| NEW{New primary identity?}
  NEW -->|Yes| CLOSE2[Close open group]
  CLOSE2 --> OPEN[Open new group with this identity]
  OPEN --> PAGE
  NEW -->|No| CONT{Same identity or<br/>continuation signals?}
  CONT -->|Yes| ATTACH[Attach page to open group]
  ATTACH --> PAGE
  CONT -->|No| WEAK{Open anchor but<br/>2 pages with no support?}
  WEAK -->|Yes| FAIL[Failed / unresolved<br/>stop attaching]
  WEAK -->|No| DECIDE[Open new or keep<br/>per remaining rules]
  DECIDE --> PAGE
```

### 5.3 After groups exist

For each group the app:

- Sets page range (`PageStart` / `PageEnd`).  
- Writes **text + layout slices** to object storage (S3 hybrid).  
- Keeps the **original File** (customer source PDF) — File-level download URL.  
- Caches signed URL + expiry on File (original) and on Document (**Document PDF only**).  
- **Document URL rule:** return/omit — **null until that Document’s PDF is generated**. Never use the parent File URL as the Document URL.  
- Materialize **per-Document PDF** after grouping so Document URLs can be issued.

---

## 6. How classification works (after split)

Classify runs **after** grouping — never “type each page then merge same types” (that would glue INV-1001 and INV-1002 together).

```mermaid
flowchart TD
  G[Document group ready] --> C1{Queue has exactly<br/>one QueueRoute type?}
  C1 -->|Yes| STAMP1[Stamp that type]
  C1 -->|No| HINT{Caller documentTypeKey?}
  HINT -->|Yes| STAMP2[Stamp caller type<br/>skip classify LLM]
  HINT -->|No| INT[Use intelligence documentType<br/>must be in QueueRoute set]
  INT --> OK{Type clear after<br/>T1 / fallback?}
  OK -->|Yes| STAMP3[Stamp that type]
  OK -->|No| FAIL[Failed unroutable_type]
  STAMP1 --> ROUTE[Route → Agent]
  STAMP2 --> ROUTE
  STAMP3 --> ROUTE
  FAIL --> STOP[No extract for that Document]
  ROUTE --> EXT[Extract with extract model]
```

---

## 7. Failure handling (what “Failed” means)

**Principle:** A wrong silent cut is worse than a clear failure. We **do not force** a split when evidence is bad.

```mermaid
flowchart TD
  F[Failure detected] --> KIND{Which kind?}
  KIND -->|Unknown documentTypeKey at upload| U400[HTTP 400 or worker<br/>Failed unroutable_type]
  KIND -->|No QueueRoute for type| UR[Failed unroutable_type]
  KIND -->|Split unresolved<br/>after T1 + fallback| UF[Failed<br/>clear failure code]
  KIND -->|Intelligence call cap exceeded| UC[Failed fail-fast<br/>poison file]
  KIND -->|Extract / validate error| UE[Document Failed<br/>reprocess possible]

  UF --> OBS[Artifacts + WorkEvents<br/>show anchors, signals, tiers]
  UR --> OBS
  UC --> OBS
  UE --> OBS
```

| Failure | Typical cause | What we do |
|---------|---------------|------------|
| `unroutable_type` | Type missing from QueueRoute, or no-hint type still unclear | Document **Failed**; no wrong Agent |
| Split unresolved | Conflicting identity / no continuation after fallback / N=2 rule | **Failed** — do **not** invent page cuts |
| Call cap | Too many intelligence/fallback calls on one File | **Failed** fast; protect cost |
| Wrong caller type | Partner asserted wrong `documentTypeKey` | Still routes to that type’s Agent — hints are **assertions** |
| Extract failure | Schema / LLM extract issues | Document Failed; split may still be valid; **reprocess** can retry |

**File rollup:** If some Documents succeed and others fail, File can be **PartialReady** (existing product status). Unresolved **groups** themselves are Failed — we don’t mark a bad cut as Ready.

**Webhooks & APIs (PDF URLs — locked 2026-09-15, Document rule amended):**

| Surface | Returns |
|---------|---------|
| Webhook `document.terminal` | Extracted `data` + Document PDF URL **only if materialized** (else omit) |
| External Document detail | Full Document; `download_url` **only if** Document PDF exists |
| External File detail | File `download_url` (original upload) + **full Document objects** (each Document URL per rule above) |

Cache URL+expiry on rows; refresh when expired. **Never** map Document URL → parent File. Slice text/layout stays internal.

**Reprocess:** Creates a new File from the same bytes and runs the pipeline again (existing product behavior).

---

## 8. Worked example

**Input:** 5-page PDF, no `documentTypeKey`. Queue routes: `invoice`, `delivery_note`.

| Page | Intelligence (T1 → maybe fallback) | App decision |
|------|-------------------------------------|--------------|
| 1 | INV-1001, starts, incomplete | Open group A |
| 2 | INV-1001, continues, complete | Attach to A; close A |
| 3 | INV-1002, starts+complete | Open+close group B |
| 4 | DN-88, starts, incomplete | Open group C |
| 5 | DN-88, continues, complete | Attach to C; close C |

**Result**

- Document A → classify `invoice` → invoice Agent → extract model  
- Document B → classify `invoice` → invoice Agent → extract model  
- Document C → classify `delivery_note` → DN Agent → extract model  
- Three webhooks (async path)

**If pages 4–5 stay ambiguous after fallback:** groups A and B can still Ready; C → **Failed**; File may be PartialReady.

---

## 9. Models vs pipeline steps (cheat sheet)

| Pipeline step | Uses LLM? | Which model | Notes |
|---------------|-----------|-------------|--------|
| Normalize / OCR | OCR provider (not this decision) | Textract / etc. | Produces per-page text+layout |
| Case A skip | No | — | Type + 1 measured page |
| Page intelligence | Yes | **T1**, then **fallback** | Identification only |
| Grouping / anchors | No | — | Pure app rules |
| Classify (no hint, multi-type Queue) | Usually from intelligence fields | Same intelligence path | No separate “classify-only” model required by lock |
| Classify (C1 or type hint) | No | — | Stamp only |
| Route | No | — | QueueRoute lookup |
| Extract | Yes | **Extract model** | Always after route; never merged with T1 |

---

## 10. Observability (for debugging)

Every real-split run should leave a trail so the team can answer “why did it cut here?”

Persisted (conceptually):

- `intelligence.page.{n}.json` — profile, evidence, model tier used  
- Grouping log — which signals fired, anchor open/close, reset reason  
- Escalation reason — why fallback ran  
- Failure codes — unresolved / unroutable / cap  

Prefer **object-store artifacts** for bulky JSON; **WorkEvents** for queryable summaries (no full OCR dumps in events).

---

## 11. What is live today vs target

| Capability | Today (DQ-0702 skeleton) | Target (Wave 4b / P9) |
|------------|--------------------------|------------------------|
| Case A skip (type + 1 page) | Yes | Same |
| Real multi-doc page split | No (placeholder / deferred) | Yes |
| T1 + fallback intelligence | No | Yes |
| Dual extract model | Extract path exists separately | Remains separate by lock |
| Full signal/anchor audit | Limited | Required |

---

## 12. Related docs

| Doc | Role |
|-----|------|
| [04-split-classify-strategy-exploration.md](./04-split-classify-strategy-exploration.md) | Full options, signal dictionary, locks |
| [03-documate-v3-implementation-plan.md](./03-documate-v3-implementation-plan.md) | Decision E, Flow 1, Wave 4b |
| [00-product-glossary.md](./00-product-glossary.md) | File / Document / Queue terms |
