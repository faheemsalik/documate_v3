# Documate v3 — Public domain events + automated actions (Exploration)

> **Status:** Phase 1 — **complete** (approved → Phase 2 2026-09-17)  
> **Type:** Product / mental design exploration  
> **Trigger:** Partner need for early lifecycle signals (e.g. file received) plus configurable outbound reactions; webhooks today are a single hard-coded `document.terminal` path  
> **Upstream:** [01-project-exploration-mental-design.md](./01-project-exploration-mental-design.md) § delivery; [02-document-queue-design.md](./02-document-queue-design.md) §9 webhooks; current Hangfire `DocumentWebhookDelivery`  
> **Related (do not merge):** Plan 14 email intake (consumer of events, not owner); Plan 15 system settings (platform ops knobs, not partner subscriptions); Plan 16 backoffice (may *observe* events later); Plan 12 BMIR (separate multi-app routing product)  
> **Downstream:** [21-public-events-actions-implementation-plan.md](./21-public-events-actions-implementation-plan.md)  
> **Created:** 2026-09-17

**One-liner:** Introduce a **first-class public domain event catalog** and a way to attach **automated actions** (webhook, notification, …) so delivery is no longer “one hardcoded document webhook.”

---



## Planning flow


| Phase                   | Document                                                                      |
| ----------------------- | ----------------------------------------------------------------------------- |
| 1 — Exploration         | **This file** ✅                                                              |
| 2 — Implementation plan | [21-public-events-actions-implementation-plan.md](./21-public-events-actions-implementation-plan.md) ✅ |
| 3 — Dispatch queue      | [21-public-events-actions-dispatch-queue.md](./21-public-events-actions-dispatch-queue.md) (Band 18) |


---



## 1. Problem Framing



### Today

- Outbound integration is **queue webhook URL + secret + enable**.
- Runtime emits **one** logical event: `document.terminal` when a Document hits ready/failed/rejected/cancelled.
- No `file.received`, no file rollup completion event, no partner-facing event catalog, no pluggable “actions.”
- Email intake and async API uploads share that path; sync extract suppresses webhooks (by design).
- Ops email (LLM failure alerts) is a **separate** path — not part of the same model.



### Pain

1. Partners cannot **ack intake early** (file/email received) without polling.
2. Extending delivery means editing pipeline code and shipping a new event name ad hoc.
3. Product wants a general pattern: **domain event happens → zero or more automated actions run** — webhook and in-app/email notification are just action types.
4. Plan 02 locked “Document is the webhook atom”; that remains valuable for **results**, but is too narrow as the *only* public signal.



### Goal

Define a **Public Event System** for Documate:

1. **Emit** well-named, versioned **public domain events** at lifecycle milestones (File / Document / Batch / IntakeRejection / …).
2. Allow Businesses (via Queue / channel settings, and later more) to **subscribe actions** to those events.
3. Keep **internal** work-events / Hangfire / metrics distinct from **public** partner-facing events.
4. Preserve backward compatibility for existing `document.terminal` consumers during migration.

**Non-goal of this exploration:** Implement BMIR, redesign poll APIs, or build a full Zapier-style workflow builder in v1.

---



## 2. Scope



### In (exploration + intended v1 product)

- Public event catalog (names, when emitted, payload principles, idempotency).  
- Action model: at least **Webhook** and a path for **Notification** (email/ops or in-app — decide depth in opens).  
- Subscription / binding: which events → which actions, at which scope (Queue vs Business).  
- Relationship to current Queue webhook settings and Plan 02 §9.  
- Compatibility strategy for existing partners.  
- What is **not** a public event (internal stages, Hangfire retries, OCR page ticks).



### Out (v1 unless locked otherwise)

- Arbitrary user-authored workflow graphs / scripting.  
- Multiple unrelated webhook URLs per event (may be phase 2).  
- Cross-Business or platform fan-out to third-party iPaaS as a product.  
- Changing sync-extract “no webhook” rule (C2) without explicit decision.  
- Replacing poll APIs.  
- BMIR / multi-app message bus (Plan 12).

---



## 3. Current-State Findings


