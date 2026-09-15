# Documate v3 — System settings in DB (Exploration)

> **Status:** Phase 1 — **complete** (locked 2026-09-14)  
> **Type:** Engineering / platform config exploration  
> **Trigger:** DQ-1401 dogfood — email (and other) limits live in `appsettings` JSON; want DB-backed system settings  
> **Related:** [14-email-intake-ses-exploration.md](./14-email-intake-ses-exploration.md) follow-on **F5**; Pipeline/OCR/LLM options today in `Infrastructure/Options/*`  
> **Downstream:** Phase 2 implementation plan — [15-system-settings-db-implementation-plan.md](./15-system-settings-db-implementation-plan.md)

---

## 1. Problem

Operational knobs (email rate/size caps, sync gates, feature toggles, domain defaults, S3 MIME retention) are stored in **appsettings / env**. That forces redeploys for ops changes and mixes poorly with secrets.

**Goal:** Platform **system settings** in SQL for non-secret operational limits; secrets remain in a **secret store** (adopted: **AWS Secrets Manager** — [Plan 17](./17-platform-secrets-store.md); Infisical reserved as backup). Local: env / user-secrets.

---

## 2. In / out of scope

**In (v1):**
- `CorSystemSetting` (`SettingKey`, `ValueJson`, audit)
- Seed defaults from current `EmailIntake` ops + `Pipeline` sync gates + MIME retention N days
- Internal/admin API to read/update
- Runtime read with in-memory cache + invalidate on write
- Wire consumers so EmailIntake / Pipeline read DB-backed values

**Out (v1):**
- Secrets in DB (Aws, LLM, OCR, SMTP, webhook secret, auth passwords)
- Business-level overrides
- Backoffice UI (Plan 11 later)
- Redis / distributed cache

---

## 3. Current state

| Area | Storage today |
|------|----------------|
| Email intake limits / domain / S3 names | `EmailIntake` section |
| Shared AWS keys | `Aws` section (secrets — stay) |
| Sync max pages/bytes/timeout | `Pipeline` section |
| OCR/LLM | `Ocr` / `Llm` (secrets — stay) |
| Auth gate | `Auth` (secrets — stay) |

---

## 4. Locked decisions (2026-09-14)

| ID | Locked |
|----|--------|
| **S1** | **Platform-only** (no Business overrides in v1) |
| **S2** | **A** — Flat `SettingKey` + `ValueJson` |
| **S3** | **A** — Seed from appsettings on first boot; afterward **DB is source of truth**; appsettings only for secrets + connection string (+ optional bootstrap) |
| **S4** | All **EmailIntake** operational knobs + **Pipeline** sync gates + related non-secret intake defaults (`DefaultDomain`, S3 bucket/prefix/region names if non-secret, **MIME retention days**) → settings table. Secrets stay outside DB (env locally; **AWS Secrets Manager** in shared hosts — Plan 17). |
| **S5** | **A amended** — Seed + **admin API** (`/api/admin/...`) with **separate admin credentials**; backoffice UI later (Plan 11). Not on customer `/api/app`. |
| **S6** | **A** — In-memory cache, short TTL, **explicit invalidate on write** |
| **N1** (with Plan 14 F1a) | Default retention **30 days** for raw inbound MIME in S3 |

---

## 5. Direction (locked)

1. Entity **`CorSystemSetting`**: `SettingKey` (unique), `ValueJson`, `UpdatedAt`, `UpdatedBy`.  
2. Seed missing keys from appsettings defaults once.  
3. `ISystemSettings` (or options post-configure) feeds `EmailIntakeOptions` / `PipelineOptions` at runtime.  
4. Hangfire or S3 lifecycle enforces delete-after-30-days (N editable via setting).

---

## 6. Readiness

**Phase 1 complete.** Phase 2 draft filed — verify implementation plan + lock DR-SS1…SS3, then approve Phase 3.

---

## Changelog

| Date | Note |
|------|------|
| 2026-09-14 | Draft opened from email intake follow-on F5 / DQ-1401 discussion. |
| 2026-09-14 | Locked S1=platform-only; S4=all EmailIntake ops + Pipeline sync gates to DB (secrets remain env). |
| 2026-09-14 | Locked S2/S3/S5/S6 + N1=30 days (use recommendations). Phase 1 complete. |
| 2026-09-15 | Secrets SoT clarified: **AWS Secrets Manager** (Plan 17); Infisical backup; still never in `CorSystemSetting`. |
