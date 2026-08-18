# Postman smoke — Waves 0–3

Collection: [`Documate-v3-Smoke-Waves-0-3.postman_collection.json`](./Documate-v3-Smoke-Waves-0-3.postman_collection.json)

## Prerequisites

1. Start API: `dotnet run --project apps/api --launch-profile http`
2. Base URL default: `http://localhost:5172`
3. Auth: **DevBypass** (Development) — no token needed

## Import

Postman → Import → select the JSON file → Run folder **00** through **04** in order (Collection Runner works).

## Covers

| Area | Endpoints |
|------|-----------|
| Health | `/health`, `/api/app/health`, ping |
| Auth / tenancy | `/api/app/me` |
| Catalogs | document-types, providers, agent-templates |
| Agents | clone, list, get, update, create |
| Queues | multi-queue CRUD, routes, lock, webhook, email mint, allowlist |

## Stop point

**After Wave 3.** Do not proceed to Wave 4 (blob storage / File pipeline) until this smoke is green.
