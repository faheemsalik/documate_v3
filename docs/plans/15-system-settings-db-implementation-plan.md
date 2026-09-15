# Documate v3 — System Settings in DB (Implementation Plan)

> **Status:** Phase 2 — **approved**; Phase 3 DQ filed (`DQ-1410`, `DQ-1411`)  
> **Downstream:** [03-documate-v3-dispatch-queue.md](./03-documate-v3-dispatch-queue.md)  
> **Type:** Engineering implementation plan  
> **Upstream:** [15-system-settings-db-exploration.md](./15-system-settings-db-exploration.md) (S1–S6 + N1 locked 2026-09-14)  
> **Enables:** [14-email-intake-followon-implementation-plan.md](./14-email-intake-followon-implementation-plan.md) retention **N** and ops edits without redeploy  
> **Downstream:** Phase 3 — amend [03-documate-v3-dispatch-queue.md](./03-documate-v3-dispatch-queue.md) after approve  
> **Created:** 2026-09-14  

**Outcome:** Platform operational settings (EmailIntake limits/caps/domain/S3 names/retention days + Pipeline sync gates) live in **`CorSystemSetting`**; seeded once from appsettings; afterward **DB is SoT**; secrets remain **AWS Secrets Manager** (Plan 17) or local user-secrets/env — never `CorSystemSetting`.

---

## Planning flow

| Phase | Document |
|-------|----------|
| 1 — Exploration | `15-system-settings-db-exploration.md` ✅ |
| 2 — Implementation plan | **This file** |
| 3 — Dispatch queue | Amend Plan 03 — after approve |

---

## Delivery Principles

1. **Secrets never in `CorSystemSetting`** (Aws, LLM, OCR, SMTP, inbound webhook secret, auth passwords). Platform SoT for secrets: **AWS Secrets Manager** ([Plan 17](./17-platform-secrets-store.md)); Infisical documented as backup only.  
2. **Platform-only** (S1) — no Business overrides in v1.  
3. **Flat keys + ValueJson** (S2).  
4. **Seed missing keys once**; then DB wins (S3).  
5. **In-memory cache + invalidate on write** (S6).  
6. **Admin API** with **separate route + credentials** from the customer portal (S5 / DR-SS1); no customer Channels editor in v1; backoffice UI later (Plan 11).  
7. Ship **before or with** email retention wave so `MimeRetentionDays` is editable.

---

## Domain Architecture Layer

| Concern | Home |
|---------|------|
| Entity `CorSystemSetting` | `Domain/` + EF config + migration |
| `ISystemSettings` + memory cache | `Infrastructure/Settings/` |
| Seed hosted service / migrator | `Infrastructure/Persistence/Seeding/` |
| Bind into `EmailIntakeOptions` / `PipelineOptions` | Options post-configure or façade wrappers used by processors |
| Admin API | Dedicated **admin** route prefix (not customer `/api/app`) + **separate admin credentials** — see locked DR-SS1; feature under `Modules/FrontendSupport` or future `apps/admin` API surface |
| Keys catalog (constants) | `Infrastructure/Settings/SystemSettingKeys.cs` |

```text
Startup
  → ensure CorSystemSetting rows for known keys (insert if missing from appsettings defaults)
  → ISystemSettings loads all → cache

EmailIntakeProcessor / Pipeline / retention job
  → ISystemSettings.Get<T>(key) or bound IOptions snapshot refreshed

PUT admin API
  → upsert ValueJson → invalidate cache
```

---

## Domain Validation Rules

### Closed (exploration)

| Rule | Behavior |
|------|----------|
| S1 | Platform scope only |
| S2 | `SettingKey` unique + `ValueJson` |
| S3 | Seed if missing; DB SoT after |
| S4 | EmailIntake ops + Pipeline sync + retention days (+ non-secret intake defaults) |
| S5 | Seed + admin API; UI later |
| S6 | Memory cache + invalidate on write |
| N1 | Default `MimeRetentionDays` = **30** |

### Initial key set (normative list for v1)

**EmailIntake (non-secret):**

| SettingKey | Example ValueJson |
|------------|-------------------|
| `EmailIntake:DefaultDomain` | `"docsintake.com"` |
| `EmailIntake:MaxAttachmentBytes` | `26214400` |
| `EmailIntake:MaxTotalAttachmentBytes` | `52428800` |
| `EmailIntake:MaxAttachments` | `20` |
| `EmailIntake:AllowedExtensions` | `[".pdf",…]` |
| `EmailIntake:RateLimitPerMailboxPerMinute` | `20` |
| `EmailIntake:RateLimitPerMailboxPerHour` | `200` |
| `EmailIntake:S3Bucket` | `"docsintake-dev-…"` |
| `EmailIntake:S3Prefix` | `"inbound/"` |
| `EmailIntake:AwsRegion` | `"us-east-1"` |
| `EmailIntake:MimeRetentionDays` | `30` |
| `EmailIntake:BodyExcerptMaxChars` | `4096` (if FO DR-FO2=C) |

**Pipeline:**

| SettingKey | Example |
|------------|---------|
| `Pipeline:SyncWaitTimeoutSeconds` | `60` |
| `Pipeline:SyncMaxPages` | `3` |
| `Pipeline:SyncMaxBytes` | `5242880` |
| `Pipeline:MaxConcurrentFiles` | `12` (optional include) |
| `Pipeline:MaxConcurrentWebhooks` | `4` (optional include) |

