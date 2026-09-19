# Documate — Remaining Work Backlog (Wrap-up)

> **Type:** Product / delivery backlog (index) — not a Phase 1–3 feature plan  
> **Status:** Active wrap-up (2026-09-16; decisions locked below)  
> **Purpose:** Single place for remaining finalize work, priority, and which plan owns each item  
> **Rule:** Do not invent product behavior here; link to exploration / implementation / DQ when work starts

---

## Locked decisions (2026-09-16)

| ID | Decision |
|----|----------|
| **A1** | **HAPA first** for live production (not Woodvale) |
| **A2** | HAPA pilot includes **all**: Inv, DN, CN, PO, multi-doc, email intake |
| **A3** | Multi-doc client unlock (**O4 / OCR-V3-0960**) — **open**; see § A3 details below |
| **A4** | Dual-path **not** verified in this workspace deploy yet — treat as **must verify before pilot** |
| **B5** | `unroutable_type` on exotic labels is **not** a blocker |
| **C6** | Marketing website / W1–W6 **after** existing clients shifted to v3 |
| **D7** | Client order: **HAPA → Woodvale → MCM** |
| **D8** | Customer FE redesign (#1) **waits** until after HAPA (and preferably client shifts) |
| **Local** | Local/dev testing of **everything**: email intake, Inv, DN, CN, (PO as needed) before live |
| **Later** | **Statements Reconciliation** added as later-phase product item (#9) |

---

## Priority summary

| # | Item | Priority | Separate plan? | Owner track |
|---|------|----------|----------------|-------------|
| 3 | Simplicity + External API finalize; **HAPA** pilot → live → Woodvale → MCM | **High** | O1/D12; [HAPA_PILOT_CHECKLIST.md](../../../../Simplicity/dev-infra-s4b/docs/plans/Ocr/HAPA_PILOT_CHECKLIST.md) | Simplicity OCR plans |
| 3a | Local/dev full E2E (email + Inv/DN/CN/PO + multi-doc packs) | **High** | Test under pilot checklists | Documate + Simplicity |
| 9 | Statements Reconciliation | **Later** | Yes — new product exploration when started | Documate product |
| 1 | Customer FE rebrand + UI/UX redesign | Medium (after clients) | Yes — exploration when unparked | Documate product |
| 2 | API reference + guide docs | Medium | Yes — docs exploration | Documate eng/docs |
| 8 | Marketing website (React/Next) | After client shifts | [19-…](./19-documate-marketing-website-business-plan.md) | Product / marketing |
| 4 | HAPA Cloud software — Documate APIs | Low | Host exploration | HAPA |
| 5 | ERP integration | Low | Per ERP host | ERP |
| 6 | More doc types / templates (Passport, ID Card, …) | Low | Product exploration | Documate product |
| 7 | ID card reading E2E | Low | Fold into #6 or separate | Documate product |

---

## A3 — Multi-doc on Simplicity (details)

**Two different layers:**

| Layer | Today | Meaning |
|-------|--------|---------|
| **Documate v3** | Can split one PDF into **N Documents**, extract each, return per-doc PDF | Already works for multi-doc packs |
| **Simplicity (O4-A)** | Round 1: if File has ≠ 1 Document → **fail / needs_attention** | Client does **not** yet import N children |

**OCR-V3-0960 (parked)** unlocks: N child rows per upload, refile each doc to typed folders, import each.

**Practical choice for HAPA:**

| Option | What you get | Risk |
|--------|----------------|------|
| **A3-Stay** — ship HAPA with O4-A | Single-doc Inv/DN/CN/PO + email of **one** doc per file work; multi-doc **packs** rejected/held on Simplicity | Safe; matches parked DQ |
| **A3-Unlock** — do OCR-V3-0960 before/with HAPA | True multi-doc packs end-to-end in Simplicity | More work; needed if HAPA uploads Inv+DN in one PDF |

Because **A2 says HAPA includes multi-doc**, recommend **A3-Unlock before HAPA live**, or explicitly defer multi-doc **packs** from HAPA until 0960 while still testing multi-doc on **Documate-only** locally.

**Pending:** Choose **A3-Stay** vs **A3-Unlock**.

---

## A4 — Dual-path status

DQ marks **OCR-V3-0130 Done**, but current `s4b-simplicitycloud-v9` checkout has **no** `DocumateV3*` sources found. Before HAPA:

1. Confirm v3 client code is on the branch you will deploy.  
2. Deploy + run `002` schema.  
3. Verify **legacy tenant** still works without v3 settings.  
4. Verify **dev tenant** with `UseDocumateV3=true`.

---

## 1 — Customer app FE redesign / rebrand (medium)

**Parked until:** After HAPA (D8); preferably after Woodvale/MCM shifts.

**Do next (when unparked):** Phase 1 exploration → Phase 2 → DQ. Marketing site (#8) stays separate.

---

## 2 — API reference + guide docs (medium)

Publish External API reference + guides. Can run during HAPA soak as capacity allows.

---

## 3 — External API + Simplicity → HAPA first → live (high)

```text
1. Local/dev E2E: email intake, Inv, DN, CN, PO, multi-doc packs
2. Verify dual-path deploy (A4)
3. Resolve A3 (Stay vs Unlock) if HAPA needs multi-doc packs in Simplicity
4. Enable HAPA only (D12) — others stay legacy
5. Soak → Woodvale → MCM
```

---

## 9 — Statements Reconciliation (later)

Reconcile bank/card statements against extracted documents / ERP postings (Phase 1 out-of-scope). New product exploration when started — **not** part of HAPA rollout.

---

## Suggested execution order

1. Local full test matrix (Documate)  
2. A4 verify Simplicity dual-path branch/deploy  
3. Decide A3 → Unlock if HAPA needs packs in Simplicity  
4. HAPA pilot  
5. Woodvale → MCM  
6. Docs (#2) during soak  
7. Website (#8), FE (#1), Statements (#9), extra types (#6–7)

---

## Output contract

| | |
|--|--|
| **Finalized** | A1 HAPA-first; A2 all types; B5; C6; D7; D8; local full test; Statements #9 later |
| **Pending** | **A3-Stay vs A3-Unlock**; confirm Simplicity v3 code branch for A4 |
| **Readiness** | Ready for local test matrix + A4 branch hunt; HAPA live blocked on A4 (+ A3 if packs required in Simplicity) |
