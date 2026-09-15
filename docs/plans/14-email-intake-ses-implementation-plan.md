# Documate v3 — Email Intake via SES (Implementation Plan)

> **Status:** Phase 2 — **complete** (DQ-1201…1203 executed 2026-09-11)  
> **Type:** Engineering implementation plan  
> **Upstream:** [14-email-intake-ses-exploration.md](./14-email-intake-ses-exploration.md) (Phase 1 complete; E1–E12 locked 2026-09-11)  
> **Also incorporates:** Plan 01 §14; Plan 02 §5.2; Plan 03 Flow 3; Decision K1 (default Queue); DQ-1201 intent  
> **Downstream:** Phase 3 — amend [03-documate-v3-dispatch-queue.md](./03-documate-v3-dispatch-queue.md) (reframe DQ-1201 + SES mailbox waves)  
> **Created:** 2026-09-11  

**Outcome:** Partners create typed (per-Agent) and multi-type intake emails on **`docsintake.com`** from the Documate portal; SES catch-all delivers MIME into Documate; gates + intake decision create Files on the **default Queue** (typed path routes to the bound Agent).

---

## Planning flow

| Phase | Document |
|-------|----------|
| 1 — Exploration | `14-email-intake-ses-exploration.md` ✅ |
| 2 — Implementation plan | **This file** |
| 3 — Dispatch queue | Amend `03-documate-v3-dispatch-queue.md` — after this plan approved |

---

## Delivery Principles

1. **Catch-all SES** — portal never calls SES to create identities per address (E2).  
2. **`OpsIntakeMailbox` is SoT** for which local-parts are valid (E3).  
3. **Default Queue remains channel** for storage/webhooks (E5 / K1).  
4. **Typed = Agent bind**; DocumentType from Agent (E4).  
5. **Reuse** `IWorkRecordService`, Hangfire, existing File pipeline.  
6. **Simulate path** for CI/dev without SES (E10).  
7. **BMIR out of scope** (E9).  
8. **No IMAP** in this plan (E10).  
9. **Migrate** legacy Queue email fields to one `multi_type` mailbox (E12).  
10. **Secrets/config** server-side: `EmailIntake:*`, AWS credentials for S3/SNS consumer.

---

## Domain Architecture Layer

| Concern | Home |
|---------|------|
| Entity `OpsIntakeMailbox` + allowlist entries | `Domain/` + EF config + migration |
| Business intake slug | `CorTenantBusiness.IntakeEmailSlug` (new) |
| Mailbox CRUD (app) | `Modules/FrontendSupport/Features/IntakeMailboxes/` |
| Agent “create intake email” action | Same feature or thin Agents command → mailbox |
| Simulate inbound (app/admin or app) | `Modules/FrontendSupport/Features/EmailIntake/` or External internal |
| SES/SNS webhook or poller + S3 fetch | `Infrastructure/EmailIntake/` |
| MIME parse → targets | `Infrastructure/EmailIntake/` |
| Layer 1 gates + allowlist match | `Modules/Core/` or Infrastructure service called from worker |
| Intake decision (typed thin / multi full) | `Modules/Core/` system-agent skeleton (`IEmailIntakeDecisionAgent`) |
| Create Files / IntakeRejection / enqueue | Existing `IWorkRecordService` + dispatcher |
| Options | Extend `EmailIntakeOptions` (`DefaultDomain`, S3 bucket, SNS, caps) |
| Customer UI | Agent detail + Channels (`apps/web`) |
| Infra as code / runbook | Ops doc under `docs/` or infra repo — domain verify steps |

```text
SES docsintake.com
  → S3 raw MIME + SNS
  → EmailIntakeWorker (Infrastructure)
       → resolve OpsIntakeMailbox by local-part
       → Layer 1 gates
       → IEmailIntakeDecisionAgent
       → IntakeRejection | Files on default Queue (+ Batch if ≥2)
       → Hangfire File pipeline (existing)
```

Typed route note: after classify/route stage, **mailbox AgentId wins** when `typed_agent` (stamp type hint at File create; ensure route selects bound Agent even if QueueRoute would differ — prefer ensuring QueueRoute exists for that DocumentType→Agent on default Queue at mailbox create time).

---

## Domain Validation Rules

### Closed (exploration E1–E12)

| Rule | Behavior |
|------|----------|
| Domain | Addresses always use `EmailIntake:DefaultDomain` = `docsintake.com` in prod config |
| Unknown local-part | Soft reject / drop with ops log; optional IntakeRejection only if Queue/Business resolvable — **prefer ops metric only** for totally unknown To: |
| Disabled mailbox | IntakeRejection `email_intake_disabled` |
| Allowlist enforced + no match | IntakeRejection `allowlist_rejected` |
| Allowlist open | Accept; stamp allowlist match result on decision record |
| Size / type caps | Configurable; fail gate → IntakeRejection (no OCR) |
| Ambiguous decision | IntakeRejection (no File) |
| Typed Agent missing/inactive | IntakeRejection `mailbox_agent_unavailable` |
| Local-part uniqueness | Filtered unique `(EmailDomain, EmailLocalPart)` |
| Rotate | New local-part; version++; old part stops resolving |
| One typed mailbox per Agent | At most one non-deleted `typed_agent` per `AgentId` (create replaces or rejects duplicate — **reject duplicate; rotate instead**) |
| Multi mailboxes | Allow multiple `multi_type` per Business (support isolation); UI may highlight “primary” |
| File source | `intake_source=email`; set `email_message_id` from MIME Message-Id |
| Sync-wait | Unchanged; email path is async only |

