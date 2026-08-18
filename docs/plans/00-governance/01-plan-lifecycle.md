# Plan Lifecycle

Draft → Active → Decisions Reached → Promoted → Archived

## Three planning phases (separate deliverables)

1. **Exploration plan** — `02-exploration-plan-template.md`
2. **Implementation plan** — `03-implementation-plan-template.md`
3. **Dispatch queue** — `04-dispatch-queue-template.md`

Each phase requires **developer verification** before the next. See `09-plan-sequence-and-step-gates.md`.

## Status Meanings

| Status | Meaning |
|---|---|
| Draft | Being written; not yet actionable |
| Active | In use; decisions are open or being executed |
| Decisions Reached | All decisions confirmed by developer; ready to promote |
| Promoted | Stable decisions extracted to architecture docs |
| Archived | Moved to archive and removed from plans README |

## Promotion Rule

1. Extract stable universal decisions into `docs/architecture/`.
2. Update status header to Promoted.
3. Move plan file to `docs/plans/99-archive/` (create when needed).
4. Remove from `docs/plans/README.md`.
5. Archive associated contract docs when appropriate.

## What Must Not Be Promoted

- Implementation details
- Endpoint-by-endpoint specs
- Anything that evolves with code

Only promote universal structural decisions.

## What Stays In Plans Permanently

- `00-governance/*`
- Active plans
- Live dispatch queues
