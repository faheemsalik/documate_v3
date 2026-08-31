# Documate v3 — Real OCR + Live LLM Extract (Implementation Plan)

> **Status:** Phase 2 — **complete**; Phase 3 DQ filed (**DQ-0704** ✅ Complete)  
> **Type:** Engineering implementation plan  
> **Upstream:** [05-ocr-normalize-real-providers-exploration.md](./05-ocr-normalize-real-providers-exploration.md) (Phase 1 complete; Phase 2 approved 2026-08-28)  
> **Also incorporates:** Queue **Decision K1** (2026-08-28) — untyped default channel + QueueRoute  
> **Downstream:** Phase 3 — amend `03-documate-v3-dispatch-queue.md` with new DQ(s)  
> **Created:** 2026-08-28  

**Outcome:** Replace stub OCR and heuristic extract so a File on the **default Queue (channel)** becomes useful Documents: real text artifacts + live LLM `ResultJson`, routed via **QueueRoute → Agent**.

---

## Planning flow

| Phase | Document |
|-------|----------|
| 1 — Exploration | `05-ocr-normalize-real-providers-exploration.md` ✅ |
| 2 — Implementation plan | **This file** ✅ (DR1 locked) |
| 3 — Dispatch queue | Amend `03-documate-v3-dispatch-queue.md` — **DQ-0704** ✅ |

---

## Delivery Principles

1. **One product slice** — real OCR + live LLM ship together.  
2. **Same Core pipeline** for async upload and sync wait.  
3. **Queue = untyped channel (K1)** — Agent via **QueueRoute**; partners use **default Queue**.  
4. **Artifacts in object storage** — not on `OpsFiles`.  
5. **Mode 1** — no customer provider picker.  
6. **Keep seams** — `IOcrNormalizeAdapter`, `IDocumentExtractAdapter`.  
7. **New DQ evidence** — do not pretend DQ-0701/0703 already delivered real providers.  
8. **Secrets server-side** — `Ocr:`, `Llm:`, `Notifications:`.  
9. **Fail visibly** — OCR dual-fail / LLM fail after retry; ops email best-effort.  
10. **Non-blocking intake** — Hangfire per-File; optional high-priority queue.

---

## Domain Architecture Layer

| Concern | Home |
|---------|------|
| OCR compose + Textract + Document AI | `Infrastructure/Ocr/` |
| LLM HTTP extract | `Infrastructure/Extract/` |
| Options | `OcrOptions`, `LlmOptions`, `NotificationOptions` |
| Pipeline | Existing `FilePipelineStub` + stages |
| Sync gates | `Modules/External/Features/Extract/` |
| App priority | `Modules/FrontendSupport/Features/Files/` → Hangfire |
| Failure email | `Infrastructure/Notifications/` — SMTP when enabled |
| Catalog | Seed `google_document_ai`; template `DefaultProviderId` → LLM |
| Queue | APIs unchanged; smoke against **default** Queue (K1) |

```text
HTTP (queueId = default channel)
  → blob + OpsFile
  → Hangfire (default | priority)
       → Normalize (Textract → Google) → artifacts
       → Split / Classify (existing)
       → Route (QueueRoute → Agent)
       → Extract (LLM) → validate → ready/failed
       → optional ops email on LLM fail
       → webhook if async
```

---

## Domain Validation Rules

### Closed (exploration 05 + K1 + DR1)

| Rule | Behavior |
|------|----------|
| Sync accept | Reject if pages **> 3** or size **> 5 MB** |
| Sync wait | **60s**; timeout → ids + `timedOut` |
| Worker OCR | Sync API if pages **≤ 8**; else async+poll in job |
| OCR fallback | Primary fail or empty/bad text → secondary |
| Formats | PDF, PNG, JPG; text passthrough; else unsupported/fail |
| LLM keys | Required all environments; no heuristic when missing |
| LLM model | Agent LLM `DefaultProviderId` else `Llm:DefaultProviderKey` |
| LLM prompt | Full normalize text + instructions + schema |
| LLM retry | Once; then fail |
| Ops email | To `faheem@manticsoftware.com` after final LLM fail |
| **DR1-B** | Send SMTP only when `Notifications:Enabled=true`; else log. **Best-effort** — mail errors never change Document/File status |
| Public façade | Document/External → `documate_meta`; WorkEvent → concrete LLM key |
| Queue | Untyped channel; QueueRoute; default Queue normal target |

---

## Process Flows

### Flow A — Async (External or App)

1. Client uploads to `/queues/{defaultQueueId}/files` (App may pass `priority=high|normal`).  
2. Persist blob + File (`received`); enqueue Hangfire (`priority` queue if high).  
3. Worker: normalize → split → classify → route → extract+validate.  
4. Terminal File status; per-Document webhook if not `api_sync`.  
5. Client polls list/get.

### Flow B — Sync wait (External)

1. `POST …/extract` with one file.  
2. **Gate:** size ≤ 5 MB; estimate pages (PdfPig for PDF; images = 1) ≤ 3 → else **400**.  
3. Persist + enqueue; HTTP waits ≤ 60s for File+Document terminal.  
4. Return body or `timedOut` + ids (no webhook).

### Flow C — LLM extract failure

1. Call LLM; on failure retry once.  
2. Still fail → Document/File `extract_failed` / rollup.  
3. Best-effort notify: if `Notifications:Enabled` → SMTP to fixed address; always log. Mail exceptions swallowed after log.

### Flow D — Missing route (unchanged product)

Classify/type without QueueRoute → `unroutable_type` (K1 auto-route on Agent create reduces this for single-Queue businesses).