### Decision Required — DR-EI1: Unknown recipient handling

| Option | Idea |
|--------|------|
| A | Silent drop + metric |
| B | Platform-level IntakeRejection (no Business) |
| C | Bounce via SES (complex) |

**Locked:** **A** — silent drop + metric/log. Do not create orphan rejections without Business scope.

### Decision Required — DR-EI2: Ensure typed route

| Option | Idea |
|--------|------|
| A | On mailbox create, upsert QueueRoute (default Queue, Agent.DocumentType → Agent) if missing |
| B | Fail route later if no QueueRoute |
| C | Bypass QueueRoute table entirely for typed mail |

**Locked:** **A** — upsert QueueRoute on create so pipeline stays consistent with K1.

### Decision Required — DR-EI3: Simulate API placement

| Option | Idea |
|--------|------|
| A | `POST /api/app/intake-mailboxes/{id}/simulate` (Business-scoped, human auth) |
| B | External API key endpoint |
| C | Both |

**Locked:** **A** for Phase 1 dogfood/tests. External simulate deferred.

---

## Process Flows

### Flow 1 — Portal create typed mailbox

```text
Agent page → Create intake email
  → ensure Business.IntakeEmailSlug
  → purposeSlug from Agent name
  → generate random ≥26 chars
  → insert OpsIntakeMailbox(typed_agent, AgentId, QueueId=default, Enabled)
  → upsert QueueRoute on default Queue
  → return full address
```

### Flow 2 — Portal create multi mailbox

```text
Channels → Create multi-type intake email
  → same slug/random rules with purposeSlug=multi
  → OpsIntakeMailbox(multi_type, QueueId=default)
```

### Flow 3 — SES inbound (production)

```text
MIME in S3 → worker
  → parse To/From/Subject/attachments/body
  → resolve mailbox
  → gates → decision → Files | IntakeRejection
  → enqueue Files
```

### Flow 4 — Simulate

```text
POST simulate with synthetic From + attachment bytes/metadata
  → same Core path as Flow 3 after resolve (skip S3)
```

### Flow 5 — Rotate / disable

```text
Rotate → new local-part + version; old invalid
Disable → EmailIntakeEnabled=false on mailbox; gates reject
```

### Decision Required — DR-EI4: Allowlist ownership

| Option | Idea |
|--------|------|
| A | Per-mailbox allowlist table |
| B | Keep only Queue allowlist for all mailboxes |
| C | Mailbox allowlist with fallback to Queue |

**Locked:** **A** — `OpsIntakeMailboxAllowlistEntry` per mailbox (typed and multi often have different senders). Migrate existing Queue allowlist rows onto the migrated multi mailbox.

---

## Instruction and Control Set

### Config (`EmailIntake`)

| Key | Purpose |
|-----|---------|
| `DefaultDomain` | `docsintake.com` |
| `S3:Bucket` / prefix | Raw MIME store |
| `Sns:TopicArn` or webhook secret | Notify worker |
| `MaxAttachmentBytes` / `MaxAttachments` / `AllowedExtensions` | Layer 1 |
| `Aws:Region` | SES/S3 region |

### System agent interface

```csharp
// Conceptual — implement in Core
Task<EmailIntakeDecision> DecideAsync(EmailIntakeContext ctx, CancellationToken ct);
// action: process | reject | process_partial
// targets: body | attachment | inline_image
```

- **typed_agent:** heuristic-first skeleton (attachments of allowed types; body-as-doc only if no attachments and body looks non-empty text); LLM optional later.  
- **multi_type:** same skeleton + explicit “ambiguous → reject”; LLM classify of targets deferred unless cheap rules suffice.

### WorkEvents

Emit refs only: mailbox id, message id, gate codes, decision action — no full MIME in SQL.

### Decision Required — DR-EI5: Intake decision v1 intelligence

| Option | Idea |
|--------|------|
| A | Rules/heuristics only (skeleton) |
| B | Always call LLM |
| C | Rules then LLM on ambiguity |

**Locked:** **A** for first ship (absorbs DQ-1201 skeleton). C later as follow-on DQ.

---

## Permissions and Security

| Control | Behavior |
|---------|----------|
| Address entropy | ≥26 char random segment (E6) |
| Capability secret | Full address; rotate on leak |
| Allowlist | Per mailbox; default mode `open` (E7); product to `allowlist_enforced` before sell |
| App APIs | Human auth + Business scope (`/api/app/...`) |
| SNS→API | Verify SNS signature / shared secret; no public anonymous intake except SES path |
| PII | MIME in S3 with Business-prefixed keys when possible; GDPR delete = delete objects + soft-delete mailbox |
| Agent delete | Block delete if typed mailbox enabled, or auto-disable mailbox |

