# Documate v3 — Email Intake Follow-on (Implementation Plan)

> **Status:** Phase 2 — **approved**; Phase 3 DQ filed (`DQ-1204`…`1206`)  
> **Type:** Engineering implementation plan  
> **Upstream:** [14-email-intake-ses-exploration.md](./14-email-intake-ses-exploration.md) follow-on **F1 / F2 / F4** (locked 2026-09-14)  
> **Depends on / pairs with:** [15-system-settings-db-implementation-plan.md](./15-system-settings-db-implementation-plan.md) (retention **N** days from settings; prefer settings wave first or same DQ band)  
> **Baseline:** SES intake shipped (DQ-1201…1203); hardening DQ-1401  
> **Downstream:** Phase 3 — amend [03-documate-v3-dispatch-queue.md](./03-documate-v3-dispatch-queue.md) after this plan approved  
> **Created:** 2026-09-14  

**Outcome:** Inbound email Files carry **sender chain** (Vendor → User → DocIntake) and **truncated body context** when attachments exist; raw S3 MIME is **deleted after 30 days** (configurable).

**Out of this plan:** F3 multi-attach partial-reject polish (deferred); F5 settings table (Plan 15); BMIR; secrets in DB.

---

## Planning flow

| Phase | Document |
|-------|----------|
| 1 — Exploration | Plan 14 follow-on locks ✅ |
| 2 — Implementation plan | **This file** |
| 3 — Dispatch queue | Amend Plan 03 — after approve |

---

## Delivery Principles

1. **Reuse** existing MIME parse → processor → `IWorkRecordService` path.  
2. **No second OCR File** for body when attachments exist (F2a=C) — stamp context only.  
3. **Best-effort** sender chain; never fail intake solely because originator cannot be inferred.  
4. **Retention** default **30 days**; value from system settings when Plan 15 is live (fallback appsettings/seed).  
5. **Do not** store full raw MIME in SQL.  
6. Prefer implementing **after or with** Plan 15 seed so `EmailIntake:MimeRetentionDays` is DB-backed.

---

## Domain Architecture Layer

| Concern | Home |
|---------|------|
| `OpsFile` provenance columns + optional `EmailSenderChainJson` | `Domain/OpsFile.cs` + EF migration |
| Parse From display name + originator heuristics | `Infrastructure/EmailIntake/` (extend MIME parser / new `EmailSenderChainResolver`) |
| Stamp body excerpt into `IntakeHintsJson` | `EmailIntakeProcessor` when attachments accepted |
| **Write email metadata onto each new File** | When calling `CreateFileWithBlobAsync`, pass `EmailSubject`, `EmailMessageId`, and one JSON blob (`EmailIntakeJson`) built from the resolver + excerpt policy — see “What ‘map provenance onto File create’ means” below |
| FE Files list/detail | `apps/web` Files feature |
| External File DTO + webhook payload | External Files + webhook builder |
| S3 MIME delete-after-N | Hangfire recurring job (**DR-FO1=B**) |
| Partial multi-attach handling (F3) | Decision agent + processor (DR-FO3=B) — see opens |
| Runbook | `docs/ops/ses-email-intake-runbook.md` |

### What “map provenance onto File create” means

Today, when email intake accepts attachments, the processor loops targets and calls `CreateFileWithBlobAsync(...)` with blob bytes plus a few email fields (`EmailFrom`, `EmailSubject`, `EmailMessageId`).

**“Map provenance onto File create”** only means: at that same moment, also pass the **resolved sender-chain / names** (and related intake metadata) so each new `OpsFile` row stores them — not a separate post-process step. No extra pipeline stage.

```text
resolve mailbox → gates → decide targets
  → build EmailIntakeJson = { from, fromName, originator, originatorName, chain, … }
  → for each attachment target:
       CreateFileWithBlobAsync(
         stream, subject, messageId,
         emailIntakeJson,   // ← provenance mapped here
         intakeHintsJson with body excerpt)
```

---

## Domain Validation Rules

### Closed (exploration + this Phase 2)

