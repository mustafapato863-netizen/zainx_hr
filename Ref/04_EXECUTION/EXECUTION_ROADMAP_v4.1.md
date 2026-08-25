# ZainX Workforce — Integrated Execution Roadmap v4.1

Each phase includes backend, DB, contracts, frontend, design system, testing, security/operations and a gate.

## Current Accepted State — 2026-08-25

- Phase 0: **PASS**
- Phase 1A: **PASS**
- Phase 1B: **PASS**
- Phase 1C: **PASS / CLOSED**
- Phase 2 — Organization + People + Documents: **APPROVED TO START**
- Operating mode: **Restricted Audit Mode**
- Current implementation checkpoint: [`HCM_GOAL_AUDIT_STATUS.md`](./HCM_GOAL_AUDIT_STATUS.md)
- Production release: **NOT SEALED**; identity provider, database-backed integration, Worker/Playwright evidence, UAT, backup/restore and deployment gates remain open.

---

# Phase 0 — Full Product Foundation

## Backend
- create/validate .NET solution structure
- API Host
- Worker Host
- SharedKernel / BuildingBlocks
- module shell convention
- ProblemDetails baseline
- configuration/secrets convention
- OpenTelemetry baseline
- background-job abstraction
- outbox baseline architecture
- architecture tests

## Database
- PostgreSQL 18 local/dev baseline
- migration infrastructure
- schema-per-module convention
- migration ordering/runner
- integration-test database strategy
- backup/restore smoke procedure skeleton

## Frontend
- `/web` Nx + pnpm workspace
- React/Vite/TypeScript strict
- route shell
- lint/typecheck/test/build
- frontend package boundary rules

## Contracts
- OpenAPI generation pipeline
- Orval generation spike
- standard error envelope / ProblemDetails
- permission ID conventions
- async-job contract draft

## Quality / Ops
- CI
- Docker Compose developer stack
- Nginx/reverse-proxy reference
- OpenTelemetry collector/reference stack as appropriate
- dependency/security update policy
- browser support ADR
- license register
- on-premise no-CDN smoke requirement

## Gate
Clean machine + CI can build backend/frontend, run tests, start PostgreSQL/API/web, generate OpenAPI client and exercise a health endpoint.

---

# Phase 1A — Platform Kernel & Contracts

## Backend
- Tenancy
- Identity/session integration baseline
- Organization shell/contracts needed for context
- authorization/permission infrastructure
- entitlement infrastructure
- Audit baseline
- Notification infrastructure shell
- job-status API implementation

## Database
- tenant/legal entity identity foundations
- user/role/scope foundations
- audit foundation
- indexes/constraints
- test fixtures

## Contracts
- auth/session
- current user/context
- tenant/legal entity switch
- permissions/entitlements
- errors
- jobs
- feature flags

## Frontend
- App providers
- TanStack Router
- TanStack Query
- Redux Toolkit
- auth/session shell
- context switcher
- permission/entitlement client
- i18n/RTL
- normalized errors
- safe storage wrapper
- OpenTelemetry web
- global route/code splitting

## Testing/Ops
- permission contract tests
- session expiry/recovery
- tenant isolation tests
- correlation propagation
- offline/on-premise API connectivity smoke

## Gate
Authenticated shell loads a permission-scoped context through real/generated contracts in EN/LTR and AR/RTL.

---

# Phase 1B — Design System P0 & Storybook

Implement everything in `DESIGN_SYSTEM_P0_GATE.md`.

## Frontend/DS
- semantic tokens/themes
- shell/navigation
- controls/forms
- overlays/feedback
- Money/SensitiveValue
- DataGrid baseline
- filters/views/columns/bulk
- Storybook

## Quality
- axe
- keyboard
- RTL
- dark/light
- reduced motion
- density
- long content
- browser smoke

## Gate
P0 DS passes the canonical quality matrix and can support Employee Directory without local ad-hoc primitives.

---

# Phase 1C — Enterprise Engine & Integration Spikes — PASS / CLOSED

