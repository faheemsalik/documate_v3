# Documate v3 — Email Intake via SES (Exploration)

> **Status:** Phase 1 — **complete** (locked 2026-09-11); Phase 2 implementation plan filed  
> **Type:** Product / Core intake exploration (Documate-owned)  
> **Upstream:** [01-project-exploration-mental-design.md](./01-project-exploration-mental-design.md) §14; [02-document-queue-design.md](./02-document-queue-design.md) §5.2; [03-documate-v3-implementation-plan.md](./03-documate-v3-implementation-plan.md) Flow 3 / Decision D; DQ-1201  
> **Downstream:** [14-email-intake-ses-implementation-plan.md](./14-email-intake-ses-implementation-plan.md)  
> **Separate track:** [12-business-message-intake-routing-exploration.md](./12-business-message-intake-routing-exploration.md) (**BMIR**) — do not merge; Documate owns SES for Documate inboxes  
> **Created:** 2026-09-11  

**Goal:** Design portal-automated purpose inboxes on **`docsintake.com`** (AWS SES catch-all), so partners can create typed (per-Agent) and multi-type intake emails with long random local-parts — without per-address SES provisioning or manual ops after one-time domain verify.

---

## Planning flow

| Phase | This document |
|-------|----------------|
| 1 — Exploration | **This file** ✅ |
| 2 — Implementation plan | `14-email-intake-ses-implementation-plan.md` |
| 3 — Dispatch queue | Amend `03-documate-v3-dispatch-queue.md` (reframe DQ-1201 / add SES waves) — after Phase 2 approve |

---

## 1. Problem Framing

Email is an **unauthenticated intake channel** (Plan 01 §14). Partners need:

| Need | Why |
|------|-----|
| **Typed inbox per Agent** | e.g. invoice Agent → only invoices; skip classify guesswork |
| **Multi-type inbox** | One email with mixed docs → split/classify → QueueRoute |
| **Safe addresses** | Long random local-part so addresses are hard to guess |
| **Portal automation** | Create / rotate / disable from Documate app; no AWS console per inbox |
| **One-time domain setup** | Verify `docsintake.com` in SES once; then scale via Documate DB |

Today Documate only mints a **single Queue address** (`q` + 24 hex). There is no SES receiver, no intake-decision agent, and no per-Agent inbox.

---

## 2. Scope

### In scope

- `IntakeMailbox` product model (typed_agent + multi_type)
- Address format + domain `docsintake.com`
- SES catch-all receive → S3 + notify → Documate worker
- Portal Create / Rotate / Disable / allowlist for mailboxes
- Relationship to default Queue (K1), Agent, QueueRoute
- Reframe DQ-1201 toward stub + SES (not IMAP for v1)
- Hard gates + IntakeRejection (Plan 01 Layer 1–2)

### Out of scope

- **BMIR** multi-app routing platform (Plan 12)
- IMAP poller (Decision D2) for v1
- Per-address SES CreateEmailIdentity API calls
- Selling email hard without allowlist UX (still gated)
- Real split/classify algorithms (Plan 04)
- Subdomain `inbound.docsintake.com` (deferred)

---

## 3. Current-State Findings

| Piece | Today |
|-------|--------|
| Queue mint | `POST .../queues/{id}/email/mint` → `q{24hex}@{EmailIntake:DefaultDomain}` |
| Domain config | `EmailIntake:DefaultDomain` (default `intake.documate.local`) |
| Allowlist | Queue CRUD + customer Channels UI |
| Inbound | **None** (no SES/IMAP/simulate) |
| Intake decision | Designed only; **DQ-1201** ⬜ |
| Agent email | **None** — Agents bind via `QueueRoute` after Queue intake |
| Business slug | `CorTenantBusiness.Name` only — no intake slug field yet |

---

## 4. Risks and Constraints

| Risk | Mitigation |
|------|------------|
| Per-address SES provisioning myth | Catch-all + Documate DB as SoT |
| Address leak / spoof | Random entropy + allowlist + enable kill-switch |
| Cost attack (OCR/LLM) | Layer 1 size/type/rate gates before AI |
| Typed inbox vs K1 (“submit to Queue”) | Mailboxes are intake endpoints; Files still land on **default Queue** for webhook/storage |
| BMIR split-brain | Documate owns this SES domain for Documate; BMIR remains separate |
| Predictable prefix | Prefix is UX only; random segment is security |
| Ambiguous mail | Bias reject → `IntakeRejection` |

---

## 5. Open Questions (resolved → Finalized)

Former opens locked 2026-09-11 with developer go-ahead to Phase 2. See Finalized Decisions.

---

## 6. Recommended Direction (locked)

### 6.1 SES model

**Domain catch-all, not per-mailbox SES identities.**

1. One-time manual: verify `docsintake.com`, MX → SES inbound, receipt rule → S3 (raw MIME) + SNS/EventBridge.  
2. Portal “Create email” writes **only** Documate `OpsIntakeMailbox`.  
3. Worker resolves `To:` local-part → mailbox row (or reject unknown).