| Rule | Behavior |
|------|----------|
| F1a | Delete raw MIME after **N** days; default **N=30** |
| DR-FO1 | **B** — Hangfire recurring job deletes aged objects under intake prefix |
| F2a | Attachments present → stamp truncated body on each File; no extra body File |
| DR-FO2 | **C** — `EmailIntake:BodyExcerptMaxChars` (default **4096**), via system settings when Plan 15 lands |
| F2 body-only | Unchanged: substantial body → `email-body.txt` File |
| F4a | Best-effort Vendor → User chain |
| F4b (**amended**) | **One JSON column** for sender/provenance intake metadata; **only** `EmailSubject` + `EmailMessageId` stay dedicated columns (see DR-FO4 for existing `EmailFrom`) |
| F4c | Expose on app Files UI + External DTOs + Document webhooks |
| DR-FO3 | **B** — Include F3 multi-attach partial handling in this plan (opens below) |
| Best-effort | Missing originator → omit/null in JSON; still set forwarder from From |

### Suggested `IntakeHintsJson` merge (email body context — F2a)

```json
{
  "documentTypeKey": "…",
  "emailBodyExcerpt": "…",
  "emailBodyTruncated": true
}
```

Keep body excerpt inside **`EmailIntakeJson`** only (**DR-FO5=B**). `IntakeHintsJson` stays for pipeline hints (e.g. `documentTypeKey`).

### `OpsFile` storage (locked)

| Column | Keep? | Meaning |
|--------|-------|---------|
| `EmailSubject` | **Yes** | Subject |
| `EmailMessageId` | **Yes** | MIME Message-Id |
| `EmailFrom` | **Yes** (DR-FO4=A) | Denormalized forwarder address; also mirrored in JSON |
| `EmailIntakeJson` (new) | **Yes** (DR-FO9=A) | from/name, originator/name, `chain[]`, `emailBodyExcerpt`, `skippedAttachments`, etc. |

**Do not add** separate columns for `EmailFromName`, `EmailOriginator`, `EmailOriginatorName`, or a second chain JSON column.

Example `EmailIntakeJson` shape (illustrative):

```json
{
  "from": { "email": "user@partner.com", "name": "Alex" },
  "originator": { "email": "ap@vendor.com", "name": "Vendor AP" },
  "chain": [
    { "role": "originator", "email": "ap@vendor.com", "name": "Vendor AP" },
    { "role": "forwarder", "email": "user@partner.com", "name": "Alex" },
    { "role": "intake", "email": "…@docsintake.com" }
  ],
  "skippedAttachments": [
    { "fileName": "note.exe", "reason": "extension_not_allowed" }
  ]
}
```

---

## Decision Required

### Locked this turn

| ID | Locked |
|----|--------|
| **DR-FO1** | **B** — Hangfire delete-after-N job |
| **DR-FO2** | **C** — configurable excerpt max, default 4096 |
| **DR-FO3** | **B** — include F3 in this plan |
| **F4b storage** | Single `EmailIntakeJson`; Subject + MessageId columns; see DR-FO4 for `EmailFrom` |
| **DR-FO4** | **A** — Keep `EmailFrom` column (denormalized forwarder) **and** mirror in `EmailIntakeJson` |
| **DR-FO5** | **B** — Body excerpt only in `EmailIntakeJson` (not `IntakeHintsJson`) |
| **DR-FO6** | **A** — Process good attachments; list skips in `EmailIntakeJson.skippedAttachments`; no IntakeRejection for partial skips |
| **DR-FO7** | **A** — All skipped + no usable body → IntakeRejection `no_processable_attachments` |
| **DR-FO8** | **B** — Oversize attachment skipped only; others processed |
| **DR-FO9** | **A** — Column name `EmailIntakeJson` |
| **DR-FO10** | **A** — Retention job **daily** |

### Still open

None for email follow-on Phase 2. (Plan 15 still has DR-SS1…SS3 if not locked.)

---

## Process Flows

### Flow 1 — Inbound with attachments + forward

```text
MIME: Vendor → User forwards → docsintake mailbox
  → resolve chain → build EmailIntakeJson
  → attachments: keep allowed; skip others per F3 (DR-FO6/8)
  → each accepted File: Subject, MessageId, EmailIntakeJson (+ hints)
  → pipeline unchanged
```

