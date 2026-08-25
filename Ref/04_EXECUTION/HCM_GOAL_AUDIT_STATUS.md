# ZainX Workforce - HCM Goal Audit Status

**Review date:** 2026-08-25  
**Status:** ACTIVE CHECKPOINT - NOT HCM COMPLETE  
**Operating mode:** Restricted Audit Mode  
**Accepted project state:** Phase 0 PASS, Phase 1A PASS, Phase 1B PASS, Phase 1C PASS, Phase 2 APPROVED TO START

## 1. Outcome

This review improved the first HCM Core slice without starting downstream Phase 2 business behavior. The following areas are now stronger and have fresh automated evidence:

- People creation no longer invents employee numbers, dates, national identifiers, nationality, or location values when the request is incomplete.
- People, Organization, and Documents API actions now enforce explicit permissions and legal-entity scope before repository access or mutation.
- Organization unit and location reads are filtered by the active legal entity when one is present in the user context.
- Documents upload and download use the corrected multipart and binary OpenAPI contracts and the real frontend service wiring.
- Administration and recruitment surfaces no longer display fabricated fallback records when the service is unavailable.
- The approved Zain X HR Brand Kit is wired into the application mark, dark-surface mark, favicon, and Apple touch icon.
- Organization positions now have real tenant/legal-entity-scoped list/create contracts and a dedicated frontend workspace; organization-unit deactivation and hierarchy-cycle checks are also enforced.
- Tenancy now has explicit tenant/legal-entity tables, scoped context and legal-entity management contracts, with development-only seed data and no production implicit tenant creation.
- Organization now has tenant/legal-entity-scoped cost-center contracts and a truthful frontend cost-center workspace alongside units, positions, and locations.
- ESS/MSS now has an explicit user-to-employment link projection, self-service profile read/update with optimistic concurrency, manager team projection, auditable link/unlink events, generated contracts, and a `/me` frontend workspace.
- ESS/MSS now also exposes contract-first, employment-scoped Leave balance/request projections and Attendance today/clock operations through a composite self-service boundary; the `/me` workspace consumes real generated queries and clock mutation state without fabricated rows.
- Documents now has real type-policy validation, expiry querying, version-aware download, archive, access logging, and an employment-scoped self-service list/download projection; self-service upload remains intentionally outside the employee boundary.
- Approvals now has current-approver enforcement plus tenant/legal-entity-scoped delegation with idempotent replay behavior and auditable delegation history.
- Reporting now enforces report/export permissions and legal-entity scope, removes fabricated generic rows, and preserves durable export idempotency and requester ownership.
- A full browser rerun exposed and then resolved a People create-path defect: an omitted nationality was being mapped to `"Unspecified"`, which exceeded the persisted `VARCHAR(10)` field. The corrected path preserves the value as unavailable and no longer invents master data.
- Attendance and Leave no longer silently substitute a development legal entity or employee identifier when required context is missing.
- Approval inbox, details, decisions, and cancellation now have explicit permission and legal-entity boundaries; cancellation is limited to the requester or administrator.
- Leave cancellation now has two explicit paths: pending requests cancel through the linked Universal Approval workflow and release reserved days; approved requests cancel through the Leave application contract and reverse used days. Both paths write auditable balance transactions and preserve stale-row-version `409` behavior.
- Session context switching now validates membership and reports `501 Not Implemented` until a secure identity-provider token/session refresh mechanism is configured; it no longer claims a context change succeeded.

This is a verified recovery checkpoint, not a release seal for the whole HCM platform.

## 2. HCM Evidence Matrix

