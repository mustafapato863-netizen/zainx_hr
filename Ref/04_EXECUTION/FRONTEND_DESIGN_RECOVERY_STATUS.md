# Frontend Design Recovery — Implementation Status

**Date:** 2026-08-25  
**Scope:** Frontend visual recovery and brand elevation only  
**Backend/API contracts:** preserved; the adjacent HCM Core continuation later added Organization contracts and a dedicated route, tracked separately in `HCM_GOAL_AUDIT_STATUS.md`

## Implemented in this pass

- Verified Tailwind v4 delivery through the existing Vite plugin and package `@source` roots.
- Replaced the contradictory indigo dark-theme fallback with the provisional ZainX ink/mineral/cyan/sand system.
- Formalized primitive → semantic → component token layers.
- Added the approved Zain X HR raster artwork to `BrandMark`, `BrandAssembly`, favicon, PWA icons, and reduced-motion fallback behavior.
- Reworked the application shell styling around one provisional brand lockup, grouped navigation, active states, skip link, mobile drawer, command palette, quick create, notifications, AI launcher, and locale switching.
- Rebuilt Home as a command center with a signature brand surface, live read-model snapshot, attention queue, and workspace access.
- Reduced generic card treatment in the shared Card and PageHeader primitives.
- Added an enterprise entry point so heavy widgets are not exported through the core design-system barrel.
- Added the brand constitution and asset board, including the official-SVG and self-hosted-font gates.

## Evidence

- `pnpm --dir web exec eslint .` — passed.
- `pnpm --dir web exec tsc --noEmit -p apps/workforce-web/tsconfig.app.json` — passed.
- `pnpm --dir web exec vitest run` — 19 test files passed, 121 tests passed.
- `pnpm --dir web exec nx build workforce-web` — passed; CSS 13.95KB gzip and initial JavaScript 119.51KB gzip.
- Browser QA — Home rendered with no default browser styling at 1440px-class viewport; Arabic `dir="rtl"` verified; no fabricated metrics displayed when services were unavailable.

## Latest implementation evidence

- AI, Administration, Reports, Payroll, Attendance, Leave, Approvals, Recruitment, People, and NotificationCenter now consume the semantic token vocabulary; no raw feature-palette utility scan remains in frontend TypeScript/TSX.
- The catch-all design-system chunk was removed, route modulepreload hints were disabled, and AG Grid is loaded through the route-scoped `ZainXDataGrid` boundary.
- `pnpm --dir web exec nx build workforce-web` — passed; initial JavaScript 387.12 kB raw / 119.51 kB gzip, initial CSS 81.03 kB raw / 13.95 kB gzip, AG Grid vendor 1,875.54 kB raw / 506.68 kB gzip.
- `pnpm --dir web exec nx build design-system-docs` — passed.
- Storybook production build — passed; existing large iframe warning remains.
- Chromium browser evidence covers the ten primary routes at desktop/mobile sizes plus Home, Reports, Payroll, and AI screenshots. Arabic mobile locale switching now works through the navigation drawer and verified `dir="rtl"` with no overflow.

## Known tracked debt

- AG Grid Enterprise license warning remains a procurement gate and is intentionally visible; its vendor chunk is the only workforce-web chunk still above 500 kB in the current build.
- Vitest remains green at 19 files / 121 tests, but emits existing React Aria/`act`, ECharts jsdom zero-size, and AG Grid trial-license warnings.
- The approved Brand Kit is exact raster artwork plus reusable source snippets; an official SVG master and licensed self-hosted Arabic-compatible font are still pending.
- Full reduced-motion, keyboard-only, light/dark, and authenticated API/Worker/PostgreSQL interaction evidence still needs a clean release seal. Existing route data, contracts, authorization, and business behavior remain intentionally preserved.

## Final 9.5/10 Verification Pass — 2026-08-25

### Verified

- Real local Chromium Playwright run: `Running 39 tests using 1 worker` → `39 passed (1.1m)`, 0 failed. This includes the People lifecycle, operational Phase 3–6 flows, AI flows A–J, and Arabic/RTL coverage. Production-equivalent PostgreSQL/Worker/IdP evidence remains a separate gate.
- 50 English route/viewport checks passed at 1440, 1024, 768, 375, and 320px. All ten primary routes returned HTTP 200, rendered one H1, and had no document-level horizontal overflow.
- 8 Arabic checks passed at desktop/mobile sizes for Home, People, Reports, and AI. The active shell now exposes both `dir="rtl"` and `lang="ar"` with no overflow.
- Skip-link focus, mobile drawer Escape dismissal/focus restoration, reduced-motion media behavior, and system dark-token behavior were verified in Chromium.
- ESLint, workforce-web TypeScript, Vitest (19 files / 121 tests), workforce-web production build, design-system docs build, and Storybook build all passed.
- The shared Dialog primitive now bounds tall dialogs to the viewport and provides an internal scroll region. People create/assignment forms preserve nullable optional-location semantics and wait for the directory refresh before closing.
- Reports now renders explicit permission/service error treatment instead of silently ignoring a failed export response. The current sandbox’s report-permission mismatch remains truthful 403 behavior and does not weaken authorization.

### Exact files changed by this frontend recovery continuation

- `web/playwright.config.ts`
- `web/apps/workforce-web/src/routes/__root.tsx`
- `web/apps/e2e/src/people-flow.spec.ts`
- `web/apps/e2e/src/phase6-operational-control.spec.ts`
- `web/apps/e2e/src/smoke.spec.ts`
- `web/packages/design-system/src/components/Dialog/Dialog.tsx`
- `web/packages/people/src/components/CreateEmployeeModal/CreateEmployeeModal.tsx`
- `web/packages/people/src/components/ChangeAssignmentModal/ChangeAssignmentModal.tsx`
- `web/packages/people/src/components/EmployeeDirectory/EmployeeDirectory.tsx`
- `web/apps/workforce-web/src/routes/people.tsx`
- `web/packages/reports/src/components/ReportsWorkspace.tsx`
- `web/packages/ai/src/components/AiWorkspace.tsx`

The broader semantic-token, shell, brand, route-chunk, and visual-recovery files listed earlier in this document were part of the same frontend recovery worktree but were mixed with pre-existing Gemini/user and Phase 7B/backend changes. No backend code, API contract, authorization rule, database schema, or business-domain behavior was changed by the final verification fixes above.

### Remaining inconsistencies and release debt

- AG Grid still emits its visible trial/license warning and remains the only workforce-web chunk above the 500 kB gzip target (506.68 kB gzip). Procurement/license injection is still required before production.
- Official final SVG brand assets and the approved self-hosted Arabic-compatible production font remain pending; the current mark and font stack are explicitly provisional.
- Vitest has 121/121 passing tests but retains existing React Aria/`act`, ECharts jsdom sizing, and similar test-environment warnings.
- The route matrix proves frontend geometry and runtime composition against the active local Development API/Worker/PostgreSQL environment; it does not replace the canonical Phase 8 production identity-provider, UAT, backup/restore, and deployment gates.
