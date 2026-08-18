# CorEnum pattern (persistence & comparisons)

**Product catalog:** `docs/plans/03-documate-v3-implementation-plan.md` (§ CorEnumType / CorEnum).  
**Authoritative for engineering:** this file + `governance/critical-rules-api.md`.

## MUST

1. Persist selectable / status / mode values as **`XxxEnumId` → `CorEnum.Id`** (FK). Do **not** map CLR `enum` columns onto domain tables.
2. In domain/handlers/workers, **compare CorEnum Ids** (`long` / bigint), not `EnumKey` / `Name` strings.
3. Validate on write that the FK belongs to the expected **`CorEnumType.EnumTypeKey`** (type safety).
4. Seed system values with stable `CorEnumType.EnumTypeKey` + `CorEnum.EnumKey`; resolve those rows to Ids once (lookup/cache) for use in comparisons.
5. DTOs may expose `EnumKey` / display name for UI and External readability; **persistence and business branching use Ids**.
6. **No hierarchy** — do not add `ParentId` / multilevel trees on `CorEnum`.

## MUST NOT

1. Do not add new static enum columns on entities (`PublicStatus`, `Source`, `AllowlistMode`, …).
2. Do not branch business logic on `EnumKey` / display strings when an Id is available (`if (row.EnumKey == "ready")` — forbidden in handlers).
3. Do not hard-code raw numeric Ids in feature code without going through a shared **typed Id resolver** / seed constants populated from DB (resolver is the single place that binds `EnumKey` → Id).
4. Do not treat `CorEnum` Ids as public partner resource ids (External contracts stay UUID work ids + optional key strings in JSON).

## Bootstrap exception

`CorEnumType.Scope` remains a constrained string (`system` | `business`) — not a CorEnum FK (avoids circularity).

## Comparison pattern

```text
// Resolve once (startup / scoped cache) — only place EnumKey is used for logic binding
var readyId = enumIds.Require("file_public_status", "ready");

// Everywhere else — Id compare
if (file.PublicStatusEnumId == readyId) { ... }
```

## Related entities (examples)

| Column | CorEnumType.EnumTypeKey |
|--------|-------------------------|
| `CorTenant.ProviderModeEnumId` | `provider_mode` |
| `Queue.AllowlistModeEnumId` | `allowlist_mode` |
| `File.SourceEnumId` / `PublicStatusEnumId` | `intake_source` / `file_public_status` |
| `Document.PublicStatusEnumId` | `document_public_status` |

**Naming:** never use a bare column named `Key` (reserved-word risk). Prefer entity-prefixed names (`EnumTypeKey`, `EnumKey`, `ProviderKey`, …).

See plan 03 seed type list for the full Phase 1 set.
