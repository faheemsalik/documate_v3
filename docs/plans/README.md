# Documate v3 — Plans Index

Active plans only. Promoted/archived plans move out of this index.

## Tracks (do not mix)

| Track | Purpose | Examples |
|-------|---------|----------|
| **Engineering / governance** | Folder structure, CQRS, coding conventions, plan-writing rules, Cursor rules | `00-governance-*`, `docs/plans/00-governance/` |
| **Product / mental design** | Product intent, agents, queues, schemas, delivery model | `01-project-exploration-mental-design.md` and later product design plans |

Agents executing **engineering** DQs must not invent product behavior. Agents on **product** plans must not redefine folder/CQRS conventions — those live under `docs/architecture/`.

## Active plans

| Plan | Type | Status |
|------|------|--------|
| [00-governance-engineering-conventions-implementation-plan.md](./00-governance-engineering-conventions-implementation-plan.md) | Implementation plan | Complete — see dispatch queue |
| [00-governance-engineering-conventions-dispatch-queue.md](./00-governance-engineering-conventions-dispatch-queue.md) | Dispatch queue | ✅ Complete |
| [00-product-glossary.md](./00-product-glossary.md) | Product glossary | Canonical Batch / File / Document (no Job) |
| [01-project-exploration-mental-design.md](./01-project-exploration-mental-design.md) | Product exploration | Frozen (product track) |
| [02-document-queue-design.md](./02-document-queue-design.md) | Product design | Aligned to exploration |
| [03-documate-v3-implementation-plan.md](./03-documate-v3-implementation-plan.md) | Implementation plan | Approved — A–I closed; entity catalog approved |
| [03-documate-v3-dispatch-queue.md](./03-documate-v3-dispatch-queue.md) | Dispatch queue | 🔄 Waves 0–6 ✅; DQ-0701–0703 ✅; DQ-0801–0901 ✅; next DQ-1001 |
| [04-split-classify-strategy-exploration.md](./04-split-classify-strategy-exploration.md) | Exploration | 🔄 Phase 1 P0/C0 locked; P8/F5 semantic split imported, not locked |

## Plan-writing rules

Mandatory process: [00-governance/README.md](./00-governance/README.md)  
Start with: [00-governance/09-plan-sequence-and-step-gates.md](./00-governance/09-plan-sequence-and-step-gates.md)

## Architecture (code conventions)

Entry: [`docs/architecture/README.md`](../architecture/README.md)
