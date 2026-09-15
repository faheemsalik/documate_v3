# Business-scoped data access (`IBusinessDb`) — Exploration Plan

| Field | Value |
|-------|--------|
| **Feature** | Enforce Business isolation for Ops\* via a scoped data façade (not convention-only `DbContext` filters) |
| **Track** | **Engineering** (governance / CQRS data access) — not product behavior |
| **Module** | `apps/api` — Auth, Persistence, External, FrontendSupport, Pipeline (Hangfire) |
| **Related** | [`iden-constraints.md`](../architecture/governance/iden-constraints.md); [`auth-wiring-placeholder.md`](../architecture/governance/auth-wiring-placeholder.md) |
| **Status** | Phase 1 — **Exploration** (draft 2026-09-08) |
| **Created** | 2026-09-08 |

## Planning flow

| Phase | Artifact | Status |
|-------|----------|--------|
| 1 — Exploration | **This file** | 🔄 Awaiting developer verify |
| 2 — Implementation plan | `13-business-scoped-data-access-implementation-plan.md` | ⬜ After Phase 1 approve |
| 3 — Dispatch queue | `13-business-scoped-data-access-dispatch-queue.md` | ⬜ After Phase 2 approve |

---

## 1. Problem framing

Documate’s isolation unit is **Business** (`BusinessId` on Ops\* rows). Today:

- Auth (API key / DevBypass) puts `BusinessId` on claims → `IBusinessContext`.
- Handlers and services inject `DocumateDbContext` and **manually** add `BusinessId == business.BusinessId`.
- EF global filters only enforce **soft-delete** (`!IsDeleted`), **not** Business.

**Risk:** forgetting the `BusinessId` predicate is a silent cross-business IDOR (GUID lookup “works” for the wrong tenant). Isolation is **convention + review**, not framework-enforced.

**Goal:** make the default Ops data path **unable to forget** Business scope, without inventing a large classic repository layer that fights MediatR + LINQ.

---

## 2. Scope

### In scope

- Ambient business scope usable from HTTP **and** Hangfire.
- `IBusinessDb` (name TBD) exposing Business-filtered `IQueryable`s for Ops\* and stamp-on-add for writes.
- Migration path for External + FrontendSupport handlers and shared work services (`WorkRecordService`, cancel/reprocess, pipeline reads).
- Explicit escape hatch for Tenant-flat / catalog / provisioning (must remain possible and obvious).
- Tests that prove Business A cannot read Business B’s File/Document/Queue by id.
- Architecture note update (`iden-constraints` / quick-reference) stating the new rule.

### Out of scope (Phase 1+)

- Live Iden JWT / Band 15 auth redesign (uses same `BusinessId` claim when it lands).
- Product changes to tenancy model.
- Per-entity classic repositories (`IFileRepository`, …) as the primary pattern.
- Soft-delete redesign.
- Simplicity / partner client changes.

---

## 3. Current-state findings

| Area | Finding |
|------|---------|
| Auth | External: API key → `CorTenantApiKey.BusinessId`. App: DevBypass + optional `X-Business-Id` (same-tenant). |
| Context | `IBusinessContext` / `BusinessContextAccessor` reads claims from `HttpContext`. |
| EF | `HasQueryFilter(!IsDeleted)` only; `BusinessId` is column + index. |
| Handlers | External & FrontendSupport typically filter `BusinessId`; pattern is copy-paste. |
| Hangfire | Jobs pass `businessId` on `FileWorkItem` / webhook args; **no** ambient HTTP context. |
| Catalogs | `CorDocumentType`, providers, enums — global; correctly **not** Business-scoped. |
| Escape | `ListBusinesses` is intentional Tenant-flat UI; must not go through Ops Business filter. |
| Governance | `iden-constraints.md`: handlers **must** scope by Business; no analyzer yet. |

---

## 4. Risks and constraints

