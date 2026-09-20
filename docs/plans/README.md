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
| [03-documate-v3-implementation-plan.md](./03-documate-v3-implementation-plan.md) | Implementation plan | Approved — A–I + **K1**; **E = P9/F6**; Document URL only after Document PDF |
| [03-documate-v3-dispatch-queue.md](./03-documate-v3-dispatch-queue.md) | Dispatch queue | ✅ Wave 4b complete (DQ-0705…0710 + DQ-0802); next active pointer: Band 17 DQ-1702, or Band 15 when unparked |
| [04-split-classify-strategy-exploration.md](./04-split-classify-strategy-exploration.md) | Exploration | ✅ Phase 1 complete; Wave 4b executed (DQ-0705…0710 + DQ-0802) |
| [04-split-classify-explained.md](./04-split-classify-explained.md) | Explainer | Plain-language + flowcharts for P9/F6 split/classify (models, failures) |
| [05-ocr-normalize-real-providers-exploration.md](./05-ocr-normalize-real-providers-exploration.md) | Exploration | ✅ Phase 1 complete |
| [05-ocr-llm-extract-implementation-plan.md](./05-ocr-llm-extract-implementation-plan.md) | Implementation plan | ✅ Phase 2 complete; Phase 3 DQ filed |
| [06-frontend-app-exploration.md](./06-frontend-app-exploration.md) | Exploration | ✅ Phase 1 complete (MVP locks 2026-08-30) |
| [07-customer-frontend-implementation-plan.md](./07-customer-frontend-implementation-plan.md) | Implementation plan | ✅ Phase 2 approved 2026-08-30 |
| [07-customer-frontend-dispatch-queue.md](./07-customer-frontend-dispatch-queue.md) | Dispatch queue | ✅ Band 16 complete (DQ-1601…1611) |
| [08-browser-extension-import-exploration.md](./08-browser-extension-import-exploration.md) | Exploration | 🔄 Phase 1 amended — Mode 1 (page fill) locked; Autopilot deferred; opens remain |
| [09-simplicity-documate-v3-client-upgrade-exploration.md](./09-simplicity-documate-v3-client-upgrade-exploration.md) | Exploration | ↪️ Canonical: `dev-infra-s4b/docs/plans/Ocr/DOCUMATE_V3_CLIENT_UPGRADE_EXPLORATION.md` — O2/O8 open |
| [10-documate-sdk-exploration.md](./10-documate-sdk-exploration.md) | Exploration | 🔄 Phase 1 draft — NuGet SDK (client + QMS + pull plugin bridge); D1–D8 open |
| [11-backoffice-frontend-exploration.md](./11-backoffice-frontend-exploration.md) | Exploration | ✅ Phase 1 complete (2026-09-14) — DR-BO1…BO7 locked |
| [16-backoffice-frontend-implementation-plan.md](./16-backoffice-frontend-implementation-plan.md) | Implementation plan | ✅ Phase 2 approved 2026-09-14 |
| [16-backoffice-frontend-dispatch-queue.md](./16-backoffice-frontend-dispatch-queue.md) | Dispatch queue | 🔄 Band 17 — DQ-1701 ✅; next ready = DQ-1702 (or 1704/1706) |
| [12-business-message-intake-routing-exploration.md](./12-business-message-intake-routing-exploration.md) | Exploration | 🔄 Phase 1 draft — BMIR (email first channel); F1–F6 locked; Q1–Q6 open |
| [13-business-scoped-data-access-exploration.md](./13-business-scoped-data-access-exploration.md) | Exploration | 🔄 Phase 1 draft — `IBusinessDb` Ops isolation façade; Q1–Q5 open |
| [14-email-intake-ses-exploration.md](./14-email-intake-ses-exploration.md) | Exploration | ✅ Phase 1 complete — E1–E12; follow-on **F1–F5 locked** (N=30d, body stamp, sender chain) |
| [14-email-intake-ses-implementation-plan.md](./14-email-intake-ses-implementation-plan.md) | Implementation plan | ✅ Phase 2 executed — DQ-1201…1203 complete |
| [14-email-intake-followon-implementation-plan.md](./14-email-intake-followon-implementation-plan.md) | Implementation plan | ✅ Phase 2 approved → DQ-1204…1206 |
| [15-system-settings-db-exploration.md](./15-system-settings-db-exploration.md) | Exploration | ✅ Phase 1 complete — S1–S6 + N=30 locked |
| [15-system-settings-db-implementation-plan.md](./15-system-settings-db-implementation-plan.md) | Implementation plan | ✅ Phase 2 approved → DQ-1410/1411 |
| [17-platform-secrets-store.md](./17-platform-secrets-store.md) | Adopted decision | ✅ AWS Secrets Manager primary; Infisical backup; API startup loader |
| [18-remaining-work-backlog.md](./18-remaining-work-backlog.md) | Delivery backlog | 🔄 Decisions 2026-09-16 — HAPA→Woodvale→MCM; local E2E; Statements #9 later; A3 open |
| [19-documate-marketing-website-business-plan.md](./19-documate-marketing-website-business-plan.md) | Business plan | 🔄 Draft — Next.js marketing site (VoomHub-inspired); W1–W6 open |
| [20-public-events-actions-exploration.md](./20-public-events-actions-exploration.md) | Exploration | ✅ Phase 1 complete — PE1–PE11 locked; Phase 2 approved |
| [21-public-events-actions-implementation-plan.md](./21-public-events-actions-implementation-plan.md) | Implementation plan | ✅ Phase 2 complete → Band 18 DQ |
| [21-public-events-actions-dispatch-queue.md](./21-public-events-actions-dispatch-queue.md) | Dispatch queue | ✅ Band 18 complete (DQ-1801…1808) |
| [22-document-agent-prompt-creation-exploration.md](./22-document-agent-prompt-creation-exploration.md) | Exploration | ✅ Phase 1 complete — PP1–PP7, P1–P5 locked (2026-09-20) |
| [22-document-agent-prompt-creation-implementation-plan.md](./22-document-agent-prompt-creation-implementation-plan.md) | Implementation plan | ✅ Phase 2 complete → Band 19 DQ |
| [22-document-agent-prompt-creation-dispatch-queue.md](./22-document-agent-prompt-creation-dispatch-queue.md) | Dispatch queue | 🔄 Band 19 — DQ-1901/1902/1903/1904/1907/1908/1909/1910 ✅; next = DQ-1905 |

## Plan-writing rules

Mandatory process: [00-governance/README.md](./00-governance/README.md)  
Start with: [00-governance/09-plan-sequence-and-step-gates.md](./00-governance/09-plan-sequence-and-step-gates.md)

## Architecture (code conventions)

Entry: [`docs/architecture/README.md`](../architecture/README.md)
