# Documate v3 — Postman

Canonical collection: [`Documate-v3-API.postman_collection.json`](./Documate-v3-API.postman_collection.json)

Older wave smoke file (kept for history): [`Documate-v3-Smoke-Waves-0-3.postman_collection.json`](./Documate-v3-Smoke-Waves-0-3.postman_collection.json)

## Prerequisites

1. Start the API: `dotnet run --project apps/api --launch-profile http`
2. Base URL: `http://localhost:5172` (collection variable `baseUrl`)
3. **App** (`/api/app/...`): Development **DevBypass** — no token
4. **External** (`/api/v1/...`): header `X-Api-Key` from `POST /api/app/api-keys` (saved as `apiKey`)

## Import

Postman → Import → select `Documate-v3-API.postman_collection.json`.

For uploads, attach [`samples/invoice-sample.txt`](./samples/invoice-sample.txt) on every request whose body has a `file` / `files` field.

## Recommended Collection Runner order

Run folders **00 → 08** in order. Folder **04** sets QueueRoute **before** the first File (routing locks after first upload). Folder **09** is optional negatives. Folder **99** deletes the smoke queue/agent.

Client timeout for **POST sync extract** should be **> 60 seconds**.

## What it covers

| Folder | Surface |
|--------|---------|
| 00 Health | `/health`, `/api/app/health`, ping |
| 01 Me | `/api/app/me` (provisions tenant/business) |
| 02 Catalogs | document-types, providers, agent-templates |
| 03 Agents | clone, list, get, update, create |
| 04 Queues | CRUD, routes, webhook, email, allowlist, lock |
| 05 App files | upload with `documentTypeKey`, get, download-url, poll until ready |
| 06 API keys | create, list, (revoke is in cleanup) |
| 07 External async | multi-file upload, list/get files, list/get documents (`resultJson`, webhook status) |
| 08 External sync | `POST /extract` — wait up to 60s, no webhook |
| 09 Negatives | unknown type 400, sync `documentCount=2` 400 |
| 99 Cleanup | revoke key, delete agent/queue |