## Spikes
- AG Grid Enterprise
- FullCalendar Scheduler
- ECharts
- Tiptap
- dnd-kit
- Motion + GSAP brand SVG feasibility
- chosen SSO/provider integration
- SignalR/SSE optional push for job updates

## Output
For each:
- technical result
- wrapper API
- bundle impact
- a11y/RTL
- license
- on-premise behavior
- accept/replace ADR

## Gate
No commercial/heavy dependency is silently committed without an approved technical/license path.

---

# Phase 2 — Workforce Core: Organization + People + Documents — APPROVED TO START

## Backend
- Organization implementation
- People implementation
- Documents implementation
- employee/employment/assignment contracts
- effective dating
- sensitive field authorization
- audited change commands
- onboarding/offboarding command shell

## Database
- organization hierarchy
- people/employment/assignment schemas
- effective-dated constraints
- document metadata
- indexes
- history tests

## Contracts
- Employee Directory query
- Employee Profile query/read model
- compensation history
- employment history
- document status
- sensitive reveal/access policy if applicable
- import/export job contracts where applicable

## Frontend
- Employee Directory
- Employee Profile
- Employment/Assignment
- Compensation
- Organization views
- Documents
- onboarding/offboarding workspace shell

## Design System/Product UI
- EmployeeCell
- EmployeeHeader
- EmployeeQuickPreview
- EmploymentHistory
- CompensationHistory
- EmployeeTimeline
- document status components

## Testing/Ops
- effective-date tests
- permission tests
- sensitive-data telemetry/storage checks
- E2E employee create/change/profile
- Arabic/RTL profile
- large-directory grid performance

## Gate
One employee journey works end-to-end with history, permissions, errors and audit.

---

# Phase 3 — Time, Leave & Universal Approvals

## Backend
- Attendance
- Leave
- Approvals
- schedule/exception/correction commands
- leave balance/policy contracts
- approval purpose models

## Database
- attendance events/imports
- schedules
- leave requests/balances
- approval tasks
- idempotency/constraints

## Contracts
- daily attendance
- schedules
- exceptions
- corrections
- leave requests/balances/calendar
- My Work inbox
- before/after approval projection

## Frontend
- attendance overview/daily
- schedules
- exceptions queue
- leave overview/requests/balances/calendar
- My Work
- approval detail/comparison

## Design System/Product UI
- AttendanceDayTimeline
- AttendanceException
- LeaveBalance
- LeaveRequest
- WorkItem
- ApprovalComparison
- ZainXScheduler accepted implementation

## Testing/Ops
- timezone/date tests
- DnD/menu alternatives
- schedule/calendar RTL
- approval permission tests
- E2E request → approve/reject
- import/correction failure diagnostics

## Gate
Attendance correction and Leave approval flows are fully auditable and accessible.

---

# Phase 4 — Payroll + Compliance + Settlement

## Backend
- Payroll
- Compliance
- Settlement
- calculation pipeline
- effective-dated rule packages
- snapshotting
- trace
- exceptions
- approval/finalization
- payment/output jobs
- adjustment/off-cycle flow
- idempotency and immutable finalization

## Database
- payroll periods/runs
- snapshots
- lines/results
- rule versions
- trace
- exceptions
- approvals
- payments/settlements
- immutable/finalized constraints
- golden historical fixtures

## Contracts
- Run state
- readiness
- calculate command
- job status
- exception query/resolve command
- result query
- calculation trace
- variance
- approve/finalize
- output/export status
- adjustment/off-cycle
- ProblemDetails/business errors

## Frontend
- Payroll Overview
- Periods/Runs
- guided Run Workspace
- Inputs/Validation/Calculate
- Exceptions
- Review
- Approve
- Finalize
- Results
- Employee Calculation / Explain
- Variance
- outputs/settlement status

## Design System/Product UI
- PayrollRunHeader
- PayrollStepper
- PayrollReadiness Spotlight
- PayrollException
- PayrollResultRow
- PayrollCalculationBreakdown
- PayrollTrace
- RuleReference
- Variance
- Finalize dialog

