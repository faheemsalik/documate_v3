# Documate v3 — Platform secrets store (adopted)

> **Status:** Adopted 2026-09-15 (developer lock) — structure implemented in API  
> **Type:** Engineering / platform security decision + wiring notes  
> **Related:** [15-system-settings-db-exploration.md](./15-system-settings-db-exploration.md) (secrets **out** of `CorSystemSetting`); Plan 03 provider credentials rule  
> **Trigger:** Admin settings / multi-host need — secrets must not be server-local files or plain SQL

---

## 1. Problem

Mode 1 platform credentials (AWS, LLM API keys, OCR, SMTP, auth gate secrets) must:

- Work the same on every API host (not tied to one machine’s `appsettings.Development.json`)
- Stay out of git and out of `CorSystemSetting` / plain DB `ValueJson`
- Bind into existing ASP.NET sections (`Aws:`, `Llm:Providers:…`, `Ocr:`, `Auth:`, …)

---

## 2. Locked decision (2026-09-15)

| ID | Locked |
|----|--------|
| **SEC1** | **Primary store = AWS Secrets Manager** — one JSON secret blob per environment (e.g. `documate/dev/api`, `documate/prod/api`) |
| **SEC2** | Secrets Manager is **not** part of the Documate app binary or SQL DB; Documate **reads** at **API process startup** (not per HTTP request, not per Hangfire job) |
| **SEC3** | JSON shape **mirrors appsettings** nested sections so it merges into `IConfiguration` unchanged for options binding |
| **SEC4** | **All hosts including local Development** load secrets from AWS Secrets Manager (`SecretsManager:Enabled=true`). Bootstrap to *call* SM uses IAM role or `~/.aws/credentials` / `AWS_ACCESS_KEY_*` env (not appsettings). Emergency env overrides still possible via ASP.NET config layering. |
| **SEC5** | Prefer **IAM instance/task role** in cloud to call `GetSecretValue`; local uses AWS CLI profile / env for the same API. |
| **SEC6** | **Never** expose SM values via `/api/admin/system-settings` or any browser-facing API |
| **SEC7** | **Backup / alternate platform = [Infisical](https://infisical.com/)** — reserved if we outgrow AWS SM or want a dedicated vault UI / self-host. Same contract: load once at startup into `IConfiguration`; keys as `Section__Nested__Key`. **Do not implement Infisical until a future switch decision.** |

Plan 15 **S4** remains: operational non-secrets → `CorSystemSetting`; credentials → secret store (now **AWS SM** primary, not “env forever”).

---

## 3. What goes where

| Store | Contents |
|-------|----------|
| **AWS Secrets Manager** | `Aws` keys (shared for S3/Textract/email), `Llm:Providers:*:ApiKey` (+ optional Model), Google `CredentialsJson`, SMTP password, auth passwords/tokens, inbound webhook secret |
| **`CorSystemSetting` / admin UI** | Ops non-secrets including Google Document AI **ProjectId** / **ProcessorId** / Location; pipeline ProviderKey dropdowns |
| **`CorProvider`** | Catalog of model/engine keys (no secrets) — seed or SQL to add new models |
| **Git / appsettings** | Bootstrap only: connection string, `SecretsManager:*`, Auth meta, Logging, seed defaults for Google ids |
| **AWS Console / CLI** | View/edit secret values (never Documate admin UI) |

**Do not** put empty `Storage:` / `Ocr:Textract:` key overrides in SM — code falls back to `Aws:`. Legacy `Providers:*ApiKey` removed.

### Add a new LLM/OCR model

1. Insert `CorProvider` (`ProviderKey`, Name, category `llm`/`ocr`, active).
2. `put-secret-value` on `documate/{env}/api` adding `Llm:Providers:{key}:ApiKey` (and Model in appsettings or SM).
3. Select the key in System settings dropdowns (T1 / fallback / extract or OCR primary/secondary).
4. Restart API after SM change.

### View secrets

AWS Console → Secrets Manager, or `aws secretsmanager get-secret-value --secret-id documate/dev/api`. Do not paste IAM keys into Documate admin.


---

## 4. Secret JSON template

See [`docs/ops/secrets-manager-secret.example.json`](../ops/secrets-manager-secret.example.json). One secret string = one JSON object; nested keys match configuration.

---

## 5. Runtime wiring (implemented)

- Config section `SecretsManager:Enabled`, `SecretId`, `Region`
- `ConfigurationManager` extension loads secret **once** at startup and `AddJsonStream`s it (later providers override earlier appsettings)
- Package: `AWSSDK.SecretsManager`
- Default credential chain (IAM role / env / shared credentials profile) used to call SM

---

## 6. Cost (order of magnitude)

- ~**$0.40 / secret / month** + **$0.05 / 10k API calls**
- Documate expected: 1–3 secrets (dev/staging/prod), startup-only reads → about **$0.40–$1.20 / month**

---

## 7. Infisical (backup — not active)

Keep as documented alternate:

- Cloud Free / Pro, or self-host MIT core
- Official .NET: `Infisical.IConfigurationProvider` / SDK
- Switch path: replace SM loader with Infisical provider; keep same appsettings key names (`Llm__Providers__gpt_5_6__ApiKey` style)
- Revisit only if: multi-cloud, stronger team vault UX, or self-host policy requires it

---

## 8. Out of scope (this adoption)

- Admin UI to edit secrets
- Encrypted secrets table in SQL
- Per-request SM fetch
- Automatic rotation Lambdas (can add later)
- Implementing Infisical now

---

## Changelog

| Date | Note |
|------|------|
| 2026-09-15 | Adopted AWS Secrets Manager primary; Infisical backup; API startup loader + example JSON. |
| 2026-09-15 | Local also uses SM (`Enabled=true`); secrets removed from appsettings; IAM `GetSecretValue`/`CreateSecret` required on bootstrap principal. |
| 2026-09-15 | Expanded DB settings (Storage/OCR/Notifications/Admin/concurrency); admin provider dropdowns; rejected browser IAM credential UI. |
