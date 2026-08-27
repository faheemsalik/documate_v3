# Documate v3 — Split & Classify Strategy (Exploration)

> **Status:** Exploration — brainstorm open for **real** split/classify; Phase 1 skip = type + one page only  
> **Type:** Product / Core pipeline exploration (feeds later split work after DQ-0702 Phase 1 slice)  
> **Upstream:** [01-project-exploration-mental-design.md](./01-project-exploration-mental-design.md) §3.4, §6.1 B; [02-document-queue-design.md](./02-document-queue-design.md) §6.3; [03-documate-v3-implementation-plan.md](./03-documate-v3-implementation-plan.md) Decision E3 + intake hints; [Documate_Multi_Document_Splitting_Final_Conclusions.md](./Documate_Multi_Document_Splitting_Final_Conclusions.md)  
> **Downstream:** After developer chooses remaining options → amend Plan 03 + implement **real split** (do not invent classify architecture inside a DQ)  
> **Created:** 2026-08-04  

**Goal of this doc:** Compare ways to **split** multi-doc Files and **classify** DocumentTypes that are **fast**, **cheap** (minimize LLM/OCR spend), and **reliable enough** for Phase 1 — plus decide **where split artifacts live**.

---

## Planning flow

| Phase | This document |
|-------|----------------|
| 1 — Exploration | **This file** |
| 2 — Amend implementation plan | Only after choices below are locked |
| 3 — DQ | Already exists as DQ-0702; evidence/technique filled after lock |

---

## 1. Problem Framing

After **normalize/OCR** (DQ-0701), a File has text/layout artifacts. The Core must produce **one or more Documents**, each with a **DocumentType**, then **route** to an Agent via QueueRoute.

| Concern | Why it hurts if wrong |
|---------|------------------------|
| **Split quality** | Wrong page cuts → bad extract, customer distrust |
| **Classify cost** | Full-file LLM on every upload burns $ and latency |
| **Classify reliability** | Unstable types → unroutable Failed / wrong Agent |
| **Artifact storage** | Re-OCR / re-download on retry is slow and expensive |
| **Caller hints** | Skip split+classify only when `documentTypeKey` **and** `pageCount==1`. Type-only multi-page still splits. |

**Design pressure (two schools — not merged yet):**

1. **Cascade-cheap (original §7 F4):** deterministic / cheap signals first; LLM only on ambiguity.  
2. **LLM-evidence then app-group (conclusions):** LLM produces **page semantic profiles**; the **application** groups pages and splits storage. Rules/regex are not the semantic engine.

---

## 2. Scope

### In scope

- Where to persist **split outputs** (page ranges, sliced text/layout, optional image crops)
- Techniques for **split** (page boundary detection)
- Techniques for **classify** (DocumentType assignment)
- Order of operations (split-then-classify vs joint vs classify-then-split)
- Interaction with **intake hints** (already locked in Plan 03 Flow 1)
- Cost / latency / reliability tradeoffs for Phase 1 vs later

### Out of scope (owned elsewhere)

- QueueRoute shape (frozen)
- Agent schemas / extract (DQ-0703)
- HITL UI product
- Real Textract body (still Mode 1 adapter; improve OCR independently)
- White-label / statements / MCM (§3.1–3.3)

---

## 3. Current-State Findings

| Piece | Today |
|-------|--------|
| Normalize | Artifacts: `…/artifacts/normalize.text.txt` + `normalize.layout.json` in **object storage** (local or S3) |
| Pipeline | normalize → split → classify → route → extract+validate → webhook; stages always exist |
| Predetermined type | Skip split+classify only if `documentTypeKey` **and** `pageCount==1`; typed multi-page stamps type after split |
| Without type | Split/classify deferred no-ops; one untyped placeholder → extract fails `no_agent` until real classify |
| Slice storage | No per-doc split blobs yet; `SliceRefJson` points at normalize artifacts |
| Label set | Platform `CorDocumentType` + QueueRoute subset |
| Sync-wait | Single Document only (DQ-0901); >1 Document after split → caller must use async |

---

## 4. Where to save split contents

Split produces **per-Document slices** of the File (logical, not necessarily physical PDF rewrite).

### Options

| ID | Store | What is saved | Pros | Cons |
|----|--------|---------------|------|------|
| **S1** | **Object storage only** (sibling keys under File) | e.g. `artifacts/docs/{n}/slice.text.txt`, `slice.layout.json`; optional `slice.pdf` later | Same pattern as OCR; cheap; survives restart; GDPR deletion = delete keys; no huge SQL | Need key convention; list/rebuild from DB refs |
| **S2** | **SQL only** (`OpsDocument.SliceRefJson` + text columns) | Page range + embedded text in DB | Simple queries | Large text in SQL; backups/PII surface; bad for big PDFs |
| **S3** | **Hybrid (recommended default)** | DB: `PageStart`/`PageEnd` + `SliceRefJson` **refs**; object storage: slice text/layout (and later binary slice) | Queryable ranges; blobs stay in object store; WorkEvents keep refs only | Two writes per Document |
| **S4** | **No materialize** — only page ranges; re-slice from normalize artifacts on demand | DB page ranges only | Least storage | Recompute on every extract/retry; couples extract to full-file layout |

### Recommendation (for discussion — not locked)

**S3 Hybrid:**  

```text
tenants/…/files/{fileSeq}/artifacts/normalize.*          # DQ-0701 (done)
tenants/…/files/{fileSeq}/artifacts/documents/{docSeq}/slice.text.txt
tenants/…/files/{fileSeq}/artifacts/documents/{docSeq}/slice.layout.json
# optional later: slice.pdf / page images
```

