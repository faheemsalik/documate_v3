# Band 18 — Evidence (DQ-1808)

Date: 2026-09-19

## Build

- `dotnet build apps/api/Documate.Api.csproj` — succeeded
- `ng build` (`apps/web`) — succeeded

## Schema (DQ-1801)

- Migration: `Band18PublicEventsActions`
- Tables: `OpsActionBindings`, `OpsOutboundDeliveries`, `OpsInAppNotifications`
- Column: `OpsQueues.PublicActionsInherit` (default true)

## Spine / emits (DQ-1802…1804)

- `IPublicEventEmitter` + `ActionBindingResolver` + `PublicActionExecutor` on Hangfire queue `webhooks`
- Event names: `File received`, `Document ready` / `Document failed` / `Document cancelled`, `File completed`
- `document.terminal` removed; `rejected` → `Document failed`
- `api_sync` suppressed in emitter
- `File received` after `CreateFilesWithBlobsBatchAsync` finalize
- `File completed` when all sibling documents Ready (from document scheduler)

## API / FE (DQ-1805…1807)

- `GET/PUT /api/app/business/public-actions`
- `GET/PUT /api/app/queues/{id}/public-actions` (inherit/override)
- `GET /api/app/notifications/in-app`, `POST …/read`
- Legacy `PUT …/queues/{id}/webhook` façades into queue override bindings
- FE: Settings → Integrations & events; Queue Intake inherit/override + event checklist
- Docs page: public events card

## Manual checklist (apply migration locally then verify)

1. Apply EF migration to Dev DB.
2. Open Business → Integrations & events — defaults include `file.received` + document terminals.
3. Upload async file with webhook URL set — expect `file.received` then `document.*` deliveries in `OpsOutboundDeliveries`.
4. Sync extract — no public deliveries.
5. Mixed failure file — no `file.completed`.