| Area | Current evidence | Status | Remaining proof or debt |
| --- | --- | --- | --- |
| Tenancy and legal entities | `platform.tenants` and `platform.legal_entities`, scoped context/read/manage contracts, development-only seed path, generated frontend contracts | Core foundation added | Production IdP claims, secure context refresh, and repeatable deployment bootstrap remain open |
| Organization | Units, locations, positions, cost centers, hierarchy validation, deactivation concurrency, frontend workspace, legal-entity filters, explicit authorization guards | Core improved | Dedicated DB-backed end-to-end acceptance flow, branch model, and production role evidence remain open |
| People | Canonical person/employment/assignment model, directory/profile UI, document summary, sensitive reveal audit path, truthful create validation | Core improved | Full employee lifecycle and cross-legal-entity acceptance remain open; ESS/MSS now has a first explicit identity-link slice |
| ESS/MSS | Explicit `people.user_employment_links`, self profile/contact update, manager team projection, link/unlink audit events, contract-first Leave submit/decision workflow with balance reservation, Attendance today/clock operations, employment-scoped document list/download, and `/me` route | Operational HCM slice | Production IdP claim provisioning, self-service upload, escalation policy, and manager approval action depth remain open |
| Documents | Secure storage/type policy validation, expiry query, version-aware multipart upload/blob download, archive, access logs, and real self-service list/download projection | Lifecycle checkpoint | Owner policy matrix by module, malware scanning, retention execution, and production storage lifecycle remain open |
| Attendance, Leave, Approvals | Existing modules plus context/permission hardening, legal-entity filtering, self-service Attendance today/clock operations, contract-first Leave submission with per-year balance reservation, pending/approved cancellation with auditable balance transactions, Universal Approval decisions, current-approver authorization, delegation idempotency/audit, and frontend test coverage | Leave request → manager approval/rejection/cross-year/cancellation paths verified | Escalation policy, schedule/overtime/holiday calculation, accrual/adjustment/year-close workflows, and a DB-backed no-MSW integration seal remain open |
| Payroll, Settlement, Compliance | Existing Egypt-first modules, permission boundaries, calculation/finalization controls, and test coverage are present | Existing implementation | Production payroll closeout, statutory evidence, bank/export operations, and UAT remain open |
| Recruitment and onboarding transition | Existing requisition/candidate/interview/offer/hire paths and frontend surfaces are present; fake interview data removed | Existing implementation | Full hire-conversion and employee-master handoff must be proven with PostgreSQL and worker services |
| Talent | No evidence that Performance, Learning, Compensation, or Succession are complete business modules | Roadmap debt | Do not represent these as live HCM capabilities until contracts and workflows exist |
| Integrations, notifications, audit | Modules and Phase 6/7 code are present, including durable worker paths in the dirty worktree | Existing implementation | Production IdP, worker, outbox, external connector, audit-store, and delivery evidence are not sealed here |
| Frontend quality | 19 Vitest files and 121 tests pass, production builds pass, Storybook passes, approved artwork is wired, and the local Chromium suite is 39/39 | Strong checkpoint | Visual recovery and production-service proof remain open; the local run is not a production release seal |

## 3. Exact Review-Scope Files Changed

The worktree was already materially dirty before this audit. The list below is the review-scope change set, not a claim that every other dirty file was created by this review.

### Backend and security boundary

