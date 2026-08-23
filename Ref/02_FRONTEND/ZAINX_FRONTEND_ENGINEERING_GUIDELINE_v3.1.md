# ZainX Workforce — Frontend Engineering Guideline v3.1

**Status:** Canonical frontend engineering reference  
**Baseline date:** 2026-08-23
**Canonical:** Nx/pnpm workspace is nested under `/web` in the full product repository.  
**Product:** ZainX Workforce / HR / Payroll / Compliance / Talent / AI  
**Companion references:** `workforce_platform_engineering_blueprint_v2.0`, `workforce_platform_frontend_ux_blueprint_v1.0`, ZainX Design System, approved ADRs.

---

## 0. Purpose and source of truth

This is the implementation-level frontend reference for ZainX. It defines architecture, stack, repository structure, module boundaries, state ownership, routing, API contracts, UI foundations, design-system integration, forms, grids, scheduling, charts, motion, accessibility, RTL, security, performance, testing, observability, CI gates, and developer Definition of Done.

Source priority:

1. Approved ADR
2. Engineering Blueprint v2.x
3. This Frontend Engineering Guideline
4. Frontend UX / IA Blueprint
5. OpenAPI / GraphQL schemas and shared contracts
6. Design System contracts
7. Module README/work package
8. Code
9. Tickets/chat/comments

No module may silently override a higher-level decision.

---

## 1. Product stance

ZainX is an enterprise Workforce, Payroll, Compliance, Talent, Reporting, Administration and AI platform. The frontend must support multi-tenant context, multiple legal entities, Egypt-first payroll/compliance, high-volume operational work, ESS/MSS, complex approvals, historical traceability, sensitive data, on-premise deployments, Arabic RTL, English LTR, and governed AI.

Treat the frontend as an **Enterprise Frontend Platform**, not as a collection of HR pages.

### Non-negotiable UX principles

- Operational first, dashboard second.
- One product, role-aware.
- Context over fragmentation.
- Progressive disclosure.
- Cross-module consistency.
- Explainability for sensitive workflows.
- Desktop-first for heavy operations; responsive-first for ESS/MSS.
- Entitlement and permission shape UX but never replace backend enforcement.
- Finalized financial states are visually and behaviorally immutable.
- Arabic and English are architectural requirements.
- Loading, empty, error, permission, read-only and finalized states are part of the screen.
- **The system is quiet. Important things glow.**
- **Light is an event, not decoration.**

---

## 2. Final technology baseline

| Layer | Standard |
|---|---|
| Runtime | React 19.2 |
| Language | TypeScript strict |
| Build | Vite |
| Package manager | pnpm |
| Workspace | Nx |
| Styling | Tailwind CSS 4 |
| Token pipeline | Style Dictionary + CSS variables |
| Accessible primitives | React Aria Components |
| Design-system source/reference | ZainX DS; shadcn selectively |
| Visual/effect inspiration | 21st.dev selectively |
| Icons | Lucide React + ZainX-owned SVG |
| Global client state | Redux Toolkit |
| Server state | TanStack Query |
| Complex workflows | XState v5 |
| Routing / URL state | TanStack Router |
| Forms | React Hook Form |
| Validation | Zod 4 |
| REST contracts | OpenAPI + Orval |
| Optional composite reads | GraphQL + GraphQL Code Generator |
| Enterprise grids | AG Grid Enterprise behind ZainXDataGrid |
| Lightweight tables | TanStack Table |
| Virtualization | TanStack Virtual |
| Scheduling | FullCalendar Scheduler behind ZainXScheduler |
| Charts | Apache ECharts behind ZainXChart |
| Motion | Motion for React |
| Complex brand SVG | GSAP, brand package only |
| Drag and drop | dnd-kit |
| Rich text | Tiptap |
| i18n | i18next + react-i18next + Intl |
| Toast mechanics | Sonner behind ZainX Toast |
| Component docs | Storybook |
| Unit tests | Vitest |
| Component tests | Testing Library |
| API mocks | MSW |
| E2E | Playwright |
| Automated a11y | axe-core / Storybook a11y |
| Telemetry | OpenTelemetry Web |
| Lint/format | ESLint + Prettier |