| Risk | Mitigation |
|------|------------|
| Hangfire with empty `IBusinessContext` | Ambient `IBusinessScope.Begin(businessId)` around every job entrypoint |
| Handlers keep using raw `db.OpsFiles` | Migration waves + optional later EF global filter; code review checklist |
| Accidental filter on catalogs | Catalogs stay on `DocumateDbContext`; `IBusinessDb` only Ops\* |
| Cross-business features break | Explicit `ITenantDb` / raw DbContext for reviewed exceptions only |
| Empty `BusinessId` writes | `IBusinessDb` throws if scope/context BusinessId missing |
| Double filter + soft-delete | Rely on existing EF soft-delete filter; façade adds Business only |
| Performance | Same SQL predicates as today; no N+1 by design of façade |

---

## 5. Open questions

| ID | Question | Options | Suggested |
|----|----------|---------|-----------|
| **Q1** | Façade name | A) `IBusinessDb` B) `IOpsDb` C) `IBusinessData` | **A** — clear, short |
| **Q2** | Enforce via EF global filter as well? | A) Façade only first B) Façade + global filter in same wave C) Global filter only | **A** then optional B in a later DQ |
| **Q3** | Migration strategy | A) Big-bang all Ops handlers B) External first, then FrontendSupport, then Pipeline/Work C) New code only | **B** |
| **Q4** | Ban raw `DocumateDbContext.Ops*` in handlers? | A) Soft (docs) B) Analyzer / CI grep C) Internal Ops sets | **A** then **B** if misses continue |
| **Q5** | Should `WorkRecordService` take only `IBusinessDb`? | A) Yes B) Keep DbContext for ExecuteUpdate edge cases | **A** with rare escape for `ExecuteUpdate` on queue lock via scoped SQL |
| **Q6** | Enforcement kernel | A) `IBusinessDb` façade B) Classic repos + `WorkContext`/`AppType` + `OnSaveDb` C) Hybrid (WorkContext + one Ops access surface + Save interceptor + ban raw Ops) | **Reopened** — see §6 critical compare |
| **Q7** | Protect kernel files | A) Docs only B) CODEOWNERS + required reviewers C) + agent confirmation / “do not edit without ask” | **B+C** if B or C chosen for Q6 |

---

## 6. Recommended direction (**contested** — was façade-first; developer critique reopened)

### Pattern A — `IBusinessDb` façade (original exploration lean)

```text
Auth / Hangfire Begin(businessId)
        ↓
   IBusinessDb  →  Ops* always filtered + stamped
        ↓
   DocumateDbContext  →  catalogs, tenant list, migrations, rare escapes
```

### Pattern B — Classic repos + `WorkContext` (developer alternative)

```text
HTTP (attr / base controller)  or  Hangfire job wrapper
        ↓
   WorkContext { AppType, BusinessId, TenantId, UserId, … }
        ↓
   IXxxRepository  →  always reads WorkContext for filters
        ↓
   OnSaveDb / SaveChanges interceptor  →  stamp + reject wrong BusinessId
        ↓
   Kernel files locked (CODEOWNERS / PR / agent confirm)
```

`AppType` (or equivalent) selects **mode**: e.g. OpsBusiness / TenantAdmin / Catalog / System — so escape hatches are explicit modes, not “inject the other Db.”

### How it works — code sketches (not production code)

These are **teaching sketches** using names from today’s codebase. Phase 1 does not ship them.

#### A) Today: every handler must remember `BusinessId` (easy to forget)

```csharp
// TODAY — GetQueueByIdHandler (real pattern)
public sealed class GetQueueByIdHandler(
    DocumateDbContext db,
    IBusinessContext business,
    ICorEnumIdResolver enums)
    : IRequestHandler<GetQueueByIdQuery, QueueDetailDto?>
{
    public async Task<QueueDetailDto?> Handle(GetQueueByIdQuery request, CancellationToken ct)
    {
        // If someone deletes "&& x.BusinessId == business.BusinessId",
        // Business B can load Business A's queue by GUID → IDOR.
        var q = await db.OpsQueues.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == request.Id && x.BusinessId == business.BusinessId,
                ct);

        if (q is null) return null;
        // ...
        return new QueueDetailDto(/* ... */);
    }
}
```