- `src/Modules/People/Api/PeopleController.cs`
- `src/Modules/People/Api/SelfServiceController.cs`
- `src/Modules/People/Application/PeopleDtos.cs`
- `src/Modules/Organization/Api/OrganizationController.cs`
- `src/Modules/Organization/Application/OrganizationDtos.cs`
- `src/Modules/Organization/Domain/CostCenter.cs`
- `src/Modules/Organization/Domain/OrganizationUnit.cs`
- `src/Modules/Organization/Domain/Position.cs`
- `src/Modules/Organization/Infrastructure/OrganizationMigrations.cs`
- `src/Modules/Organization/Infrastructure/OrganizationRepository.cs`
- `src/Modules/People/Infrastructure/PeopleMigrations.cs`
- `src/Modules/People/Infrastructure/PeopleRepository.cs`
- `src/Modules/Attendance/Api/AttendanceController.cs`
- `src/Modules/Attendance/Infrastructure/AttendanceRepository.cs`
- `src/Modules/Attendance/Application/Contracts/IAttendanceSelfServiceContract.cs`
- `src/Modules/Attendance/Application/Services/AttendanceSelfServiceService.cs`
- `src/Modules/Leave/Api/LeaveController.cs`
- `src/Modules/Leave/Infrastructure/LeaveRepository.cs`
- `src/Modules/Leave/Application/Contracts/ILeaveSelfServiceQueryContract.cs`
- `src/Modules/Leave/Application/Services/LeaveSelfServiceQueryService.cs`
- `src/Modules/Leave/Application/Services/LeaveActionService.cs`
- `src/Modules/Leave/Application/Contracts/ILeaveActionContract.cs`
- `src/Modules/Leave/Application/Contracts/ILeaveRequestApplicationContract.cs`
- `src/Modules/Leave/Application/Contracts/ILeaveSelfServiceQueryContract.cs`
- `src/Modules/Leave/Domain/LeaveBalance.cs`
- `src/Modules/Leave/Infrastructure/LeaveMigrations.cs`
- `src/Modules/Approvals/Api/ApprovalsController.cs`
- `src/Modules/Approvals/Application/Contracts/IApprovalDecisionSideEffect.cs`
- `src/Workforce.Host.Api/Application/LeaveApprovalDecisionSideEffect.cs`
- `src/Workforce.Host.Api/Controllers/SelfServiceOperationsController.cs`
- `src/Workforce.Host.Api/Middleware/TenantResolutionMiddleware.cs`
- `src/Modules/Identity/Infrastructure/AdministrationMigrations.cs`
- `web/tooling/openapi/workforce.openapi.json`
- `web/packages/contracts/workforce.openapi.json`
- `web/packages/contracts/src/api/generated.ts`
- `web/packages/contracts/src/api/generated.schemas.ts`
- `src/Modules/Approvals/Api/ApprovalsController.cs`
- `src/Modules/Approvals/Infrastructure/ApprovalsRepository.cs`
- `src/Workforce.Host.Api/Controllers/SessionController.cs`
- `src/Workforce.Host.Api/Program.cs`
- `src/Workforce.Host.Api/Workforce.Host.Api.csproj`
- `src/Modules/Documents/Api/DocumentsController.cs`
- `src/Modules/Documents/Infrastructure/DocumentsMigrations.cs`
- `src/Modules/Documents/Infrastructure/DocumentsRepository.cs`
- `src/Modules/Approvals/Infrastructure/ApprovalsMigrations.cs`
- `src/Modules/Approvals/Infrastructure/ApprovalsRepository.cs`
- `src/Modules/Reporting/Api/ReportsController.cs`
- `src/Modules/Reporting/Infrastructure/ReportingRepository.cs`
- `tests/Architecture.Tests/Phase7AiOperationalControlTests.cs`
- `src/Workforce.Host.Api/Middleware/TenantResolutionMiddleware.cs`
- `src/Workforce.Host.Api/Controllers/SelfServiceOperationsController.cs`
- `src/Modules/Ai/Application/Tools/LeaveAiTools.cs`
- `src/Modules/Tenancy/Api/TenancyController.cs`
- `src/Modules/Tenancy/Application/TenancyDtos.cs`
- `src/Modules/Tenancy/Domain/Tenant.cs`
- `src/Modules/Tenancy/Domain/LegalEntity.cs`
- `src/Modules/Tenancy/Infrastructure/TenancyMigrations.cs`
- `src/Modules/Tenancy/Infrastructure/TenancyRepository.cs`
- `src/Modules/Tenancy/Workforce.Modules.Tenancy.csproj`
- `tests/Architecture.Tests/HcmCoreAuthorizationTests.cs`
- `tests/Architecture.Tests/Architecture.Tests.csproj`

### Frontend, contracts, and truthful data handling