## Testing/Ops
- golden payroll tests
- historical reproducibility
- decimal/rounding tests
- permission/finalization tests
- job retry/idempotency
- load/performance
- E2E calculate → exceptions → review → approve → finalize → explain
- Arabic/RTL payroll summary
- support correlation/trace diagnostics

## Gate
A finalized run is reproducible, immutable, explainable and safe under retries/failures.

---

# Phase 5 — Recruitment

## Backend
- Recruitment
- requisitions/jobs
- candidate/application
- stage transitions
- interviews/evaluations
- offers/versioning
- hire conversion contract

## Database
- candidate/application pipeline
- interview/evaluation
- offer history
- retention/privacy
- hire-conversion idempotency

## Contracts
- candidate grids/profile
- pipeline
- move-stage command
- interview scheduling
- scorecards
- offers
- hire conversion

## Frontend
- dashboard
- requisitions/jobs
- candidates
- pipeline
- Candidate Profile
- interviews
- scorecards
- offers
- hire conversion

## Design System/Product UI
- CandidateCell/Card
- PipelineColumn
- CandidateHeader/QuickPreview
- InterviewSchedule
- Scorecard
- OfferSummary/Status

## Testing/Ops
- optimistic DnD rollback
- keyboard move-stage alternative
- retention/privacy
- offer version tests
- E2E applied → interview → offer → hired → People conversion

## Gate
Candidate remains separate from Employee until backend hire conversion succeeds.

---

# Phase 6 — Reporting + Administration + Integrations + Notifications + Audit

## Backend
- Reporting read models
- admin/configuration contracts
- integration adapters/health
- notification templates/delivery
- Audit Explorer
- system operations

## Database
- reporting projections as required
- integration/outbox state
- notification delivery state
- audit indexes/retention

## Contracts
- report library/builder
- export jobs
- role/permission management
- configuration publish/effective date
- integration health/sync
- notifications
- audit explorer
- system health

## Frontend
- Reports
- constrained Report Builder
- Admin
- Permissions Matrix
- Integrations
- Notifications/Templates
- Audit Explorer
- System Operations

## Testing/Ops
- authorization-filtered reports
- export/job testing
- integration failure/retry
- audit query performance
- admin high-risk confirmation
- on-premise diagnostics

## Gate
Operators can configure, diagnose and audit the system without database access.

---

# Phase 7A — Governed AI: Read / Analyze / Explain

## Backend
- AI provider abstraction
- RAG/read tools
- source/provenance contracts
- payroll explain tool
- approved read-only domain tools
- evaluation infrastructure

## Contracts
- AI conversation/context
- tool execution status
- citations/provenance
- feedback
- usage/cost

## Frontend
- contextual Copilot
- full AI workspace
- Ask / Analyze / Explain
- source badges/citations
- truthful tool/activity status
- feedback

## Testing/Ops
- permission inheritance
- prompt-injection testing
- source correctness
- evaluation suite
- privacy/log filtering
- provider failure/fallback

## Gate
AI can answer and explain with traceable sources but cannot mutate business state.

---

# Phase 7B — Governed AI: Proposed / Confirmed Actions

## Backend
- action proposal tools
- validation/impact
- command invocation
- approval handoff
- tool governance
- Learning Inbox
- quality/usage management

## Frontend
- proposed action card
- before/after
- effective date/impact
- confirm/edit/cancel
- execution result
- learning/quality admin

## Testing/Ops
- no permission bypass
- no direct DB writes
- confirmation tests
- high-risk action restrictions
- audit
- evaluation/regression

## Gate
Every AI mutation is equivalent to a normal authorized backend command with explicit user confirmation.

---

# Phase 8 — Production Hardening, On-Premise Validation & UAT

Quality has existed throughout; this phase verifies the release candidate.

- performance/load
- security review
- accessibility regression
- RTL regression
- browser matrix
- offline/no-CDN deployment
- install/update/rollback
- backup/restore
- observability/privacy
- license compliance
- disaster scenarios
- support diagnostics
- UAT
- release packaging

## Final gate
Production-ready release candidate passes critical workflows and operational runbooks.