```csharp
// TODAY — CreateQueueHandler stamps BusinessId by hand
var q = new OpsQueue
{
    BusinessId = business.BusinessId, // forget this → wrong/empty tenant row
    Name = command.Request.Name.Trim(),
    // ...
};
db.OpsQueues.Add(q);
await db.SaveChangesAsync(ct);
```

#### B) Proposed: `IBusinessScope` — one place that knows “current BusinessId”

HTTP already has `IBusinessContext` from claims. Hangfire does **not** (no `HttpContext`). Scope fills that gap.

```csharp
public interface IBusinessScope
{
    string? BusinessId { get; }

    /// <summary>Sets ambient BusinessId for this async flow. Dispose clears it.</summary>
    IDisposable Begin(string businessId);
}

// Sketch: AsyncLocal so nested awaits / Hangfire keep the same id
public sealed class BusinessScope : IBusinessScope
{
    private static readonly AsyncLocal<string?> Current = new();

    public string? BusinessId => Current.Value;

    public IDisposable Begin(string businessId)
    {
        if (string.IsNullOrWhiteSpace(businessId))
            throw new InvalidOperationException("BusinessId is required.");

        var previous = Current.Value;
        Current.Value = businessId;
        return new Pop(() => Current.Value = previous);
    }

    private sealed class Pop(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
```

**Who calls `Begin`?**

| Call site | Source of BusinessId |
|-----------|----------------------|
| HTTP request | Usually claims via `IBusinessContext` (middleware can `Begin(business.BusinessId)` once per request) |
| Hangfire `ProcessFileAsync` | Job arg `businessId` already on `FileWorkItem` |
| Hangfire webhook job | Job arg `businessId` |

```csharp
// TODAY — Hangfire job entry (no ambient Business)
public sealed class FilePipelineJobs(IFilePipelineStub stub)
{
    public Task ProcessFileAsync(Guid fileId, string businessId, string? userId) =>
        stub.ProcessAsync(new FileWorkItem(fileId, businessId, userId));
}

// PROPOSED — same job, but ambient scope is set first
public sealed class FilePipelineJobs(IFilePipelineStub stub, IBusinessScope scope)
{
    public Task ProcessFileAsync(Guid fileId, string businessId, string? userId)
    {
        using (scope.Begin(businessId))
        {
            return stub.ProcessAsync(new FileWorkItem(fileId, businessId, userId));
        }
    }
}
```

Inside the pipeline, code can use `IBusinessDb` and stop re-typing `item.BusinessId` on every query (still fine to keep it on the work item for logging).

#### C) Proposed: `IBusinessDb` — Ops tables already filtered

