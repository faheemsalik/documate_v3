# Angular state and forms

## State

| Scope | Approach |
|-------|----------|
| Component-local | `signal`, `computed`, `linkedSignal` as needed |
| Feature | Injectable service with signals (feature `*.store.ts` / `*.service.ts`) |
| App-wide (auth session, tenant) | `core/` services; populate from Iden tokens/claims |
| Complex multi-feature store | **Not default** — require a DQ before adding NgRx or similar |

## Forms

- Use `FormBuilder` / reactive forms for create/edit flows with validation.
- Keep validators aligned with API contracts where practical; never trust UI-only validation.
- Map form values ↔ API request DTOs in the feature `data` layer — do not leak raw `FormGroup` into services.

## Async

- Prefer `async` pipe or signal-based resource patterns over manual subscribe sprawl.
- Unsubscribe safely when subscribing manually (`takeUntilDestroyed`).

## Errors

- Surface API errors via a consistent core helper or interceptor messaging pattern (defined at scaffold).
- Do not swallow HTTP errors silently.
