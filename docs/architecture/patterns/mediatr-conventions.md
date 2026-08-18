# MediatR conventions

## Registration

Register MediatR from the API assembly in `Program.cs` (scaffold DQ). Handlers live in feature folders; assembly scanning should pick them up.

## Commands vs queries

| | Command | Query |
|--|---------|--------|
| Intent | Change state | Read state |
| Naming | `CreateQueueCommand` | `GetQueueByIdQuery`, `ListQueuesQuery` |
| Return | Result / id / DTO | DTO or list DTO |
| Side effects | Yes | No (except harmless logging/metrics) |

## Handler shape

```csharp
public sealed record CreateQueueCommand(...) : IRequest<QueueDto>;

public sealed class CreateQueueHandler : IRequestHandler<CreateQueueCommand, QueueDto>
{
    // inject DbContext or abstractions — not into controller
}
```

## Pipeline behaviors (when introduced)

Prefer MediatR behaviors for cross-cutting: validation, logging, transaction boundaries. Introduce via dedicated DQ; do not invent ad-hoc wrappers per feature.

## Validation

FluentValidation (or equivalent) validators colocated in the feature folder; invoke via pipeline behavior when available, otherwise at start of handler — stay consistent once chosen at scaffold time.

## Errors

Map domain/handler failures to HTTP in one place (controller filter or result type). Handlers should not reference `HttpContext`.
