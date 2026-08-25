# ZainX_HR Full Project Audit Findings

This file records evidence, discoveries, contradictions, and unresolved questions during the read-only Phase 7B release-readiness audit.

## Evidence Classification

- **Verified:** Direct repository or command evidence supports the claim.
- **Contradicted:** Current evidence conflicts with the claim.
- **Incomplete:** Some evidence exists, but the requirement is not fully proven.
- **Unverified:** Evidence could not be obtained during this audit.

## Baseline Findings

### B-001 Repository is dirty at audit start

**Classification:** Incomplete context / audit boundary

The repository contains a substantial pre-existing working tree with modified and untracked backend, frontend, test, OpenAPI, and AI Phase 7B files. The current HEAD is `0cc9116b3fe700dbfc5ff8bdebd78807e23b01fe` (`test(phase6): add full 38-gate operational control integration tests and JIT warmup`). This audit must distinguish pre-existing user changes from the three temporary audit notes created for this run and the previously requested visual reference board.

**Evidence:** `git status --short`, captured at audit start.

**Impact:** The audit can assess the current worktree, but cannot treat the last commit as the complete project state or attribute every working-tree change to the current audit.

### B-002 Phase 7B implementation is present in the current worktree

**Classification:** Evidence observed, not yet verified

The worktree contains Phase 7B-related source, tests, routes, and frontend components, including `src/Modules/Ai/Api/`, `src/Modules/Ai/Application/`, `src/Modules/Ai/Domain/`, `src/Modules/Ai/Infrastructure/`, `tests/Architecture.Tests/Phase7BAiActionProposalTests.cs`, `tests/Architecture.Tests/Phase7AiOperationalControlTests.cs`, `web/apps/e2e/src/phase7b-ai-actions.spec.ts`, `web/apps/workforce-web/src/routes/ai.tsx`, and `web/packages/ai/src/components/`.

**Impact:** The supplied Phase 7B walkthrough is plausible as a description of current worktree scope, but its claims still require direct source and command verification.

### B-003 Canonical roadmap defines Phase 8 as the release-candidate gate

**Classification:** Verified

`Ref/04_EXECUTION/phase-matrix.json`, `Ref/04_EXECUTION/EXECUTION_ROADMAP_v4.1.md`, and `Ref/04_EXECUTION/AI_Missions/08_Phase_8_Production_RC.md` define Phase 8 as production hardening, on-premise validation, UAT, security, performance, accessibility, RTL, browser matrix, backup/restore, license compliance, support diagnostics, and release packaging.

**Impact:** A Phase 7B implementation walkthrough is not equivalent to production readiness. The current audit must verify the Phase 8 release-candidate gate before any PASS verdict.

### B-004 Canonical frontend/design direction intentionally limits visual emphasis

**Classification:** Verified

The canonical visual direction uses a quiet foundation for most of the interface, operational emphasis for meaningful states, and rare signature spotlights. It explicitly forbids full-screen neon ambience, universal glow, 3D tilt on finance/HR cards, and glass panels that reduce table readability. The visual reference board is compatible only when applied within these constraints.

**Impact:** The visual audit must distinguish missing creative direction from a legitimate calm enterprise baseline; adding images or shadows everywhere would violate the canonical design contract.

### B-005 Current governance register is older than the current Phase 7B worktree

**Classification:** Documentation inconsistency candidate

`Ref/05_GOVERNANCE/ADR_REGISTER.md`, `DEPENDENCY_LICENSE_REGISTER.md`, and related Phase 1C governance documents are dated August 24, 2026 and describe Phase 1C as the latest closeout. The worktree contains Phase 7A/7B source and tests, but no evidence has yet been found that the governance register or roadmap status has been amended to record Phase 7B completion.

**Impact:** Phase status and governance synchronization is a likely release-readiness finding; verify against all roadmap/status documents before classifying it.

### B-006 Current Phase 7B E2E claims are overstated by the current spec

`web/apps/e2e/src/phase7b-ai-actions.spec.ts` declares nine tests covering A–J, but Flow C only performs a normal confirmation and never tampers with a proposal. Flow D & E only asserts that a new proposal is `ReadyForConfirmation`; neither expiry nor execution-time permission revocation is exercised in this E2E file.

**Impact:** The supplied “A–J verified” claim is incomplete until actual runtime tests or stronger unit/integration coverage proves those behaviors.

### B-007 Playwright configuration does not start the backend

`web/apps/e2e/playwright.config.ts` starts only `npx nx serve workforce-web --port=4200`; the Phase 7B spec calls the API at `http://localhost:5041/api/v1`. Real backend/PostgreSQL/Worker participation cannot be inferred from the config alone and must be verified from the actual run environment.

### B-008 Proposal hash does not cover all proposal payload fields