- `web/apps/workforce-web/index.html`
- `web/apps/workforce-web/src/routes/people.tsx`
- `web/apps/workforce-web/src/routes/organization.tsx`
- `web/apps/workforce-web/src/routes/me.tsx`
- `web/apps/workforce-web/src/routes/router.ts`
- `web/apps/workforce-web/src/routes/__root.tsx`
- `web/apps/workforce-web/src/routes/router.ts`
- `web/apps/workforce-web/src/routes/__root.tsx`
- `web/apps/workforce-web/src/routes/index.tsx`
- `web/packages/administration/src/components/AdministrationWorkspace.tsx`
- `web/packages/recruitment/src/components/InterviewCalendar.tsx`
- `web/packages/people/src/components/CreateEmployeeModal/CreateEmployeeModal.tsx`
- `web/packages/people/src/components/DocumentsTab/DocumentsTab.tsx`
- `web/packages/people/src/components/EmployeeProfile/EmployeeWorkspace.tsx`
- `web/packages/people/src/tests/hcm-master-data-input.spec.tsx`
- `web/apps/workforce-web/src/routes/organization.tsx`
- `web/tooling/openapi/workforce.openapi.json`
- `web/packages/contracts/workforce.openapi.json`
- `web/packages/contracts/src/api/generated.ts`
- `web/packages/contracts/src/api/generated.schemas.ts`
- `web/apps/e2e/src/phase6-operational-control.spec.ts`

### Approved brand assets and brand components

- `web/packages/design-system/src/components/BrandMark/BrandMark.tsx`
- `web/packages/design-system/src/components/BrandAssembly/BrandAssembly.tsx`
- `web/apps/workforce-web/public/brand/logos/zainx-hr-mark.webp`
- `web/apps/workforce-web/public/brand/logos/zainx-hr-mark-white.png`
- `web/apps/workforce-web/public/brand/logos/zainx-hr-primary-lockup.webp`
- `web/apps/workforce-web/public/brand/logos/zainx-hr-wordmark.webp`
- `web/apps/workforce-web/public/brand/pwa/favicon.ico`
- `web/apps/workforce-web/public/brand/pwa/apple-touch-icon.png`
- `web/apps/workforce-web/public/brand/icons/zainx-hr-app-icon-approved.png`

The source of these artwork files is the approved reference directory:
`D:\Projects\ZainX_HR\Ref\03_DESIGN_SYSTEM\ZainX_HR_Brand_Kit_v1.1_APPROVED`.

## 4. Validation Evidence