`OpsDocument`: `PageStart`, `PageEnd`, `SliceRefJson` = `{ textKey, layoutKey, … }`.  
Do **not** put full slice text in WorkEvents.

---

## 5. Split techniques

Assume normalize layout/text already exists. Goal: decide **page ranges** (or byte/region ranges) for each logical Document.

| ID | Technique | How | Speed | AI $ | Reliability | Pros | Cons |
|----|-----------|-----|-------|------|------------|------|------|
| **P0** | **Caller hints** | Complete: type **and** single-doc guarantee → no split | Instant | $0 | As good as caller | Shipped | Type **without** count still needs identity split (Case B) |
| **P1** | **Single-doc default** | MIME/ext: image/plain text → 1 doc, all pages | Instant | $0 | High for true singles | Cheap default | Wrong for multi-invoice PDF |
| **P2** | **Heuristic page breaks** | Rules on layout: blank pages, large heading jumps, “Page 1 of”, form headers, repeated letterhead | Fast | $0 | Medium | No LLM; tunable | Brittle across customers |
| **P3** | **TOC / barcode / QR** | Detect separators customers already print | Fast | $0 | High when present | Very cheap when signal exists | Rare; not universal |
| **P4** | **Embedding / clustering pages** | Embed page text; cluster consecutive pages | Medium | Low–med (embed API) | Medium | Better than pure rules | Needs embed model; tuning |
| **P5** | **LLM split** | Send page summaries or full text; ask for **ranges** | Slow | **High** | Variable | Flexible | Costly; LLM owns cut points (weaker than P8) |
| **P6** | **Hybrid cascade** | P1 → P2/P3 → only ambiguous packs to P4/P5 | Fast usual case | Low average | Good | Best cheap-first profile | More code paths |
| **P7** | **Always 1 Document for Phase 1** | Never split PDFs | Instant | $0 | Wrong for E3 multi-doc | Ships fast | **Conflicts with E3** unless hints-only multi-doc |
| **P8** | **LLM page intelligence + app grouping** | Per-page structured evidence (identity, refs, start/continue/complete); **app** groups pages | Med | Med (reuse extract on complete single-page) | High if identity is good | LLM does not cut files; fits many suppliers | Needs first-stage schema/prompt |
| **P9** | **Anchor + evidence fusion + model escalation** (§15 proposal) | P8 plus: identity **anchors** hold the open document, app-side confidence from fused evidence, cheap model default with escalation (stronger → local 3-page window → vision) only on ambiguity, reduced header/footer LLM input | Med (cheap tier) | Low–med average; spikes on ambiguous packs | Highest of the options if telemetry backs it | Explainable evidence; spend concentrated on hard pages; degrades gracefully | Most moving parts (tiering, confidence, escalation); needs per-page OCR + telemetry first |

### Split discussion notes

- E3 says multi-doc PDFs are first-class on async path → **P7 alone is not enough** unless product softens E3.
- Cheapest honest Phase 1 (already shipped): **P0 + C0**. Real split still needs a pick among **P2 / P6 / P8**.
- **P8 vs P5:** asking the LLM for page ranges (`"split this PDF"`) is weaker than asking for **identity + continuation + completeness** and letting Core group pages.
- **P8 vs P6:** conclusions argue pure regex/rules cannot own semantics across thousands of suppliers; P2 blank-page / “Page x of y” stay as **cheap supporting** signals, not the foundation.
- Physical PDF splitting (new PDF files) is **optional later**; logical page ranges + text slices are enough for extract.

---

## 6. Classification techniques

Goal: assign each Document (or each candidate slice) a **`CorDocumentType`** from the **Queue’s routable set** (types that appear in QueueRoute), not the entire global catalog when avoidable.

| ID | Technique | How | Speed | AI $ | Reliability | Pros | Cons |
|----|-----------|-----|-------|------|------------|------|------|
| **C0** | **Caller `documentTypeKey`(s)** | Skip classify | Instant | $0 | Caller-dependent | Already planned | Requires partner knowledge |
| **C1** | **Queue single-type short-circuit** | If QueueRoute has **exactly one** type → assign it | Instant | $0 | Perfect for single-purpose queues | Huge win for many customers | Multi-type queues need more |
| **C2** | **Filename / MIME heuristics** | `*invoice*`, `DN_`, content-type | Instant | $0 | Low–med | Free signal | Easy to spoof / miss |
| **C3** | **Keyword / regex on first N chars or first page** | Dictionary per DocumentType | Fast | $0 | Med for structured docs | No LLM; Queue-scoped vocab | Language/layout sensitive |
| **C4** | **Lightweight ML / embeddings** | Classify page/slice embedding vs type prototypes | Med | Low | Med–high with training | Cheap at scale | Needs seed examples / maintenance |
| **C5** | **LLM classify** | Prompt with type list + snippet/pages | Med–slow | **High** | Med–high with good prompt | Flexible | Cost; hallucination → must constrain to Route keys only |
| **C6** | **Two-stage** | C1–C3 first; **C5 only if confidence &lt; threshold** or top-2 close | Fast average | Low average | High | Best $/quality | Threshold tuning |
| **C7** | **Force type per Queue** (no classify) | Config: queue is invoice-only | Instant | $0 | High if true | Simplest ops | Less flexible than Route map |

### Classification discussion notes

