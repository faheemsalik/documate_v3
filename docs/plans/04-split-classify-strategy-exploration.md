# Documate v3 — Split & Classify Strategy (Exploration)

> **Status:** Exploration — brainstorm open; **no technique locked**  
> **Type:** Product / Core pipeline exploration (feeds DQ-0702)  
> **Upstream:** [01-project-exploration-mental-design.md](./01-project-exploration-mental-design.md) §3.4, §6.1 B; [02-document-queue-design.md](./02-document-queue-design.md) §6.3; [03-documate-v3-implementation-plan.md](./03-documate-v3-implementation-plan.md) Decision E3 + intake hints  
> **Downstream:** After developer chooses options → amend Plan 03 + implement in **DQ-0702** (do not invent classify architecture inside the DQ)  
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
| **Caller hints** | Already planned: complete hints **skip** split+classify — this doc covers the **no-hint** (and partial-hint) path |

**Design pressure:** Prefer **deterministic / cheap signals first**; use **LLM only as fallback** or on ambiguous packs.

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
| Pipeline after normalize | Stub stage names only — not real split/classify |
| Placeholder Document | One row per File with `SliceRefJson` pointing at normalize artifacts |
| Intake hints | **Planned** in Plan 03; not wired on External upload yet |
| Label set | Platform `CorDocumentType` + QueueRoute subset |

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
| **P0** | **Caller hints** | `documentCount` + types → no split | Instant | $0 | As good as caller | Already planned | Only when hints complete |
| **P1** | **Single-doc default** | MIME/ext: image/plain text → 1 doc, all pages | Instant | $0 | High for true singles | Cheap default | Wrong for multi-invoice PDF |
| **P2** | **Heuristic page breaks** | Rules on layout: blank pages, large heading jumps, “Page 1 of”, form headers, repeated letterhead | Fast | $0 | Medium | No LLM; tunable | Brittle across customers |
| **P3** | **TOC / barcode / QR** | Detect separators customers already print | Fast | $0 | High when present | Very cheap when signal exists | Rare; not universal |
| **P4** | **Embedding / clustering pages** | Embed page text; cluster consecutive pages | Medium | Low–med (embed API) | Medium | Better than pure rules | Needs embed model; tuning |
| **P5** | **LLM split** | Send page summaries or full text; ask for ranges | Slow | **High** | Variable | Flexible | Costly; needs strict JSON schema + validation |
| **P6** | **Hybrid cascade** | P1 → P2/P3 → only ambiguous packs to P4/P5 | Fast usual case | Low average | Good | Best cost profile | More code paths |
| **P7** | **Always 1 Document for Phase 1** | Never split PDFs | Instant | $0 | Wrong for E3 multi-doc | Ships fast | **Conflicts with E3** unless hints-only multi-doc |

### Split discussion notes

- E3 says multi-doc PDFs are first-class on async path → **P7 alone is not enough** unless product softens E3.
- Cheapest honest Phase 1: **P0 + P1 + P2**, with **P5 only on low-confidence** packs (P6).
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

---

## 7. Pipeline shape options (split × classify)

| ID | Shape | Flow | Pros | Cons |
|----|-------|------|------|------|
| **F1** | Split then classify | Ranges → then type per slice | Clear; matches mental model | Classify N times |
| **F2** | Classify pages then merge** | Type each page → merge consecutive same type | Good for mixed packs | N page classifies (expensive if LLM) |
| **F3** | Joint LLM | One call: ranges + types | One round-trip | Expensive; hard to validate; all-or-nothing |
| **F4** | Cascade (recommended discussion default) | Hints → single-route → heuristics split/classify → LLM fallback | Cheap happy path | Complexity |

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

1. **Skip work** when hints or single QueueRoute type allow.  
2. **Cap LLM context** (first page, titles, 2–4k chars).  
3. **Batch** page embeddings locally if used; avoid per-page LLM.  
4. **Cache** classify result on `ContentHash` + QueueId (optional later).  
5. **Persist slices** (S3 hybrid) so extract/reprocess does not re-split.  
6. **Fail fast** unroutable instead of retrying LLM loops.  
7. **Mode 1:** platform picks models; customers don’t see provider knobs.

---

## 9. Risks and Constraints

