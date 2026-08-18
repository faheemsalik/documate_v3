# Iden constraints (engineering)

Iden is the external identity product. Documate integrates with it; Documate does **not** become a second identity island.

## Iden tenancy model (given)

Iden provides **two levels**:

1. **Tenant** — top organization.
2. **Business** — child under a Tenant.

Users and membership across Businesses are owned by Iden. Documate consumes validated context; it only mirrors Tenant/Business for Documate-specific settings (`CorTenant`, `CorTenantBusiness`).

**Documate isolation unit = Business** — operational rows carry **`BusinessId` only**. Tenant linkage and **`TenantName` projection** live on `CorTenantBusiness` (not repeated on Queue/File/Document). A Documate **Queue** is not an Iden Business.

## MUST

1. Human authentication goes through **Iden**.
2. Tenant **and Business** + user identity in API handlers comes from validated tokens / Iden integration abstractions (names TBD in Iden plan).
3. Handlers scope data by **Business** (never Tenant-flat across all Businesses unless an explicit cross-business admin feature is designed).
4. Machine-client approach (service accounts vs API keys vs client credentials) is decided in the **product/Iden plan** — engineering follows that decision when coded; keys must still resolve to a Business scope. **F2 Documate API keys are Phase 1 bridge (J3)**; Band 15 (follow-on) retires them in favor of Iden M2M.
5. After Phase 1, treat Documate as an **Iden integration harness** (Band 15): discover real Iden APIs, exercise Tenant→Business through Documate, and fix Iden defects in Iden (or explicit waiver) — do not leave temporary auth as the permanent story.

## MUST NOT

1. Do not create a parallel user/password store in Documate for product login.
2. Do not invent a third org hierarchy that duplicates Iden Tenant/Business.
3. Do not invent claim names, role matrices, or tenant/business resolution rules in random features — centralize when the Iden plan lands.
4. Do not bypass auth “temporarily” on External APIs without an explicit DQ.
5. Do not keep fixed/dev tokens or F2 keys as the permanent External auth story once Band 15 M2M is available.

See also: `auth-wiring-placeholder.md`.