- **Always restrict labels to QueueRoute DocumentTypes** (+ explicit `unknown` → Failed `unroutable_type`). Never free-text LLM labels.
- Send **slice text / first page**, not whole 100-page pack, when LLM is used.
- Prefer **C0 → C1 → C3 → C6** for Phase 1 cost control.
- Unreliable classify is worse than Failed: wrong Agent ⇒ wrong schema. Prefer **Failed + reprocess** over silent misroute (unless confidence is high).
- **Type ≠ split key.** Invoice / credit note / delivery note often share layout. Consecutive pages with the same type can still be **different documents** (INV-1001 then INV-1002). Classify chooses Agent/schema; **primary document number + continuation** choose page groups.

---

## 7. Pipeline shape options (split × classify)

| ID | Shape | Flow | Pros | Cons |
|----|-------|------|------|------|
| **F1** | Split then classify | Ranges → then type per slice | Clear; matches mental model | Classify N times |
| **F2** | Classify pages then merge consecutive **same type** | Type each page → merge | Mixed-type packs | **Type is a weak boundary** (invoice vs CN vs DN look alike; two invoices share a type). Prefer identity grouping (P8). |
| **F3** | Joint LLM | One call: ranges + types | One round-trip | Expensive; hard to validate; all-or-nothing |
| **F4** | Cascade (cheap-first) | Hints → single-route → heuristics split/classify → LLM fallback | Cheap happy path | Weak if rules cannot generalize |
| **F5** | Adaptive semantic (conclusions) | Case A skip → else page intelligence → group → extract once per group (reuse call if single-page complete) | Avoids merge-of-partial-extracts; type hint ≠ skip split | Early LLM cost; needs profile schema |
| **F6** | Adaptive + escalation (§15 proposal) | F5 plus explicit confidence gate: cheap tier → fuse evidence → escalate only uncertain pages/windows → groups → extract | Cost concentrated where it is needed; auditable evidence trail | Needs model tiers, confidence model, telemetry; more failure modes to test |

```text
normalize artifacts
  → if complete intake hints → Documents from hints → route
  → else if QueueRoute count == 1 and (image|text or heuristic single-doc)
        → 1 Document that type → route
  → else heuristic split (P2) → heuristic classify (C3) per slice
  → if any slice low confidence → LLM classify (C5) for those slices only
  → route / Failed unroutable
  → (extract later)
```

---

## 8. Cost & latency levers (checklist)

1. **Skip work** when Case A holds (single doc + type known) or single QueueRoute type allows.  
2. **Cap LLM context** for intelligence (identity-heavy regions, not whole pack).  
3. **Reuse one LLM call** when a page is a complete single document (intelligence + `documentData`).  
4. **Do not full-extract every page** then merge.  
5. **Batch** page embeddings locally if used; avoid per-page LLM.  
6. **Cache** classify result on `ContentHash` + QueueId (optional later).  
7. **Persist slices** (S3 hybrid) so extract/reprocess does not re-split.  
8. **Fail fast** unroutable instead of retrying LLM loops.  
9. **Mode 1:** platform picks models; customers don’t see provider knobs.

---

## 9. Risks and Constraints

| Risk | Mitigation |
|------|------------|
| Heuristics fail on novel packs | P8 evidence + app grouping; Failed + reprocess; hints |
| LLM cost blow-up | Case A skip; compact prompts; reuse extract on complete singles; measure |
| Misclassify → wrong Agent | High threshold or Failed; never invent types outside Route |
| Type used as split key | Two invoices same type under-split; use primary identity |
| Merge of per-page extracts | Forbidden default (incomplete line items / totals) |
| Artifact sprawl | SequenceId paths; retention aligned with File purge |
| Over-building Phase 1 | P0/C0 already shipped; lock P6 vs P8 before coding real split |
| Sync-wait vs multi-doc | DQ-0901 fails if split yields >1 Document — async is the multi-doc path |

---

## 10. Open Questions (need developer input)

1. **Phase 1 split bar:** Accept heuristic-only split (P2) with Known limitations, or require LLM split fallback (P6) from day one?  
2. **Single-type queues:** Do we implement **C1 short-circuit** immediately (recommended)?  
3. **LLM provider for classify:** Same Mode 1 meta/LLM as extract, or smaller/cheaper classify model?  
4. **Confidence:** Numeric threshold + PartialReady vs binary Failed for ambiguous packs?  
5. **Materialize slice binaries?** Text/layout only in Phase 1, or also cropped PDF pages?  
6. **Partial hints:** Keep “run full pipeline” (current Plan 03) or reject incomplete hints with 400?  
7. **Page-level vs pack-level classify** when split is uncertain (F1 vs F2)?  
8. **P6 vs P8 for real split:** cheap heuristics-first, or LLM page intelligence + app grouping?  
9. **Type-only hint:** **Locked 2026-08-18** — skip split **and** classify only when `documentTypeKey` is set **and** the File has **one page**. Type-only multi-page does not skip split.  
10. **Single-page fast path:** first LLM call returns intelligence **and** Agent `documentData` when the page is complete (avoid a second extract)?  
11. **Corpus before lock:** measure primary-number presence/OCR/header vs references, multi-page rate, page-number availability, type-hint frequency (see §13).

---

## 11. Recommended Direction (proposal — **not locked**)