| Risk | Mitigation |
|------|------------|
| Heuristics fail on novel packs | Confidence + LLM fallback; clear Failed; reprocess; hints API |
| LLM cost blow-up | Cascade; route-scoped labels; snippet-only prompts |
| Misclassify → wrong Agent | High threshold or Failed; never invent types outside Route |
| Artifact sprawl | SequenceId paths; retention aligned with File purge |
| Over-building Phase 1 | Ship P0+P1+C0+C1+C3 first; add P2/C6 deliberately |

---

## 10. Open Questions (need developer input)

1. **Phase 1 split bar:** Accept heuristic-only split (P2) with Known limitations, or require LLM split fallback (P6) from day one?  
2. **Single-type queues:** Do we implement **C1 short-circuit** immediately (recommended)?  
3. **LLM provider for classify:** Same Mode 1 meta/LLM as extract, or smaller/cheaper classify model?  
4. **Confidence:** Numeric threshold + PartialReady vs binary Failed for ambiguous packs?  
5. **Materialize slice binaries?** Text/layout only in Phase 1, or also cropped PDF pages?  
6. **Partial hints:** Keep “run full pipeline” (current Plan 03) or reject incomplete hints with 400?  
7. **Page-level vs pack-level classify** when split is uncertain (F1 vs F2)?

---

## 11. Recommended Direction (proposal — **not locked**)

| Topic | Proposal |
|-------|----------|
| Split storage | **S3 Hybrid** — object store slices + DB page range + SliceRefJson |
| Default path | **F4 cascade** |
| Phase 1 minimum | **P0 hints + P1 single-doc MIME + C0 hints + C1 single-route + C3 keywords**; optional **C6 LLM fallback** if demos need it |
| Defer | P4 embeddings, P5-first, physical PDF split, HITL |
| Amend Plan 03 | Only after you pick among options above |

---

## 12. Exploration Exit Criteria

This exploration is **ready for decisions** when the developer has chosen:

- [ ] Storage option (**S1–S4**)  
- [ ] Split Phase 1 set (**P0–P7** subset)  
- [ ] Classify Phase 1 set (**C0–C7** subset)  
- [ ] Pipeline shape (**F1–F4**)  
- [ ] Answers to Open Questions §10  

Then:

1. Record decisions in this doc (Finalized Decisions).  
2. Amend [03-documate-v3-implementation-plan.md](./03-documate-v3-implementation-plan.md) (Decision E / Core pipeline).  
3. Execute **DQ-0702** against that locked technique.

---

## Finalized Decisions

| Topic | Locked (2026-08-18) |
|-------|---------------------|
| Pipeline shape | **F4 cascade structure** always: normalize → split → classify → route → extract |
| Phase 1 split/classify | **P0 + C0 only.** If upload includes `documentTypeKey`, **skip split and classify**. |
| Without type | Stages still run as **deferred no-ops** (one placeholder Document). Real P2/C3/C6 later. |
| Route | Always runs. QueueRoute binds Agent; missing route → Failed `unroutable_type`. |
| Storage of slices | Keep normalize artifacts; skip materializing per-doc split blobs until real split. **S3 hybrid** remains the target when split is implemented. |
| `documentCount` | Optional with type (default 1). Same type applied to all N Documents this phase. |

## Pending Decisions

- Real split set (**P2/P6**) and classify set (**C1/C3/C6**) for the no-hint path  
- Slice artifact layout (**S3**) when real split ships  
- Remaining §10 questions for that later phase

## Assumptions

- Normalize artifacts remain source of truth for text/layout entering split.  
- Intake hints skip path stays as locked in Plan 03.  
- QueueRoute remains the routable type set.

## Risks

- Callers who omit `documentTypeKey` will not get true multi-doc split until the later phase.  
- Wrong caller type still routes to the wrong Agent — treat hints as assertions.

## Readiness

**Phase 1 skip path locked and implemented (DQ-0702 slice).**  
**Not ready** for real split/classify algorithms until remaining Pending Decisions are chosen.

---

## Changelog

| Date | Note |
|------|------|
| 2026-08-04 | Initial exploration: split storage, split/classify techniques, cascade recommendation. |
| 2026-08-18 | **Locked Phase 1:** P0/C0 predetermined `documentTypeKey` skips split+classify; pipeline stages remain; real split/classify deferred. |
