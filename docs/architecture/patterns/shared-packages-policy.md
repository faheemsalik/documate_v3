# Shared packages policy

**Decision (Plan 00):** No `packages/` folder and no shared NuGet/TS libraries **for now**.

## Why

- One .NET API project (`apps/api`) owns Domain and features.
- Angular (`apps/web`) consumes HTTP contracts via OpenAPI-generated or hand-written clients.
- A shared package is only justified when a **second .NET host** (worker, second API) must reuse code.

## Rules

1. Do not create `packages/` unless a DQ explicitly adds it.
2. Do not share C# projects between imaginary future apps “just in case.”
3. Do not create a monorepo TS package for API DTO types — generate from OpenAPI instead.
4. When a second .NET consumer appears, open a band-11 DQ to introduce a shared project and migrate deliberately.