`src/Modules/Ai/Domain/AiActionProposal.cs` includes snapshots, target, expected row version, effective date, tenant/legal entity, action, and required permission in the SHA-256 input, but omits `ImpactSummaryJson`, `ConversationId`, `RequestedByUserId`, `IdempotencyKey`, and expiry/validity. If those are considered tamper-protected proposal attributes, the current integrity claim is incomplete.

### B-009 Execution state has no verified recovery path in the service

`src/Modules/Ai/Application/Services/AiProposalService.cs` persists the proposal after `MarkConfirmed()`/`MarkExecuting()` and before invoking the bounded-context handler. An unhandled handler or database failure after that point can leave a proposal in `Executing`; a transaction, recovery, or outbox policy must be verified before release readiness.

### B-010 Production authentication/authorization is not wired in the API host

`src/Workforce.Host.Api/Program.cs` registers a `DefaultUserContextProvider` and falls back to a hard-coded user/tenant context with `"*"`, `admin`, and broad payroll/people/recruitment permissions when no context exists. The host does not register or invoke ASP.NET authentication/authorization middleware. `src/Workforce.Host.Api/Middleware/TenantResolutionMiddleware.cs` also accepts `X-Allowed-Tenants`, `X-Allowed-Legal-Entities`, and `X-Permissions` request headers as authority when claims are absent, and supplies default memberships/permissions.

**Impact:** This is a release-blocking security finding unless an external trusted gateway is the explicit, documented authority and the API is never directly reachable. The current source does not prove that boundary.

### B-011 Database secrets have insecure fallbacks in both API and Worker

`src/Workforce.Host.Api/Program.cs:56` and `src/Workforce.Host.Worker/Program.cs:23` default `ZAINX_DB_PASSWORD` to the literal `123456` when configuration is absent.

**Impact:** Production startup can silently use a known database password; this contradicts production/on-premise secret hygiene and must be removed or restricted to an explicit development-only mode.

### B-012 CORS is unrestricted in the API host

`src/Workforce.Host.Api/Program.cs:258` configures `AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()` and applies it at line 313 without an environment or trusted-origin restriction.

**Impact:** Cross-origin browser callers are not constrained to the workforce web origin; this is not production-ready for a credentialed enterprise API.

### B-013 Migration failure is swallowed during API startup

`src/Workforce.Host.Api/Program.cs:266-311` wraps all schema migrations, AI retention, and compliance seeding in one broad `try/catch`, logs a warning, and continues to map endpoints. A partially initialized database can therefore be served as healthy.

**Impact:** Readiness and data-integrity behavior is unsafe for release until startup fails closed or health/readiness explicitly reports migration failure.

### B-014 Frontend unit suite passes with material runtime warnings

`pnpm --dir web exec vitest run` exited 0 with 19 files and 120 tests passed, but emitted repeated `HTMLCanvasElement.prototype.getContext` not-implemented errors from axe/jsdom, React updates not wrapped in `act(...)`, duplicate React key warnings, React Aria `PressResponder`/`textValue` warnings, and AG Grid Enterprise trial/license-key watermark messages.

**Impact:** The green count is valid, but the suite is not warning-clean and its accessibility signal needs cleanup before treating it as a release-quality gate.

### B-015 Actual Phase 7B Playwright execution passed, but A–J evidence remains partial

The exact supplied command `pnpm --dir web exec playwright test src/phase7b-ai-actions.spec.ts` executed 9 tests and reported `9 passed (20.9s)`. The configured project is Chromium; during the run a real `workforce-web` Vite process was listening on 4200 and a real `Workforce.Host.Api.exe` was listening on 5041, with PostgreSQL processes present. A `Workforce.Host.Worker.exe` process was also present, but no Phase 7B test assertion proves that the Worker executed any A–J path.

**Impact:** The run is verified as a real Chromium/frontend/API/PostgreSQL integration against the currently running local environment. It is not sufficient evidence for Worker participation, and the Flow C and Flow D & E assertions do not prove their stated tamper/expiry/reauthorization behaviors.

### B-016 Current production build contradicts the approved initial-shell performance baseline

`pnpm --dir web exec nx build workforce-web` passed, but emitted `module-approvals` at 3,589.98 kB (1,057.23 kB gzip). The generated `web/dist/apps/workforce-web/index.html` also module-preloads this chunk, while `Ref/05_GOVERNANCE/ENGINE_BUNDLE_AUDIT.md:28-35` records an approved initial shell of 115.41 kB gzip with heavy engines absent from the initial shell.

**Impact:** Build success is verified, but the current bundle is not compliant with the documented performance baseline until the route/module graph is re-audited and the large approvals chunk is removed from initial preload or the baseline is formally revised.

### B-017 Dependency versions and governance records are out of sync

**Classification:** Contradicted / documentation and reproducibility drift