**Decision Required — Process Flows:** *Closed* by exploration + DR1 + K1. No further options.

---

## Instruction and Control Set

### Config (illustrative)

```json
"Ocr": {
  "PrimaryProviderKey": "aws_textract",
  "SecondaryProviderKey": "google_document_ai",
  "SyncMaxPages": 8,
  "Textract": { "Region": "us-west-2", "AccessKey": null, "SecretKey": null },
  "GoogleDocumentAi": { "ProjectId": "", "Location": "", "ProcessorId": "", "CredentialsJson": null }
},
"Llm": {
  "DefaultProviderKey": "gpt_5_6",
  "Providers": {
    "gpt_5_6": { "ApiKey": "", "Model": "", "BaseUrl": null },
    "claude_sonnet_6": { "ApiKey": "", "Model": "", "BaseUrl": null }
  }
},
"Notifications": {
  "Enabled": false,
  "ToAddress": "faheem@manticsoftware.com",
  "Smtp": { "Host": "", "Port": 587, "User": "", "Password": "", "From": "" }
},
"Pipeline": {
  "MaxConcurrentFiles": 4,
  "SyncWaitTimeoutSeconds": 60,
  "SyncMaxPages": 3,
  "SyncMaxBytes": 5242880
}
```

### Control instructions (for implementers)

1. Replace `Mode1OcrNormalizeAdapter` body with composing primary→secondary; keep interface.  
2. Replace heuristic-only extract path with HTTP LLM; keep `IDocumentExtractAdapter`.  
3. Startup (all envs): require `Llm` default provider key present or fail host.  
4. Seed `google_document_ai`; set template `DefaultProviderId` to default LLM.  
5. Clone/create Agent copies template provider (already have field).  
6. Hangfire: add queue name `priority`; App `priority=high` enqueues there; server listens `priority`, `default`, `webhooks`.  
7. Sync gates in External extract handler before persist when cheap (size always; PDF pages via PdfPig).  
8. Notifications: `IOpsAlertSender` — if disabled, `LogError`; if enabled, SMTP; never throw to pipeline.  
9. Postman: default Queue from `me`; typed Agent with route; real keys in user-secrets for smoke.  
10. Do not change Queue typing; use default channel in docs/smoke.

**Decision Required — Instruction set:** *Closed* (Hangfire `priority` queue chosen as concrete control; DR1-B locked).

---

## Permissions and Security

| Surface | Auth |
|---------|------|
| External upload/extract/poll | API key → Business scope |
| App upload + priority | `[Authorize]` / DevBypass today |
| OCR/LLM/Notification secrets | Server config / user-secrets / env only |
| Hangfire dashboard | Dev only (existing) |
| Ops email | Fixed recipient Phase 1; no customer PII beyond File/Document ids + error codes in body (avoid dumping full OCR text in email) |

Email body: FileId, DocumentId, QueueId, BusinessId, error code/message, timestamps — **not** full OCR/LLM payloads.

---

## Dispatch Index (preview for Phase 3 — not executed yet)

| DQ (proposed) | Outcome |
|---------------|---------|
| **DQ-0704** | Real OCR + live LLM + sync gates + app priority + ops alert (single slice per L7a) |

Optional split only if Phase 3 amends — exploration preferred **one** item.

Existing ✅ DQ-0701/0703 remain historical stub evidence; 0704 is the real-provider delivery.

**Ready queue context:** DQ-1001 cancel is next in main index but **this plan’s DQ should be inserted under Wave 07** and can run when selected; does not block cancel forever — product priority is OCR+LLM.

---

## Wave Sections (this feature)

### Wave R1 — Config + catalog

- `OcrOptions`, `LlmOptions`, `NotificationOptions`  
- Seed Google OCR + template DefaultProviderId  
- Host requires LLM keys  

### Wave R2 — OCR adapter

- Textract + Document AI clients  
- Compose fallback; artifacts C1  
- Sync vs async+poll at 8 pages  

### Wave R3 — LLM extract + alerts

- Live LLM adapter; retry; meta façade + WorkEvent concrete key  
- Schema validate unchanged  
- Ops alert DR1-B  

### Wave R4 — Gates + priority + smoke

- Sync 3 pages / 5 MB / 60s  
- App priority → Hangfire `priority`  
- Postman against **default Queue** + routed Agent  
- Evidence for DQ-0704  

---

## Phase-end contract

### Finalized Decisions

| Topic | Decision |
|-------|----------|
| Product slice | Real OCR + live LLM together |
| Queue | K1 default untyped channel + QueueRoute |
| OCR | Textract→Google; artifacts in object storage; ≤8 sync OCR |
| Sync API | ≤3 pages, ≤5 MB, 60s wait |
| LLM | Agent/template provider; keys required; full text; retry once |
| Clients see | Documate; real model in WorkEvents |
| **DR1** | **B** — SMTP when `Notifications:Enabled`; else log; **best-effort** |
| Priority | Hangfire queue `priority` for high |
| Email content | Ids + errors only (no full OCR text) |

### Pending Decisions

*None for Phase 2.*

### Assumptions

- In-process Hangfire remains.  
- K1 bootstrap/auto-route already shipped (DQ-0204/0304).  
- AWS/GCP credentials available for smoke when Enabled paths tested.

### Risks

- 60s sync + dual OCR + LLM.  
- Priority queue starvation if all App uploads mark high (document in smoke: use sparingly).  

### Readiness

**Phase 3 complete for this feature** — execute when selected: **DQ-0704**.