**Dependency rule:** every runtime dependency must have one clear responsibility and must not create a competing state, design, or infrastructure layer.

---

## 3. Architectural style — Modular Frontend Monolith

ZainX uses a modular frontend monolith, not microfrontends.

Goals:

- strong module boundaries
- one shell
- one design system
- one session/auth model
- one routing model
- route-level code splitting
- module ownership without deployment fragmentation
- simpler on-premise packaging and upgrades

High-level layering:

```text
Application
  ↓
Platform
  ↓
Feature Modules
  ↓
Product / Enterprise UI
  ↓
Design System
  ↓
Accessible Primitives + Tokens
```

A feature may depend on platform contracts, design system, generated contracts, and shared utilities. It must not import another feature's internals.

---

## 4. Repository structure

The full product repository is canonical. The frontend Nx workspace is nested under `web/`.

```text
zainx-workforce/
├── src/                                  # .NET modular monolith
│   ├── Workforce.Host.Api/
│   ├── Workforce.Host.Worker/
│   ├── Workforce.SharedKernel/
│   ├── Workforce.BuildingBlocks/
│   └── Modules/
│       ├── Tenancy/
│       ├── Identity/
│       ├── Organization/
│       ├── People/
│       ├── Documents/
│       ├── Attendance/
│       ├── Leave/
│       ├── Approvals/
│       ├── Payroll/
│       ├── Compliance/
│       ├── Settlement/
│       ├── Recruitment/
│       ├── Reporting/
│       ├── Integrations/
│       ├── Notifications/
│       ├── Audit/
│       └── Ai/
├── web/                                  # Nx + pnpm frontend workspace
│   ├── apps/
│   │   ├── workforce-web/
│   │   ├── design-system-docs/
│   │   └── e2e/
│   ├── packages/
│   │   ├── platform/
│   │   ├── design-system/
│   │   ├── contracts/
│   │   ├── people/
│   │   ├── attendance/
│   │   ├── leave/
│   │   ├── payroll/
│   │   ├── recruitment/
│   │   ├── approvals/
│   │   ├── reports/
│   │   ├── administration/
│   │   └── ai/
│   ├── tooling/
│   ├── package.json
│   ├── pnpm-workspace.yaml
│   ├── nx.json
│   └── tsconfig.base.json
├── tests/
├── deploy/
├── docs/
│   └── adr/
└── Ref/
```

The frontend is not an independent product repository unless a future approved ADR explicitly changes this decision.

---


## 5. Feature module structure

Recommended pattern:

```text
packages/payroll/
├── domain/
│   ├── models/
│   ├── ui-state/
│   └── mappings/
├── data-access/
│   ├── queries/
│   ├── mutations/
│   ├── graphql/
│   └── adapters/
├── feature/
│   ├── overview/
│   ├── runs/
│   ├── run-workspace/
│   ├── explanation/
│   └── variance/
├── ui/
├── routes/
├── state-machines/
├── permissions/
├── testing/
├── index.ts
└── README.md
```

Only the package public API (`index.ts`) is consumed externally. Deep imports across packages are forbidden.

---

## 6. Nx module boundaries

Use tags:

```text
scope:platform
scope:design-system
scope:people
scope:attendance
scope:leave
scope:payroll
scope:recruitment
scope:approvals
scope:reports
scope:admin
scope:ai

type:app
type:domain
type:data-access
type:feature
type:ui
type:util
type:contract
```

Rules:

- design-system cannot depend on feature modules
- UI packages cannot call APIs directly
- data-access owns transport/client access
- features compose local UI, state and data-access
- cross-feature dependencies require explicit public contracts
- Nx boundary violations fail CI

---

## 7. State ownership model

ZainX intentionally does not have one universal store.