`web/package.json` keeps the Phase 1C ranges (`@tiptap/react` `^2.11.5`, AG Grid `^33.1.1`, Motion `^12.4.3`), but the current lockfile and installed tree resolve `@tiptap/react@2.27.2`, `ag-grid-enterprise@33.3.2`, `ag-grid-react@33.3.2`, and `motion@12.43.0`. The canonical governance register and ADRs still describe the older evaluated versions.

**Evidence:** `web/pnpm-lock.yaml:53-89,3064,3428,5348`; `web/packages/design-system/src/components/ZainXDataGrid/ZainXDataGrid.tsx:3,11`; `Ref/05_GOVERNANCE/DEPENDENCY_LICENSE_REGISTER.md:18`; `Ref/05_GOVERNANCE/PHASE_1C_DECISION_MATRIX.md:14,17,19`.

**Impact:** The current source is reproducibly installable, but the documented benchmark/license evidence no longer identifies the exact shipped versions. Re-run the approved spikes or update the governance records before a production release.

### B-018 AG Grid commercial/license gate is documented but not implemented in the wrapper

**Classification:** Incomplete / production gate risk

`ZainXDataGrid` imports and registers `AllEnterpriseModule` unconditionally. A repository search found no `LicenseManager` or `VITE_AG_GRID_LICENSE_KEY` usage in the frontend source. The unit run emitted the AG Grid trial watermark, while governance requires a secure production key and a graceful community-safe path before procurement.

**Evidence:** `web/packages/design-system/src/components/ZainXDataGrid/ZainXDataGrid.tsx:2-11`; `Ref/05_GOVERNANCE/AG_GRID_LICENSE_GATE.md:23-56`; Vitest output included the trial/license watermark.

**Impact:** Procurement remains a tracked commercial debt, but the current wrapper does not demonstrate the documented production injection/degraded-mode contract.

### B-019 Proposal read/cancel endpoints are not owner-scoped

**Classification:** Incomplete authorization boundary / likely IDOR

Proposal listing is scoped by `tenant_id` and `requested_by_user_id`, but `GetProposalByIdAsync` and the service methods used by GET and cancel query only `tenant_id` plus proposal ID. The cancel path also performs no owner or permission check before changing the proposal state.

**Evidence:** `src/Modules/Ai/Infrastructure/AiRepository.cs:628-646` (tenant-only GET), `:677-687` (user-scoped list), `src/Modules/Ai/Application/Services/AiProposalService.cs:130-150,361-390`, and `src/Modules/Ai/Api/AiController.cs:150-163,204-218`.

**Impact:** A caller who knows another proposal UUID and is in the same tenant may be able to read its snapshots/impact data or cancel it. Confirm the intended sharing policy; if proposals are personal by default, this is a release-blocking authorization defect.

### B-020 Proposal snapshots and impact are client-supplied, not server-derived or schema-validated

**Classification:** Incomplete integrity/truthfulness contract

`CreateProposalRequest` accepts `BeforeSnapshotJson`, `AfterSnapshotJson`, and `ImpactSummaryJson` directly. `AiProposalService` stores these values, and handlers later parse selected fields from `AfterSnapshotJson`; no server-side current-entity read, before-snapshot comparison, action input-schema validation, or impact recomputation was found in the proposal creation path.

**Evidence:** `src/Modules/Ai/Application/Contracts/AiProposalDtos.cs:7-18`; `src/Modules/Ai/Application/Services/AiProposalService.cs:78-87`; `src/Modules/Ai/Application/Actions/AiActionHandlers.cs:48-60,124-136,199-215`; the registered schemas are metadata in `AiActionDefinition` rather than a validation call.

**Impact:** The SHA-256 hash can prove that stored client/model-provided content was not changed after creation, but it cannot prove that the before/after/impact values describe the real target state. This is weaker than the canonical deterministic proposal contract and should be closed before sensitive actions are expanded.

### B-021 CI does not enforce the full release evidence set

**Classification:** Incomplete operational gate

`.github/workflows/ci.yml` runs backend build/test, frontend lint/typecheck/Vitest, Nx builds, and Storybook. It does not run the Phase 7B/6 Playwright suites, dependency audits, license/procurement checks, Lighthouse/performance budgets, or a real database/Worker integration environment.

**Evidence:** `.github/workflows/ci.yml` entire workflow; the Playwright configuration itself starts only `workforce-web` in `web/apps/e2e/playwright.config.ts:16-25`.

**Impact:** A green CI result cannot currently certify the release-critical browser, Worker, dependency, license, or performance claims. These checks must be explicit Phase 8 gates or added to the release pipeline.

### B-022 Frontend visual-token adoption is inconsistent across feature packages

**Classification:** UX/design-system drift, non-security

The rendered Home and Workforce AI shell is a real, polished responsive interface and follows the calm enterprise direction. Several feature packages still use one-off `bg-white`, `text-slate-*`, `bg-indigo-*`, `rounded-2xl`, and raw shadow classes instead of the semantic design tokens used by the shell; the Reports workspace is the clearest example.