| Concern                   | As-built                                                                           |
| ------------------------- | ---------------------------------------------------------------------------------- |
| Config                    | `OpsQueue`: `WebhookEnabled`, `WebhookUrl`, `WebhookSecretProtected`               |
| Emit                      | `DocumentWebhookScheduler` on terminal document public status                      |
| Deliver                   | Hangfire queue `webhooks` → HMAC POST → retry/exhaust on **Document** webhook meta |
| Event name                | Hard-coded `document.terminal`                                                     |
| File-level outbound       | None                                                                               |
| Intake rejection outbound | None (`OpsIntakeRejection` is listable only)                                       |
| Email vs API              | Same pipeline; email payloads include provenance / original_file hints             |
| Internal audit            | `OpsWorkEvent` (status_changed, webhook_*) — **not** partner contract              |
| Platform alerts           | `IOpsAlertSender` / notification options — separate from customer webhook          |


**Implication:** We should not bolt “file.received” only onto `DocumentWebhookDelivery`. We need an **emit → match subscriptions → execute actions** spine; today’s webhook becomes the first **action executor**.

---



## 4. Risks and Constraints

### Summary


| Risk                  | Note                                                                                         |
| --------------------- | -------------------------------------------------------------------------------------------- |
| **Event spam**        | Status-change-on-every-tick overwhelms partners and our Hangfire workers                     |
| **Breaking change**   | Renaming/removing `document.terminal` without alias breaks integrations                      |
| **Two event systems** | Confusing public events with `OpsWorkEvent` / metrics                                        |
| **Scope creep**       | “Actions” → full automation platform                                                         |
| **Idempotency**       | Retries must not look like new business occurrences                                          |
| **Ordering**          | Cannot promise global order; must define partial order guarantees                            |
| **Delivery tracking** | Today meta is Document-scoped; File/Batch events need a home                                 |
| **Security**          | Secrets stay protected; public payloads must not leak other Businesses                       |
| **Plan 02 tension**   | “Webhook unit = Document” stays true for **result** actions; public **catalog** can be wider |


### Detail (with examples)

#### Event spam

If every internal status tick becomes a public webhook or email, partners and Hangfire get flooded.

**Bad example:** A document goes `Received → OcrRunning → Split → Classified → Routed → Extracting → Ready` (~6–10 status changes). At 1,000 docs/hour that becomes thousands of outbound POSTs, retries, and support emails.

**Per-status opt-in flags alone are weak:** UI becomes a long checklist of pipeline internals; partners must understand Documate stages; new stages later force new flags / migration noise; easy to turn on too much by accident.

**Preferred generic control (layered):**

| Layer | Rule |
| ----- | ---- |
| **Catalog** | Only emit a **small fixed set of milestones** (`file.received`, `document.ready` / `failed` / `cancelled`, `file.completed`) — not every `status_changed` |
| **Subscription** | Per action: checklist of those catalog events (opt-in/out) |
| **Audience** | Partner webhook vs Documate email can subscribe to **different** events on the same occurrence |

Opt-in/out still exists — at the **public event type** layer, not on every raw status. That is the spam control.

#### Breaking change

Partners already integrate against today’s contract: event name `document.terminal` plus current payload/headers.

**Example:** Today `X-Documate-Event: document.terminal` fires when Ready/Failed/Cancelled. If we rename only to `document.ready` / `document.failed` and stop sending `document.terminal`, partner code that does `if event == "document.terminal"` stops firing — their CRM never updates, and they may not notice until production.

**Mitigations (PE2):** Keep `document.terminal` for a period (**alias**), or keep it forever as the “any terminal” umbrella and add typed events alongside. “Breaking” = a change that makes existing working integrations fail without a coordinated migration.

#### Two event systems — `OpsWorkEvent` / metrics vs public events

**`OpsWorkEvent`** = **internal audit trail** in our DB (`OpsWorkEvents` table). Pipeline/stages append rows such as:

- `status_changed` (OCR → split → classify → extract, …)
- `webhook_attempted` / `webhook_succeeded` / `webhook_failed`
- `cancelled`

Used for admin timeline, debugging, and analytics (e.g. stage duration from consecutive `status_changed` rows). **Not** a partner contract — no HMAC POST to their URL.

