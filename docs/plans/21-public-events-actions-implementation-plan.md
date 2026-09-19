# Documate v3 — Public domain events + automated actions (Implementation Plan)

> **Status:** Phase 2 — **complete** (approved → Phase 3 2026-09-18)  
> **Type:** Engineering implementation plan  
> **Upstream:** [20-public-events-actions-exploration.md](./20-public-events-actions-exploration.md) (PE1–PE11 locked)  
> **Related:** Plan 02 §9 webhooks; Plan 14 email intake (emits same public events); Plan 15 notifications SMTP (platform transport); Plan 16 admin may observe deliveries later  
> **Downstream:** [21-public-events-actions-dispatch-queue.md](./21-public-events-actions-dispatch-queue.md) (Band 18)  
> **Created:** 2026-09-17

**Outcome:** Replace hard-coded `document.terminal` delivery with a **public event catalog** + **action bindings** (webhook, email, in-app). Business holds defaults; queues inherit/override. Emit milestones; track all outbound attempts in `OpsOutboundDelivery`. Partner FE on **Business + Queue** settings. Suppress all public actions on `api_sync`.

**Out of this plan:** Multiple partner webhook URLs per scope (PE7=B); Zapier graphs; BMIR; changing poll APIs; admin as primary configurator.

---

## Planning flow

| Phase | Document |
|-------|----------|
| 1 — Exploration | [20-public-events-actions-exploration.md](./20-public-events-actions-exploration.md) ✅ |
| 2 — Implementation plan | **This file** ✅ |
| 3 — Dispatch queue | [21-public-events-actions-dispatch-queue.md](./21-public-events-actions-dispatch-queue.md) (Band 18) |

---

## Delivery Principles

1. **Public events ≠ `OpsWorkEvent`.** Catalog milestones only; internal audit stays on `OpsWorkEvents`.  
2. **Catalog (PE1=A, PE2=B, PE3=B):** `file.received`, `document.ready` / `document.failed` / `document.cancelled`, `file.completed` (all docs Ready only). **No** `document.terminal`.  
3. **Actions (PE4=C):** Webhook + email + in-app — wave-staged if needed, same spine.  
4. **Scope (PE5=B, PE6=B, PE11):** Business defaults (`file.received` + document terminals on create); Queue inherit/override.  
5. **Partner webhook UX (PE7=A):** One URL/secret/enable + event checklist per effective scope.  
6. **Spine (PE8=A):** Emit → resolve bindings → Hangfire per action execution.  
7. **Delivery meta (PE9=A):** `OpsOutboundDelivery` resource-typed (not Document columns only).  
8. **Sync (PE10=A):** Suppress all public actions for `api_sync`.  
9. **Idempotency:** Stable `event_id` per occurrence; retries share it.  
10. **FE in scope:** Business Integrations/Events + Queue inherit/override (+ notify settings).  
11. **Reuse** Hangfire `webhooks` queue, HMAC signing, Data-Protected secrets where applicable.

---

## Domain Architecture Layer

| Concern | Home |
|---------|------|
| Public event names + payload builders | `Infrastructure/PublicEvents/` (catalog constants, `IPublicEventEmitter`) |
| Action binding persistence | New domain entity/entities (see **DR-EA1**) + EF migration |
| `OpsOutboundDelivery` | `Domain/` + EF; replaces Document-only webhook meta over time (**DR-EA2**) |
| Resolve bindings (Business → Queue) | `Infrastructure/PublicEvents/ActionBindingResolver` |
| Executors | `WebhookActionExecutor` (evolve today’s `DocumentWebhookDelivery`), `EmailActionExecutor`, `InAppActionExecutor` |
| Hangfire jobs | Extend `WebhookJobs` / add `PublicActionJobs` on existing `webhooks` queue (or sibling queue — **DR-EA3**) |
| Emit call sites | File create path (`file.received`); document terminal paths (extract/cancel); file rollup when all docs Ready (`file.completed`) |
| Partner API | FrontendSupport: Business events/actions + Queue override endpoints |
| Partner FE | `apps/web` Business page(s) + Queue overview Intake/Integrations |
| Platform SMTP transport | Reuse `NotificationOptions` / SMTP for email action (recipients from binding config) |
| External docs | `apps/web` docs / API reference: new event names |

```text
Pipeline / intake milestone
  → IPublicEventEmitter.Emit(name, resource, payload, event_id)
      → skip if source == api_sync (PE10=A)
      → resolve ActionBindings (Business defaults, Queue override)
      → for each matching action: enqueue Hangfire execution
          → executor delivers → write OpsOutboundDelivery (+ optional OpsWorkEvent audit)
```

### Effective config resolution (normative)

```text
For Queue Q under Business B:
  if Q.OverridePublicActions:
      use Q webhook + Q event checklist + Q email/in-app bindings
  else:
      use B webhook + B event checklist + B email/in-app bindings
```

Exact column/table shape: **DR-EA1**.

---

## Domain Validation Rules

### Closed (exploration PE\*)

