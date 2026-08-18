# Web (Angular) — Critical Rules (authoritative)

Apply before writing `apps/web` code. Detail: `patterns/angular-*.md`.

1. **Standalone components** for new UI — no new feature NgModules.
2. **Feature code stays under** `src/app/features/{feature}/`.
3. **Do not put business/extraction logic in the UI** — call FrontendSupport APIs.
4. **Do not use Domain entities** from the API project — use generated clients or feature models.
5. **Do not add NgRx (or equivalent) without a DQ.**
6. **Do not modify `core/` or global styles** for a single-feature ask without confirmation.
7. **Do not modify `old_code`.**
8. **Auth via Iden** — no parallel login/user store in the Angular app.
9. **Ask before widening scope** — `governance/prompt-clarification.md`.
10. **Colocate unit tests** as `*.spec.ts`.
11. **CorEnum values from API** — prefer binding/selecting by enum **Id** from lookup endpoints; do not invent parallel TypeScript enums that diverge from CorEnum seeds (`patterns/cor-enum.md`).