**Evidence:** `web/apps/workforce-web/src/routes/__root.tsx:137-159` and `web/packages/design-system/src/styles/theme.css:25-104` establish the tokenized shell; `web/packages/reports/src/components/ReportsWorkspace.tsx:286-593` and `web/packages/platform/src/components/NotificationCenter.tsx:113-215` contain the raw feature styling.

**Impact:** This does not invalidate the current UI or accessibility test count, but it creates visual inconsistency and makes the “full attention to detail” requirement harder to maintain as more modules are built. Track as a P1/P2 design-system consolidation item, not a Phase 7B security blocker.

### B-023 Legacy unused frontend shell remains in source

**Classification:** Documentation/code hygiene

`web/apps/workforce-web/src/main.tsx` renders `RouterProvider`, while `web/apps/workforce-web/src/app/App.tsx` still contains an older health-only `App` component with inline system styling and a “System Status” message. It is not the current entry path, but it can confuse future implementation and review.

**Impact:** No current runtime impact was observed. Remove or clearly mark the dead shell during normal cleanup after the audit, without changing the active router behavior.

### B-024 Reports workspace fails its default browser integration path

**Classification:** Verified runtime integration defect in the current local configuration

The rendered Reports page loads its catalog, but running `ATTENDANCE_MONTHLY` from the browser shows “The governed report query could not be completed.” A direct request without headers returned HTTP 403 because the default `TenantResolutionMiddleware` permission set does not include `attendance.read`. The same request with `X-Permissions: attendance.read` returned HTTP 200 and two rows. `ReportsWorkspace` does not add that header; it relies on the authenticated user context that is currently not wired in the host.

**Evidence:** `web/packages/reports/src/components/ReportsWorkspace.tsx:154-213`; `src/Workforce.Host.Api/Middleware/TenantResolutionMiddleware.cs:158-185`; direct runtime checks: `GET /api/v1/reports` → 200, `POST /api/v1/reports/ATTENDANCE_MONTHLY/run` without permission → 403, same POST with `X-Permissions: attendance.read` → 200.

**Impact:** The current UI can look healthy while a normal operator cannot execute a report under the default local auth path. This is an integration/auth configuration issue, not evidence that report authorization should be weakened. It must be resolved by wiring real claims/session permissions or an explicit development profile before calling the full frontend operational flow complete.

### B-025 Current Phase 7A Playwright suite is not green

**Classification:** Contradicted

The existing `pnpm --dir web exec playwright test src/phase7-ai.spec.ts` run executed five Phase 7A tests and reported `3 passed` and `2 failed`. Flow A & D expected a Product Knowledge response containing `Product`, but the rendered response was the prompt-injection defense response. Flow C & E expected `Policy`, but the rendered response was a payroll access-denied response containing tool traces instead.

**Evidence:** `web/apps/e2e/src/phase7-ai.spec.ts:5-40,79-105`; actual Playwright output: `2 failed`, `3 passed (18.5s)`.

**Impact:** Phase 7A cannot be marked PASS on the current runtime. Because Phase 7B is explicitly gated on 7A, this is a direct dependency for the AI release track and must be explained/fixed or the tests must be corrected with evidence of the intended behavior.

## Evidence Log

- Current rendered UI captured read-only with Playwright Chromium at `1440x900` and `375x812` for `/` and `/ai`; Home and Workforce AI rendered successfully. No product files were changed by the capture.
- `web/apps/workforce-web/src/main.tsx` confirms the active entry renders `RouterProvider`; the legacy `src/app/App.tsx` is not the active shell.
- Installed dependency inspection: `ag-grid-enterprise@33.3.2`, `ag-grid-react@33.3.2`, `@tiptap/react@2.27.2`, `motion@12.43.0`; FullCalendar `6.1.15`, ECharts `6.1.0`, and dnd-kit `6.3.1` remain at the governed versions.
- `pnpm --dir web exec eslint .` — exit 0; no output.
- `pnpm --dir web exec tsc --noEmit -p apps/workforce-web/tsconfig.app.json` — exit 0; no output.
- `dotnet build Workforce.slnx --no-restore --configuration Release` — exit 0; 0 warnings, 0 errors.
- `dotnet test Workforce.slnx --no-build --configuration Release` — exit 0; 188 passed, 0 failed, 0 skipped.
- `pnpm --dir web exec nx build design-system-docs` — exit 0; 15 modules transformed; 60.30 kB gzip entry.
- `pnpm --dir web exec storybook build --config-dir apps/design-system-docs/.storybook -o dist/storybook/design-system-docs` — exit 0; Storybook completed, with large-chunk warnings.
- `pnpm audit --prod` from repository root — exit 1 because the root has no lockfile; `pnpm --dir web audit --prod` — exit 0, no known vulnerabilities.
- Runtime report check: `GET /api/v1/reports` returned 200; `POST /api/v1/reports/ATTENDANCE_MONTHLY/run` without permission returned 403; the same POST with `X-Permissions: attendance.read` returned 200 with two rows. This explains the visible Reports error in the current browser session.
- `pnpm --dir web exec playwright test src/phase7-ai.spec.ts` — exit 1; 5 tests executed, 3 passed, 2 failed, reported duration 18.5 s. Failures were Product Knowledge and Policy response-content assertions.