| Rule | Behavior |
|------|----------|
| Catalog | Only listed milestones; no per-tick `status_changed` public emit |
| Naming | `document.ready` / `failed` / `cancelled` — drop `document.terminal` |
| `file.completed` | Emit iff every Document under File is **Ready** (not PartialReady / mixed failure) |
| Defaults | New Business: subscribe `file.received` + three document terminals; `file.completed` off unless checked |
| Sync | No public actions for `api_sync` |
| Ordering | `file.received` before doc events for that file; `file.completed` only after all Ready |
| Secrets | Webhook secret remains Data-Protected; never in public payloads |

### Emit semantics (normative)

| Event | When | Resource | Notes |
|-------|------|----------|-------|
| `file.received` | After File + blob committed (async/email intake) | File | Before OCR; idempotent `file:{id}:received` |
| `document.ready` | Document public status → Ready | Document | Result payload (extract/schema URLs as today) |
| `document.failed` | Document → Failed (**and rejected? — DR-EA4**) | Document | Include error code/message |
| `document.cancelled` | Document → Cancelled | Document | Cancel file/doc paths |
| `file.completed` | All child Documents Ready | File | PE3=B; no emit on PartialReady |

### Payload principles

- Snake_case JSON (match current webhook style).  
- Headers: `X-Documate-Event` = event name; `X-Documate-Delivery` = attempt/delivery id; `X-Documate-Signature` = HMAC over raw body.  
- Body includes: `event`, `event_id`, `occurred_at`, resource ids, `business_id`, `queue_id`, `source`.  
- Document events carry today’s logical document fields (status, errors, URLs when ready).  
- File events carry file identity + original_file hints when known — **not** full bytes.

---

## Decision Required

### Locked (2026-09-18) — all recommended

| ID | Locked |
|----|--------|
| **DR-EA1** | **A** — `OpsActionBinding` (Business defaults; optional QueueId for override) + Queue `PublicActionsInherit` (default true). Migrate from `OpsQueue` webhook columns. |
| **DR-EA2** | **A** — `OpsOutboundDelivery` is SoT for attempts; Document webhook columns projected/cache (cleanup later). |
| **DR-EA3** | **A** — Single Hangfire queue `webhooks` for webhook + email + in-app executions. |
| **DR-EA4** | **A** — Public status `rejected` emits **`document.failed`** (payload distinguishes reason). |
| **DR-EA5** | **A** — `OpsInAppNotification` table + list API; partner FE bell/list can follow in EA-6. |
| **DR-EA6** | **C** — Platform Documate-support email binding **and** optional partner email binding on Business/Queue. |
| **DR-EA7** | **A** — Business **Integrations/Events** section + Queue **Intake** inherit/override + checklist. |

---

## Process Flows

### F1 — Async file intake → `file.received`

```text
CreateFileWithBlob (or email intake equivalent) commits File
  → Emit file.received (if not api_sync)
  → Resolve bindings → enqueue actions
  → Pipeline continues (OCR…)
```

### F2 — Document terminal

```text
Document reaches Ready | Failed | Cancelled (| Rejected→DR-EA4)
  → Emit document.* (typed name)
  → Resolve bindings → enqueue actions
  → (Existing DocumentWebhookScheduler path retired/replaced)
```

### F3 — `file.completed`

```text
After a Document becomes Ready:
  → If all siblings Ready → Emit file.completed once (event_id file:{id}:completed)
  → Else no file.completed (PartialReady / any Failed/Cancelled)
```

### F4 — Partner configures Business defaults

```text
Business Integrations save
  → Upsert Business-scoped webhook + event keys + email/in-app bindings
  → Queues with inherit=true take effect immediately on next emit
```

### F5 — Queue override

```text
Queue sets Override
  → Saves queue-scoped bindings / webhook fields
  → Emit uses queue config until Inherit restored
```

---

## Instruction and Control Set

| Control | Rule |
|---------|------|
| Emit only at catalog milestones | No public emit on every `OpsWorkEvent` |
| `event_id` uniqueness | One logical occurrence; executors dedupe succeeded deliveries |
| Max attempts | Align with today’s webhook MaxAttempts (5) unless options say otherwise |
| HTTPS | Same as today (HTTP only Development) |
| Business isolation | Resolver and queries always filter `BusinessId` |
| api_sync | Hard suppress in emitter (not only webhook executor) |
| Secret handling | Protect at rest; FE shows write-only secret (no read-back plaintext) |

---

## Permissions and Security

- Partner APIs: existing Business-scoped FrontendSupport auth; user may only read/write own Business / its Queues.  
- Webhook secret: Data Protection; never return plaintext on GET (presence flag / last-four optional).  
- Public payloads: no cross-Business ids; no other tenants’ files.  
- Email action: no secrets in email body; recipients validated as emails.  
- Admin observation of `OpsOutboundDelivery` can come later (Plan 16) — not required to configure partner settings.

---

## Dispatch Index (proposed — Phase 3)

Exact DQ ids assigned in Phase 3 (likely new band after current active bands).

