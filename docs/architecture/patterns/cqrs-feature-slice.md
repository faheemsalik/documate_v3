# CQRS feature slice

Documate API uses **CQRS via MediatR** with **feature folders inside modules** (decision A2).

## Placement

```text
Modules/{Module}/Features/{Feature}/
  {Feature}Controller.cs
  Commands/
  Queries/
  Dtos/
```

One feature owns its HTTP surface and its write/read models for that surface.

## Responsibilities

| Layer | Does | Does not |
|-------|------|----------|
| Controller | Auth attributes, bind, `Send`, map HTTP status | Business rules, EF queries |
| Command/Query | Intent + validated input | Side effects (handler does that) |
| Handler | Load/save via Infrastructure, enforce rules, return DTO/result | HTTP concerns |
| Domain entity | Persistence model | API contract |
| DTO | API contract | EF mapping leaking out of feature |

## Core vs HTTP modules

- **FrontendSupport / External:** Controllers + Commands/Queries as usual.
- **Core:** Feature folders may expose handlers/services invoked by other modules’ handlers (pipeline). Prefer no public controllers unless a DQ requires ops endpoints.

## Example flow

1. `POST /api/app/queues` → `QueuesController`
2. `CreateQueueCommand` → `CreateQueueHandler`
3. Handler uses `AppDbContext` (or abstraction) from Infrastructure
4. Returns `QueueDto` — never the entity

See also: `mediatr-conventions.md`, `folder-structure.md`, `critical-rules-api.md`.
