# OpenAPI and clients (light)

## Source of truth

- **HTTP contracts** are defined by `apps/api` (External and FrontendSupport controllers + DTOs).
- Prefer enabling OpenAPI (Swagger/Scalar) on the API host at scaffold time.

## Angular consumption

1. Prefer **generated TypeScript clients** from the OpenAPI document.
2. Until generation exists, hand-written feature `*.api.ts` services are allowed — keep shapes aligned with API DTOs.
3. Do not duplicate Domain entities into the web app.

## Versioning

- External APIs under `/api/v1` — breaking changes require a versioning decision (future DQ).
- FrontendSupport `/api/app` may evolve with the UI more freely but still avoid silent breaking changes without coordinating web updates.

## Out of scope here

Full CI generation pipeline — add in an infrastructure/scaffold DQ.
