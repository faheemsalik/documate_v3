# Documate v3 — Product glossary (canonical terms)

> **Source of truth for product language** in plans `01`, `02`, `03` and later DQs.  
> Domain **product terms** stay `Batch` / `File` / `Document` / `Agent` / `Queue`.  
> CLR entity names use prefixes: infrastructure/catalogs **`Cor*`**, operational work **`Ops*`** (e.g. `OpsFile`, `CorDocumentType`). FK **columns** drop the prefix (`TenantId`, `FileId`, `AgentId`).

---

## Hierarchy (remember this)

```text
Iden Tenant
 └── Iden Business          ← Documate isolation unit
      └── Queue
           └── Batch?       ← only when ≥2 Files arrive together (log / correlation)
                └── File[]  ← one stored upload or email target
                     └── Document[]   ← one logical business document (result + webhook)
```

Single-file intake: **no Batch** — just `File` → `Document`(s).

**Iden:** Tenant → Businesses (two levels). Documate operational data hangs off **Business**. A **Queue** is an ops **intake channel** *inside* a Business, not a substitute for Business (and not an org “department” / “biz unit” — those words collide with Iden Business).

**Phase 1 UX:** Each Business gets one **default Queue** at Business create (system channel). Multi-queue remains in the model for later; UI may hide Queues and show **Default channel (Queue ID: …)** on the Business page for External API targeting.

---

## Canonical terms

| Term | Short | Means | Does **not** mean |
|------|-------|--------|-------------------|
| **Iden Tenant** | Tenant | Top org in Iden | A Documate Queue |
| **Iden Business** | Business | Child org under a Tenant; Documate data isolation boundary | A Queue or a Document |
| **Batch** | — | Optional **intake correlation** record when **two or more Files** are received in one API upload or one email. For logs, support, and `batch_id` filters only. | A delivery contract, a status state machine, or something that locks processing |
| **File** | — | One **stored** binary or artifact in a Queue (PDF, image, DOCX, email body artifact, etc.). Owns split/classify/route and **rollup status** for UI/ops. | A logical invoice/DN inside a PDF (that is a **Document**) |
| **Document** | **Doc** | One **logical business document** produced after split + classify on a File (e.g. one invoice). Runs one Agent → one schema. Holds **ResultJson** when Ready. **Webhook unit** (async). | The whole uploaded PDF when that PDF contains multiple logical docs |
| **Result** | — | Schema-shaped JSON on a **Document** when status is Ready | Webhook delivery success |
| **Queue** | Channel | Untyped **intake channel** within a Business: webhook, email, allowlist, workflow attach + **QueueRoute** map (`DocumentType` → `Agent`). Phase 1: one **default** Queue auto-created per Business. | An Agent, a File, an Iden Business, or a typed “one doc-type only” bucket |
| **QueueRoute** | Route | On a Queue: this **DocumentType** → this **Agent**. Unique `(QueueId, DocumentTypeId)`. | Multiple Agents for the same type on one Queue |
| **Agent** | — | Customer **AI Agent**: instructions + **output schema** + document-type intent + **post-processing** | A Queue or a System AI step |
| **System AI Agent** | — | Platform AI step (classify, intake decision, …); not user-cloned | Customer Agent |

---

## What is a “Job”?

**Job is not a first-class product term in Documate v3.**

Earlier drafts said “Document job” / “work item.” That mixed “work” language with the business object.

| Use | Instead |
|-----|---------|
| Document job / Doc job | **Document** (or **Doc**) |
| File job / upload job (as entity) | **File** |
| “Jobs list” in UI | **Documents** (and/or **Files**) monitor |
| In-flight retry | Still the same **Document** (`Processing` / internal retry) — not a new Job entity |
| Reprocess | New **File** (and new **Documents**), linked via `ReprocessOfFileId` |

Plain English in narrative is fine (“the upload used to block other work”) — do not invent a `Job` entity.

---

## IDs (API / events)

| Wire field | DB | Notes |
|------------|-----|--------|
| `business_id` | Iden Business id | **Isolation scope** for Documate work (ops rows) |
| `tenant_id` | Iden Tenant id | Auth/UI context; **not** repeated on Queue/File/Document — use `CorTenantBusiness` |
| `queue_id` | Queue.Id (UUID PK) | External upload target; Phase 1 often the Business **default channel** id shown on the Business page |
| `agent_id` | Agent.Id (UUID PK) | |
| `batch_id` | Batch.Id (UUID PK) | |
| `file_id` | File.Id (UUID PK) | |
| `document_id` | Document.Id (UUID PK) | |
| — | `SequenceId` (bigint) | On all wire-facing entities; **not** the API resource id |
| catalogs | Provider/DocumentType/AgentTemplate.Id (bigint) + prefixed key (`ProviderKey`, `DocumentTypeKey`, …) | Not end-user resource ids; avoid bare column name `Key` |

---

## Status owners

| Object | Public statuses (Phase 1) |
|--------|---------------------------|
| **Document** | Received, Processing, Ready, Failed, Rejected, Cancelled |
| **File** | Received, Processing, Ready, PartialReady, Failed, Rejected, Cancelled |
| **Batch** | No delivery status machine — log record only |

Webhook metadata lives on **Document**, not on File or Batch.

---

## Related plans

- Mental design: `01-project-exploration-mental-design.md`
- Queue design: `02-document-queue-design.md`
- Implementation plan (entities): `03-documate-v3-implementation-plan.md`