## P0 Security Boundary Remediation Evidence (2026-08-25)

The P0 remediation was implemented without changing business modules, Phase 7B behavior, or frontend code from this goal.

- API and Worker database startup now fail closed when neither an explicit configured connection string nor `ZAINX_DB_PASSWORD` is present; the prior hard-coded password fallback was removed.
- API migration startup now marks readiness failed and rethrows instead of continuing after a migration/database error. `/health/ready` reports migration and database connectivity checks after successful startup.
- Production requests without an approved authenticated principal return `401`, including requests carrying legacy client-controlled identity, tenant, legal-entity, or permission headers. Development/Test uses a fixed sandbox context; those headers are ignored as authority.
- CORS now requires explicit origins outside Development/Test and uses explicit headers/methods; `AllowAnyOrigin`, `AllowAnyHeader`, and `AllowAnyMethod` are absent from the API boundary.
- `dotnet build Workforce.slnx --no-restore --configuration Release` — exit 0; 0 warnings, 0 errors.
- `dotnet test Workforce.slnx --no-build --configuration Release` — exit 0; 192 passed, 0 failed, 0 skipped, reported duration 5 s.
- `pnpm --dir web exec eslint .` — exit 0; no output.
- `pnpm --dir web exec tsc --noEmit -p apps/workforce-web/tsconfig.app.json` — exit 0; no output.
- Runtime Development check on a temporary API port: `/health` 200, `/health/ready` 200, approved localhost CORS origin returned, unauthorized origin returned no CORS origin, and an injected `X-Permissions` value was not echoed into the session context.
- Runtime Production check with forged legacy headers: `/api/session/current` returned 401.
- Runtime Production check with an invalid database password: startup exited non-zero with `[MIGRATIONS] Startup failed; API will not serve requests`; no listener remained.
- Runtime Worker check without database configuration: startup exited non-zero with the same fail-closed database configuration error.

### B-026 Approved identity-provider integration remains external

The repository still has no configured/implemented production OIDC, Entra, AD/LDAP, or equivalent identity-provider adapter. This is intentionally not replaced with a fake provider. Until the approved provider is integrated and supplies server-issued subject, tenant-membership, legal-entity, permission, and entitlement claims, Production API business requests remain fail-closed with `401`. This is the remaining external deployment blocker, not a justification to trust request headers.

## Current Status Rating vs Market HCM Systems (2026-08-25)

This is a status assessment, not a claim that ZainX has feature parity with a mature commercial HCM suite.

- Engineering architecture: **8.0/10** — modular .NET host, PostgreSQL, Worker, OpenAPI contracts, tenant/legal-entity boundaries, design-system wrappers, and governed AI action contracts are unusually strong for this stage.
- Security/production readiness: **5.5/10** — the P0 boundary now fails closed and is directly verified, but the approved production identity provider is not integrated and Phase 8 operational/UAT gates are not complete.
- Frontend product quality: **6.5/10** — real routed application, responsive shell, Arabic/RTL and accessibility coverage exist; current production build still reports a `design-system-core` chunk of about 1.05 MB gzip, and test output contains AG Grid trial and React `act` warnings.
- Implemented product breadth: **5.0/10** — the repository contains broad module shells and tests, but the canonical roadmap still has major lifecycle areas and Phase 8 release evidence ahead; the current route tree has ten primary routes and no dedicated Documents or full employee-profile journey.
- Governed AI direction: **7.5/10** — explicit proposal/confirmation and bounded command principles are stronger than many early AI integrations; previous audit evidence still requires re-verification for proposal ownership/snapshot truthfulness and the non-green 7A response assertions.
- Overall current product readiness: **6.0/10** as a serious enterprise-HR foundation; **4.5/10** for parity with a mature market HCM product; **not yet production-ready**.

The main governance inconsistency is that the repository contains Phase 7A/7B implementation evidence while the accepted roadmap says Phase 2 is the next approved phase and Phase 8 remains the production gate. This is manageable if the work was intentionally parallelized, but status documents should be synchronized before a market-facing claim.

## Frontend 9.5/10 Continuation Findings

### F-001 Current worktree is still a mixed parallel state

The worktree contains frontend design-recovery changes alongside Phase 7B AI, backend security-boundary, generated-contract, test, and planning changes. This continuation must review rendered behavior and touched files without assuming every dirty file belongs to the same coherent change set.

### F-002 AI workspace still has legacy visual styling

