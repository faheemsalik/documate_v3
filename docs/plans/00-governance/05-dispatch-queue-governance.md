# Dispatch Queue Governance

## DQ Numbering

Format: `DQ-XXYY` where **XX** = band, **YY** = sequence within band.

| Band | Focus |
|------|--------|
| 00 | Baseline and scope guard |
| 01 | Domain / engineering architecture |
| 02 | Validation / critical rules |
| 03 | Process flows (plan governance) |
| 04 | API feature patterns (`apps/api`) |
| 05 | Web UI patterns (`apps/web`) |
| 06 | Permissions and security (Iden constraints) |
| 07 | Migration / `old_code` preservation |
| 08 | Contract governance (OpenAPI / clients) |
| 09 | Infrastructure conventions |
| 10 | Authentication wiring (Iden integration) |
| 11 | Shared packages policy |

A feature that touches API + Web gets **separate** DQ items under bands 04 and 05 with dependencies — not one mixed item.

## Status Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Complete |
| 🔄 | In Progress |
| ⬜ | Ready |
| ⏸ | Parked |
| ❌ | Cancelled |