| Topic | Proposal |
|-------|----------|
| Split storage | **S3 Hybrid** — object store slices + DB page range + SliceRefJson |
| Cheap-first (original) | **F4 cascade:** P0+P1+C0+C1+C3 then P2/C6 |
| Semantic-first (conclusions) | **F5 + P8:** OCR → page intelligence → app groups → extract once per group; reuse LLM on complete single-page |
| Semantic + escalation (§15) | **F6 + P9:** P8 plus identity anchors, app-side confidence, cheap→premium→vision escalation on ambiguous pages only |
| Phase 1 already shipped | Skip split+classify only when `documentTypeKey` **and** `pageCount==1` |
| Defer until 8–11 answered | Physical PDF split, P4 embeddings, P5-as-range-cutter, HITL |
| Amend Plan 03 | Only after P6 vs P8 and remaining §14 rule conflicts are chosen |

---

## 12. Exploration Exit Criteria

This exploration is **ready for decisions** when the developer has chosen:

- [ ] Storage option (**S1–S4**)  
- [ ] Split set including **P8** vs **P2/P6**  
- [ ] Classify Phase 1 set (**C0–C7** subset)  
- [ ] Pipeline shape (**F1–F5**)  
- [ ] Answers to Open Questions §10  

Then:

1. Record decisions in this doc (Finalized Decisions).  
2. Amend [03-documate-v3-implementation-plan.md](./03-documate-v3-implementation-plan.md) (Decision E / Core pipeline).  
3. Execute **real split** against that locked technique (follow-on after DQ-0702 Phase 1 slice).

---

## 13. Semantic split conclusions (imported 2026-08-18)

Source: [Documate_Multi_Document_Splitting_Final_Conclusions.md](./Documate_Multi_Document_Splitting_Final_Conclusions.md). **Not locked** here — competing with F4/P6.

### Division of roles

| Layer | Owns |
|-------|------|
| OCR / Textract | Page text + layout (already DQ-0701 artifacts) |
| Early LLM | **Evidence:** type, **primary document number**, referenced numbers, start / continue / complete, page n of m |
| Application | Group pages, persist slices, decide when to extract |
| Extract LLM | Full Agent schema **after** grouping — unless the page was already a complete single-doc and `documentData` was returned in the first call |

Do **not** let the LLM cut files or be the only split authority. Do **not** ask it only “split this PDF.”

### Strong vs weak signals

| Strong | Weak / supporting only |
|--------|-------------------------|
| **Primary document identity** (invoice no, CN no, DN no, PO no, …) | Document **type** as a boundary (layouts look alike) |
| Continuation language, line-item continuation, totals on last page | Logo / header / footer / global visual similarity (thousands of suppliers) |
| Blank page as separator (cheap, rare) | Page numbers (use when present; many docs omit them) |
| Distinguishing **primary vs referenced** numbers (Against Invoice, Customer PO) | Filename heuristics |

Same-type consecutive pages can still be **two documents** (`INV-1001` then `INV-1002`). F2 “merge same type” is unsafe as the main rule.

### Boundary vs completeness (separate flags)

`startsDocument` ≠ `documentComplete`. Page 1 of 3 starts and is incomplete; page 3 of 3 continues and completes.

### Do not extract-every-page-then-merge

Full extract per page then merge line items/totals is a second hard problem. Group first; extract the **combined** slice once.

### Adaptive cases

| Case | When | Work |
|------|------|------|
| **A** | Workflow guarantees **one** document, **one** page, type known | Skip split/classify; existing extract |
| **B** | Unknown pack or possible multi-doc | Page intelligence → app grouping (even if type hint is set) |
| **C** | Multi-page group confirmed | One full extract on the grouped slice |

**Hint refinement vs today’s code:** `documentTypeKey` can skip **classify**. It does **not** skip identity/continuation split unless the caller also guarantees a single document (`documentCount=1` / single-page). A queue of invoices still needs INV-1001 vs INV-1002 cuts.

Phase 1 skips **both** split and classify only for Case A (`documentTypeKey` + one page). Type-only multi-page Files still run split (Case B); classify stamps the caller type until real split ships.

### Single-page cost trick (hypothesis, not architecture)

Many files are one page. If the first LLM call is confident `documentComplete`, return `documentIntelligence` **and** `documentData` (Agent schema) in one response and skip a second extract. **Do not hard-code “90%.”** Measure.

### Compact LLM input

Segmentation does not need every OCR byte. Prefer identity-heavy regions (often header); keep full OCR locally for later extract. Measure; do not hard-code a “top 50%” rule.

### What to measure on a real corpus (before locking P8)

Primary number present / OCR’d / in header vs reference; multi-page rate and pages/doc; continuation pages without numbers; page-number availability; how often type is already hinted; completeness mistakes; token $ and latency; reprocess rate; segmentation accuracy.

### Next design artifact if P8 is chosen

Exact **first-stage LLM JSON schema + prompt** and the **page semantic profile** stored next to normalize artifacts. Working signal dictionary: **§14**.

---

## 14. Signal dictionary (working — not locked)

Build **elements** (atomic facts) → **signals** (named combinations) → **rules** (evaluation order). The app owns grouping; LLM fills evidence fields; it does not return “split this PDF here.”

**Locked today:** Case A only (`received_doctype` + `page_count == 1` → skip split+classify). Everything else in this section is a working draft to iterate.

LLM input sizes below are **proposals to measure**, not hard-coded cuts. Full OCR stays in object storage for extract; intelligence calls should not send the whole File.

### 14.1 Element dictionary

How each atom is retrieved, and what text the LLM sees if it is used.