**Metrics** = operational aggregates derived from work events / statuses (throughput, stage latency, webhook success rates) for admin dashboards — also internal.

**Public events** = the partner/admin **notification contract** (webhook body, support email trigger).

**Confusion example:** Someone wires “notify on every `OpsWorkEvent.status_changed`” thinking that is the public API → spam + unstable API (we can add/rename internal event types anytime).

**Rule:** Pipeline writes `OpsWorkEvent` always; public events only at chosen milestones.

#### Scope creep

“Actions” can grow into a full Zapier (conditions, branches, scripts, multi-step workflows).

**Example:** Start with “on file failed → webhook + support email,” then “only if queue=X and type=invoice and retry count>2 and between 9–5…” → months of product, not a delivery spine.

**Guard:** v1 = event catalog + bindings (webhook / email / notify). No user-authored graphs.

#### Idempotency

Retries must not look like new business occurrences to the partner.

**Example:** Hangfire retries a webhook after timeout. Partner creates a new CRM ticket each time → duplicate tickets for one failed file.

**Need:** Stable logical `event_id` per occurrence (e.g. `file:{guid}:failed`). Delivery attempts share that id; partners dedupe on it. Our retries are delivery attempts, not new events.

#### Ordering

We cannot promise global order across files, documents, or workers.

**Example:** Doc B Ready webhook arrives before Doc A’s for the same file; or `file.completed` arrives before a late `document.failed` if designed poorly.

**Promise we can make:** For one file: `file.received` first; document terminals next (any order among siblings); `file.completed` last. No cross-file / cross-queue order.

#### Delivery tracking

Today webhook success/fail meta lives on the **Document**. File-level or batch-level events have nowhere obvious to store attempts.

**Example:** `file.received` webhook fails 5 times — if we only have Document webhook columns, ops cannot see delivery status for that file event in admin.

**Need:** Generalized outbound delivery record (or file-scoped meta), not Document-only forever.

#### Security

Payloads or mis-scoped subscriptions must not leak another Business’s data; secrets must stay protected.

**Example:** Support email includes another tenant’s filenames; webhook secret logged in plaintext; or a Business-level binding accidentally fans out to the wrong queue URL.

**Need:** Business isolation on emit + resolve bindings; secrets stay protected; public payloads stay scoped to that resource’s Business.

#### Plan 02 tension

Plan 02 says “Document is the webhook unit.” Expanding the catalog can feel like contradicting that.

**Clarification:** Document remains the unit for **result** delivery (ready/failed payload with extract outcome). The **catalog** can also signal file intake (`file.received`) and file rollup (`file.completed`) — different milestones, not replacing Document as the result atom.

---



## 5. Open Questions (Pending Decisions)



### Catalog & semantics


| ID      | Decision                 | Options (sketch)                                                                                                                                                                                                                 |
| ------- | ------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **PE1** | v1 public event set      | **A)** Minimal: `file.received`, document terminal (`ready`/`failed`/`cancelled`), optional `file.completed` · **B)** Broader: + `intake.rejected`, + classified/routed · **C)** Include generic `*.status_changed` from day one |
| **PE2** | Document terminal naming | **A)** Keep legacy `document.terminal` + add typed events · **B)** Replace with `document.ready` / `document.failed` / `document.cancelled` only (breaking) · **C)** New names + temporary alias                                 |
| **PE3** | `file.completed` meaning | **A)** File public status terminal (incl. PartialReady) · **B)** Only when all docs Ready · **C)** Defer file rollup event to phase 2                                                                                            |




### Actions & subscriptions


| ID      | Decision                             | Options (sketch)                                                                                                                                                          |
| ------- | ------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **PE4** | v1 action types                      | **A)** Webhook only (notifications = later) · **B)** Webhook + email notification · **C)** Webhook + in-app notification + email                                          |
| **PE5** | Subscription scope                   | **A)** Queue only (align Plan 02 / current settings) · **B)** Business defaults inherited by queues · **C)** Business-only (breaks multi-queue story)                     |
| **PE6** | Default subscriptions (new Business) | **A)** Document terminal only · **B)** `file.received` + document terminals · **C)** Empty until configured                                                               |
| **PE7** | Multiple actions / endpoints         | **A)** One partner webhook endpoint; event checklist · **B)** N partner webhook endpoints each with event filter · **C)** Action instances table (webhook, email, …) each bound to events |