```text
Server/API state              → TanStack Query
Cross-module application UI   → Redux Toolkit
Complex workflow state        → XState
URL/shareable state           → TanStack Router
Form state                    → React Hook Form
Small isolated state          → React
```

### TanStack Query owns

Employees, candidates, payroll runs, attendance, leave, reports, reference data, remote queries, mutations, invalidation, remote cache.

Never copy server collections into Redux merely for convenience.

### Redux Toolkit owns

Cross-module client/application state:

- current UI tenant/legal-entity context
- app shell state
- display preferences
- global overlay coordination
- cross-feature UI events
- AI panel presentation context
- recent client-owned navigation context

Suggested slices:

```text
sessionSlice
contextSlice
shellSlice
preferencesSlice
aiUiSlice
notificationsUiSlice
```

Use RTK listener middleware for application-wide reactions.

### XState owns

State-machine-heavy UI orchestration:

- Payroll Run
- Onboarding / Offboarding
- Import processing
- Hire conversion
- AI action proposal / confirmation
- complex approval flows
- long-running client workflows

Backend remains source of truth.

### TanStack Router owns

Path, filters, sort, page/cursor, view, shareable tab state, and validated search parameters.

Example:

```text
/people/employees
?view=active
&legalEntity=eg-cairo
&department=finance
&sort=joinDate.desc
&page=4
```

### React Hook Form owns

Field values, dirty/touched, validation state, field arrays, logical multi-step form values.

### React local state owns

Tiny state that is not shared or persisted.

---

## 8. Redux Toolkit rules

- no server-cache duplication
- semantic actions, not arbitrary global set-state
- small slices
- selectors for reading state
- listener middleware for cross-app reactions
- no async business workflow inside reducers
- no secrets or sensitive employee/payroll data in persisted state
- persistence only for approved preferences
- hardened production may restrict DevTools according to security policy

Example application event:

```text
legalEntityChanged
  → close incompatible sensitive drawers
  → reset transient selections
  → refresh permission context
  → validate current route
  → clear incompatible AI context
```

---

## 9. Routing

Use TanStack Router.

Routes must be stable, human-readable, type-safe, code-split, permission-aware in UX, and deep-linkable when safe.

Examples:

```text
/
/people/employees
/people/employees/$employeeId
/time/attendance
/leave/requests
/payroll/runs
/payroll/runs/$runId
/recruitment/candidates
/recruitment/pipeline/$requisitionId
/reports
/ai
/admin/roles
```

Route guards improve UX only. Backend authorization remains mandatory.

Heavy libraries are lazy-loaded with their feature routes.

---

## 10. REST / OpenAPI

REST/OpenAPI is the canonical command and operational API contract.

Use for:

- commands
- operational grids
- exports/imports
- file operations
- payroll
- approvals
- high-risk mutations
- integration/admin operations

Pipeline:

```text
ASP.NET OpenAPI
  ↓
Orval
  ↓
Generated TypeScript models
Generated client
Generated TanStack Query integrations where configured
Generated MSW mocks
```

Do not manually reproduce backend transport DTOs.

---

## 11. GraphQL

GraphQL is optional and additive, not universal.

Good uses:

- Employee Profile composite reads
- Manager Home
- role-aware Home composition
- cross-module contextual summaries
- selected mobile composite views

Commands remain primarily REST.

Frontend pipeline:

```text
GraphQL Schema
  ↓
GraphQL Code Generator
  ↓
Typed documents/fragments
  ↓
Thin GraphQL transport
  ↓
TanStack Query
```

Do not add Apollo Client as a second server-state cache while TanStack Query is canonical unless a future ADR changes the architecture.

AI never gets arbitrary GraphQL or SQL write access. AI uses governed backend tools.

---

## 12. API error contract

Normalize backend errors:

```ts
interface AppError {
  code: string;
  message: string;
  correlationId?: string;
  fieldErrors?: Record<string, string[]>;
  retryable: boolean;
  severity: "info" | "warning" | "error";
}
```

Mapping:

- validation → inline field/form summary
- 401 → session recovery
- 403 → Access Gate/context restriction
- 404 → not found
- 409 → concurrency/conflict flow
- 422 → business validation
- 429 → retry/rate-limit state
- 5xx → retryable error + correlation ID
- network → offline/network state

Never render raw server exception text.

---

## 13. Authentication, authorization and entitlement

Authentication answers **who are you?**  
Authorization answers **what may you do?**  
Entitlement answers **is the capability licensed/enabled?**

Frontend:

- permission-aware navigation/actions
- read-only states
- permission reasons when useful
- no sensitive data pre-render
- semantic permission helpers

Backend:

- always enforces reads/writes
- enforces tenant/legal entity/org scope
- enforces domain transitions

Use:

```ts
can("payroll.run.finalize", context)
can("people.compensation.read", employeeScope)
```

Avoid role-name checks spread across screens.

SSO/MFA/OIDC-ready. Prefer secure HttpOnly session/token handling where architecture permits. Do not persist long-lived credentials in localStorage.

---

## 14. Design-system integration

Vendor UI engines are implementation details.

```text
React Aria        → ZainX primitives
AG Grid           → ZainXDataGrid
ECharts           → ZainXChart
FullCalendar      → ZainXScheduler
Motion            → ZainX motion utilities
Lucide            → ZainX icon wrapper/registry
```

Feature modules should not style vendor components independently.

---

## 15. Tailwind and token rules

Tailwind is the styling engine, not the product language.

Good:

```tsx
className="bg-surface-canvas text-text-primary border-border-default"
```

Avoid in feature code:

```tsx
className="bg-slate-950 text-purple-400 border-gray-800"
```

Token flow:

```text
Primitive
→ Semantic
→ Component
→ State
```

Style Dictionary generates CSS variables, TypeScript tokens, docs, and future platform mappings.

Use CSS logical properties to support RTL.

---

## 16. Forms

Stack:

```text
React Hook Form + Zod + ZainX Form Components
```

Rules:

- backend remains business-validation authority
- client validation improves speed and clarity
- field errors stay near fields
- large forms may show a summary
- dirty-navigation warning where required
- effective dates are explicit
- sensitive changes show impact/before-after
- do not turn long forms into dozens of equal cards

High-risk forms may require reason, effective date, approval, impact summary and confirmation.

---

## 17. Effective dating

First-class for employment, assignment, compensation, contracts, payroll rules, policies and organization changes.

UI supports:

- effective from
- effective to
- open-ended
- overlap warning
- future scheduled
- historical read-only
- version/timeline inspection

Historical truth is not represented by editing a single current value.

---

## 18. Data grid architecture

`ZainXDataGrid` is the heavy-grid public API. AG Grid Enterprise is internal.

Required capabilities:

- server filtering/sorting
- pagination/cursor strategies
- column resize/reorder/pinning/visibility
- grouping/totals
- saved views
- density
- row selection
- bulk actions
- export
- quick preview
- expandable rows
- status/money/sensitive cells
- permission-aware columns
- keyboard navigation
- loading/error/empty/no-results
- RTL/LTR
- virtualization

State ownership:

```text
Shareable view state → Router
Saved user view      → backend
Transient selection  → grid/local
Remote rows          → Query/grid datasource
Global preference    → Redux only if cross-module
```

Typical row heights:

- compact 36px
- standard 44px
- comfortable 52px

---

## 19. Scheduling and calendars

Use React Aria date/time primitives for date selection.

Use `ZainXScheduler` wrapping FullCalendar Scheduler for:

- shifts
- attendance schedules
- team leave
- interviews
- resource scheduling

Calendar/scheduler must have an accessible list/agenda alternative. Drag is never the sole action path.

---

## 20. Charts

Use ECharts behind ZainXChart.

Approved chart families:

- line
- bar
- stacked bar
- area
- sparkline
- waterfall
- heatmap when justified
- donut only when useful

No decorative rainbow/3D charts. Charts support decisions and provide accessible summaries/data where required.

---

## 21. Motion

```text
CSS/Tailwind      → micro motion
Motion for React  → product motion
GSAP              → complex brand SVG only
```