| Check | Result |
| --- | --- |
| `dotnet build src/Workforce.Host.Api/Workforce.Host.Api.csproj --no-restore` using an isolated temporary output directory | PASS, 0 warnings, 0 errors after current HCM changes |
| `dotnet test tests/Architecture.Tests/Architecture.Tests.csproj --no-restore --filter FullyQualifiedName~HcmCoreAuthorizationTests` | PASS, 4 passed, 0 failed |
| `dotnet test tests/Architecture.Tests/Architecture.Tests.csproj --no-restore` with the active PostgreSQL 18 runtime connection | PASS after the cross-year Leave correction, 197 passed, 0 failed, 197 total |
| `pnpm --dir web exec eslint .` | PASS |
| `pnpm --dir web exec tsc --noEmit -p apps/workforce-web/tsconfig.app.json` | PASS |
| `pnpm --dir web exec vitest run` | PASS, 19 files, 121 tests |
| `pnpm --dir web exec playwright test` | PASS after the People nationality correction, 39 tests using 1 worker; 39 passed, 0 failed, 1.1m; local Chromium evidence only |
| `pnpm --dir web exec nx build workforce-web` | PASS; CSS 14.02 KB gzip, initial JS 123.72 KB gzip; AG Grid vendor 506.68 KB gzip warning remains |
| `pnpm --dir web exec nx build design-system-docs` | PASS |
| `pnpm --dir web exec storybook build --config-dir apps/design-system-docs/.storybook` | PASS; known iframe chunk warning remains |
| `pnpm --dir web exec orval --config ./tooling/openapi/orval.config.ts` | PASS |
| `dotnet build src/Workforce.Host.Api/Workforce.Host.Api.csproj --no-restore -p:OutDir=artifacts\validation-hcm-leave-cancel-1` | PASS, 0 warnings, 0 errors |
| `pnpm --dir web exec orval --config ./tooling/openapi/orval.config.ts` after Leave cancellation contract synchronization | PASS; generated approved Leave cancellation hook and optional approval-cancellation reason |
| `dotnet build src/Workforce.Host.Api/Workforce.Host.Api.csproj --no-restore -p:OutDir=artifacts\\validation-ess-mss\\` | PASS, 0 warnings, 0 errors |
| `dotnet build src/Workforce.Host.Api/Workforce.Host.Api.csproj --no-restore -p:OutDir=artifacts\\validation-ess-ops\\` | PASS, 0 warnings, 0 errors |
| `dotnet build src/Workforce.Host.Api/Workforce.Host.Api.csproj --no-restore -p:OutDir=artifacts\\validation-self-documents\\` | PASS, 0 warnings, 0 errors |
| Real Documents lifecycle runtime check against PostgreSQL 18 | PASS: upload 201; version 1 download 200; replacement version 2; expiring query included the document; archive 204; access logs 4; exact document/version/log/storage cleanup returned 0 |
| Real Approvals delegation runtime check against PostgreSQL 18 | PASS: first delegation 201; idempotent replay 200 with the same delegation ID; delegation count 1; history count 1; exact request cascade cleanup returned 0 |
| Real Reporting runtime check against PostgreSQL 18 | PASS: catalog 6; headcount status code 118 with 10 real rows; generic Attendance fallback 0 rows/0 total; duplicate CSV exports 202 with the same job ID; completed storage artifact; exact job/storage cleanup completed |
| Self-service Documents runtime check against PostgreSQL 18 | PASS: unlinked list 404; link 201; list 200 with real document; self-service PDF download 200 / 2,696,352 bytes / `application/pdf`; access logs 2; unlink 204; exact document/version/log/storage cleanup returned 0 |
| `pnpm --dir web exec orval --config ./tooling/openapi/orval.config.ts` after Documents/Approvals additions | PASS; generated self-service Documents list/download, archive, expiry, and delegation hooks |
| Real ESS/MSS runtime check against PostgreSQL 18 | PASS: unlinked profile 404; link 201; profile/team 200; profile update 200; stale update 409; unlink 204; post-cleanup profile 404; active links 0; 9 actor-scoped self-service audit rows retained by design |
| Real ESS operational runtime check against PostgreSQL 18 | PASS: unlinked balances 404; unlinked attendance 404; link 201; balances 200 with real count 0; requests 200 with real total 0; attendance before clock 204; clock 200; attendance after clock 200; exact clock/day cleanup completed; active links 0 |
| Real ESS Leave submit/approval runtime check against PostgreSQL 18 | PASS: linked self-service types 200; submit 201 with `PendingApproval`; pending balance `0 → 1`; Universal Approval approve produced Leave `Approved`, used `2`, pending `0`; rejection released pending `1 → 0`; direct Leave approve returned 409; exact requests/approvals/outbox/fixtures cleaned and manager assignment restored |
| Real PostgreSQL 18 Leave cancellation runtime check | PASS: pending approval cancellation returned 200 and moved pending `2 → 0` with `CancelPending`; approved Leave cancellation returned 200 and moved used `2 → 0` with `CancelApproved`; stale direct cancellation returned 409; exact transactions, requests, approvals, outbox, balance, policy, type, links, and manager assignment cleanup verified |
| Real PostgreSQL 18 cross-year Leave runtime check | PASS: `2026-12-31` → `2027-01-02` reserved `1`/`2`, approved to used `1`/`2`, approved cancellation returned used to `0`/`0`; pending `4`/`1` cancellation returned pending to `0`/`0`; per-year transaction rows verified; exact cleanup returned `0|0|0|0` |
| `dotnet build src/Workforce.Host.Api/Workforce.Host.Api.csproj --no-restore` after cross-year Leave implementation | PASS, 0 warnings, 0 errors |
| Native Chromium `/me` browser check at 375x812 | PASS: page 200; Daily Operations visible; attendance query 200; no horizontal overflow; no console errors; exact clock/day test records cleaned up |
| `git diff --check` on the review-scope files | PASS; Git reported normal LF/CRLF normalization warnings only |

## 5. Known Inconsistencies and Non-Blocking Debt