#### PE7 — detail and examples

**What the options mean in practice:**

| Option | Partner webhook UX | Email / in-app | Example |
| ------ | ------------------ | -------------- | ------- |
| **A** | One URL + secret + enable; checklist of which catalog events fire that URL | Separate settings (not extra webhook URLs) | Business has `https://partner.example/hooks/documate`. Checked: `file.received`, `document.ready`, `document.failed`. Unchecked: `file.completed`. All matching events POST to that single URL. |
| **B** | Several partner URLs, each with its own event filter | Same as A or also multi | URL-1 (CRM) only `document.ready`; URL-2 (archive) only `file.received` + `file.completed`. Deferred — multi-URL partner fan-out. |
| **C** | Data model: rows of “action instances” (type + config + event bindings) | First-class rows for email / in-app | Row1: webhook → events {…}; Row2: email → support@… on `document.failed`; Row3: in-app → Documate ops on `file.received`. |

**Important split:** “Multiple unrelated webhook URLs” (B) is **not** the same as “webhook + email + in-app for one event” (PE4). The latter is different **action types**, not multiple partner endpoints.

**Example — one file fails extract on two documents:**

1. Emit `document.failed` (doc A), `document.failed` (doc B). With PE3=B, **no** `file.completed` (not all docs Ready).
2. **Partner (PE7=A):** if `document.failed` checked → two HMAC POSTs to the single Business/Queue webhook URL.
3. **Documate email:** if support email is bound to `document.failed` → one or two support emails (product can batch later).
4. **In-app:** admin notification list shows the failures for ops.

**Recommendation (aligned with PE4=C):** Lock **product UX = A** for partner webhook (one endpoint + event checklist). Implement notifications as **separate action configs** (email recipients / in-app rules) — not as extra webhook URLs. Under the hood Phase 2 may still use an action-bindings table (C-shaped persistence) so webhook + email + in-app share one dispatch spine; that does **not** expose PE7=B to partners in v1.

---



### Platform shape


| ID       | Decision                    | Options (sketch)                                                                                                                                        |
| -------- | --------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **PE8**  | Emit spine                  | **A)** In-process domain dispatcher → Hangfire per action · **B)** Persist outbox then worker · **C)** Keep ad hoc schedule calls (not recommended)     |
| **PE9**  | Delivery / attempt metadata | **A)** Generalize to `OpsOutboundDelivery` (resource-typed) · **B)** Keep Document meta + add File meta · **C)** Log-only for non-document events in v1 |
| **PE10** | Sync extract (`api_sync`)   | **A)** Keep suppress all public actions · **B)** Allow opt-in `file.received` only · **C)** Full parity with async                                      |
| **PE11** | Who may configure           | **Partner/Business settings** (Business defaults + Queue inherit/override); Admin may observe later                                                     |


#### PE9 — explanation (choose A / B / C)

Today, webhook attempt state lives on the **Document** (`webhook_status`, attempt counts, last error, etc.). That fits `document.*` events only.

Once we emit `file.received` / `file.completed`, those deliveries are **File-scoped**. Email and in-app also need “did we send / fail / retry?” visibility for ops.

| Option | Meaning | Pros | Cons | Example |
| ------ | ------- | ---- | ---- | ------- |
| **A** | One generalized outbound-delivery table keyed by resource type + id + action + `event_id` | Single place for admin “delivery history”; works for Document, File, email, in-app | More schema/work in Phase 2 | Admin opens File F → sees `file.received` webhook attempt 2/5 failed; Document D → sees `document.failed` email sent |
| **B** | Keep Document webhook columns; add parallel File webhook columns | Smaller change; mirrors today’s Document model | Duplicated shape; email/in-app still orphaned; Batch later needs yet another place | File has `WebhookStatus`; Document keeps existing columns; support email only in logs |
| **C** | For non-document events in v1: log only (Serilog / `OpsWorkEvent`), no first-class delivery UI | Fastest ship for `file.received` | Ops cannot retry/inspect File deliveries in UI; weak for PE4=C email | Partner says “we never got file.received” — you grep logs |