| Element | Grain | How retrieved | LLM input (if used) | Notes |
|---------|-------|---------------|---------------------|-------|
| `received_doctype` | File | Caller: upload/sync form `documentTypeKey` | None | Assertion, not OCR truth. Unknown key → 400 / `unroutable_type`. |
| `received_document_count` | File | Caller: optional `documentCount` | None | Audit/hint only. Does **not** skip split. |
| `intake_source` | File | API: `api` / `api_sync` / later `email` | None | Sync-wait still single Document. |
| `filename` | File | Original file name | None | Weak classify hint (`*invoice*`). Easy to spoof. |
| `content_type` | File | MIME / extension | None | Image/text → usually 1 page (P1). PDF → may be multi-doc. |
| `page_count` | File | Normalize: PDF catalog (PdfPig) or OCR page list; image/text = 1 | None | If PDF count unknown, treat as multi-page (do not skip). |
| `content_hash` | File | Hash of stored bytes | None | Later cache of classify/split. |
| `queue_route_type_count` | Queue | Count of QueueRoute rows | None | `== 1` → C1 short-circuit type. |
| `queue_routable_types` | Queue | QueueRoute DocumentType keys | Passed as **label allow-list** when LLM classifies | Never invent types outside this set. |
| `page_text` | Page | OCR / stub normalize artifact | Source for other LLM elements; not a signal by itself | Keep full text locally for extract. |
| `page_layout` | Page | OCR layout JSON (blocks, coords) | Optional: block types to pick header/footer/table bands | Prefer bands over whole page when measured. |
| `is_blank_page` | Page | Heuristic: near-zero OCR chars / coverage | None | Strong separator if genuinely blank. |
| `printed_page_index` / `printed_page_total` | Page | **First:** regex on header+footer band (`Page 1 of 3`, `1/2`). **Fallback:** LLM | Regex: header+footer text only. LLM fallback: **same bands only**, not body | Absent on many docs; use when present. |
| `barcode_or_qr` | Page | OCR / barcode scan | None | Rare explicit separator. |
| `heading_jump` | Page | Layout heuristic (font size / new form header) | None | P2 supporting break. Brittle. |
| `letterhead_repeat` | Page | Layout/visual heuristic | None | Weak across many suppliers. |
| `keyword_first_page` | Page | Regex/dictionary vs Queue types on first N chars | None | C3. Language/layout sensitive. |
| `llm_document_type` | Page or group | LLM classify, labels = `queue_routable_types` | **Per page or first page of group:** QueueRoute keys + compact identity region (header + first ~25–40 lines, or header layout blocks). Fallback: full **page** text. Never whole File. | Stamp `received_doctype` instead when Case B has a type hint. |
| `primary_document_number` | Page | LLM (preferred) or regex on labeled fields | **Identity region:** header + labeled ID lines (`Invoice No`, `CN No`, …). Fallback: full page. Never whole File. | **Strongest split key.** |
| `referenced_documents` | Page | LLM: numbers marked as Against / Customer PO / related DN | Same identity region as primary number (labels matter) | Do **not** use as a new-document boundary. |
| `vendor` / `customer` | Page | LLM | Identity / header region only | Supporting: same party across pages of one doc. |
| `document_date` / `currency` | Page | LLM | Header / totals band | Supporting fingerprint after grouping. |
| `starts_document` | Page | LLM | **Full page** text (title, “page 1”, new header). Optional: last ~15 lines of previous page | Boundary flag. Not the same as complete. |
| `continues_previous` | Page | LLM | Full page + last ~15 lines of previous page | Keeps INV-1001 p.2 with p.1 when number is missing. |
| `document_complete` | Page | LLM | Full page, especially bottom: totals, “end of invoice” | Close group when true **and** identity matches. |
| `continuation_language` | Page | LLM or cheap phrase list | Full page or footer+table | “Continued”, “carried forward”. |
| `line_item_continuation` | Page | LLM | Table region + last lines of previous page | Supporting continue. |
| `totals_on_page` | Page | LLM or regex (`Total`, `Amount due`) | Bottom band of page | Often last page of a doc; not a split by itself. |
| `visual_similarity` | Page-pair | Optional later embed/vision | Not default LLM | Supporting only; thousands of templates. |

**LLM budget (proposal):** one intelligence call **per page** (or batched pages with compact payloads). Identity elements share **one** prompt/schema (page semantic profile). Do not add a second LLM call per element. Extract LLM is a **later** call on the grouped slice, except Case A / complete single-page reuse.

### 14.2 Signals (element combinations)