`web/packages/ai/src/components/AiWorkspace.tsx` contains raw slate, indigo, violet, purple, and gradient classes, including a purple/indigo AI badge, indigo active states, and raw light/dark surface pairs. This conflicts with the approved ink/mineral/cyan ZainX visual system and is a direct P1 visual-drift target.

### F-003 Administration workspace still has legacy visual styling

`web/packages/administration/src/components/AdministrationWorkspace.tsx` uses raw white/slate/indigo/emerald/amber/red styling, repeated rounded-2xl cards, and one-off modal treatments. It is functionally present but visually inconsistent with the canonical Shell and shared design-system tokens.

### F-004 Semantic design tokens already exist and can be reused

`web/packages/design-system/src/styles/theme.css` defines semantic canvas, surface, text, border, primary, danger, success, warning, and info tokens with light/dark behavior. The first safe recovery action is to migrate feature styling to these existing tokens, preserving architecture and behavior.

### F-005 Current basic route geometry is healthy but not a full release gate

The previous route sweep confirmed one H1 and no horizontal overflow across ten routes at desktop/mobile sizes. It also reported non-zero small-target counts; the final audit must apply the inline-link exemption and fix genuine standalone controls before claiming the 9.5/10 gate.

### F-006 Bundle and test warnings remain release debt

The latest documented build passes, but the design-system core remains approximately 1.05 MB gzip and test output still contains React act, React Aria, ECharts zero-size, and AG Grid license warnings. These are not proof of failure by themselves, but they prevent a clean release-quality evidence claim.

### F-007 First implementation slice completed: AI and Administration token migration

`AiWorkspace.tsx` and `AdministrationWorkspace.tsx` were mechanically migrated from raw feature colors and dark-mode pairs to the existing semantic token vocabulary. Purple/indigo gradient treatment was removed, repeated oversized card radii were reduced to the canonical radius, and legacy shadow aliases were normalized. Functional handlers, API calls, authorization behavior, and component structure were preserved.

### F-008 AI support surfaces migrated with the workspace

The AI package support components, including proposal and launcher surfaces, were included in the same semantic token migration so the AI route does not reintroduce legacy styling through child components. One remaining cancelled-state slate opacity class was normalized manually in `ProposalCard.tsx`.

### F-009 Bundle isolation materially improved

The catch-all `design-system-core` manual chunk was removed from `web/apps/workforce-web/vite.config.ts`, route modulepreload hints were disabled, and `ZainXDataGrid` now lazy-loads `AgGridView`. Feature packages now import the grid wrapper directly instead of the enterprise barrel. The production build no longer emits the former approximately 1.05 MB gzip shared core or preloads Administration, Recruitment, AI, and Attendance on Home. Current evidence: initial JavaScript 116.55 kB gzip; CSS 13.78 kB gzip; Attendance route 5.49 kB gzip; AG Grid remains an explicit 506.68 kB gzip vendor exception.

### F-010 Remaining route families migrated to semantic tokens

Reports, Payroll, Attendance, Leave, Approvals, Recruitment, People, and the platform notification surface were migrated away from raw neutral and feature-palette utilities to the existing semantic surface, text, border, primary, and status tokens. A repository scan now returns no raw slate/gray/neutral/indigo/purple/violet/emerald/amber/blue/rose/red utility classes or `dark:` overrides in frontend TypeScript/TSX.

### F-011 Mobile locale access corrected

The language switch was previously hidden on mobile without an equivalent control in the drawer. The mobile Sidebar now exposes the locale switch. Browser evidence at 375×812 confirms Arabic sets `lang="ar"` and `dir="rtl"`, retains one H1, has no horizontal overflow, and produces no page errors.

### F-012 Release warnings remain explicit

The current frontend checks are green, but Vitest still reports existing React Aria/`act`, ECharts jsdom zero-size, backend-unavailable, and AG Grid trial-license warnings. The AG Grid warning remains a procurement issue. The route browser sweep also exercises the frontend with the API offline; data-backed routes correctly render truthful error/empty states, but a full production interaction seal still requires the approved API/Worker/PostgreSQL runtime.

## HCM Core Continuation Evidence — 2026-08-25

### HCM-001 Organization position contract and workspace completed

Organization now exposes tenant/legal-entity-scoped position list/create APIs, validates the owning organization unit, rejects hierarchy cycles, and supports organization-unit deactivation with persisted row-version concurrency. The workforce-web route `/organization` consumes the generated OpenAPI hooks and renders real units, positions, and locations with loading/error/empty states and an explicit create-position form; it does not fabricate master data.

### HCM-002 Silent context defaults removed

Attendance no longer substitutes a fixed legal entity when recording clocks or reading schedules. Leave no longer substitutes the development legal entity or a fixed employee identifier for leave types, balances, requests, or creation. Missing required context now returns an explicit 400 response; permissions are checked before repository access.

### HCM-003 Approval boundary hardened

