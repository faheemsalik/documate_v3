# Preservation Rules

- **`old_code/` is reference-only** until cutover. Do not add features, fix bugs “while here,” or modernize it unless a DQ explicitly says so.
- After cutover, deletion of `old_code/` is a dedicated DQ — do not delete casually.
- Do not change unrelated modules or features.
- Do not refactor neighboring features to “match the new pattern” without explicit approval.
- When reading `old_code` for behavior, **re-implement** under `apps/api` / `apps/web` using current architecture — do not copy layered Controllers/Services structure.
- Do not invent product rules from `old_code`; confirm against product plans when behavior is unclear.