| Signal | Combination | True when | Strength | Intended effect |
|--------|-------------|-----------|----------|-----------------|
| `sig_case_a` | `received_doctype` + `page_count == 1` | Type given and file is one page | **Locked skip** | Skip split **and** classify; one Document; route → extract. |
| `sig_typed_multipage` | `received_doctype` + `page_count > 1` | Type given, pack may hold several same-type docs | Strong “must split” | Do **not** skip split. Stamp type after groups exist (skip classify). |
| `sig_no_type` | no `received_doctype` | Caller did not name a type | — | Split + classify required. |
| `sig_single_route` | `queue_route_type_count == 1` | Queue has one Agent type | Strong classify skip | Assign that type; still split if multi-page. |
| `sig_p1_single` | `content_type` in image/text (and `page_count == 1`) | Typical photo/scan/txt | High for true singles | One Document; no PDF pack split. |
| `sig_blank_separator` | `is_blank_page` | Genuinely empty page | Very strong boundary | Close previous group; do not extract the blank page. |
| `sig_new_identity` | `primary_document_number` ≠ previous page’s primary (and not a `referenced_documents` hit) | Number changed | **Strongest cut** | Start a new Document group. |
| `sig_same_identity` | primary number equals previous (and not only a reference) | Same ID continues | Strong join | Same group. |
| `sig_identity_missing` | no reliable primary number | OCR/LLM miss or continuation page | — | Fall back to continue / page-n-of-m / complete / blank. |
| `sig_continue` | `continues_previous` OR `continuation_language` OR `line_item_continuation` | Page belongs to open doc | Strong join | Same group even if number absent. |
| `sig_start` | `starts_document` | New logical doc on this page | Strong cut unless `sig_continue` also true | Prefer identity if they conflict. |
| `sig_complete` | `document_complete` OR (`totals_on_page` + looks like end) | Group may close | Medium–strong close | Close group after this page if identity matches. |
| `sig_page_of_sequence` | `printed_page_index` / `printed_page_total` consecutive | `1/3 → 2/3 → 3/3` | Strong join/close when present | Join until index == total; then close. Reset if sequence restarts at 1. |
| `sig_type_changed` | `llm_document_type` ≠ previous | Type flipped | **Weak cut** | Do **not** split on type alone (invoice vs CN look alike; two invoices share type). |
| `sig_parties_match` | `vendor`/`customer` consistent with open group | Same parties | Weak join | Supporting only. |
| `sig_unroutable` | resolved type ∉ `queue_routable_types` | No QueueRoute | Terminal | Document Failed `unroutable_type`. |
| `sig_reuse_extract` | (`page_count == 1` OR (`starts_document` ∧ `document_complete` on same page)) and type known | Confident single complete doc | Cost save | Same LLM call may return intelligence **and** Agent `documentData`. Measure; do not assume ~90%. |

### 14.3 Rules (evaluation order)

Cheapest facts first. Stop when a rule decides skip vs group vs fail. Later rules do not override Case A.

| # | When | Then | Else |
|---|------|------|------|
| R1 | Normalize done | Always persist `page_count`, `page_text`, `page_layout`. Detect `is_blank_page` (no LLM). Try regex `printed_page_*` on header/footer. | — |
| R2 | `sig_case_a` | **Skip split+classify.** One Document of `received_doctype`. Route → extract. **Locked.** | Go R3 |
| R3 | `sig_p1_single` and (`received_doctype` or `sig_single_route`) | One Document; classify skipped if type known. | Multi-page or unknown type → R4 |
| R4 | `sig_typed_multipage` | Split **must** run. Type is known → skip classify after groups. | `sig_no_type` → classify after groups (C1 then LLM). |
| R5 | Any `sig_blank_separator` | Hard boundary: close open group before the blank; skip blank. | Continue |
| R6 | Need identity/continuation (not Case A) | Per remaining page: **one** LLM intelligence call with compact identity region + full page for start/continue/complete (see §14.1). Fill page semantic profile. | If LLM skipped/fails → P2 heuristics only (`heading_jump`, `sig_page_of_sequence`) and/or Failed + reprocess |
| R7 | App grouping (in order per page) | 1. Blank → R5. 2. `sig_new_identity` → new group. 3. `sig_same_identity` or `sig_continue` or `sig_page_of_sequence` join → append. 4. `sig_complete` or page-of last → close group. 5. `sig_start` without identity → new group if no open continue. 6. **Ignore `sig_type_changed` as a cut.** | Unresolved pages: keep with previous if `sig_continue`, else new group + low confidence |
| R8 | Groups exist | If `received_doctype` or `sig_single_route`: stamp type. Else `llm_document_type` constrained to `queue_routable_types` (prefer first page / majority in group). | `sig_unroutable` → Failed |
| R9 | Type + pages known | Extract **once per group** on combined slice. If `sig_reuse_extract`, reuse first-call `documentData` and skip second extract. | Never extract-every-page-then-merge |
| R10 | File rollup | Ready / PartialReady / Failed as today. Sync-wait fails if >1 Document. | Webhook per Document |

```text
elements (facts)
  → signals (named combos)
    → R2 Case A skip
    → else group by identity + continuation (R5–R7)
      → stamp or classify type (R8)
        → extract once per group (R9)
```

**Conflict policy (proposal):** `sig_new_identity` beats `sig_type_changed`. `sig_continue` + missing number beats a weak `sig_start`. `sig_blank_separator` always cuts. Referenced numbers never cut.

---

## 15. Option P9/F6 — adaptive anchor + escalation (imported 2026-08-19)

Source: [`02_GPT_Documate_Multi_Document_Splitting_Proposed_Solution_for_Dev_Agent.md`](./02_GPT_Documate_Multi_Document_Splitting_Proposed_Solution_for_Dev_Agent.md) (“Option E”). Recorded here as a **candidate option**, not locked. It competes with **P6 cheap-first** and extends **P8/F5**.

### 15.1 What it proposes

| Piece | Proposal |
|-------|----------|
| Pipeline | pages → OCR → cheap early LLM → page semantic fingerprint → identity/boundary engine → (escalate if uncertain) → groups → extract |
| Split authority | **App** groups and stores; LLM only supplies semantic evidence; LLM never cuts PDFs or touches storage |
| Strongest signal | **Primary identity + same vendor/customer** (stronger than totals, which delivery notes lack) |
| Anchors | A confirmed primary identity stays the **active document**; later pages attach to it until strong contrary evidence |
| Three flags | `startsNewDocument` / `continuesPrevious` / `documentComplete` kept separate |
| LLM input | Reduced region: top ~30–60% + bottom ~5%, **adaptive** by text density; full OCR kept for extract |
| Model tiers | T1 cheap default → T2 stronger on ambiguity → T3 vision/large context; escalate **pages or a local window**, not the file |
| Cross-page | Sliding local window (`N-1, N, N+1`) for continuation pages that carry no number |
| Confidence | Do **not** trust the LLM's own number; app fuses signals into its own confidence |
| Evidence | LLM returns `evidence[]` per assessment (explainable, debuggable, measurable) |
| Single-page reuse | One call returns `documentIntelligence` **and** `documentData`; becomes the final result |
| Anti-patterns | No regex-as-semantics, no logo matching, no type-as-splitter, no embeddings-as-identity, no full-file premium by default, no extract-every-page-then-merge |