Approval inbox, details, decisions, and cancellation now require explicit permissions and legal-entity context. Delegation/escalation tables and workflow behavior remain unimplemented roadmap debt and are not represented as complete.

### HCM-004 Session context switch is truthful

`POST /api/session/context` now validates tenant and legal-entity membership and returns `501 Not Implemented` until the approved identity provider can issue a secure refreshed token/session. The endpoint no longer returns a success message while changing nothing.

### HCM-005 Fresh validation

- API host build: 0 warnings, 0 errors.
- `HcmCoreAuthorizationTests`: 4 passed, 0 failed.
- Frontend ESLint and workforce-web TypeScript: passed.
- Vitest: 19 files / 121 tests passed.
- workforce-web production build: initial JavaScript 120.16 kB gzip, CSS 13.95 kB gzip; AG Grid vendor 506.68 kB gzip remains the explicit exception.
- design-system-docs build and Storybook build: passed.

These results now include a full PostgreSQL-backed Architecture.Tests pass. They do not replace the Worker-backed Playwright provenance seal, production identity-provider integration, UAT, backup/restore, or deployment evidence.

### HCM-006 Current browser execution and Attendance legal-entity isolation

- Updated the People lifecycle fixture to provide the now-required employee number and date of birth. The prior failure was a truthful modal validation response, not a product defect.
- Added legal-entity predicates to Attendance day detail, adjustment, approval, and get-or-create repository paths. The controller now requires legal-entity context before recording a clock event, before reading a day, and before mutating or approving a day.
- Reran the complete local Playwright collection: `Running 39 tests using 1 worker` -> `39 passed (1.3m)`, 0 failed.
- The full backend suite subsequently passed against PostgreSQL 18: 196 passed, 0 failed. The browser run remains local evidence; service provenance is not instrumented in the current Playwright configuration, so production identity provider, Worker/no-MSW topology proof, UAT, backup/restore, and deployment gates remain open.

### HCM-007 Tenancy and legal-entity foundation added

The platform now has explicit `platform.tenants` and `platform.legal_entities` persistence, tenant/legal-entity-scoped context and management contracts, development-only seed data, and generated frontend contracts. Production does not create an implicit tenant or legal entity. The secure identity-provider claims and context-refresh boundary remain open.

### HCM-008 Organization cost-center master data added

Organization now exposes tenant/legal-entity-scoped cost-center list/create contracts and a truthful `/organization` frontend workspace with loading, error, empty, and create states. The validation run created and removed one exact test record; no test fixture was retained. A dedicated end-to-end organization acceptance flow and branch model remain open.

### HCM-009 Current validation supersedes earlier counts

- Architecture.Tests with active PostgreSQL 18: 196 passed, 0 failed, 196 total.
- Frontend Vitest: 19 files, 121 tests passed.
- Local Chromium Playwright: `Running 39 tests using 1 worker` -> `39 passed (1.1m)`, 0 failed.
- Workforce-web production build: CSS 14.00 KB gzip, initial JavaScript 121.94 KB gzip, AG Grid vendor 506.68 KB gzip warning remains.

### HCM-010 ESS/MSS identity projection and self-service slice

- Added `people.user_employment_links` as an explicit, tenant/legal-entity-scoped relationship between an authenticated user and an authoritative employment record. User and Employee remain separate aggregates.
- Added permission-gated link/unlink management with active-link uniqueness, historical retention, and People outbox events for audit/delivery processing.
- Added `/api/v1/self-service/profile` read/update and `/api/v1/self-service/team` projection endpoints. Profile updates change only contact fields and require the employment row version; stale updates return `409`.
- Added the real `/me` frontend route with profile editing, direct-team projection, loading/error/empty states, English/Arabic support, and no fabricated employee fallback.
- Real PostgreSQL runtime evidence: unlinked profile `404`, link `201`, profile/team `200`, update `200`, stale update `409`, unlink `204`, and post-cleanup profile `404`; no active link remained.
- The historical link/unlink audit row was retained intentionally. The temporary contact values used for verification were removed from the exact test employee; the original pre-test contact values were not recoverable from the existing database history.

### HCM-011 People create-path overflow corrected

- The first full Playwright rerun after the ESS/MSS slice exposed 9 failures: creating an employee without an optional nationality returned PostgreSQL `22001` because `PeopleController` supplied `"Unspecified"` to `people.persons.nationality VARCHAR(10)`.
- Root cause was traced across the browser fixture, API mapping, and live PostgreSQL schema. The controller now preserves omitted nationality as an empty unavailable value, consistent with the no-fabricated-master-data rule and the existing schema/domain contract.
- The focused regression run passed **2/2** and the subsequent full local Chromium run passed **39/39 in 1.1m**. Architecture.Tests passed **196/196** after the correction.
- The approved brand source of truth remains `D:\Projects\ZainX_HR\Ref\03_DESIGN_SYSTEM\ZainX_HR_Brand_Kit_v1.1_APPROVED`; runtime artwork continues to use the copied, hash-verified assets from that directory.