### 6.2 Product model: `IntakeMailbox`

```text
Business
  ├── Queue (default channel — webhook, QueueRoutes)
  ├── Agents
  └── IntakeMailbox[]
        ├── typed_agent → AgentId (required); Files → default Queue
        └── multi_type  → QueueId = default Queue
```

| Kind | Address purpose | Pipeline |
|------|-----------------|----------|
| **typed_agent** | Partner sends known type to matching Agent | Lightweight intake decision → Files stamped with Agent’s DocumentType → route to that Agent |
| **multi_type** | Mixed packs | Full Plan 01 §14 intake decision → split/classify → QueueRoute |

### 6.3 Address format

```text
{businessSlug}-{purposeSlug}-{random}@docsintake.com
```

| Part | Rule |
|------|------|
| `businessSlug` | Stable slug on Business (new field or derived once and stored) |
| `purposeSlug` | From Agent name/key sanitized, or `multi` |
| `random` | ≥26 chars cryptographic base32/hex |
| Local-part | ≤ 64 chars; unique `(EmailDomain, EmailLocalPart)` filtered soft-delete |

Examples: `mcm-invoice-k7x9m2p4q8w1n3v6h4j2g8@docsintake.com`, `mcm-multi-…@docsintake.com`.

### 6.4 Portal automation

- Agent page: Create / rotate / disable typed intake email  
- Channels page: Create / rotate / disable multi-type email; allowlist per mailbox  
- Migrate legacy Queue `EmailLocalPart` → one `multi_type` mailbox  

### 6.5 Inbound flow

```text
SES @docsintake.com → S3 MIME → SNS → EmailIntakeWorker
  → resolve mailbox
  → Layer 1 gates (enabled, allowlist, size, type)
  → Layer 2 intake decision (thin typed / full multi)
  → reject: IntakeRejection | accept: Files (+ Batch if ≥2)
  → existing File pipeline
```

---

## 7. Exploration Exit Criteria

- [x] Problem framing + SES catch-all recommendation  
- [x] Typed vs multi mailbox model  
- [x] Address format + security posture  
- [x] Portal automation vs one-time DNS  
- [x] BMIR kept separate  
- [x] Open questions locked  
- [x] Output contract below  

**Gate:** Developer approved Phase 2 (2026-09-11).

---

## Output contract

### Finalized Decisions

| ID | Decision |
|----|----------|
| E1 | Domain = **`docsintake.com`** (canonical spelling) |
| E2 | **Catch-all SES**; Documate DB is mailbox SoT; no per-address SES create |
| E3 | New entity **`OpsIntakeMailbox`**: `typed_agent` \| `multi_type` |
| E4 | Typed mailbox binds to **Agent** (DocumentType via Agent) |
| E5 | All Files land on Business **default Queue** (webhook/storage); typed path **overrides Agent** |
| E6 | Address = `{businessSlug}-{purposeSlug}-{random}@docsintake.com` |
| E7 | New mailbox allowlist default = **`open`** for dogfood; switch to **`allowlist_enforced`** before selling email hard |
| E8 | Subdomain `inbound.docsintake.com` = **later**; v1 uses `docsintake.com` |
| E9 | BMIR = **separate**; does not block Documate SES |
| E10 | Inbound v1 = **SES (D1)** + **simulate stub** for tests; **no IMAP (D2)** in this plan |
| E11 | Ambiguity → **IntakeRejection** (Plan 01 bias) |
| E12 | Legacy Queue mint migrates to one `multi_type` mailbox |

### Pending Decisions

| ID | Decision | Notes |
|----|----------|-------|
| — | None blocking Phase 2 | Wave-level detail in implementation plan |

### Assumptions

- Partner will complete SES domain verify + MX once before production inbound.  
- Business slug can be added/stored for stable prefixes.  
- Hangfire (or equivalent) can host the email intake worker.  
- Existing `IWorkRecordService` File/Batch/IntakeRejection paths are reused.

### Risks

See §4. Highest delivery risks: SES/DNS misconfig, allowlist too open in production, typed Agent deleted while mailbox still enabled.

### Readiness

**Ready for next phase** — Phase 2 implementation plan.

---

## Follow-on backlog (post DQ-1201 / DQ-1401 dogfood)

Captured 2026-09-14 after live SES smoke. **Not** executed in Phase 1 email waves; needs Phase 1 exploration locks (below) then Phase 2 amendment / new DQ band.

### Current behavior (as-built — reference)

