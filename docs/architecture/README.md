# Documate v3 — Architecture

**Load when:** Implementing or reviewing code structure, CQRS, conventions.  
**Do not load for:** Product behavior (agents, queues, schemas) — see product plans under `docs/plans/`.

## Conflict order

1. `governance/critical-rules-*.md`
2. Pattern shards under `patterns/`
3. Module/feature local README (when present)

## Router

| Need | Document |
|------|----------|
| Folder tree (A2) | [patterns/folder-structure.md](./patterns/folder-structure.md) |
| Naming / routes | [patterns/naming-conventions.md](./patterns/naming-conventions.md) |
| CQRS feature slice | [patterns/cqrs-feature-slice.md](./patterns/cqrs-feature-slice.md) |
| MediatR | [patterns/mediatr-conventions.md](./patterns/mediatr-conventions.md) |
| API never-dos | [governance/critical-rules-api.md](./governance/critical-rules-api.md) |
| API quick ref | [governance/quick-reference-api.md](./governance/quick-reference-api.md) |
| Web never-dos | [governance/critical-rules-web.md](./governance/critical-rules-web.md) |
| Web quick ref | [governance/quick-reference-web.md](./governance/quick-reference-web.md) |
| Angular conventions | [patterns/angular-conventions.md](./patterns/angular-conventions.md) |
| Ambiguous scope | [governance/prompt-clarification.md](./governance/prompt-clarification.md) |
| `old_code` | [governance/preservation-rules.md](./governance/preservation-rules.md) |
| Shared packages | [patterns/shared-packages-policy.md](./patterns/shared-packages-policy.md) |
| OpenAPI / clients | [patterns/openapi-and-clients.md](./patterns/openapi-and-clients.md) |
| Infrastructure | [patterns/infrastructure-conventions.md](./patterns/infrastructure-conventions.md) |
| Iden constraints | [governance/iden-constraints.md](./governance/iden-constraints.md) |
| CorEnum (no static enum columns; compare Ids) | [patterns/cor-enum.md](./patterns/cor-enum.md) |
| Auth wiring TBD | [governance/auth-wiring-placeholder.md](./governance/auth-wiring-placeholder.md) |

## Apps

| Path | Stack |
|------|--------|
| `apps/api` | .NET 10, MediatR CQRS, single project |
| `apps/web` | Angular (standalone) |
| `tests/api` | .NET tests |
| `old_code/` | Reference only until cutover |

Planning process: `docs/plans/00-governance/`.
