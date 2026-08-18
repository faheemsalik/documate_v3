# Auth wiring placeholder

**Status:** Placeholder — Phase 1 human wiring in **DQ-0101**; full live Iden + M2M in **Band 15** (Decision **J3**: after Phase 1 product).

## Intent

Wire ASP.NET Core authentication (JWT/OIDC as Iden dictates) and Angular interceptors so both apps trust Iden-issued credentials. Machine clients ultimately use Iden M2M (Band 15), not permanent Documate-only keys.

## Phase 1 (Decision J3)

- Do not scaffold a custom Documate identity database.
- Controllers/handlers assume authenticated user / Business context will exist.
- **F2 Business API keys** and interim/fixed tokens are allowed to ship Phase 1 product — labeled temporary.
- Prefer wiring toward Iden shapes early (claims, Business scope) so Band 15 is a swap, not a rewrite.
- Follow `iden-constraints.md`.

## Band 15 (follow-on — activate after Phase 1 done-when)

1. Inventory Iden APIs → Documate-facing contract note (DQ-1501).
2. Live human auth; no fixed shipping tokens (DQ-1502).
3. Tenant→Business harness through Documate (DQ-1503).
4. Iden defect loop — fix in Iden, don’t paper over (DQ-1504).
5. Iden M2M for External; retire F2 (DQ-1505/1506).
6. Auth regression suite (DQ-1507).

## Non-goals of this file

Claim schemas, RBAC matrices, service-account UX, token lifetimes — product/Iden docs and the DQ-1501 contract note own those.