**Guidance:** With PE4=C and file-level events, **A** is the durable fit. **B** is OK only if email/in-app delivery tracking is explicitly deferred. **C** is a short-term spike tradeoff, not a good end state for support email.

#### PE10 — explanation (choose A / B / C)

**Sync extract (`api_sync`)** = client waits on the HTTP request for final results. Plan 02 already **suppresses webhooks** on that path (C2): the response *is* the delivery; a webhook would duplicate and race the HTTP return.

| Option | Meaning | Example |
| ------ | ------- | ------- |
| **A** | Suppress **all** public actions on sync (webhook, email, in-app) | Client POSTs sync extract → gets JSON in response → no `document.ready` webhook, no support email, no in-app toast from this submission |
| **B** | Still no result webhooks, but allow opt-in `file.received` (intake ack) even on sync | Rare: partner wants “we accepted the bytes” while still waiting on the same HTTP call — usually redundant because HTTP 200 already means accepted |
| **C** | Full parity with async (all subscribed events fire) | Sync call returns Ready **and** POSTs `document.ready` to webhook → partner may double-process unless they dedupe |

**Guidance:** Prefer **A** unless a concrete sync partner needs an early ack distinct from the HTTP response. Async + email-intake remain the paths that need public events.

---



## 6. Recommended Direction (updated from locks)

**Locked / proposed direction:**

1. **Public events ≠ internal work events.** Small milestone catalog only.
2. **v1 catalog (PE1=A, PE2=B, PE3=B):**
   - `file.received` — after File+blob committed, before OCR  
   - `document.ready` / `document.failed` / `document.cancelled` — **no** legacy `document.terminal` (system not released)  
   - `file.completed` — **only when all documents are Ready** (not on PartialReady / mixed failure)
3. **Defer** generic `*.status_changed`, mid-pipeline classified/routed, and `intake.rejected` until asked.
4. **Actions (PE4=C):** Webhook + in-app notification + email in product scope (Phase 2 waves may still stage delivery).
5. **Subscriptions (PE5=B):** **Business defaults**, queues **inherit** (with override on Queue pages). **FE must update both Business and Queue settings** in Phase 2.
6. **Defaults (PE6=B — see note):** On **new Business** create, pre-check `file.received` + document terminals. New queues inherit those Business defaults.
7. **Partner webhook shape (PE7=A):** One webhook URL/secret + event checklist. Email/in-app are separate action settings (not N partner URLs). Persistence may be C-shaped internally.
8. **Spine (PE8=A):** In-process domain dispatcher → Hangfire per action execution.
9. **Idempotency:** Logical `event_id` per occurrence; delivery attempts are separate.
10. **Ordering promise:** `file.received` before document events for that file; `file.completed` only after all docs Ready (and thus after those ready events); no global cross-file order.
11. **Sync (PE10=A):** Suppress all public actions for `api_sync` (HTTP response is delivery).
12. **Delivery meta (PE9=A):** Generalized `OpsOutboundDelivery` (resource-typed) for Document/File/webhook/email/in-app attempts.
13. **Config (PE11):** Partner **Business** (+ Queue inherit/override) settings — not admin-only.
14. **Email intake:** Same public events as async API upload (source stamped on payload).
15. **Phase 2 must include FE** for Business event/action defaults and Queue inherit/override + notification settings surfaces — not API-only.

### Glossary — public vs internal events

**Public domain events** are the partner-facing (and admin-notify) contract: a small, versioned catalog of lifecycle milestones (`file.received`, `document.ready` / `failed` / `cancelled`, `file.completed`) that may trigger subscribed **actions** (webhook, email, in-app). **`OpsWorkEvent`** and metrics are **internal** only — fine-grained pipeline/audit rows (`status_changed`, `webhook_attempted`, …) used for timelines, debugging, and analytics; they are not webhook payload names and must not be exposed as the integration API. Pipeline stages always write internal work events as needed; they emit public events only at catalog milestones.

### Conceptual model

```text
Pipeline milestone
  → emit PublicEvent (name, resource, payload, event_id)
    → resolve ActionBindings (Business defaults → Queue override × event)
      → enqueue ActionExecution (webhook | email | in-app | …)
        → deliver + retry + audit
```