```csharp
public interface IBusinessDb
{
    IQueryable<OpsFile> Files { get; }
    IQueryable<OpsDocument> Documents { get; }
    IQueryable<OpsQueue> Queues { get; }
    IQueryable<OpsAgent> Agents { get; }
    // …other Ops* sets that carry BusinessId

    void Add<T>(T entity) where T : class; // stamps BusinessId when entity has it
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

```csharp
public sealed class BusinessDb(
    DocumateDbContext db,
    IBusinessScope scope,
    IBusinessContext httpBusiness) : IBusinessDb
{
    // Prefer ambient scope (Hangfire); fall back to HTTP claims
    private string RequireBusinessId()
    {
        var id = scope.BusinessId;
        if (string.IsNullOrWhiteSpace(id))
            id = httpBusiness.BusinessId;

        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidOperationException(
                "No BusinessId in scope/context. Call IBusinessScope.Begin(...) or authenticate.");

        return id;
    }

    public IQueryable<OpsQueue> Queues =>
        db.OpsQueues.Where(q => q.BusinessId == RequireBusinessId());

    public IQueryable<OpsFile> Files =>
        db.OpsFiles.Where(f => f.BusinessId == RequireBusinessId());

    public IQueryable<OpsDocument> Documents =>
        db.OpsDocuments.Where(d => d.BusinessId == RequireBusinessId());

    public IQueryable<OpsAgent> Agents =>
        db.OpsAgents.Where(a => a.BusinessId == RequireBusinessId());

    public void Add<T>(T entity) where T : class
    {
        // Sketch: stamp known Ops entity types
        if (entity is OpsQueue queue)
            queue.BusinessId = RequireBusinessId();
        else if (entity is OpsFile file)
            file.BusinessId = RequireBusinessId();
        // …same for other Ops*

        db.Add(entity);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
```

Soft-delete (`!IsDeleted`) stays on EF’s existing global filter. `IBusinessDb` only adds **Business**.

#### D) After: handlers use `IBusinessDb` — no manual `BusinessId ==`

```csharp
// PROPOSED — Get by id: only Id; Business filter is already on Queues
public sealed class GetQueueByIdHandler(
    IBusinessDb biz,
    DocumateDbContext db, // still OK for catalogs / joins to Cor*
    ICorEnumIdResolver enums)
    : IRequestHandler<GetQueueByIdQuery, QueueDetailDto?>
{
    public async Task<QueueDetailDto?> Handle(GetQueueByIdQuery request, CancellationToken ct)
    {
        var q = await biz.Queues.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        // Business B asking for Business A's queue id → null (filter hid the row)

        if (q is null) return null;
        // LoadRoutes can still use db for CorDocumentTypes (catalog = not Business-scoped)
        return new QueueDetailDto(/* ... */);
    }
}
```

```csharp
// PROPOSED — List: no .Where(BusinessId == ...)
public sealed class ListQueuesHandler(IBusinessDb biz, /* ... */)
    : IRequestHandler<ListQueuesQuery, IReadOnlyList<QueueDto>>
{
    public async Task<IReadOnlyList<QueueDto>> Handle(ListQueuesQuery request, CancellationToken ct)
    {
        var rows = await biz.Queues.AsNoTracking()
            .OrderBy(q => q.Name)
            .ToListAsync(ct);
        // ...
    }
}
```

```csharp
// PROPOSED — Create: Add stamps BusinessId
public sealed class CreateQueueHandler(IBusinessDb biz, IBusinessContext business, /* ... */)
    : IRequestHandler<CreateQueueCommand, QueueDto>
{
    public async Task<QueueDto> Handle(CreateQueueCommand command, CancellationToken ct)
    {
        var q = new OpsQueue
        {
            // BusinessId omitted on purpose — biz.Add stamps it
            Name = command.Request.Name.Trim(),
            CreatedByUserId = business.UserId,
            UpdatedByUserId = business.UserId,
            // ...
        };

        biz.Add(q);
        await biz.SaveChangesAsync(ct);
        return /* dto */;
    }
}
```

#### E) Escape hatch: Tenant-flat / catalogs stay on raw `DocumateDbContext`

`ListBusinesses` must see **all businesses for the tenant**, not only the current one. It must **not** go through `IBusinessDb`.

```csharp
// INTENTIONAL ESCAPE — keep DocumateDbContext
public sealed class ListBusinessesHandler(DocumateDbContext db, IBusinessContext business)
    : IRequestHandler<ListBusinessesQuery, IReadOnlyList<BusinessListItemDto>>
{
    public async Task<IReadOnlyList<BusinessListItemDto>> Handle(
        ListBusinessesQuery request,
        CancellationToken ct)
    {
        // Tenant-flat on purpose — not Ops Business isolation
        var rows = await db.CorTenantBusinesses.AsNoTracking()
            .Where(b => b.Tenant!.IdenTenantId == business.TenantId && !b.IsDeleted)
            .OrderBy(b => b.Name)
            .ToListAsync(ct);
        // ...
    }
}
```

Same for `CorDocumentTypes`, `CorProviders`, `CorEnums` — inject `DocumateDbContext`, not `IBusinessDb`.

#### F) Pipeline read — before vs after

```csharp
// TODAY — FilePipelineStub
var file = await db.OpsFiles.FirstOrDefaultAsync(
    f => f.Id == item.FileId && f.BusinessId == item.BusinessId && !f.IsDeleted,
    ct);

// PROPOSED — after job called scope.Begin(item.BusinessId)
var file = await biz.Files.FirstOrDefaultAsync(f => f.Id == item.FileId, ct);
// Soft-delete still applied by EF; Business applied by IBusinessDb
```

#### G) Mental model in one sentence

| Piece | Job in plain English |
|-------|----------------------|
| `IBusinessScope` | “For this request/job, we are Business X.” |
| `IBusinessDb` | “Give me Ops rows for Business X only; stamp X on new rows.” |
| `DocumateDbContext` | “Everything else: catalogs, tenant business list, migrations, rare reviewed escapes.” |

### Critical compare: Pattern A (`IBusinessDb`) vs Pattern B (repos + `WorkContext`)

Developer critique of A is **valid**: if every handler/service must *choose* the safe API again, isolation is still convention — just a nicer convention.

| Dimension | A — `IBusinessDb` façade | B — Repos + `WorkContext` + `OnSaveDb` |
|-----------|--------------------------|----------------------------------------|
| **Where Business is chosen** | Often still per consumer (`inject IBusinessDb` vs `DocumateDbContext`) | Ideally **once** at HTTP/Hangfire boundary into `WorkContext` |
| **Read-path IDOR** | Strong *only if* handlers use façade; weak if anyone uses `db.Ops*` | Strong *only if* handlers use repos; weak if anyone uses `db.Ops*` |
| **Write-path mistakes** | `Add` stamp helps; easy to bypass with raw `db.Add` | **`OnSaveDb` is a real last gate** — reject/stamp wrong `BusinessId` even if caller forgot |
| **“Mistake on every step”** | Yes — each new handler is another chance to inject raw DbContext | Better *if* Features cannot see DbContext; worse if repos proliferate and some repos forget filter |
| **Escape hatches** | “Use `DocumateDbContext`” — easy, too easy | `AppType` / mode on `WorkContext` — explicit TenantAdmin vs OpsBusiness |
| **Fit with Documate CQRS** | High — keep LINQ in MediatR handlers | Lower — many `IFileRepository` / `IQueueRepository` methods, or fat repos that re-hide LINQ |
| **Hangfire** | `Begin(businessId)` at job entry | Same idea: job filter/wrapper builds `WorkContext` before repo use |
| **Protecting the mechanism** | Optional CI grep / docs | CODEOWNERS + agent confirm on WorkContext / repos / OnSave — **orthogonal and valuable for either pattern** |
| **Cost to ship** | Smaller surface (one façade) | Larger (context + N repos + save hook + ownership rules) |
| **Failure mode if incomplete migration** | Residual IDOR via raw DbContext | Residual IDOR via raw DbContext **or** a repo that doesn’t filter |

#### Honest verdict

1. **A alone is weak** against “developer injects `DocumateDbContext`.” Moving filters into `IBusinessDb` does not remove choice at the call site. That matches your concern.

2. **B’s `OnSaveDb` is stronger on writes** than A’s stamp-only `Add`. But **IDOR is mostly a read bug** (get-by-GUID). `OnSaveDb` does **not** stop Business B from *reading* Business A’s row if the read path bypasses repos.

3. **B is only stronger than A if Features cannot touch Ops `DbSet`s.** Classic repos without banning DbContext are the same hole with more files. The real strength of B is the **boundary WorkContext + single allowed data path**, not “repository” as a noun.

4. **File locks / agent confirmation** protect the *kernel* (context builder, save interceptor, base repo). They do **not** protect every Feature handler. Useful for both A and B; not a substitute for a banned bypass.

5. **Documate-specific cost of classic per-entity repos:** Controllers already must not use DbContext (MediatR only). Putting all LINQ behind per-entity repos fights the current CQRS slice style and tends to grow `GetById` / `List` / `Update` boilerplate. A **single Ops unit-of-work / façade** that *must* take `WorkContext` is usually a better fit than 15 repositories — that is Pattern C.

#### Pattern C — Hybrid (often the strong design)

```text
Boundary builds WorkContext (HTTP attr/base | Hangfire wrapper)
        ↓
   AppType = OpsBusiness | TenantAdmin | Catalog | System
        ↓
   One Ops access surface (façade or base Ops repository) — requires OpsBusiness + BusinessId
        ↓
   SaveChanges interceptor — stamp / reject BusinessId mismatch
        ↓
   Features: no DocumateDbContext for Ops* (analyzer / Internal DbSets / CI)
        ↓
   Kernel files: CODEOWNERS + agent confirmation
```

| Piece | Role |
|-------|------|
| `WorkContext` + `AppType` | Built once; modes make escapes explicit |
| One Ops access API | Avoids N classic repos; still MediatR-friendly LINQ |
| `OnSaveDb` | Write last line of defense |
| Ban raw Ops access | Closes the “choose wrong dependency” hole |
| CODEOWNERS / agent gate | Stops silent weakening of the kernel |

**Compared to A:** C fixes “every step re-implements trust” by moving trust to boundary + save + ban.  
**Compared to pure B:** C keeps MediatR LINQ and avoids repo sprawl while keeping your WorkContext / OnSave / ownership ideas.

#### Sketch — what B/C add that A lacked

```csharp
// Boundary (HTTP) — once per request
public abstract class BusinessApiController : ControllerBase
{
    protected WorkContext Work => HttpContext.Items[nameof(WorkContext)] as WorkContext
        ?? throw new InvalidOperationException("WorkContext missing.");
}

// Attribute / middleware builds:
// WorkContext { AppType = OpsBusiness, BusinessId = claim, TenantId = claim, UserId = claim }

// Hangfire
public Task ProcessFileAsync(Guid fileId, string businessId, string? userId)
{
    using var _ = workContextAccessor.Begin(new WorkContext(
        AppType: AppType.OpsBusiness,
        BusinessId: businessId,
        UserId: userId,
        TenantId: null)); // or load tenant if needed
    return stub.ProcessAsync(...);
}

// OnSave — write gate (both B and C)
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    var work = _work.Current;
    foreach (var e in ChangeTracker.Entries())
    {
        if (e.Entity is IBusinessOwned owned) // Ops* marker
        {
            if (work.AppType != AppType.OpsBusiness)
                throw new InvalidOperationException("Ops entity save requires OpsBusiness AppType.");
            if (e.State == EntityState.Added)
                owned.BusinessId = work.BusinessId;
            else if (owned.BusinessId != work.BusinessId)
                throw new InvalidOperationException("Cross-business save rejected.");
        }
    }
    return await base.SaveChangesAsync(ct);
}
```

Without also forcing **reads** through a scoped query surface, the save gate alone is incomplete.

#### Pattern C — **read path** (this is the important part)

Writes = `OnSaveDb`. Reads = **only** through an Ops query surface that always applies `WorkContext.BusinessId`. Handlers never call `db.OpsQueues` / `db.OpsFiles`.

```text
WorkContext (built once at HTTP/Hangfire)
        ↓
IOpsDb.Queues / Files / Documents   ← every get/list goes here
        ↓
.Where(x => x.BusinessId == work.BusinessId)  ← baked into the property
        ↓
Handler: FirstOrDefaultAsync(x => x.Id == id)  ← Id only; wrong business → null
```

**Layer 1 — Ops query surface (required for reads)**

```csharp
public enum AppType
{
    OpsBusiness,   // normal External / FrontendSupport / pipeline
    TenantAdmin,   // ListBusinesses, provisioning
    Catalog,       // Cor* lookups only (or use DocumateDbContext for Cor*)
    System,        // migrations / rare jobs — no Ops Business filter by design
}

public sealed record WorkContext(
    AppType AppType,
    string BusinessId,
    string TenantId,
    string? UserId);

public interface IOpsDb
{
    IQueryable<OpsQueue> Queues { get; }
    IQueryable<OpsFile> Files { get; }
    IQueryable<OpsDocument> Documents { get; }
    IQueryable<OpsAgent> Agents { get; }
    // …other Ops*

    void Add<T>(T entity) where T : class;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public sealed class OpsDb(DocumateDbContext db, IWorkContextAccessor work) : IOpsDb
{
    private WorkContext RequireOpsBusiness()
    {
        var w = work.Current
            ?? throw new InvalidOperationException("WorkContext missing.");
        if (w.AppType != AppType.OpsBusiness)
            throw new InvalidOperationException(
                $"IOpsDb requires AppType.OpsBusiness, got {w.AppType}.");
        if (string.IsNullOrWhiteSpace(w.BusinessId))
            throw new InvalidOperationException("WorkContext.BusinessId required.");
        return w;
    }

    // READ GATE: filter is on the property — caller cannot “forget” BusinessId
    public IQueryable<OpsQueue> Queues
    {
        get
        {
            var w = RequireOpsBusiness();
            return db.OpsQueues.Where(q => q.BusinessId == w.BusinessId);
        }
    }

    public IQueryable<OpsFile> Files
    {
        get
        {
            var w = RequireOpsBusiness();
            return db.OpsFiles.Where(f => f.BusinessId == w.BusinessId);
        }
    }

    public IQueryable<OpsDocument> Documents
    {
        get
        {
            var w = RequireOpsBusiness();
            return db.OpsDocuments.Where(d => d.BusinessId == w.BusinessId);
        }
    }

    public IQueryable<OpsAgent> Agents
    {
        get
        {
            var w = RequireOpsBusiness();
            return db.OpsAgents.Where(a => a.BusinessId == w.BusinessId);
        }
    }

    public void Add<T>(T entity) where T : class => db.Add(entity); // stamp in OnSave
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
```

**Layer 2 — Handler read (no `BusinessId ==` in Feature code)**

```csharp
public sealed class GetQueueByIdHandler(IOpsDb ops, ICorEnumIdResolver enums)
    : IRequestHandler<GetQueueByIdQuery, QueueDetailDto?>
{
    public async Task<QueueDetailDto?> Handle(GetQueueByIdQuery request, CancellationToken ct)
    {
        // Business B + A's queue GUID → null (row filtered out at IOpsDb.Queues)
        var q = await ops.Queues.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (q is null) return null;
        return /* dto */;
    }
}
```

**Layer 3 — Close the bypass (without this, C collapses back to A)**

```csharp
// Idea: Ops DbSets are internal; Features cannot compile against db.OpsQueues
public class DocumateDbContext : DbContext
{
    internal DbSet<OpsQueue> OpsQueues => Set<OpsQueue>();
    internal DbSet<OpsFile> OpsFiles => Set<OpsFile>();
    // Cor* stay public for TenantAdmin / Catalog handlers
    public DbSet<CorDocumentType> CorDocumentTypes => Set<CorDocumentType>();
    public DbSet<CorTenantBusiness> CorTenantBusinesses => Set<CorTenantBusiness>();
}
```

Plus CI / analyzer: Feature projects must not use `DocumateDbContext` for Ops entity types (only `IOpsDb`). Kernel files (`WorkContext`, `OpsDb`, `SaveChanges` interceptor) → CODEOWNERS + agent confirmation.

**Layer 4 — optional later defense in depth (reads too)**

```csharp
// After WorkContext is always present for Ops HTTP/Hangfire:
modelBuilder.Entity<OpsQueue>().HasQueryFilter(q =>
    !q.IsDeleted &&
    q.BusinessId == WorkContextForFilters.BusinessId); // ambient; same idea as soft-delete
```

Use only after boundary WorkContext is reliable; TenantAdmin paths must use a mode that ignores or replaces this filter.

**Escape for Tenant-flat reads (not IOpsDb)**

```csharp
// AppType.TenantAdmin — ListBusinesses still uses Cor* on DocumateDbContext
public sealed class ListBusinessesHandler(DocumateDbContext db, IWorkContextAccessor work)
{
    public async Task<IReadOnlyList<BusinessListItemDto>> Handle(...)
    {
        var w = work.Current!;
        if (w.AppType != AppType.TenantAdmin)
            throw new InvalidOperationException("TenantAdmin required.");

        return await db.CorTenantBusinesses.AsNoTracking()
            .Where(b => b.Tenant!.IdenTenantId == w.TenantId && !b.IsDeleted)
            .Select(/* ... */)
            .ToListAsync(ct);
    }
}
```

| Read concern | How C addresses it |
|--------------|--------------------|
| Forget `BusinessId ==` on get-by-id | Filter lives inside `IOpsDb.Queues` / `Files` / … |
| Inject raw `db.Ops*` and skip filter | `internal` Ops sets + ban in Features |
| Wrong mode for Tenant list | `AppType` — TenantAdmin does not use `IOpsDb` |
| Hangfire reads | Job wrapper builds `WorkContext` before `IOpsDb` use |

**Where to see this code today:** only in this exploration file (sketches). There is **no production `IOpsDb` / `WorkContext` implementation yet** — that comes after Phase 2 + Phase 3 execute.

### Why not global filter alone

Still harder with Hangfire / empty context / Tenant-flat queries before ambient `WorkContext` exists. Can be **defense in depth** after WorkContext is reliable (optional later).

---

## 7. Exploration exit criteria

Phase 1 is done when the developer:

1. Agrees isolation must move off convention-only call-site filters.  
2. Chooses **Q6** (A / B / C) and **Q7** (ownership/agent gate).  
3. Chooses or amends **Q1–Q5** (or defers to Phase 2).  
4. Approves moving to **Phase 2 — Implementation plan** (no code yet).

---

## Finalized decisions (exploration)

| ID | Decision |
|----|----------|
| D1 | Isolation unit remains **Business** (unchanged from `iden-constraints`). |
| D2 | **Reopened.** Mechanism not locked: A façade vs B classic repos+WorkContext vs C hybrid. |
| D3 | Hangfire (and HTTP) must establish ambient Business / WorkContext from a **single boundary**, not ad-hoc per query. |
| D4 | Catalogs and Tenant-flat features need an **explicit mode/escape**, not silent raw DbContext habit. |

## Pending decisions

Q1–Q7 (name, EF filter timing, migration order, ban strength, WorkRecordService, **enforcement kernel A/B/C**, kernel file protection).

## Assumptions

- Band 15 Iden will continue to supply a Business claim compatible with `IBusinessContext` / WorkContext.  
- Soft-delete global filter remains as-is.  
- No product change to “Tenant as isolation unit.”  
- CODEOWNERS / agent confirmation can be enforced in this repo’s process.

## Risks

- Incomplete migration leaves some handlers on raw DbContext (residual IDOR) — **true for A and B**.  
- Relying on `OnSaveDb` alone while reads stay open (false sense of safety).  
- Classic per-entity repos drift from Documate CQRS conventions / become boilerplate.  
- Ambient scope / WorkContext bugs in background jobs (empty Business → throw vs empty filter).  
- Over-locking files slows legitimate fixes if ownership is too narrow.

## Readiness

**Blocked on Q6/Q7** (and optionally Q1–Q5). Not ready for Phase 2 until enforcement kernel is chosen.

---

### Gate

Your critique stands: **A alone is still opt-in per step.** Reply with a choice:

- **Q6:** A (façade) / **B** (classic repos + WorkContext + OnSave) / **C** (hybrid — recommended if you want B’s guarantees without repo sprawl)  
- **Q7:** how hard to lock kernel files (CODEOWNERS / agent confirm)

Then answer or defer **Q1–Q5**, and say **approve Phase 2** when ready (implementation plan only — no production code until Phase 3 + execute).
