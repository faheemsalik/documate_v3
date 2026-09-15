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
        PlatformAdmin/
          Features/{FeatureName}/     # APIs for apps/admin (/api/admin)
      Infrastructure/                 # DbContext, Iden client, storage, providers, email
      Program.cs
    web/                              # Angular customer app (port 4202)
      src/app/
        core/                         # auth, interceptors, singleton services
        shared/                       # reusable UI primitives
        features/                     # feature routes/components
    admin/                            # Angular platform admin app (port 4203)
      src/app/
        core/
        shared/
        features/
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
| PlatformAdmin | `apps/admin` | `/api/admin/...` (PlatformAdmin policy / AdminGate) |

## Rules

1. Domain entities live only under `Domain/`.
2. HTTP features live under `Modules/{Module}/Features/{Name}/` — never a flat root `Features/` tree.
3. DTOs are feature-local. Do not share DTOs across modules unless an explicit shared contract is introduced later.
4. Controllers do not reference `Infrastructure` types directly except via MediatR results/DTOs.
5. Angular customer UI lives under `apps/web/src/app/features/`; admin UI under `apps/admin/src/app/features/`. Do not put API code in Angular apps.
6. Prefer Plan 15 system-settings under `/api/admin` to use **PlatformAdmin** policy; do not expose admin writes on `/api/app`.
