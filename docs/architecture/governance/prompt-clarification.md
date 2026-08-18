# Prompt clarification (authoritative)

When the request is **ambiguous or scope is unclear**, do **not** assume a large blast radius. **Ask the user** before implementing.

## MUST

1. **Prefer one short clarifying question** over guessing module, feature, or API vs Web.
2. **Stop and ask** when the change target could be:
   - **Shared** (`Infrastructure/`, `Domain/` base types, `apps/web` core/shared) vs **one feature**
   - **One module** (Core / FrontendSupport / External) vs **cross-module**
   - **API only**, **Web only**, or **both**
   - **Engineering conventions** vs **product behavior**
3. **Offer 2–3 concrete options** when helpful.
4. After the user answers, proceed with the **minimum** scope they chose.

## Planning work

Three phases (exploration → implementation plan → dispatch queue) are **separate** — never in one command unless the developer explicitly skips a phase. See `docs/plans/00-governance/09-plan-sequence-and-step-gates.md`.

## MUST NOT

1. **Silently change** shared layout, global styles, base classes, or Infrastructure because the user named one screen or endpoint.
2. **Expand scope** (refactor neighbors, “fix while here”, apply pattern repo-wide) without explicit approval.
3. **Treat a vague noun** (“queue”, “document”, “login”) as a unique file path when multiple matches exist.
4. **Mix product policy into engineering DQs** (or the reverse).

## Examples

| User says | Risky assumption | Ask instead |
|-----------|------------------|-------------|
| “Add a queues endpoint” | Wrong module (app vs external) | “FrontendSupport (`/api/app`) or External (`/api/v1`)?” |
| “Fix the queue form” | API + Web + all queue features | “Angular form only, API only, or both?” |
| “Update Domain Document” | Broad entity refactor | “Which fields / which feature needs this?” |
| “Follow the standard pattern” | Invent new folder layout | Confirm module + existing feature to copy |

**Cursor:** `.cursor/rules/05-clarify-before-assuming.mdc` points here.