Tokens:

```text
instant     80ms
micro       140ms
standard    220ms
context     320ms
expressive  640ms
brand       900ms
```

Signature moments:

- login logo assembly
- startup/logout
- access gate
- AI context scan
- payroll finalization
- high-value success
- controlled spotlight

Rules:

- motion never delays work
- no endless decorative loops
- reduced-motion required
- no fake progress
- no confetti for payroll/compliance
- spotlight is rare and semantic

---

## 22. Rich text

Tiptap is reserved for real rich-content needs:

- job descriptions
- offer templates
- email templates
- notification templates
- selected policy content

Do not use it for ordinary notes. Sanitize and centrally govern allowed extensions.

---

## 23. i18n and RTL

Use `i18next + react-i18next + Intl`.

Suggested namespaces:

```text
common
shell
people
attendance
leave
payroll
recruitment
approvals
reports
administration
ai
errors
```

No hard-coded user-facing strings.

RTL is not a post-release CSS patch. Components must define icon mirroring, alignment, tab order, drawer direction, breadcrumbs, tables, numeric behavior and mixed Arabic/Latin text.

Directional icons mirror semantically. Non-directional icons do not.

---

## 24. Responsive strategy

Desktop-heavy:

- Payroll
- Admin
- Report Builder
- Recruitment Pipeline
- complex configuration

Responsive-first:

- Employee Home
- Manager Home
- My Team
- Leave
- Attendance status
- Payslips
- Approvals
- Notifications
- AI

Do not squeeze desktop data grids into mobile. Use priority data, responsive rows/cards, detail screens, and focused actions.

---

## 25. Accessibility

Minimum target: WCAG AA.

Each interactive component defines:

- keyboard behavior
- focus behavior
- screen-reader semantics
- error semantics
- touch target
- reduced-motion
- non-color status cues

Requirements:

- DnD has keyboard/menu alternative
- AI streaming does not announce every token
- async completion is meaningfully announced
- overlays restore focus
- grids preserve accessible structure
- status is never color-only

Accessibility is a CI gate.

---

## 26. Loading / empty / error / restricted

Do not use one centered spinner for everything.

Loading:

- app bootstrap → brand moment then structural loading
- page → structural skeleton
- grid → column-aware skeleton
- drawer → local skeleton
- file → real progress/validation
- payroll → truthful process state
- AI → truthful context/tool state

State taxonomy:

- first use
- no data
- no results
- successful empty queue
- partial data
- offline/network
- permission restricted
- read-only
- conflict
- archived
- finalized/locked
- API error
- maintenance

Permission restriction is not an empty state.

---

## 27. Sensitive data

Sensitive examples:

- salary
- national ID
- bank account
- tax data
- payroll results
- private documents

Rules:

- backend authorization first
- frontend masking second
- never reveal by hover
- no sensitive browser persistence
- no accidental analytics/telemetry capture
- no sensitive error payloads
- permission denial must not leak the hidden value

---

## 28. Payroll frontend rules

**Critical boundary:** the frontend never implements statutory payroll calculation, tax/social-insurance rules, rounding authority, historical payroll truth, or finalization logic. It orchestrates backend commands and renders server-produced results, trace, rule version, variance, exceptions and output status.

Payroll is financial operations software.

Priorities:

- state
- readiness
- blockers
- exact numbers
- explainability
- variance
- history
- legal entity
- approval trace
- immutable finalization

Canonical process:

```text
Inputs
→ Validation
→ Calculate
→ Exceptions
→ Review
→ Approve
→ Finalize
→ Pay / Export
```

Finalized:

```text
View
Export
Create Adjustment
```

No edit.

---

## 29. Recruitment rules

Pipeline movement is a domain transition.

DnD is convenience, not the only mechanism.

Candidate Profile owns applications, interviews, evaluations, communication, offers and timeline.

Offer version/history and hire conversion must preserve auditability.

---

## 30. AI frontend rules

AI is a cross-product interaction layer, not a decorative chatbot.