1. The full backend suite now passes against the active PostgreSQL 18 container using a runtime `ZAINX_DB_CONNECTION`. CI and deployment validation still need to provision the same database dependency explicitly rather than relying on a developer workstation.
2. `MODULE_START_GATE_ORGANIZATION.md`, `MODULE_START_GATE_PEOPLE.md`, and `MODULE_START_GATE_DOCUMENTS.md` are now correctly marked `IMPLEMENTATION CHECKPOINT — NOT RELEASE SEALED`; they are not evidence that the modules are production-complete. The generic `MODULE_START_GATE.md` remains a reusable pre-implementation template and should not be read as a live module status.
3. The canonical engineering blueprint already selects Egypt-first payroll and compliance, and the Compliance module seeds Egypt rules. A generic instruction to select a country is therefore already resolved by the higher-priority source of truth.
4. The approved brand source is exact raster artwork, not a native vector master. The application now uses the approved raster assets and does not claim that an official SVG master exists.
5. The production build still reports the approved AG Grid vendor chunk at approximately 506.68 KB gzip. Route-level isolation exists, but the remaining budget exception needs a documented optimization or procurement decision.
6. The local development middleware grants a broad development context, including `admin`. This is useful for local smoke testing but is not evidence of production identity-provider role mapping.
7. The frontend full unit/accessibility suite is green, but existing warnings remain around React `act`, React Aria press responders, and ECharts zero-size test containers. They are not test failures but should be cleaned up before the final UI quality seal.
8. The current local Playwright suite was rerun after the People HCM form, Attendance scope, Tenancy, Organization, and ESS/MSS changes: `Running 39 tests using 1 worker` -> `39 passed (1.1m)`, 0 failed. API readiness, PostgreSQL 18 health, and an active Worker process were also observed locally, but the Playwright configuration does not instrument service provenance; a production-equivalent no-MSW topology seal is therefore still open.
9. Performance and visual recovery are not complete across every HCM route. The build is healthy, but page-family redesign, approved typography delivery, and route-by-route visual evidence remain future work.
10. `web/packages/contracts/rest-generated` is a separate legacy generated client and was not regenerated by the Orval command used by the active workforce-web route. Its consumption must be confirmed before it is treated as a production contract source.
11. The canonical approved artwork source is `D:\Projects\ZainX_HR\Ref\03_DESIGN_SYSTEM\ZainX_HR_Brand_Kit_v1.1_APPROVED`; it is raster artwork plus reusable source snippets, not an official SVG master. The app uses copied, hash-verified runtime assets from that directory.
12. The current context-switch endpoint intentionally returns `501` after authorization validation until the approved production identity provider can issue a refreshed secure token/session. This is a truthful capability boundary, not a failed authorization check.
13. ESS/MSS link/unlink and profile concurrency were verified against the real database and the active link was removed afterward. Nine actor-scoped self-service audit rows from the repeated runtime verification remain by design. The temporary contact values used during verification were removed from the exact test employee; the original pre-test contact values were not available in the database history and must be supplied by data-owner review if that fixture is considered authoritative.
14. The first post-ESS full browser rerun found 9 failures caused by the omitted-nationality SQL overflow described above. The controller now stores an empty unavailable value instead of `"Unspecified"`; the focused regression run passed 2/2 and the subsequent full browser run passed 39/39.
15. ESS Leave submission now uses linked employment identity, manager-identity resolution, a persisted Universal Approval workflow, atomic per-year balance reservation, and auditable pending/approved cancellation reversal. Cross-year segmentation is implemented and runtime-verified; escalation, accrual/adjustment/year-close policy, and production IdP claims remain open.
16. Attendance clock operations currently reuse the existing day evaluation path. Schedule, holiday, overtime, geofence/device policy, accrual calculation, and manager correction/approval integration remain unsealed HCM behavior.
17. Documents self-service is intentionally read/download only. Employee upload, document-owner policy per module, malware scanning, retention worker execution, and production object storage are not claimed complete.
18. Delegation validates the current approver and persists idempotent history, but production target-user membership still depends on the approved IdP/user-directory boundary; the local development context cannot prove directory membership.
19. Reporting is now scoped and fake-row-free with durable idempotency evidence. Worker provenance, external queue execution, and no-MSW production-equivalent evidence remain open.
20. The Phase 6 browser test previously described the sandbox as read-only. The current fixed development context is explicitly admin; the test now accepts and verifies either a governed 403 for a restricted identity or an explicit successful export notice for the sandbox admin.