| Topic | Today |
|-------|--------|
| **S3 raw MIME after process** | **Left in place** indefinitely. Worker only `GetObject`; no delete, no lifecycle tag, no copy-to-archive. |
| **Email body text** | If ≥1 allowed attachment → body is **not** stored as a File and **not** attached as context on Files. If **no** allowed attachments and body looks substantial (≥80 chars with newline, or ≥200 chars) → one File `email-body.txt`. Else → `ambiguous_email` rejection. |
| **Multiple attachments** | Each allowed, non-empty attachment → **separate `OpsFile`** in one `OpsBatch`; each enqueued. Disallowed extensions / empty parts skipped (no partial-reject File). |
| **Sender fields on File** | `EmailFrom`, `EmailSubject`, `EmailMessageId` only (MIME From address). **No** display name, **no** forward chain (Vendor → User → DocIntake). |
| **Limits config** | `EmailIntake:*` (+ shared `Aws:*`) in **appsettings** / env — not DB. |

### Follow-on items

| ID | Item | Intent |
|----|------|--------|
| **F1** | S3 MIME retention | Decide delete-after-success, delayed delete, S3 lifecycle, or keep for audit/reprocess |
| **F2** | Body vs attachments | When attachments exist: ignore body (today), also store body File, or attach body/context_links on each File |
| **F3** | Multi-attach edge cases | Partial reject when some attachments fail type/size; surface skipped names on Batch/rejection |
| **F4** | Sender chain on Files | Persist forward path **Vendor → User → DocIntake** (emails + display names) on received Files — see opens below |
| **F5** | System settings in DB | Move operational limits (and related knobs) from JSON appsettings into a **system settings** table — cross-cutting; see [15-system-settings-db-exploration.md](./15-system-settings-db-exploration.md) |

### Pending Decisions (follow-on)

| ID | Decision | Options (sketch) |
|----|----------|------------------|
| **F4a** | How to detect Vendor vs User in a forward chain | **A)** Parse MIME `From` = immediate sender (User), use `Reply-To` / first non-docsintake Received / body “From:” / `Resent-From` for Vendor · **B)** Always treat `From` as Vendor (no chain) · **C)** Require structured partner forward (headers Documate defines) |
| **F4b** | What to store on `OpsFile` | **A)** Columns: `EmailFrom`, `EmailFromName`, `EmailOriginator`, `EmailOriginatorName`, optional `EmailSenderChainJson` · **B)** Single `EmailProvenanceJson` · **C)** Separate `OpsEmailIntakeMessage` row linked by Batch/MessageId |
| **F4c** | FE / External API visibility | Show originator + forwarder on Files list/detail; include in webhook payload |
| **F1a** | Post-process S3 object | **A)** Delete after successful File create · **B)** Delete after N days (lifecycle) · **C)** Keep forever (ops cost) · **D)** Move to cold prefix/` Glacier` |
| **F2a** | Body when attachments present | **A)** Keep ignore (today) · **B)** Always add `email-body.txt` File in same Batch · **C)** Stamp truncated body / context on each File `IntakeHintsJson` |

**Default recommendation (not locked):** F1a=**B** (lifecycle); F2a=**C** (context stamp, no extra OCR File unless body-only); F4a=**A** best-effort parse + store both hops; F4b=**A**+json fallback; F5 → Plan 15.

### Locked Decisions (follow-on) — 2026-09-14

| ID | Locked |
|----|--------|
| **F1a** | **B** — Delete raw MIME in S3 after **N days**; default **N = 30** (configurable via system settings once Plan 15 lands); S3 lifecycle and/or Documate-scheduled cleanup |
| **F4a** | **A** — Best-effort: MIME `From` (+ display name) = immediate sender (User/forwarder); Vendor/originator from `Reply-To` / `Resent-From` / body “From:” / Received heuristics when present |
| **F4b** | **Amended 2026-09-14:** single `EmailIntakeJson` on `OpsFile` (from/originator/chain/skipped…); **only** `EmailSubject` + `EmailMessageId` as dedicated email columns (existing `EmailFrom` → see follow-on DR-FO4) |
| **F4c** | Yes — surface on Files list/detail + webhook payload |
| **F2a** | **C** — When attachments exist, stamp truncated body / context onto each File (`IntakeHintsJson` / related), **not** a separate OCR File unless body-only path |
| **F5 / S\*** | Plan 15 **complete**: S1–S6 locked; secrets stay env; default MIME retention **30** days |

---

## Changelog

| Date | Note |
|------|------|
| 2026-09-11 | Phase 1 exploration written; E1–E12 locked; BMIR kept separate; Phase 2 approved to proceed. |
| 2026-09-14 | Follow-on backlog F1–F5 after SES smoke (S3 retention, body, multi-attach, sender chain, system settings). |
| 2026-09-14 | Locked F1a=B (delete after N days); F4a/b/c as recommended (sender chain on Files + FE/webhook). |
| 2026-09-14 | Locked F2a=C (stamp truncated body context on Files when attachments present). |
| 2026-09-14 | Locked N1=30 days; Plan 15 S2/S3/S5/S6 via recommendations — follow-on exploration closed. |
| 2026-09-14 | F4b amended: single EmailIntakeJson (not per-field columns); Subject/MessageId stay columns. |