**Stay in AWS Secrets Manager (Plan 17) / bootstrap appsettings only:**

- Secrets: `Aws:*`, `Llm:Providers:*:ApiKey`, `Ocr` credential fields, `Notifications:Smtp:Password`, `Auth:*` passwords/tokens, `EmailIntake:InboundWebhookSecret`
- Bootstrap (not admin DB): `ConnectionStrings:*`, `SecretsManager:*`, Auth Mode/usernames/DevBypass ids, Logging, AllowedHosts, `Llm:Providers:*:Model` (model id string alongside SM ApiKey)

**Moved to `CorSystemSetting` (SoT after seed):** EmailIntake ops, Pipeline sync + concurrency + model ProviderKeys, Storage non-secrets, OCR non-secrets (primary/secondary keys, sync pages, regions), Notifications non-secrets, Admin dashboard URLs.

---

## Decision Required

### Locked (2026-09-14)

| ID | Locked |
|----|--------|
| **DR-SS1** | **A (amended)** — Human/session-style admin API for system settings, **not** on the customer app routes. Use a **separate route prefix** (e.g. `/api/admin/system-settings`) and **separate admin credentials** (dedicated InterimFeGate / Auth config for admin — not the partner portal username/token). No External API-key access. Aligns toward Plan 11 backoffice; v1 can be API-only until `apps/admin` UI exists. |
| **DR-SS2** | **A** — Thin adapters read `ISystemSettings` per use (fresh after cache invalidate) |
| **DR-SS3** | **Amended 2026-09-15** — `MaxConcurrentFiles` / `MaxConcurrentWebhooks` / `StubStageDelayMs` **are** DB settings (admin-editable). Hangfire worker counts still apply at process start — change may need API restart. |

### Prior options (superseded)

~~SS1 A/B/C as originally listed~~ — replaced by lock above (admin-dedicated A).

---

## Process Flows

### Flow 1 — First boot seed

```text
HostedService / migrator
  → for each known SettingKey
      if not exists → insert ValueJson from IConfiguration section default
  → do not overwrite existing DB values
```

### Flow 2 — Read path

```text
ISystemSettings.GetJson(key)
  → cache hit? return
  → else load from DB (or seed default), cache, return
```

### Flow 3 — Write path

```text
PUT /api/admin/system-settings/{key}   (admin credentials)
  → validate key in allowlist catalog
  → upsert ValueJson + UpdatedBy/At
  → invalidate cache (key or all)
```

Customer `/api/app/*` does **not** expose system-settings write (or read) in v1.

---

## Instruction and Control Set

| Wave | Work |
|------|------|
| **SS-0** | Entity + migration + `SystemSettingKeys` catalog |
| **SS-1** | `ISystemSettings` + memory cache + seed hosted service |
| **SS-2** | Wire EmailIntake + Pipeline consumers to settings (adapter) |
| **SS-3** | Admin GET list + GET by key + PUT upsert under `/api/admin/...` + separate admin auth config (DR-SS1) |
| **SS-4** | Tests: seed idempotent, cache invalidate, unknown key reject; smoke notes |

**Ordering vs email FO:** SS-0…SS-2 before FO-4 retention job.

---

## Permissions and Security

- Catalog allowlist: only known keys writable.  
- Reject writes to secret-like keys if somehow listed.  
- Audit `UpdatedBy` from `IBusinessContext.UserId` or `"system-seed"`.  
- No settings values in client bundles except via authenticated admin API.

---

## Dispatch Index (proposed — Phase 3)

| DQ (proposed) | Wave | Outcome |
|---------------|------|---------|
| DQ-1601… | — | *(Band 16 is customer FE — use new band or DQ-1410+)* |
| **DQ-1410** | SS-0…SS-2 | Table + cache + seed + wire EmailIntake/Pipeline |
| **DQ-1411** | SS-3…SS-4 | Admin API + tests |

Email follow-on DQ-1204…1206 after or overlapping once SS-2 done for retention.

Exact IDs assigned in Phase 3.

---

## Wave Sections

### SS-0 — Schema
- `CorSystemSetting`; unique index on `SettingKey`; soft-delete optional (prefer hard row keep).

### SS-1 — Runtime
- Cache dictionary; TTL optional backup (e.g. 60s) plus invalidate-on-write.

### SS-2 — Consumers
- Replace direct `IOptions<EmailIntakeOptions>` reads for moved knobs in processor/rate limiter/handler; Pipeline sync gates similarly.

### SS-3 — API
- List, get, put; validation; no delete of keys (reset = put default).

### SS-4 — Evidence
- Unit + manual: change MaxAttachments in DB, next email respects without restart (after invalidate).

---

## Output contract

### Finalized from exploration

S1–S6, N1=30 locked.

### Locked (this Phase 2)

| ID | Choice |
|----|--------|
| DR-SS1 | **A amended** — `/api/admin/system-settings` + **separate admin credentials** (not customer app auth) |
| DR-SS2 | **A** — adapter → `ISystemSettings` |
| DR-SS3 | **B** — concurrency stays appsettings |

### Pending

None. Approve Phase 3 to file DQs (prefer settings DQ band before email FO retention).

---

## Changelog

| Date | Note |
|------|------|
| 2026-09-14 | Phase 2 draft for DB system settings. |
| 2026-09-14 | Locked SS1=A+admin route/creds, SS2=A, SS3=B. Phase 2 complete. |
