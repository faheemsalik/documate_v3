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
| [00-product-glossary.md](./00-product-glossary.md) | Product glossary | Canonical Batch / File / Document; Queue = channel + K1 default |
| [01-project-exploration-mental-design.md](./01-project-exploration-mental-design.md) | Product exploration | Frozen (product track) |
| [02-document-queue-design.md](./02-document-queue-design.md) | Product design | Aligned + **K1 default channel** (2026-08-28) |
| [03-documate-v3-implementation-plan.md](./03-documate-v3-implementation-plan.md) | Implementation plan | Approved — A–I + **K1**; entity catalog approved |
| [03-documate-v3-dispatch-queue.md](./03-documate-v3-dispatch-queue.md) | Dispatch queue | ✅ DQ-1101 complete; next ready = DQ-1201; **last Phase 1 = DQ-1402 upload perf** |
| [04-split-classify-strategy-exploration.md](./04-split-classify-strategy-exploration.md) | Exploration | 🔄 Phase 1 P0/C0 locked; P8/F5 semantic split imported, not locked |
| [05-ocr-normalize-real-providers-exploration.md](./05-ocr-normalize-real-providers-exploration.md) | Exploration | ✅ Phase 1 complete |
| [05-ocr-llm-extract-implementation-plan.md](./05-ocr-llm-extract-implementation-plan.md) | Implementation plan | ✅ Phase 2 complete; Phase 3 DQ filed |
| [06-frontend-app-exploration.md](./06-frontend-app-exploration.md) | Exploration | ✅ Phase 1 complete (MVP locks 2026-08-30) |
| [07-customer-frontend-implementation-plan.md](./07-customer-frontend-implementation-plan.md) | Implementation plan | ✅ Phase 2 approved 2026-08-30 |
| [07-customer-frontend-dispatch-queue.md](./07-customer-frontend-dispatch-queue.md) | Dispatch queue | ✅ Band 16 complete (DQ-1601…1611) |
| [08-browser-extension-import-exploration.md](./08-browser-extension-import-exploration.md) | Exploration | 🔄 Phase 1 amended — Mode 1 (page fill) locked; Autopilot deferred; opens remain |

## Plan-writing rules

Mandatory process: [00-governance/README.md](./00-governance/README.md)  
Start with: [00-governance/09-plan-sequence-and-step-gates.md](./00-governance/09-plan-sequence-and-step-gates.md)

## Architecture (code conventions)

Entry: [`docs/architecture/README.md`](../architecture/README.md)