It must communicate:

- current context
- source/provenance
- tool execution where relevant
- proposed action
- confirmation
- execution result
- permission/limitations

Modes:

```text
Ask
Analyze
Explain
Act
```

AI does not write directly to the DB. It invokes normal governed backend tools and high-risk actions require explicit confirmation.

---

## 31. Entitlements and feature flags

Do not confuse:

```text
permission
entitlement
feature flag
```

Flags are for rollout/migration/beta and require owner, purpose, creation date, removal criterion, and target removal version.

Avoid permanent flag architecture.

---

## 32. Performance

Guidelines:

- route-level splitting
- lazy-load AG Grid/ECharts/FullCalendar/Tiptap where possible
- server operations for huge grids
- virtualize large custom lists
- prefetch predictable routes/data
- avoid oversized global subscriptions
- maintain bundle budgets
- inspect route chunks
- do not import heavy feature engines into the shell bundle

Performance is measured, not guessed.

---

## 33. Browser and on-premise

Publish a supported-browser policy.

Prefer managed modern browsers. On-premise does not imply legacy browser support.

Critical runtime assets must not depend on public CDNs. Production must be deployable inside customer-controlled networks.

---

## 34. Browser storage

Allowed:

- safe UI preferences
- approved non-sensitive client settings

Forbidden:

- salary/payroll datasets
- national IDs
- bank details
- private document data
- unrestricted profile snapshots
- long-lived credentials when architecture can avoid them

Use a central storage abstraction rather than scattered localStorage calls.

---

## 35. Observability

Use OpenTelemetry Web.

Trace:

- route/page loading
- API calls
- high-value workflow actions
- errors
- long-running frontend tasks

Propagate correlation IDs.

Examples:

```text
payroll.calculate.clicked
payroll.finalize.confirmed
candidate.stage_move.requested
employee.profile.loaded
```

Never log sensitive payloads by default.

Product analytics and technical observability are separate concerns.

---

## 36. Testing strategy

### Unit — Vitest
Selectors, reducers, utilities, mappings, pure machine behavior.

### Component — Testing Library
Test user-visible behavior, not implementation details.

### Storybook
Every important DS/product component includes states:

- default
- variants
- loading
- empty/error
- disabled
- read-only/permission when relevant
- dark
- RTL
- long content
- reduced motion

### API — MSW
Mocks align with generated contracts.

### E2E — Playwright
Critical journeys:

- login/session
- tenant/legal-entity change
- employee creation
- compensation change
- leave request/approval
- attendance correction
- payroll calculate/exceptions/review/approve/finalize/explain
- recruitment stage/interview/offer/hire
- access denied
- AI explain/action proposal
- Arabic RTL critical flows

---

## 37. Storybook as executable spec

Storybook is not a gallery. It is the canonical interactive component reference for:

- component API
- variants/states
- accessibility
- RTL
- responsive behavior
- motion
- interactions

A shared component is not finished if key states are undocumented.

---

## 38. Test data

Fixtures must include:

- short/long names
- Arabic names
- mixed Arabic/Latin
- zero/missing values
- huge monetary values
- negative variance
- permission-restricted data
- archived/finalized entities
- large datasets

Do not validate the product only with perfect short English data.

---

## 39. TypeScript and React coding standards

TypeScript:

- `strict: true`
- no `any` in public APIs
- discriminated unions for meaningful states
- exhaustive checks for critical state models
- clear public types
- generated transport contracts remain generated

React:

- focused components
- side effects isolated
- presentational UI does not call APIs
- DS primitives do not read feature/global business state
- avoid premature memoization
- do not solve prop ownership by putting everything into Redux

---

## 40. Naming and imports

Examples:

```text
Component: PayrollRunHeader
Hook: usePayrollRun
Query key: payrollQueries.run(id)
Route: payrollRunRoute
Machine: payrollRunMachine
Permission: payroll.run.finalize
```

Use package aliases and public exports.

Good:

```ts
import { Button } from "@zainx/design-system";
import { PayrollRunRoute } from "@zainx/payroll";
```

Deep imports across packages are forbidden.

---

## 41. DTOs and mapping

Generated DTOs are transport models.

Introduce adapters/domain view models only when justified:

```text
API DTO
→ adapter
→ feature model
→ UI
```

Good reasons include composite reads, transport-specific fields, derived UI state, or REST/GraphQL source differences.

Do not create mapping layers as ceremony.

---

## 42. Money, dates and time

Money display uses explicit currency and locale-aware formatting. Frontend is never authoritative for payroll arithmetic.

Dates must distinguish:

- date-only
- local date-time
- UTC instant
- effective date
- payroll period
- timezone

Centralize parsing/formatting. Do not pass ambiguous date strings freely.

---

## 43. Long-running operations

Examples:

- payroll calculation
- imports
- exports
- large reports
- AI tools
- integration sync

Support:

- accepted/queued
- running
- real progress when known
- warning
- failed/retry
- complete
- background continuation where backend supports it

Do not trap users inside a modal for a multi-minute operation.

---

## 44. Concurrency

Handle optimistic concurrency explicitly.

On 409/conflict:

- explain that record changed
- allow refresh/review
- compare versions when valuable
- never silently overwrite high-risk data

---

## 45. Security

Never:

- inject unsanitized HTML
- render arbitrary rich HTML from AI
- store secrets in source
- rely on hidden UI for authorization
- leak sensitive data into logs
- show stack traces
- trust client route/query values as authorization

Use CSP/security headers in deployment. Sanitize rich content. Audit high-risk commands.

---

## 46. AI security UI contract

Before AI-triggered mutation, show:

- entity
- current value
- proposed value
- effective date
- impact
- required permission/approval
- Confirm / Edit / Cancel

AI cannot create a privileged path around normal product rules.

---

## 47. PR requirements

Frontend PRs include, when applicable:

- module/feature link
- screenshots or Storybook
- RTL evidence for shared/affected UI
- loading/error/permission evidence
- tests
- accessibility result
- API contract note
- bundle/performance note for heavy dependencies
- ADR for architecture changes

No architecture changes hidden inside ordinary feature PRs.

---

## 48. Dependency policy

A new runtime dependency must answer:

1. What problem?
2. Why current stack is insufficient?
3. Bundle impact?
4. License?
5. Security/support?
6. Accessibility/RTL implications?
7. On-premise implications?
8. Upgrade owner?
9. Does it duplicate another layer?

Major libraries require an ADR.

---

## 49. Upgrade policy

Use scheduled platform upgrades, not blind major auto-upgrades.

For major versions:

- review migration guide
- test design system first
- verify browser support
- verify bundle impact
- test RTL/a11y
- run critical workflows
- update ADR/baseline when architecture changes

Commit the lockfile.

---

## 50. Module frontend Definition of Done

A feature/module is complete only when:

- shared shell used
- approved page pattern used
- design-system components used
- API is generated/typed
- permission behavior exists
- loading exists
- empty exists
- error exists
- read-only/finalized exists where relevant
- responsive behavior defined
- RTL verified
- keyboard flow usable
- critical tests exist
- high-value failures/actions are observable
- no unapproved dependency/boundary violation
- module README updated

---

## 51. Foundation build order

Before parallel module work:

1. Nx workspace/boundaries
2. Vite app shell
3. TypeScript strict
4. token pipeline
5. design-system primitives
6. TanStack Router
7. session/auth
8. permissions/entitlements
9. i18n/RTL
10. Redux Toolkit store
11. TanStack Query
12. OpenAPI/Orval
13. global errors
14. overlays/toasts
15. data-grid baseline
16. forms baseline
17. Storybook
18. testing infrastructure
19. OpenTelemetry
20. global search shell
21. My Work shell
22. AI panel shell

Then scale feature teams.

---

## 52. Recommended frontend ADR list

