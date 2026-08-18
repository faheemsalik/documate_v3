# Naming conventions

## Domain entities

| Kind | Prefix | Examples |
|------|--------|----------|
| Infrastructure / catalogs | `Cor` | `CorTenant`, `CorProvider`, `CorDocumentType` |
| Operational work | `Ops` | `OpsAgent`, `OpsQueue`, `OpsFile`, `OpsDocument` |

FK **columns** drop the prefix: `CorTenant` → `TenantId`, `OpsFile` → `FileId`.

## .NET namespaces

Pattern: `Documate.Api.Modules.{Module}.Features.{Feature}.{Area}`

Examples:

- `Documate.Api.Modules.FrontendSupport.Features.Queues`
- `Documate.Api.Modules.FrontendSupport.Features.Queues.Commands`
- `Documate.Api.Modules.External.Features.Uploads.Dtos`
- `Documate.Api.Domain`
- `Documate.Api.Infrastructure`

(Project assembly name may be finalized at scaffold time; keep this namespace shape.)

## Files

| Kind | Name |
|------|------|
| Command | `{Verb}{Noun}Command.cs` — e.g. `CreateQueueCommand.cs` |
| Handler | `{Verb}{Noun}Handler.cs` or nest handler with command — pick one style per feature and stay consistent |
| Query | `{Get\|List}{Noun}Query.cs` |
| DTO / request | `{Noun}Dto.cs`, `{Verb}{Noun}Request.cs` |
| Controller | `{Feature}Controller.cs` |

## HTTP routes

| Module | Prefix | Example |
|--------|--------|---------|
| FrontendSupport | `/api/app` | `GET /api/app/queues/{id}` |
| External | `/api/v1` | `POST /api/v1/queues/{queueId}/documents` |

Use kebab-case or plural resource segments consistently within a module; prefer plural nouns.

## Angular

| Kind | Convention |
|------|------------|
| Feature folder | `apps/web/src/app/features/{feature-name}/` (kebab-case) |
| Component file | `{name}.component.ts` |
| Service | `{name}.service.ts` |
| Routes | `{feature}.routes.ts` |

## Catalog / code columns (avoid reserved `Key`)

Do **not** name a column or property bare `Key` (SQL/JSON/framework reserved-word friction).

Use entity-prefixed names, e.g.:

| Entity | Column |
|--------|--------|
| `CorEnumType` | `EnumTypeKey` |
| `CorEnum` | `EnumKey` |
| `Provider` | `ProviderKey` |
| `DocumentType` | `DocumentTypeKey` |
| `AgentTemplate` | `AgentTemplateKey` |
| `WorkflowDefinition` | `WorkflowKey` |

## Tests

- .NET: `tests/api/` mirroring feature/module under test.
- Angular: colocated `*.spec.ts` next to the unit under test.