### Flow 2 — Retention (DR-FO1=B)

```text
Daily Hangfire job
  → read MimeRetentionDays (settings / fallback 30)
  → list S3 prefix → delete LastModified older than N days
  → metric email_intake.mime_deleted
```

### Flow 3 — UI / API

```text
Files / External / webhook
  → EmailSubject, EmailMessageId as fields
  → parse/expose EmailIntakeJson (from, originator, chain, skipped)
```

---

## Instruction and Control Set

| Wave | Work |
|------|------|
| **FO-0** | Migration: add `EmailIntakeJson`; Subject/MessageId unchanged; EmailFrom per DR-FO4 |
| **FO-1** | Sender chain resolver + wire into File create (provenance JSON) |
| **FO-2** | Body excerpt stamp (DR-FO5) |
| **FO-3a** | F3 partial attach / skip / reject codes (DR-FO6…8) |
| **FO-3b** | App + External + webhook surface |
| **FO-4** | Hangfire retention job + runbook |
| **FO-5** | Unit tests + evidence |

**Depends:** Plan 15 SS-0…SS-2 ideally before FO-4.

---

## Permissions and Security

- No full MIME in DB or WorkEvents.  
- `EmailIntakeJson` may contain PII — Business-scoped reads only.  
- Retention job: `s3:ListBucket` + `s3:DeleteObject` on intake prefix.  
- Do not log full body excerpt at Information level.

---

## Dispatch Index (proposed — Phase 3)

| DQ (proposed) | Wave | Outcome |
|---------------|------|---------|
| DQ-1204 | FO-0…FO-2 | JSON provenance + excerpt |
| DQ-1205 | FO-3a…FO-3b | F3 partial + API/UI/webhook |
| DQ-1206 | FO-4…FO-5 | Retention job + tests |

Exact IDs in Phase 3.

---

## Wave Sections

### FO-0 — Schema
- Add `EmailIntakeJson` (nvarchar(max)); null for non-email Files.

### FO-1 — Sender chain → File create
- Resolver; pass JSON into `CreateFileWithBlobAsync`.

### FO-2 — Body excerpt
- Per DR-FO5 / DR-FO2.

### FO-3a — F3
- Skip list + reject codes per DR-FO6…8.

### FO-3b — Surfaces
- DTO + Angular + webhook.

### FO-4 — Retention
- Hangfire daily (or DR-FO10).

### FO-5 — Evidence
- Tests + smoke.

---

## Output contract

### Locked

| ID | Choice |
|----|--------|
| F1a / N1 | Delete after N; default 30 |
| DR-FO1 | **B** Hangfire daily delete |
| F2a | Stamp excerpt when attachments (not body File) |
| DR-FO2 | **C** default 4096 |
| F4 storage | **`EmailIntakeJson`** + Subject/MessageId + keep `EmailFrom` |
| DR-FO3 | **B** include F3 |
| DR-FO4 | **A** |
| DR-FO5 | **B** excerpt in `EmailIntakeJson` only |
| DR-FO6 | **A** partial skip, no rejection |
| DR-FO7 | **A** `no_processable_attachments` |
| DR-FO8 | **B** skip oversize only |
| DR-FO9 | **A** `EmailIntakeJson` |
| DR-FO10 | **A** daily |

### Pending

None (email follow-on). Approve Phase 2 → Phase 3 DQ after Plan 15 DR-SS\* locked if shipping settings first.

**Status:** Phase 2 — **ready for developer verify / approve Phase 3** (email follow-on).

---

## Changelog

| Date | Note |
|------|------|
| 2026-09-14 | Phase 2 draft for email follow-on F1/F2/F4. |
| 2026-09-14 | Explained File-create provenance mapping; F4b → single JSON; locked FO1=B, FO2=C, FO3=B; opened FO4–FO10. |
| 2026-09-14 | Locked FO4–FO10 (A/B/A/A/B/A/A). Phase 2 decisions complete for email follow-on. |