### Suggested v1 UI (partner) — both pages

**Business → Integrations / Events (defaults):**

```text
Webhook: URL / secret / enable
Events (defaults for new queues / inherit):
☑ File received
☑ Document ready
☑ Document failed
☑ Document cancelled
☐ File completed   (all docs Ready)

Email notify: … (recipients / which events)
In-app notify: … (which events)
```

**Queue → Integrations / Events (inherit + override):**

```text
○ Inherit Business defaults
○ Override — same checklist + webhook fields
```

---



## 7. Exploration Exit Criteria

Phase 1 is **complete** when:

1. PE1–PE11 decided (or explicitly deferred with owner).
2. Public vs internal event boundary written in one paragraph for glossary.
3. Compatibility approach for `document.terminal` locked — **N/A / drop name** (PE2=B, pre-release).
4. Developer approves starting **Phase 2 implementation plan** (architecture, schema, **API + FE** flows, waves) — **not** coding yet.

---



## Finalized Decisions


| ID | Locked |
| -- | ------ |
| **PE1** | **A** — Minimal: `file.received`, document terminals, `file.completed` |
| **PE2** | **B** — `document.ready` / `document.failed` / `document.cancelled` only (no `document.terminal`; pre-release) |
| **PE3** | **B** — `file.completed` only when **all** documents are Ready |
| **PE4** | **C** — Webhook + in-app notification + email |
| **PE5** | **B** — Business defaults inherited by queues (FE on **both** Business and Queue pages) |
| **PE6** | **B** — New Business defaults: `file.received` + document terminals; queues inherit |
| **PE7** | **A** (product) — One partner webhook endpoint + event checklist; email/in-app are separate action settings. Multi partner URLs deferred. Persistence may be action-bindings (C-shaped) internally |
| **PE8** | **A** — In-process dispatcher → Hangfire per action |
| **PE9** | **A** — Generalized `OpsOutboundDelivery` (resource-typed) |
| **PE10** | **A** — Suppress all public actions on `api_sync` |
| **PE11** | Partner **Business** settings (+ Queue inherit/override) |


---



## Pending Decisions

| ID | Need |
| -- | ---- |
| — | None for Phase 1. Phase 2 DRs live in [21-public-events-actions-implementation-plan.md](./21-public-events-actions-implementation-plan.md). |

---



## Assumptions

- Partners prefer **milestone** events over full CDC.  
- System is **pre-release** — breaking rename away from `document.terminal` is acceptable (PE2=B).  
- Business is the default subscription owner; Queue inherits (PE5=B).  
- Webhook HMAC + Hangfire retries remain the delivery quality bar for webhook actions.  
- Phase 2 covers **API + partner FE** (Business + Queue pages) for events/actions.  
- Sync extract clients rely on the HTTP response; no duplicate public actions (PE10=A).

---



## Risks

See §4. Highest for this lock set: PE3=B means mixed success/failure files never emit `file.completed` (partners must listen to document terminals); PE4=C scope (email + in-app) if not waved carefully; confusing public events with `OpsWorkEvent`.

---



## Readiness

**Phase 1 complete.** Downstream: Phase 2 implementation plan opened.  

---



## Changelog


| Date       | Note                                                                                                            |
| ---------- | --------------------------------------------------------------------------------------------------------------- |
| 2026-09-17 | Phase 1 draft opened from product discussion: multi-event webhooks + general public events / automated actions. |
| 2026-09-17 | Expanded §4 risks with examples; clarified spam control via milestone catalog + per-action opt-in (not every status). |
| 2026-09-17 | Locked PE1–PE5, PE8, PE11 from review; PE6 intent→B note; PE7 detail+examples+recommendation; PE9/PE10 expanded; Phase 2 must include FE (Business + Queue). |
| 2026-09-17 | Confirmed PE6=B, PE7=A; locked PE9=A, PE10=A; added glossary paragraph; Phase 1 ready for Phase 2 approve. |
| 2026-09-17 | Developer approved Phase 2 → [21-public-events-actions-implementation-plan.md](./21-public-events-actions-implementation-plan.md). |


