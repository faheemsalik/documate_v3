# Angular conventions (B2)

Engineering standards for `apps/web`. **No product screen specs** here.

## Locked defaults (Plan 00)

| Topic | Default |
|-------|---------|
| Components | **Standalone** components (no new NgModules for features) |
| Local / feature state | **Signals** + injectable services |
| Global client state | Prefer signals/services; **do not add NgRx** unless a DQ explicitly adopts it |
| Forms | **Reactive forms** for multi-field forms; simple bindings OK for trivial controls |
| HTTP | `HttpClient` via feature or core services; prefer generated OpenAPI clients when available (`openapi-and-clients.md`) |
| Auth | Integrate with **Iden** (tokens via interceptor) — details in Iden plan |
| Tests | Colocated `*.spec.ts` |

## Do

- Feature folders under `src/app/features/{feature}/`
- Shared UI only in `shared/` when reused by 2+ features
- Keep `core/` for auth, interceptors, app-wide config
- Prefer OnPush-friendly patterns (signals + immutable inputs)

## Do not

- Put API business rules in Angular — call FrontendSupport APIs
- Import from `old_code`
- Create a shared monorepo TS package for API types — generate from OpenAPI
- Silently change `core/` or global styles when asked to fix one feature

Detail: `angular-feature-structure.md`, `angular-state-and-forms.md`, `critical-rules-web.md`.