## 6. Next Gate

The next allowed gate is **HCM Core Integration Seal**, not Phase 3 or Phase 8 release closure. It requires:

1. Provision PostgreSQL 18 through the repeatable CI/deployment harness and retain the 197/197 full-suite evidence there.
2. Run the real API, workforce-web, Worker, and Playwright suite together with no MSW.
3. Verify employee create, assignment change, document upload/download/versioning, cross-tenant denial, cross-legal-entity denial, sensitive PII reveal audit, and recruitment hire conversion.
4. Reconcile the three stale module-gate statuses using the resulting evidence.
5. Continue frontend visual recovery from the approved Brand Kit without changing backend contracts or authorization behavior.
6. Provision production identity claims for `self.profile.read`, `self.profile.update`, and `self.team.read`, then repeat ESS/MSS browser evidence with a real linked employee and manager/team fixture.

Until those gates pass, the correct project status is **HCM Core implementation checkpoint, not final HCM release**.

## HCM Core Continuation — ESS/MSS operational projections

- Added `IAttendanceSelfServiceContract` and `AttendanceSelfServiceService` behind the Attendance bounded context. The service records server-timestamped clock events for the authenticated user's linked employment and returns the persisted day projection; it does not accept an arbitrary employee identifier.
- Added `ILeaveSelfServiceQueryContract` and `LeaveSelfServiceQueryService` as read-only projections over the existing Leave repository. No duplicate balances or request storage was introduced, and empty results remain truthful empty results.
- Added `SelfServiceOperationsController` as a host-level composition boundary for Leave balances/requests and Attendance today/clock. It resolves the user-to-employment link first, enforces permissions and legal-entity context, and returns the existing explicit 404 identity-link boundary when no mapping exists.
- Registered the new contracts, extended development-only permissions, synchronized both OpenAPI source documents, regenerated Orval contracts, and added the responsive bilingual `/me` operational cards for attendance and leave.
- Real PostgreSQL 18 runtime evidence: unlinked balances `404`, unlinked attendance `404`, link `201`, balances `200` with count `0`, requests `200` with total `0`, attendance before clock `204`, clock `200`, attendance after clock `200`; the exact clock event and attendance day were deleted afterward and active links returned to `0`.
- Native Chromium evidence at `375x812`: `/me` returned `200`, the operational section rendered, attendance query returned `200`, there was no horizontal overflow, and no console errors were observed.
- Full post-slice validation: Architecture.Tests **196/196**, Vitest **121/121**, ESLint passed, Workforce TypeScript passed, workforce-web build passed, design-system-docs build passed, Storybook build passed, and local Playwright **39/39 in 1.1m**.
- This remains an implementation checkpoint. Leave submission/decision is now implemented and runtime-verified; documents self-service lifecycle, delegation/escalation, production IdP claims, and service-provenance release evidence remain open.

## HCM Core Continuation — Documents, delegation, and reporting — 2026-08-25