### HCM-012 ESS/MSS operational projections — 2026-08-25

- Added a contract-first self-service operations boundary for the authenticated user's linked employment. Leave balances and recent requests are read-only projections over the existing Leave repository; Attendance today and clock operations reuse the existing Attendance domain path.
- The controller resolves the active user-to-employment link before querying or mutating, enforces permission and legal-entity context, and never accepts an arbitrary employee ID from the browser. Unlinked users receive the existing explicit `404 Employee Identity Link Required` boundary.
- Real PostgreSQL 18 runtime evidence: unlinked balances `404`; unlinked attendance `404`; link `201`; balances `200` with count `0`; requests `200` with total `0`; attendance before clock `204`; clock `200`; attendance after clock `200`. The exact clock event and day were removed after verification, and active links returned to `0`.
- Native Chromium evidence at `375x812` confirmed `/me` renders the daily operations section, attendance query status `200`, no horizontal overflow, and no console errors.
- OpenAPI sources were synchronized and Orval regenerated `useGetApiV1SelfServiceLeaveBalances`, `useGetApiV1SelfServiceLeaveRequests`, `useGetApiV1SelfServiceAttendanceToday`, and `usePostApiV1SelfServiceAttendanceClock`.
- Leave request submission was intentionally not added: a truthful manager approval route, approval authority, and manager fixture are not yet configured. Schedule/holiday/overtime/geofence policies and documents self-service lifecycle remain follow-up HCM work.

### HCM-013 Documents lifecycle, delegation, and reporting hardening — 2026-08-25

- Documents now enforces configured MIME/size/expiry policy, supports active expiry queries and archive, records access actions, and returns the requested version's own filename/content type during downloads.
- Self-service Documents is employment-scoped and read/download-only. It requires the current user's active People identity link and rejects arbitrary document ownership; self-service upload remains deferred.
- Approval decisions now require the assigned current approver (or an active delegation), while delegation is tenant/legal-entity scoped, expiry-aware, idempotent, and persisted in decision history. Production user-directory membership is still external.
- Reports now enforce explicit read/export permissions, legal-entity scope, requester/admin job ownership, and idempotent CSV export. Generic report fallback is an empty truthful result rather than fabricated rows.
- Runtime evidence against PostgreSQL 18: Documents upload/version/download/expiry/archive/access logs passed and cleaned; self-service documents unlinked 404, linked list 200, PDF download 200 with 2,696,352 bytes, and cleanup returned zero; delegation 201 then replay 200 with one row/history; reports returned six definitions, ten real headcount rows, zero generic fallback rows, and same export job ID on duplicate requests.
- OpenAPI source files were synchronized and Orval regenerated. Full validation: Architecture.Tests 196/196, Vitest 19 files/121 tests, ESLint, TypeScript, workforce-web build, design-system-docs, Storybook, and Playwright 39/39.
- Remaining blockers are production IdP/user-directory claims, Worker/no-MSW provenance, document upload/malware/retention production services, attendance policy depth, visual recovery, UAT, and release evidence. Leave escalation, cross-year policy, and cancellation balance release remain narrower HCM debts.

### HCM-014 Leave submission and Universal Approval integration — 2026-08-25

- Replaced direct Leave request persistence with `ILeaveRequestApplicationContract`. The service derives inclusive duration from server-validated dates and persists only after a configured balance is locked and pending days are reserved.
- Added the host composition boundary `ILeaveApprovalWorkflowStarter`. It resolves the current assignment's manager employment and requires an active People identity link for that manager; it never falls back to the requester or an administrator.
- Added ESS `POST /api/v1/self-service/leave/requests`; it never accepts an employment ID. It resolves the authenticated user's linked employment, starts a persisted Universal Approval request, and compensates by cancelling the workflow if Leave persistence fails.
- Added `IApprovalDecisionSideEffect` so final Universal Approval decisions update Leave and its balance in one Leave transaction. Approval confirms used days; rejection releases pending days. The former direct Leave approve/reject endpoints now return `409 Universal Approval Required`.
- Real PostgreSQL 18 runtime evidence passed with temporary, explicitly cleaned fixtures: submit returned `PendingApproval`; pending balance moved from `0` to `1`; approval produced `Approved`, used days increased by `2`, and pending returned to `0`; rejection released a one-day reservation; direct Leave approval returned `409`. Exact requests, approvals, histories/steps, outbox rows, balance, policy, identity links, and temporary manager assignment were removed/restored afterward.
- OpenAPI source documents were synchronized and Orval regenerated the self-service Leave type, submit, and response contracts. Validation: API build 0/0 warnings/errors, Architecture.Tests **196/196**, ESLint passed, Workforce TypeScript passed, Vitest **19 files / 121 tests**, and workforce-web production build passed.
