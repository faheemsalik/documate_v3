# Backend API — Quick Reference

Full rules: `critical-rules-api.md` and pattern shards. Orientation only.

## Where things go

| Thing | Location |
|-------|----------|
| Entity | `apps/api/Domain/` |
| HTTP feature | `Modules/{Module}/Features/{Name}/` |
| DbContext / external clients | `Infrastructure/` |
| Tests | `tests/api/` |

## Module → route

| Module | Prefix |
|--------|--------|
| FrontendSupport | `/api/app` |
| External | `/api/v1` |
| Core | Internal (not default public HTTP) |

## Feature checklist

- [ ] Controller thin (MediatR only)
- [ ] Command or Query + Handler
- [ ] Request/response DTOs (no entities)
- [ ] Files under correct module `Features/` folder
- [ ] Persisted modes/statuses = `*EnumId` → CorEnum (no CLR enum columns)
- [ ] Domain compares CorEnum **Ids**, not `EnumKey`
- [ ] Operational rows scoped by `BusinessId` only (no `TenantId` everywhere)

## CorEnum

See `patterns/cor-enum.md`. Persist FKs; compare Ids; `EnumKey` for seed/DTO only. No `ParentId`.

## CQRS

MediatR. See `patterns/cqrs-feature-slice.md` and `patterns/mediatr-conventions.md`.
