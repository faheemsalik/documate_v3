# Plan sequence and step gates (mandatory)

Planning is **three separate phases**. They **cannot** be combined in one user command or one agent run unless the developer explicitly says to skip a phase.

| Phase | Deliverable | Governance template |
|-------|-------------|---------------------|
| **1 — Exploration** | Exploration plan | `02-exploration-plan-template.md` |
| **2 — Implementation plan** | Implementation plan | `03-implementation-plan-template.md` |
| **3 — Dispatch queue** | Numbered dispatch queue | `04-dispatch-queue-template.md` |

Execution of DQ items: `06-dispatch-queue-execution.md` (one item at a time unless user batches).

---

## MUST NOT

1. **One-shot planning** — do not produce exploration + implementation plan + dispatch queue in a single response or single “plan this feature” command.
2. **Skip Phase 1** when the work is greenfield, ambiguous, or the user asked to “explore”.
3. **Start Phase 2** until Phase 1 is **verified by the developer**.
4. **Start Phase 3** until Phase 2 is **verified by the developer**.
5. **Proceed past any “Decision Required”** subsection without presenting options and receiving developer choice.
6. **Start coding** from a plan until the relevant phase gate and pending decisions are closed (unless user explicitly says “implement step N” / “execute DQ-…” / “do all steps”).

---

## Phase 1 — Exploration only

**Do:** Problem framing, current state, risks, open questions, recommended direction.  
**Do not:** Implementation sections, dispatch items, or production code.

**End:** Ask developer to review, answer opens, then approve Phase 2.

---

## Phase 2 — Implementation plan only

**Do:** Architecture, validation, flows, instruction set, security constraints, waves — per template.  
At each **Decision Required**: options → stop → record choice.

**End:** Ask developer to approve Phase 3.

**Do not:** Create dispatch queue or write production code unless user requests an out-of-band spike.

---

## Phase 3 — Dispatch queue only

**Do:** Dispatch Index, waves, DQ entries.  
**End:** Ask which DQ to execute (unless user already batched execution).

---

## Agent output contract (every phase end)

From `07-agent-behavior-and-output-contract.md`:

- Finalized Decisions
- Pending Decisions
- Assumptions
- Risks
- Readiness: **Ready for next phase** | **Blocked**

---

## User command → phase mapping

| User says | Agent does |
|-----------|------------|
| “Explore …” | Phase 1 only → gate |
| “Write implementation plan …” | Phase 2 only if exploration approved |
| “Create dispatch queue …” | Phase 3 only if implementation plan approved |
| “Plan the whole feature end-to-end” | Phase 1 only — explain three phases |
| “Implement step N” / “Execute DQ-…” / “do all steps” | Execution per `06-dispatch-queue-execution.md` |

---

## Related

- Scope ambiguity: `docs/architecture/governance/prompt-clarification.md`
- Cursor: `.cursor/rules/06-plan-sequence-gates.mdc`
