# FrontendSupport API clients (Band 16)

Thin typed wrappers for `/api/app` used by the customer Angular app.

| Service | Path |
|---------|------|
| `MeApiService` | `/api/app/me` |
| `AgentsApiService` | `/api/app/agents` |
| `CatalogsApiService` | `/api/app/catalogs` |
| `QueuesApiService` | `/api/app/queues` |
| `ApiKeysApiService` | `/api/app/api-keys` |
| `BusinessApiService` | `/api/app/business/profile` |
| `FilesApiService` | `features/files/data` (queue-scoped files) |

Base URL: `core/api-base.ts` — empty on `localhost` so `ng serve` uses `proxy.conf.json` → `http://localhost:5172` (avoids CORS).

**OpenAPI regen (DQ-1611):** When CI publishes `/openapi/v1.json`, prefer generated client + thin wrappers per `docs/architecture/patterns/angular-feature-structure.md`.