### 15.2 Overlap with what this doc already holds

Already covered — the proposal reinforces, does not change: P8 evidence-then-group; identity over type; primary vs referenced numbers; blank page as strong mechanical cut; page `x of y` when present; boundary ≠ completeness; single-page reuse; forbid per-page extract merge; compact LLM input with no hard-coded 50%; Case A skip; corpus measurement before locking.

**New relative to P8/F5** (this is what makes it P9/F6):

1. **Anchors** — grouping keeps an *active document* keyed by identity, instead of re-deciding each page pair.
2. **Explicit model tiers with escalation triggers** — cheap default; premium only on listed ambiguity conditions.
3. **App-side confidence** distinct from LLM self-confidence, fused from ranked signals (§28 hierarchy).
4. **`evidence[]` as a required output**, not just verdict fields.
5. **Local 3-page window** as a defined escalation step before vision/large context.
6. **Vendor/customer continuity promoted** to a first-class continuation signal paired with identity.
7. **Named experiments A–F and a metrics list** to pick the production configuration.

### 15.3 Elements/signals this adds to §14

Add to the dictionary if P9/F6 is chosen:

| New element | Grain | How retrieved | LLM input |
|-------------|-------|---------------|-----------|
| `identity_confidence` | Page | LLM field, treated as **evidence only** | Same identity region |
| `identity_role` (primary vs reference) | Page | LLM: label semantics (`Invoice No` vs `Against Invoice`) | Identity region incl. field labels |
| `evidence_list` | Page | LLM `evidence[]` per boundary/continuation assessment | Same call, no extra tokens |
| `active_anchor` | Group | App state: identity of the open document | None |
| `app_segmentation_confidence` | Page-pair / group | App: fused from ranked signals | None |
| `model_tier_used` | Page | App: which tier answered | None |
| `escalation_reason` | Page | App: which ambiguity rule fired | None |
| `local_window_profile` | Page window | T2/T3 call over `N-1, N, N+1` | Reduced regions of 3 pages |
| `region_strategy` | Page | App: which reduction produced the input (A full / B 30+5 / C adaptive) | None — telemetry |

| New signal | Combination | Effect |
|------------|-------------|--------|
| `sig_anchor_continue` | `sig_same_identity` **+** `sig_parties_match` | Dominant join. Promotes today's weak `sig_parties_match` when paired with identity. |
| `sig_ambiguous` | conflicting identity vs continuation, reference-looks-primary, poor OCR, no number on a continuation page, contradictory neighbours | Trigger T2 escalation for that page/window |
| `sig_unresolved` | `sig_ambiguous` still true after T2/T3 | Do not force a cut — low-confidence group → review/Failed (policy is an open question) |

### 15.4 Where it conflicts with what is locked here

| Proposal | Our locked/current position | Resolution |
|----------|------------------------------|------------|
| Case A keyed on caller `documentCount = 1` + “single page guaranteed” | **Locked:** skip needs `documentTypeKey` **and** real `page_count == 1` from normalize | **Keep ours.** Callers can misstate count; page count is measured. `documentCount` stays audit-only. |
| “Split into physical pages” first, store documents “separately” | **S3 hybrid** target: page ranges + `SliceRefJson` + text/layout slices; physical PDF rewrite optional later | Keep S3 hybrid. Physical slicing is not required for extract. |
| Type hint “eliminates classification” | Same, but only after grouping | No conflict. We stamp the hint onto groups (already implemented). |
| Model choice discussed openly | **Mode 1:** platform picks providers; customers see no model knobs | Tiers must be internal config/`CorProvider` rows, never customer-facing. |

### 15.5 Gaps in our codebase before P9/F6 can run

1. **No real per-page OCR.** `Mode1OcrNormalizeAdapter` writes one text artifact and a synthetic page list; binaries get a stub. P9 needs genuine per-page text/layout (`normalize.page.{n}.*`) plus real blank-page detection. This lands in the OCR work, not the split DQ.
2. **No LLM is actually wired.** Extract is schema-guided fill; `documate_meta` is armed only when a key exists. Tiering has nothing to tier yet.
3. **No telemetry sink** for the §33 metrics. WorkEvents can carry counters, but a query surface is undecided.
4. **No confidence-to-status mapping.** We have Document `Ready`/`Failed` and File `PartialReady`. “Uncertain group” has no home yet (open question 4).

### 15.6 My suggestions on top of the proposal

