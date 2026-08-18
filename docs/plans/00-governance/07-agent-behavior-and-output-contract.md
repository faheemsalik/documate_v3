# Agent Output Contract

Provide at **each phase end** (exploration, implementation plan, dispatch queue):

- Finalized Decisions
- Pending Decisions
- Assumptions
- Risks
- Readiness Statement

Readiness:

- **Ready for next phase** (exploration → impl plan → dispatch queue → execution)
- **Blocked** (with reason)

# Mandatory Behavior Rules

- **Three phases are separate** — see `09-plan-sequence-and-step-gates.md`. Never exploration + implementation plan + dispatch queue in one command (unless developer explicitly skips a phase).
- **Phase gates:** After Phase 1, ask developer to verify and approve Phase 2 — even if open questions are empty. Same after Phase 2 → Phase 3.
- Do not proceed past **Decision Required** without developer confirmation.
- Keep options concrete.
- Surface uncertainty explicitly in open questions.
- Populate Dispatch Index before wave sections (Phase 3 only).
- Do not invent DQ items beyond the approved implementation plan.
- Do not start implementation code until dispatch queue is approved and user selects a DQ item (unless explicitly asked to execute / batch).
- Promote plans when decisions are reached.
- Archive promoted plans.
- Plans README contains active plans only.
- Do not mix **product** decisions into **engineering** DQs (and vice versa). See `docs/plans/README.md`.