---

## Dispatch Index (proposed — Phase 3 will number formally)

| Wave | Outcome |
|------|---------|
| EI-0 | Options + `IntakeEmailSlug` + `OpsIntakeMailbox` (+ allowlist) migration; migrate Queue email → multi mailbox |
| EI-1 | App APIs: list/create/rotate/disable mailbox; upsert QueueRoute on typed create |
| EI-2 | Customer UI: Agent intake email + Channels multi + allowlist |
| EI-3 | Simulate endpoint + gates + IntakeRejection + File create enqueue (no SES yet) |
| EI-4 | Intake decision skeleton (typed + multi heuristics) |
| EI-5 | SES infra runbook + S3/SNS consumer worker → same Core path |
| EI-6 | Hardening: metrics, rate limits, docs; allowlist_enforced guidance |

**DQ-1201:** Supersede outcome to “simulate + intake skeleton” covered by EI-3/EI-4; mark complete when those land. **DQ-1202:** Reframe activation to SES (D1) only under EI-5; IMAP stays parked.

---

## Wave Sections

### Wave EI-0 — Schema

- Add `CorTenantBusiness.IntakeEmailSlug` (nullable → set on first mailbox create).  
- Add `OpsIntakeMailbox`: Id (UUID), SequenceId, BusinessId, QueueId, KindEnumId (`intake_mailbox_kind`: `typed_agent` \| `multi_type`), AgentId?, EmailLocalPart, EmailDomain, EmailAddressVersion, Enabled, AllowlistModeEnumId, soft-delete, RowVersion.  
- Add `OpsIntakeMailboxAllowlistEntry`.  
- Seed CorEnum `intake_mailbox_kind`.  
- Data migration: if Queue has EmailLocalPart, insert multi mailbox and clear advertising of Queue mint (keep columns deprecated or null out after copy).

### Wave EI-1 — App APIs

- CQRS under `FrontendSupport/Features/IntakeMailboxes/`.  
- Create typed (from AgentId), create multi, get, list by Business, rotate, set enabled, allowlist CRUD.  
- On typed create: upsert default-Queue QueueRoute.

### Wave EI-2 — Web UI

- Agent page: show address, create/rotate/copy/disable.  
- Channels: multi mailboxes list + allowlist mode/entries.  
- Deprecate Queue-only mint controls (redirect to mailboxes).

### Wave EI-3 — Simulate + Core intake path

- `POST .../intake-mailboxes/{id}/simulate`.  
- Shared `IEmailIntakeProcessor`: gates → decision → work records.  
- Unit tests: allowlist, disabled, unknown agent, happy path file enqueue.

### Wave EI-4 — Decision skeleton

- `IEmailIntakeDecisionAgent` rules implementation.  
- Ambiguity → reject evidence in tests.

### Wave EI-5 — SES

- Runbook: domain verify, MX, receipt rule, bucket policy, SNS subscription to API/worker.  
- `EmailIntakeSqsOrSnsHandler` downloads S3, parses MIME (MimeKit or similar), calls processor.  
- Smoke: send real mail to typed + multi addresses.

### Wave EI-6 — Harden

- Metrics: unknown recipient, gate rejects, accept latency.  
- Rate limit per mailbox.  
- Checklist before `allowlist_enforced` default flip.

---

## Security constraints (summary)

- No full MIME in WorkEvents or SQL.  
- No customer-visible AWS keys.  
- Simulate is Business-auth only.  
- Do not market email until allowlist UX proven.

---

## Output contract

### Finalized Decisions

| ID | Decision |
|----|----------|
| E1–E12 | From exploration (domain, catch-all, mailbox model, Agent bind, default Queue, address format, allowlist default open, no subdomain v1, BMIR separate, SES+simulate, reject ambiguity, migrate Queue mint) |
| DR-EI1 | Unknown To: → drop + metric |
| DR-EI2 | Typed create upserts QueueRoute |
| DR-EI3 | Simulate on `/api/app/...` only |
| DR-EI4 | Per-mailbox allowlist |
| DR-EI5 | Heuristic intake decision for v1 |

### Pending Decisions

None blocking Phase 3. Infra account/region details are ops inputs at EI-5 execution.

### Assumptions

- AWS account and DNS for `docsintake.com` available before EI-5 smoke.  
- MimeKit (or chosen library) acceptable in Infrastructure.  
- Hangfire worker process can reach S3.

### Risks

- SES receipt rule misconfig delays EI-5.  
- Heuristic intake too aggressive/rejective — tune with simulate corpus.  
- Multiple multi mailboxes confuse UX — UI should label clearly.

### Readiness

**Ready for Phase 3** after developer verifies this implementation plan — then create/amend dispatch queue entries (EI-0…EI-6 / DQ-1201 reframe). **Do not start coding** until a DQ is selected per governance.

---

## Changelog

| Date | Note |
|------|------|
| 2026-09-11 | Phase 2 implementation plan drafted; DR-EI1…EI5 locked to exploration defaults. |