- Documents now validates configured type policy on upload and replacement (MIME, size, and required expiry), records upload/replace/download/archive access actions, supports active expiring-document queries, selects the requested version's metadata during binary download, and archives through a tenant/legal-entity-scoped transaction.
- Self-service Documents reads only the current user's linked employee-owned documents and downloads only a document attached to that exact employment. It records `self-service-download`; arbitrary document IDs and unlinked identities do not cross the boundary.
- Approvals now enforces current-step approver authorization for decisions and supports tenant/legal-entity-scoped delegation with expiry, active-target inbox projection, idempotent replay, and one persisted delegation history record. Target-user membership remains an external directory responsibility until IdP integration is available.
- Reporting now applies explicit reports/export permissions, legal-entity predicates, requester/admin job visibility, CSV-only durable exports, and idempotency-key replay. The generic report fallback returns an empty truthful result instead of manufactured operational rows.
- Exact PostgreSQL 18 runtime evidence was captured and cleaned for Documents, self-service Documents, delegation, and Reports. OpenAPI sources were synchronized and Orval regenerated after the new paths.
- Current validation remains green: Architecture.Tests **196/196**, Vitest **19 files / 121 tests**, ESLint, workforce-web TypeScript, workforce-web build, design-system-docs build, Storybook build, and full Chromium Playwright **39/39 in 1.1m**.
- This remains an implementation checkpoint. Self-service upload, production IdP/user-directory membership, Worker/no-MSW provenance, retention/malware scanning, visual recovery, UAT, and release gates remain open.

## HCM Core Continuation — Leave cancellation and auditable balance history — 2026-08-25

- Added `leave.leave_transactions` as the auditable balance movement trail while retaining mutable counters as query projections. Submission, approval, rejection, pending cancellation, and approved cancellation now record before/after used and pending values with actor and reason metadata.
- Pending Leave cancellation is coordinated through the linked Universal Approval cancellation boundary. The Leave side effect changes the Leave request to `Cancelled`, releases reserved pending days, writes `LeaveCancelled`, and remains requester/admin scoped at the approval boundary.
- Approved Leave cancellation is exposed through `ILeaveActionContract` and the Leave API. It locks the request and balance in one transaction, reverses used days, writes `CancelApproved`, emits `LeaveCancelled`, and refuses direct cancellation of pending requests so approval ownership is preserved.
- OpenAPI sources and Orval output now include the Leave cancellation endpoint and the optional approval-cancellation reason. The AI Leave cancellation action continues to route through the existing application contract and therefore cannot silently bypass authorization or concurrency checks.
- Real PostgreSQL 18 runtime evidence: pending cancellation returned `200`, Leave moved to `Cancelled`, pending balance returned `2 → 0`, and `CancelPending` was persisted; approved cancellation returned `200`, used balance returned `2 → 0`, and `CancelApproved` was persisted; stale cancellation returned `409`. Exact temporary records were removed and the manager assignment restored.
- Validation: API build **0 warnings / 0 errors**; Architecture.Tests **196/196**; ESLint passed; workforce-web TypeScript passed; Vitest **19 files / 121 tests**; workforce-web production build passed with initial JS **124.33 KB gzip**, CSS **14.02 KB gzip**, and the documented AG Grid **506.68 KB gzip** exception; full local Chromium Playwright **39/39 in 1.2m**. OpenAPI/Orval generation passed.

## HCM Core Continuation — Cross-year Leave segmentation — 2026-08-25

- Added inclusive `LeaveYearSegment` calculation to the Leave domain so a request spanning calendar years is split into one segment per year.
- Leave submit, approve, reject/release, pending cancellation, and approved cancellation now lock and update one configured balance per segment year and record one auditable transaction per year with `balance_year`.
- The transaction migration adds `balance_year` and replaces the request/type uniqueness key with request/type/year so cross-year lifecycles retain one trail row per operation and year without weakening idempotency.
- Removed the obsolete application-service guard that rejected cross-year requests before the new segmentation path could run. Each segment still requires a tenant/legal-entity-scoped configured balance.
- Real PostgreSQL 18 runtime evidence passed with temporary linked ESS identity and manager assignment: `2026-12-31` → `2027-01-02` reserved `1`/`2` days, approval used `1`/`2`, approved cancellation returned both used projections to zero, and a second pending cross-year request (`4`/`1`) was cancelled through Universal Approval and returned both pending projections to zero.
- Per-year transaction evidence contained the expected `ReservePending`, `Approve`, `CancelApproved`, and `CancelPending` rows. Exact temporary requests, approvals, outbox messages, balances, policy, leave type, identity link, and manager assignment cleanup returned `0|0|0|0`.
- Added the cross-year domain test. Current API build is **0 warnings / 0 errors** and Architecture.Tests pass **197/197**.
