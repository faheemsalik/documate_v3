# Infrastructure conventions (light)

## What belongs in `apps/api/Infrastructure/`

- EF Core `DbContext` and configurations
- Iden token validation / HTTP clients (when wired)
- Blob/S3, email intake adapters, OCR/LLM provider adapters
- Cross-cutting technical services (clock, id generators)

## What does not

- HTTP controllers (Features)
- Product workflow definitions as UI concepts (product plans)
- Feature-specific DTOs (stay in Features)

## Rules

1. Features **depend inward** on abstractions when useful; Infrastructure implements them.
2. Handlers may use `DbContext` directly in early phases; introduce repositories only if a DQ requires it — **never** inject them into controllers.
3. Provider SDKs (AWS, Google, OpenAI, …) stay behind Infrastructure adapters — Core/Feature handlers call adapters, not raw SDKs, once adapters exist.
4. No cloud account/topology blueprint in this document — hosting is a separate plan.
