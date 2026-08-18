# Angular feature structure

```text
apps/web/src/app/
  core/
    auth/
    interceptors/
    config/
  shared/
    components/
    pipes/
    utils/
  features/
    queues/
      queues.routes.ts
      pages/
        queue-list.page.ts
        queue-detail.page.ts
      components/
      data/
        queues.api.ts          # or generated client wrapper
        queues.store.ts        # signals store/service if needed
      models/                  # UI view models (not Domain entities)
```

## Routing

- Lazy-load feature routes from `app.routes.ts`.
- One `*.routes.ts` per feature.

## API access

- Feature `data/*` calls `/api/app/...` (FrontendSupport).
- Do not call External `/api/v1` from the product UI unless a DQ explicitly requires it.

## Naming

Kebab-case folders; Angular style guide file suffixes (`.component.ts`, `.service.ts`, etc.).