1. **Make the boundary engine a pure function.** `IReadOnlyList<PageProfile> → IReadOnlyList<DocumentGroup>` with no DB, no HTTP, no LLM inside. It becomes unit-testable against fixture packs and it is the only place grouping rules live. It slots behind today's `IFileSplitStage`.
2. **Cap LLM spend per File.** `Pipeline:MaxIntelligenceCallsPerFile` and a max escalation count. Hangfire retries can otherwise multiply cost on a poison file. Exceeding the cap → fail fast, not silent degradation.
3. **Cache page profiles on `ContentHash` + page index.** Reprocess (`ReprocessOfFileId`) and Hangfire retries should not re-pay for identical bytes.
4. **Sync-wait must not run the full ladder.** `/extract` is single-Document with a 60s budget. Escalation to T2/T3 plus a 3-page window can blow it. Suggest: sync path allows Case A and the single-page fast path only; anything ambiguous returns `timedOut` and finishes async.
5. **Persist page profiles as artifacts, not WorkEvents.** `…/artifacts/intelligence.page.{n}.json`, with refs in `SliceRefJson`. Existing rule: no bulk text in WorkEvents.
6. **Make region selection an interface with A/B/C strategies** so Experiments A–F are a config switch, and log `region_strategy` plus whether the identity was found inside the selected region. That single log line is what tunes the percentages later.
7. **Escalation triggers must be app-side rules**, derived from missing/conflicting fields — never “the model said it was unsure.” Consistent with §27 of the proposal and with not trusting self-reported confidence.
8. **Constrain type output to `queue_routable_types` even in T2/T3.** Ambiguity escalation must not widen the label set.
9. **Prefer explicit failure over a forced cut.** A wrong split silently produces wrong business documents. `sig_unresolved` should surface as reviewable rather than a confident-looking result.
10. **Anchor reset needs a rule.** Define when an anchor expires (blank page, new identity, printed sequence restart, N pages without supporting evidence) or a bad anchor will swallow the rest of the pack.

### 15.7 Cost shape (rough, for comparison only)

| Case | Calls under P9/F6 |
|------|-------------------|
| Case A (type + 1 page) | 0 intelligence calls (locked skip) |
| Single page, no type | 1 cheap call, reused as extract when complete |
| Clean multi-doc pack, N pages | N cheap calls + 1 extract per group |
| Ambiguous pages | N cheap + escalations on the ambiguous subset only |

Worst case is bounded by the per-File cap in suggestion 2. Actual figures come from Experiments A–F.

---

## Finalized Decisions

| Topic | Locked (2026-08-18) |
|-------|---------------------|
| Pipeline shape | **F4 cascade structure** always: normalize → split → classify → route → extract |
| Phase 1 split/classify | **P0 + C0** only when `documentTypeKey` **and** `pageCount==1` (Case A). Type-only multi-page does **not** skip split (Case B). Real multi-doc split later. |
| Without type | Stages still run as **deferred no-ops** (one placeholder Document). Real P2/C3/C6 later. |
| Route | Always runs. QueueRoute binds Agent; missing route → Failed `unroutable_type`. |
| Storage of slices | Keep normalize artifacts; skip materializing per-doc split blobs until real split. **S3 hybrid** remains the target when split is implemented. |
| `documentCount` | Optional audit/hint. Does **not** skip split. Single-page skip creates one Document. |

## Pending Decisions

- Real split: **P9/F6 (§15 anchor + escalation)** vs **P8/F5** vs **P2/P6 cheap-first**  
- If P9/F6: model tiers + escalation triggers, per-File LLM cap, confidence-to-status mapping, anchor reset rule (§15.6)  
- Classify set (**C1/C3/C6**) for the no-hint path  
- Slice artifact layout (**S3**) when real split ships  
- First-stage LLM schema/prompt if P8  
- Remaining §10 questions  
- §14 signal dictionary: LLM region sizes, R7 grouping conflict policy, whether C1 (`sig_single_route`) ships with real split  

## Assumptions

- Normalize artifacts remain source of truth for text/layout entering split.  
- Skip split+classify only for type + one page; typed multi-page still runs split.  
- QueueRoute remains the routable type set.  
- Hints are **caller assertions**, not OCR truth.  
- Sync-wait remains single-Document; multi-doc Files use async.

## Risks

- Callers who omit `documentTypeKey` will not get true multi-doc split until the later phase.  
- Wrong caller type still routes to the wrong Agent — treat hints as assertions.  
- Typed multi-page Files still produce one placeholder Document until real split ships.  
- Visual/template matching will not generalize across suppliers.

## Readiness

**Phase 1 skip path amended:** type + one page only.  
**Not ready** for real split/classify algorithms until **P6 vs P8 vs P9/F6** is chosen. **Blocked** on that — not ready to amend Plan 03 for real split. P9/F6 additionally needs real per-page OCR and a wired LLM first (§15.5).

---

## Changelog

| Date | Note |
|------|------|
| 2026-08-04 | Initial exploration: split storage, split/classify techniques, cascade recommendation. |
| 2026-08-18 | **Locked Phase 1:** P0/C0 predetermined `documentTypeKey` skips split+classify; pipeline stages remain; real split/classify deferred. |
| 2026-08-18 | Imported semantic-split conclusions: **P8/F5**, identity vs type, no per-page-extract-merge, Case A/B/C hint refinement, corpus metrics. Not locked. |
| 2026-08-18 | **Locked type-only hint:** skip split+classify only when `documentTypeKey` **and** `pageCount==1`; type-only multi-page still splits. |
| 2026-08-18 | **§14 Signal dictionary:** elements (retrieval + LLM input), named signal combinations, cheapest-first rules R1–R10. Working; Case A locked. |
| 2026-08-19 | **§15 Option P9/F6** imported from the dev-agent proposal: anchors, model tiers + escalation, app-side confidence, `evidence[]`, local 3-page window. Candidate only; conflicts, gaps, and suggestions recorded. |
