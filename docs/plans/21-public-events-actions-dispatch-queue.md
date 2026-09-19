# Documate v3 — Public domain events + automated actions — Dispatch Queue

> **Document type:** Dispatch queue (Phase 3)  
> **Status:** ✅ **Band 18 complete** (executed 2026-09-19)  
> **Source plan:** [21-public-events-actions-implementation-plan.md](./21-public-events-actions-implementation-plan.md)  
> **Upstream:** [20-public-events-actions-exploration.md](./20-public-events-actions-exploration.md)  
> **Evidence:** [21-band18-evidence.md](./21-band18-evidence.md)

**Status legend:** ✅ Complete · 🔄 In Progress · ⬜ Ready · ⏸ Parked · ❌ Cancelled  

**Band 18** = Public events & automated actions (plans 20 + 21).

---

## Completion Summary

| Metric | Value |
|--------|--------|
| Total DQ items | 8 |
| ✅ Complete | 8 |
| 🔄 In Progress | 0 |
| ⬜ Ready | 0 |
| ⏸ Parked | 0 |
| ❌ Cancelled | 0 |

### Finalized decisions

All PE\* + DR-EA\* as locked in Plan 21.

### Pending decisions

None.

### Assumptions

- Apply `Band18PublicEventsActions` migration on each environment before relying on emit.
- SMTP must be enabled for live partner/platform email actions.

### Risks

| Risk | Mitigation |
|------|------------|
| Migration not applied | Hosted migrate for bindings no-ops until tables exist; apply EF migration first |
| Inherit + empty Business URL | Legacy queue webhook fallback in resolver |

---

## Dispatch Index

| DQ | Band | Title | Status | Depends on |
|----|------|--------|--------|------------|
| **DQ-1801** | 18 | Schema + migration + Business defaults | ✅ | — |
| **DQ-1802** | 18 | Spine: emitter, resolver, webhook executor | ✅ | DQ-1801 |
| **DQ-1803** | 18 | Document terminal emits | ✅ | DQ-1802 |
| **DQ-1804** | 18 | File received + completed emits | ✅ | DQ-1802 |
| **DQ-1805** | 18 | Partner API Business + Queue | ✅ | DQ-1801 |
| **DQ-1806** | 18 | Partner FE Integrations + Queue Intake | ✅ | DQ-1805 |
| **DQ-1807** | 18 | Email + in-app actions | ✅ | DQ-1802, DQ-1805 |
| **DQ-1808** | 18 | Docs + evidence | ✅ | DQ-1803…1807 |

---

## Wave / DQ Entries

### DQ-1801 — Schema + migration + Business defaults

- **Status:** ✅ Complete  
- **Evidence:** Migration `Band18PublicEventsActions`; entities `OpsActionBinding`, `OpsOutboundDelivery`, `OpsInAppNotification`; `PublicActionsInherit`; `ActionBindingBootstrap` + migrate hosted service; CreateBusiness seeds defaults.

### DQ-1802 — Emit spine + webhook executor

- **Status:** ✅ Complete  
- **Evidence:** `Infrastructure/PublicEvents/*`; `PublicActionJobs` on `webhooks`; typed event headers; sync suppress.

### DQ-1803 — Document terminal emits

- **Status:** ✅ Complete  
- **Evidence:** `DocumentWebhookScheduler` emits typed document events; rejected→failed; projects Document webhook meta.

### DQ-1804 — File emits

- **Status:** ✅ Complete  
- **Evidence:** `WorkRecordService` emits `file.received`; scheduler emits `file.completed` when all Ready.

### DQ-1805 — Partner API

- **Status:** ✅ Complete  
- **Evidence:** `PublicActionsController` Business/Queue GET/PUT; queue webhook PUT façade.

### DQ-1806 — Partner FE

- **Status:** ✅ Complete  
- **Evidence:** `/business/integrations` page + nav; Queue Intake inherit/override UI.

### DQ-1807 — Email + in-app

- **Status:** ✅ Complete  
- **Evidence:** `PublicActionExecutor` email/in_app; in-app list/mark-read API; Business FE controls.

### DQ-1808 — Docs + evidence

- **Status:** ✅ Complete  
- **Evidence:** Docs page public-events card; [21-band18-evidence.md](./21-band18-evidence.md).

---

## Changelog

| Date | Note |
|------|------|
| 2026-09-18 | Band 18 filed; DQ-1801…1808. |
| 2026-09-19 | Batch execution complete — all DQ-1801…1808 ✅. |