- ADR-FE-001 Modular Frontend Monolith / Nx
- ADR-FE-002 React Aria primitive layer
- ADR-FE-003 Redux Toolkit global state
- ADR-FE-004 TanStack Query server state
- ADR-FE-005 XState workflow orchestration
- ADR-FE-006 TanStack Router / URL state
- ADR-FE-007 REST/OpenAPI command contract
- ADR-FE-008 Optional GraphQL read composition
- ADR-FE-009 AG Grid Enterprise abstraction
- ADR-FE-010 Style Dictionary token pipeline
- ADR-FE-011 Motion / GSAP scope
- ADR-FE-012 FullCalendar Scheduler abstraction
- ADR-FE-013 ECharts abstraction
- ADR-FE-014 Browser support
- ADR-FE-015 Frontend telemetry/privacy

---

## 53. Explicit non-goals

Not baseline:

- microfrontends
- Next.js
- Redux for all API state
- RTK Query beside TanStack Query
- Apollo cache beside TanStack Query
- MUI/Ant as the visual foundation
- default shadcn identity
- direct unreviewed 21st.dev imports
- Rive without a real requirement
- GSAP throughout normal UI
- frontend payroll calculation truth
- arbitrary AI GraphQL/SQL access
- offline-first payroll mutation architecture
- legacy browser support without a business requirement

---

## 54. Canonical mental model

```text
                    ZainX Web Platform

             React + TypeScript + Vite
                        │
                       Nx
                        │
        ┌───────────────┼────────────────┐
        │               │                │
     Platform       Design System     Features
        │               │                │
 Auth / Session     React Aria       People
 Permissions       Tailwind/Tokens   Time
 Tenancy            Motion            Leave
 Router             ZainX UI          Payroll
 Redux                                Recruitment
 Query                                Reports/Admin/AI
        │               │                │
        └───────────────┼────────────────┘
                        │
              Enterprise UI Engines
                        │
       AG Grid / ECharts / Scheduler / Tiptap
                        │
                  Typed Contracts
                        │
              OpenAPI + optional GraphQL
                        │
                   ASP.NET Core
```

---


## 54. Long-running operation standard

Long-running frontend workflows must follow `../04_EXECUTION/LONG_RUNNING_OPERATION_CONTRACT.md`.

Polling via TanStack Query is the correctness baseline. Optional SignalR/SSE push may accelerate updates but must reconcile with the canonical job query.

A background job state never replaces the domain entity state.

Frontend must not fabricate determinate progress.

---

## 55. Module-start contract gate

No feature module starts from a screen list alone.

Before implementation, complete `../04_EXECUTION/MODULE_START_GATE.md`, including domain invariants, database ownership, commands, queries, events, permissions, API contracts, errors, async jobs, frontend page patterns and tests.

---

## 56. Integrated product delivery

Feature development follows `../04_EXECUTION/INTEGRATED_DELIVERY_MODEL.md`.

Backend, database, contracts, frontend, Design System and QA work from the same approved module contract.

Frontend may work in parallel using generated contracts/MSW mocks after the contract is approved.

---

## 57. Design System P0 gate

Business feature teams may not recreate missing primitives locally.

Before Phase 2 scale, `../04_EXECUTION/DESIGN_SYSTEM_P0_GATE.md` must pass.

---

## 58. AI release separation

Production AI ships as:

- Phase 7A: Read / Analyze / Explain
- Phase 7B: Proposed / Confirmed Actions

See `../04_EXECUTION/AI_RELEASE_MODEL_7A_7B.md`.

Read-only AI may ship before AI actions. AI actions require the same backend authorization, validation, approval and audit path as normal application commands.

---

## 59. Final engineering position

The ZainX frontend must be:

**modular but not fragmented, strongly typed but not overabstracted, data-heavy but readable, motion-rich only when meaningful, accessible and RTL-native, auditable around sensitive workflows, API-contract-driven, and strong enough for multiple teams to evolve for years.**

The standard is not “a modern React app.”

The standard is:

> **An enterprise frontend platform with disciplined state ownership, governed contracts, reusable interaction patterns, and a distinctive ZainX product language.**
