# Documate v3 — Document Queue Design

> **Document type:** Product / system design (queue & work lifecycle)  
> **Status:** Draft aligned to frozen mental design  
> **Depends on:** [01-project-exploration-mental-design.md](./01-project-exploration-mental-design.md)  
> **Out of scope:** Tech stack, broker choice, DB schema DDL, API route paths, code structure, estimates  

**Related documents:**
- Product glossary — [00-product-glossary.md](./00-product-glossary.md)
- Mental design — `01-project-exploration-mental-design.md`
- Implementation plan — `03-documate-v3-implementation-plan.md`

---

## 1. Purpose

Defines how **Queues** and work units behave: intake, **optional Batch (log only)**, File → Document lifecycle, statuses, **per-Document webhooks**, poll, cancel, reprocess, routing lock.

Uses canonical terms from the product glossary: **Batch**, **File**, **Document** (Doc). **Job** is not a product term.

Not an implementation plan — but §2.1 records a hard **implementation goal** from product history.

---

## 2. Design principles

1. **Queue = operational lane** (multi-queue day one). **Customer AI Agents** own schemas/extraction/**post-processing**; queue owns routing, intake, delivery.
2. **Document is the result atom** and the **webhook unit**.
3. **File** owns split/classify and rollup for UI/ops; may contain many Documents.
4. **Batch is optional and log/correlation only** — created when **multiple files** arrive together (multi-file API upload or multi-target email). Not a status machine customers depend on.
5. **Multi-file receive is first-class** — many files in one intake must be allowed; processing must **not** block other work (see §2.1).
6. **Public status ≠ internal stage ≠ event log.**
7. **No customer-facing Delivered status.** Webhook attempts are metadata on the **Document**.
8. **Routing map locks** once the queue has any Files.
9. **Cancel** may target a **file** or a **Document**.
10. Pack partial success at file level uses `PartialReady`; async consumers get **one webhook per Document**.
11. **Two External API styles:** most intake is **async** (ids + webhook + poll); **one sync wait API** returns final results in the HTTP response (client-side).

### 2.1 Implementation goal — concurrent multi-file intake (IMPORTANT)

In a previous Documate version, uploading / receiving multiple files was not properly supported: an upload effectively **stopped other processes**.

**v3 goal:**  
- Accept **multiple files at a time** (API multi-upload and email multi-attachment).  
- Enqueue each file (and later each Document) so intake is **non-blocking**.  
- Other queues, files, and Documents continue while new uploads land.  
- Batch exists only to **correlate** that intake event for logs/support — it must not become a global lock.

(Exact concurrency tech is for the implementation plan; this is the product/ops constraint.)

---

## 3. Queue as a configured object

Identity, **type → Customer AI Agent** routing map + lock, webhook URL/secret/enable, email intake. Post-processing on **Agent** (not queue).

While routing locked: map immutable; webhook/email/name still editable.

---

## 4. Work hierarchy — DECIDED

```
Queue
 └── [Batch]?                 optional — only if ≥2 files in one intake; LOG ONLY
      └── File[]         each stored file / email target
           └── Document[] logical docs after split + classify
```

### 4.1 When is Batch created?

| Intake | Batch? |
|---|---|
| API upload of **one** file | **No** — `batch_id` null on the file |
| API upload of **N≥2** files in one request | **Yes** — one Batch linking those N files (log/correlation) |
| Email with **one** accepted target | **No** |
| Email with **N≥2** accepted targets | **Yes** — one Batch for that message’s files |
| Intake fully rejected (no files) | **No** — `IntakeRejection` only |

### 4.2 What Batch is (and is not)

| Batch IS | Batch is NOT |
|---|---|
| Correlation id for “these files arrived together” | A required parent for every file |
| Log / support / poll filter (`batch_id`) | A customer delivery contract |
| Lightweight record: id, queue, source, created_at, file_ids, email_message_id? | A full status state machine we webhook on |
| Optional | Something that gates concurrency or locks the system |

Optional derived fields for UI (e.g. counts) are fine; **do not** make Batch the primary operational object.

### 4.3 File & Document

| Object | Role | Result JSON? | Webhook? |
|---|---|---|---|
| **File** | Store original; split/classify/route; rollup status | No | No (not the push unit) |
| **Document** | Extract to agent schema; post-process | **Yes** on Ready | **Yes** on terminal |

Single-invoice PDF: 1 file → 1 Document (with or without Batch).

---

## 5. Intake paths

### 5.1 API — multi-file allowed — DECIDED

```
Auth → queue_id → upload 1..N files in one request
  → if N≥2: create Batch (log)
  → for each file: store → File (Received), link batch_id if any
  → lock routing if first files on queue
  → enqueue each file independently (non-blocking — §2.1)
  → return file_ids (+ batch_id if any); document ids appear after split
```

- Callers may also send parallel single-file requests; both patterns are valid.
- API webhooks do **not** include original file bytes.

### 5.2 Email intake

```
Mail → gates
  fail → IntakeRejection
  → intake decision agent
  reject → IntakeRejection
  process → File per target; Batch if ≥2 targets
  → each file enqueued independently
```

Ambiguity → reject. Body may be its own file. Shared `email_message_id` (+ `batch_id` when N≥2).

### 5.3 IntakeRejection

When no file is created: listable rejection record (reason, codes, email meta). No webhook.

---

## 6. File pipeline

### 6.1 Stages (internal)

```
received → queued → normalizing → ocr → text_ready
  → splitting → classifying → routing → awaiting_children
  → ready | partial_ready | failed | rejected | cancelled
```

### 6.2 Public file statuses

`Received` | `Processing` | `Ready` | `PartialReady` | `Failed` | `Rejected` | `Cancelled`

**Rollup** (after children exist), unless whole-file cancel:

| Children | File status |
|---|---|
| Any active | `Processing` |
| All `Ready` | `Ready` |
| ≥1 Ready + ≥1 Failed/Rejected/Cancelled | `PartialReady` |
| Zero Ready, all terminal bad | `Failed` |

**Whole-file cancel:** file → `Cancelled` (overrides rollup).

File status is for poll/UI/ops. **Push delivery is per document**, not per file terminal.

### 6.3 Split / classify / route

Unroutable / unclassified → Document `Failed` (still a child for rollup).  
Split hard fail → file `Failed`, no children.

---

## 7. Document pipeline

### 7.1 Stages

```
received → queued → extracting → schema_validating
  → post_processing? → ready | failed | rejected | cancelled
```

In-flight retries stay public `Processing`.

### 7.2 Public statuses

`Received` | `Processing` | `Ready` | `Failed` | `Rejected` | `Cancelled`

### 7.3 Result on Ready

`data` (schema JSON), type, agent, schema id, `file_id`, `queue_id`, optional `batch_id`, optional `email_message_id`, slice refs.

### 7.4 Post-processing

After schema validate, **Agent** workflow (MCP tools internal) → then Ready / Failed.

---

## 8. Events

Timelines on Batch (minimal), File, Document, IntakeRejection.

Include: intake/gates, ocr/split/classify/route, extract/schema/workflow, retry, `cancel.file` / `cancel.document`, **`webhook.*` on Document**.

---

## 9. Delivery APIs — async vs sync wait

### 9.0 Two styles — DECIDED

| Style | Accept | Response | Track |
|---|---|---|---|
| **Async / disconnected** (most APIs) | Upload / email / etc. | Immediate ids (`file_id`, `document_id`s as known, optional `batch_id`) | Webhook per Document + poll |
| **Sync / wait** (one client API) | Typically one file (recommended) | **Blocks** until Document(s) terminal or **timeout**; body contains final `data` / errors | Same ids always returned so timeout can fall back to poll |

Same File → Document pipeline underneath. Sync is a **delivery preference on the call**, not a separate Core.

### 9.1 Sync wait behavior

```
Client POST (sync extract)
  → create File (+ docs after split) as usual
  → process on normal workers (non-blocking to other work)
  → If >1 Document after split → fail (sync-wait single-doc only)
  → If 1 Document: HTTP waits until terminal OR 60s
  → 200 with result / failure
     OR timeout + ids for poll
  → No webhook (C2)
```

**Rules (DECIDED — see implementation plan B / C / G):**
- Sync-wait supports **one Document per File only**. If split/classify produces **more than one** Document → **fail** the sync-wait call (use async for multi-doc packs).
- If exactly one Document: HTTP waits until that Document is terminal (**Ready** / **Failed** / …) or **60 seconds**, whichever first. On timeout → return timeout + ids for poll.
- Must not lock the whole system while waiting (§2.1).
- Multi-file in one sync call: **discourage**; use async multi-upload.
- Webhooks on sync-wait submissions: **suppressed (C2)**. Async/email paths still webhook per Document.

### 9.2 Async webhook — per Document

Fire when a **Document** becomes terminal:

`Ready` | `Failed` | `Rejected` | `Cancelled`

### 9.3 Logical webhook payload fields

| Field | Purpose |
|---|---|
| `event` | e.g. `document.terminal` |
| `event_id` | Idempotency |
| `queue_id` | |
| `batch_id` | Optional |
| `file_id` | |
| `document_id` | |
| `status` | Document terminal status |
| `document_type` | |
| `agent_id` | |
| `data` | If Ready |
| `error` | If not Ready |
| `source` | `api` \| `email` \| `api_sync` (optional stamp) |
| `email_message_id` | Optional |
| `original_file` | Only if `source` is non-API upload |
| `occurred_at` | |

### 9.4 Webhook metadata — on **Document**

`webhook_status`: `not_configured` | `pending` | `succeeded` | `exhausted`  
+ attempts, last_at, last_http_status  

### 9.5 Ordering

No order guarantee across documents/files. Sync response ordering can match `documents[]` stable order (e.g. page range).

### 9.6 Sync response shape (conceptual)

| Field | Purpose |
|---|---|
| `file_id` | |
| `file_status` | Ready / PartialReady / Failed / … |
| `timed_out` | bool |
| `documents[]` | Same logical fields as webhook body items |
| ids for poll | Always present |

---

## 10. Poll contract

**Documents** (primary): filters — ids, id range, date range, status, queue, file, optional batch, optional email_message_id, document_type.

**Files:** rollup + children summary + source.

**Batches (optional):** list/get for log correlation — file_ids, source, timestamps. No requirement for rich batch status machine.

Documents may be Ready and webhook **before** siblings finish (file still `Processing`). Intentional.

---

## 11. Cancellation & reprocess — DECIDED

### 11.1 Cancel file

- File → `Cancelled` (override rollup).  
- Non-terminal Documents → `Cancelled` (each gets its **document** webhook).  
- Already Ready docs stay Ready (already may have webhooks).

### 11.2 Cancel Document

- That Document → `Cancelled` → **Document webhook**.  
- Siblings continue; file uses normal rollup.

### 11.3 Reprocess

- **Never automatic** — explicit caller only.  
- New **File** (same bytes), optional link `reprocess_of_file_id`; new Documents; new document webhooks.  
- If reprocess request covers multiple files together, may create a log Batch; single-file reprocess → no batch.  
- In-flight retry ≠ reprocess.

---

## 12. Routing lock

First file on queue locks type→Agent map. Fix via new queue. Webhook/email/name still editable.

---

## 13. Error codes (starter)

Same catalog as before (`intake_rejected`, `unroutable_type`, `schema_unsatisfied`, `cancelled`, …). Attach to IntakeRejection, file, or Document as appropriate.

---

## 14. Concurrency (design intent)

- Multi-file intake enqueues **per file** without stopping other files/queues (§2.1).  
- Documents under a file may run in parallel after spawn.  
- Document webhooks fire as each child terminals — do not wait for file rollup.

---

## 15. Security / tenancy

Tenant + **Business** isolation (Business = Documate scope); email address = capability secret; webhook HMAC (conceptual); `source` stamped.

---

## 16. Responsibilities vs Core / APIs

| Concern | Owner |
|---|---|
| Queue CRUD, routing lock | Frontend APIs + domain |
| Multi-file upload (non-blocking enqueue) | External APIs → queue domain |
| Email gates + intake decision | Intake → queue domain |
| Optional Batch create (log) | Queue domain on multi-file intake |
| Split/classify/route | Orchestrator + Core |
| Extract + schema | **Core** |
| Post-processing | Workflow runner (MCP internal) |
| Webhook **per Document** | Delivery component |
| Poll | External + Frontend APIs |

---

## 17. Sequence sketches

### 17.1 Multi-file API upload (2 PDFs)

```
Upload F1, F2 → Batch B (log) → File F1, File F2 enqueued in parallel
F1 splits to J1, J2 → each Ready → webhook J1, webhook J2 (order free)
F2 splits to J3 → webhook J3
```

### 17.2 Single-file API

```
Upload F → no Batch → split → docs → per-doc webhooks
```

### 17.3 Email, 3 attachments

```
Batch B (log) + F1,F2,F3 → independent pipelines → webhooks per Document
```

### 17.4 One PDF, 3 logical docs; cancel one

```
J1 Ready → webhook J1
Cancel J2 → webhook J2 (Cancelled)
J3 Ready → webhook J3
File → PartialReady (poll)
```

---

## 18. Phase 1 scope (queue)

**In**
- Optional Batch (log only) for multi-file intake  
- Multi-file API upload + email multi-target  
- Non-blocking concurrent intake (§2.1)  
- File → Document; multi-doc/multi-type inside file  
- **Webhook per Document**  
- Cancel file + document; explicit reprocess  
- IntakeRejection; routing lock; PartialReady on file  

**Out / later**
- Batch as primary status/delivery object  
- Batch webhook  
- Auto-reprocess  
- Upload that globally blocks other processing (explicitly forbidden)

---

## 19. Decision log (this revision)

| Topic | Decision |
|---|---|
| Batch | **Optional**, only when ≥2 files in one intake; **log/correlation only** |
| Multi-file upload/receive | **Required**; must not block other processes |
| Hierarchy | Optional Batch → File → Document |
| Webhook | **Per Document** (async) |
| Sync wait API | **One** client API; same pipeline; HTTP waits for results or timeout → poll fallback |
| Webhook metadata | On Document |
| Cancel | File and Document |
| Reprocess | Explicit only; new File |
| Refuse no file | IntakeRejection |

---

## 20. Next step

**Implementation plan** — must call out non-blocking multi-file intake, per-Document webhooks, and the sync wait API (+ timeout policy).

---

## 21. Revision log

| Date | Change |
|---|---|
| 2026-07-31 | Initial queue design; later iterations on cancel/reprocess. |
| 2026-07-31 | Temporarily removed Batch / multi-file API. |
| 2026-07-31 | Optional log-only Batch; multi-file non-blocking; webhook per Document. |
| 2026-07-31 | **Async default APIs + one sync wait API for in-call client results.** |

| 2026-08-02 | Sync-wait: single-doc only, 60s, no webhook (C2); async keeps multi-doc (E3). |