| Proposed | Wave | Outcome |
|----------|------|---------|
| DQ-EA-01 | EA-0 | Schema: bindings + `OpsOutboundDelivery` (+ in-app table if DR-EA5=A) |
| DQ-EA-02 | EA-1 | Emitter + resolver + Hangfire executors (webhook first); retire `document.terminal` name |
| DQ-EA-03 | EA-2 | Emit `document.*` from extract/cancel; dual-write/project delivery meta |
| DQ-EA-04 | EA-3 | Emit `file.received` + `file.completed` |
| DQ-EA-05 | EA-4 | Partner API: Business + Queue settings (inherit/override) |
| DQ-EA-06 | EA-5 | Partner FE: Business Integrations + Queue Intake/override |
| DQ-EA-07 | EA-6 | Email + in-app executors + FE controls |
| DQ-EA-08 | EA-7 | Docs / external reference + evidence tests |

---

## Wave Sections

### EA-0 — Schema

- Action bindings (per DR-EA1); `OpsOutboundDelivery`; enums for action type / delivery status if needed.  
- Queue inherit flag.  
- Seed/migrate existing `OpsQueue` webhook fields into bindings.  
- New Business create: default bindings (PE6).

### EA-1 — Spine

- `IPublicEventEmitter`; binding resolver; webhook executor (HMAC, retries) using typed event names.  
- Remove hard-coded `document.terminal` constant from payload.

### EA-2 — Document emits

- Replace `DocumentWebhookScheduler` usage with emitter at terminal transitions.  
- Map rejected per DR-EA4.  
- Preserve payload usefulness for Ready (URLs, errors).

### EA-3 — File emits

- `file.received` after successful file+blob create (async/email).  
- `file.completed` when all docs Ready (guard + idempotent).

### EA-4 — Partner API

- GET/PUT Business public-actions settings.  
- GET/PUT Queue public-actions (inherit | override).  
- Deprecate or wrap `PUT .../queues/{id}/webhook` as façade during transition.

### EA-5 — Partner FE

- Business Integrations/Events UI (DR-EA7).  
- Queue page: inherit toggle + checklist + webhook fields when override.  
- Wire API clients in `apps/web`.

### EA-6 — Email + in-app

- Executors; platform + partner email per DR-EA6; in-app store/API per DR-EA5.  
- FE toggles/recipients.

### EA-7 — Docs & evidence

- Update customer docs event names/payloads.  
- Tests: emit suppress on sync; inherit vs override; idempotent `event_id`; delivery rows written.

---

## Output contract

### Finalized from exploration

| ID | Locked |
|----|--------|
| PE1 | A — minimal catalog |
| PE2 | B — typed document events; no `document.terminal` |
| PE3 | B — `file.completed` only all Ready |
| PE4 | C — webhook + email + in-app |
| PE5 | B — Business defaults, Queue inherit |
| PE6 | B — defaults on new Business |
| PE7 | A — one partner webhook URL + checklist |
| PE8 | A — dispatcher → Hangfire |
| PE9 | A — `OpsOutboundDelivery` |
| PE10 | A — suppress all on `api_sync` |
| PE11 | Partner Business (+ Queue override) |

### Locked (this Phase 2)

| ID | Choice |
|----|--------|
| **DR-EA1** | **A** — `OpsActionBinding` + Queue inherit flag |
| **DR-EA2** | **A** — `OpsOutboundDelivery` SoT |
| **DR-EA3** | **A** — single Hangfire `webhooks` queue |
| **DR-EA4** | **A** — `rejected` → `document.failed` |
| **DR-EA5** | **A** — `OpsInAppNotification` |
| **DR-EA6** | **C** — platform support email + partner email binding |
| **DR-EA7** | **A** — Business Integrations + Queue Intake |

### Pending

None — Phase 3 DQ filed ([21-public-events-actions-dispatch-queue.md](./21-public-events-actions-dispatch-queue.md)).

### Assumptions

- Pre-release: no partner migration alias for `document.terminal`.  
- SMTP for email actions available via existing notification options (Plan 15/admin settings).  
- Default channel bootstrap remains; webhook may stay empty until Business defaults supply URL.

### Risks

- PE3=B surprises partners expecting file-level “done” on PartialReady — document events remain source of truth for mixed outcomes.  
- PE4=C scope if EA-6 slips — keep webhook spine shippable in EA-1…5.  
- Binding migration bugs leaving queues with inherit=false empty config.

### Readiness

**Phase 2 complete.** Downstream: Band 18 dispatch queue. Execute via DQ selection (start **DQ-1801**).

---

## Changelog

| Date | Note |
|------|------|
| 2026-09-17 | Phase 2 draft from exploration approve; PE\* closed; DR-EA1…EA7 opened. |
| 2026-09-18 | Locked DR-EA1…EA7 all recommended (A/A/A/A/A/C/A). Ready for Phase 3 approve. |
| 2026-09-18 | Phase 3 approved (“do next”) → Band 18 DQ-1801…1808 filed. |
