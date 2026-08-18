# Backend API — Critical Rules (authoritative)

Apply before writing any `apps/api` code. Detail: `docs/architecture/patterns/`.

1. **Never expose domain entities as DTOs** — use request/response model classes in the feature `Dtos/` folder.
2. **Never inject DbContext into controllers** — MediatR handlers (or infrastructure services used by handlers) only.
3. **Never inject repositories into controllers** — handlers only.
4. **Controllers are thin** — bind → `IMediator.Send` → return DTO/result. No business logic in controllers.
5. **New endpoints live in** `Modules/{Core|FrontendSupport|External}/Features/{Name}/` with Commands/Queries/Dtos colocated.
6. **Do not create shared packages** unless a DQ explicitly adds them (`patterns/shared-packages-policy.md`).
7. **Do not modify `old_code`** unless the DQ says so (`governance/preservation-rules.md`).
8. **Do not invent product behavior** in engineering work — point to product plans under `docs/plans/`.
9. **Do not bypass Iden** for authentication/authorization once integration exists (`governance/iden-constraints.md`).
10. **Do not touch unrelated features/modules** unless explicitly requested (`governance/prompt-clarification.md`).
11. **Prefer matching an existing feature slice** over inventing a new folder style.
12. **Cross-module DTOs are forbidden by default** — keep DTOs feature-local.
13. **CorEnum for persisted modes/statuses** — no static CLR enum columns on domain tables; use `XxxEnumId` FK → `CorEnum` (`patterns/cor-enum.md`).
14. **Compare CorEnum Ids, not keys** — business/handler logic branches on `*EnumId` (resolved Ids). Do not compare `EnumKey`/`Name` in domain logic; `EnumKey` is for seed, admin, and DTO display only (`patterns/cor-enum.md`).

Enforcement in Plan 00: documentation + Cursor rules (no custom analyzers yet).
