# Folder structure (authoritative)

Single monorepo. **No shared `packages/`** unless a future DQ adds one.

```text
documate_v3/
  apps/
    api/                              # one .NET 10 Web API project
      Domain/                         # entities + base types only
      Modules/
        Core/
          Features/{FeatureName}/     # pipeline / extraction orchestration
        FrontendSupport/
          Features/{FeatureName}/     # APIs for apps/web
        External/
          Features/{FeatureName}/     # partner / integration APIs
      Infrastructure/                 # DbContext, Iden client, storage, providers, email
      Program.cs
    web/                              # Angular app
      src/app/
        core/                         # auth, interceptors, singleton services
        shared/                       # reusable UI primitives
        features/                     # feature routes/components
  tests/
    api/                              # .NET test project(s)
  docs/
    plans/
    architecture/
  .cursor/rules/
  old_code/                           # reference only
```

## Feature folder (API) — inside a module

```text
Modules/FrontendSupport/Features/Queues/
  QueuesController.cs
  Commands/
    CreateQueueCommand.cs
    CreateQueueHandler.cs
  Queries/
    GetQueueByIdQuery.cs
    GetQueueByIdHandler.cs
  Dtos/
    QueueDto.cs
    CreateQueueRequest.cs
```

Validators and mapping helpers stay in the same feature folder when needed.

## Module boundaries

| Module | Consumer | Typical route prefix |
|--------|----------|----------------------|
| Core | Internal pipeline / workers invoked from handlers | Not a public HTTP surface by default |
| FrontendSupport | `apps/web` | `/api/app/...` |
| External | Customer systems | `/api/v1/...` |

## Rules

1. Domain entities live only under `Domain/`.
2. HTTP features live under `Modules/{Module}/Features/{Name}/` — never a flat root `Features/` tree.
3. DTOs are feature-local. Do not share DTOs across modules unless an explicit shared contract is introduced later.
4. Controllers do not reference `Infrastructure` types directly except via MediatR results/DTOs.
5. Angular feature UI lives under `apps/web/src/app/features/`; do not put API code in the web app.
