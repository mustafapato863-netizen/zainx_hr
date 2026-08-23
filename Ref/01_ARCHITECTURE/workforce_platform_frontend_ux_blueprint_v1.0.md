# Workforce Platform - Frontend, UX & Information Architecture Blueprint v1.0

**Document type:** Product Frontend Specification / Multi-Team Reference  
**Status:** Baseline for implementation and visual benchmarking  
**Audience:** Product owner, frontend engineers, backend engineers, QA, UX/UI designers, module owners  
**Companion reference:** `workforce_platform_engineering_blueprint_v2.0`  
**Primary objective:** Define how the product is organized and used before visual styling decisions are made.

---

## 1. Purpose

This document defines the **functional shape of the frontend** for the Workforce Platform without prescribing brand colors, illustration style, typography personality, or a final visual theme.

It answers:

- What appears in the global navigation.
- Which pages exist and why.
- Which pages are full workspaces versus tabs, drawers, dialogs, or side panels.
- What each user role sees.
- Which screen patterns must be reused across modules.
- How People, Time, Leave, Payroll, Recruitment, Performance, Reporting, Administration, and AI fit together.
- How desktop-heavy operational work coexists with mobile-friendly employee and manager workflows.
- How multiple frontend developers can build modules in parallel without producing different products.
- Which visual references should be collected from competitors during the next design phase.

This is an **information architecture and UX behavior specification**, not a final UI style guide.

---

## 2. Product UX Principles

### 2.1 Operational first, dashboard second

The product exists to help users **complete work**, not only observe dashboards.

Primary actions are verbs:

- Approve
- Correct
- Calculate
- Hire
- Review
- Explain
- Export
- Resolve
- Request
- Finalize

Charts and KPIs support decisions but must not dominate workflows.

### 2.2 One product, role-aware experience

The application uses one shared shell, but content and navigation are filtered by:

- User role
- Permission scope
- Licensed modules
- Legal entity access
- Organizational scope
- Tenant configuration

A payroll user should not see recruitment complexity. An employee should not see administration. A recruiter should not be forced through payroll navigation.

### 2.3 Context over fragmentation

When the user is working on a specific entity, keep that entity as the context.

Examples:

- An Employee Profile owns tabs for employment, compensation, attendance, leave, payroll, documents, performance, assets, and timeline.
- A Candidate Profile owns tabs for applications, interviews, evaluations, communication, offers, and timeline.
- A Payroll Run is a guided workspace with its own steps rather than a collection of unrelated routes.

### 2.4 Progressive disclosure

Do not expose advanced configuration until the user needs it.

Examples:

- Common filters are visible; advanced filters open on demand.
- A payroll run shows high-level readiness first, then exceptions, then employee-level calculation details.
- AI can explain a calculation without exposing internal implementation details unless the user requests them and has permission.

### 2.5 Consistency across modules

Every developer must use shared page patterns and shared interaction conventions.

The product should feel like one system even if separate engineers build People, Payroll, Recruitment, and AI.

### 2.6 Explainability for sensitive workflows

For payroll, leave, attendance corrections, approvals, and AI-assisted actions, the product should answer:

- What happened?
- Why did it happen?
- Which input caused it?
- Which rule/policy applied?
- Who changed or approved it?
- When did it happen?

### 2.7 Desktop-first, not desktop-only

Heavy operational workflows are desktop-first:

- Payroll
- Administration
- Complex reporting
- Recruitment pipeline
- Configuration

The following must still work exceptionally well on a phone-sized browser:

- Employee home
- Manager home
- My team
- Approvals
- Leave requests
- Attendance check/status
- Payslips
- Notifications
- AI Copilot

---

## 3. Global Application Shell

The application shell is shared by all modules.

```text
+-------------------------------------------------------------------+
| Company / Workspace | Global Search | My Work | Notifications | AI|
+----------------------+--------------------------------------------+
|                      |                                            |
| Sidebar              | Main Workspace                             |
|                      |                                            |
| Home                 | Page Header                                |
| People               | Breadcrumb / Context                      |
| Time                 | Actions                                    |
| Leave                | Content                                    |
| Payroll              |                                            |
| Talent               |                                            |
| Reports              |                                            |
| AI Copilot           |                                            |
|                      |                                            |
| Administration       |                                            |
+----------------------+--------------------------------------------+
```

### 3.1 Global shell elements

1. **Tenant / company switcher**
   - Shows current tenant and legal entity context where applicable.
   - If a user has access to multiple legal entities, switching must be obvious and auditable.

2. **Global search / command palette**
   - Suggested shortcut: `Ctrl/Cmd + K`.
   - Searches authorized employees, candidates, jobs, payroll runs, documents, reports, and commands.
   - Search must never return records outside the user's authorization scope.

3. **My Work**
   - Universal work inbox for approvals and tasks.

4. **Notifications**
   - Grouped into `Needs Action`, `Information`, `System`, and optionally `AI`.

5. **AI Copilot launcher**
   - Opens contextual side panel from any page.
   - The current page/entity becomes part of the AI context only when authorized.

6. **Quick Create**
   - Optional global `+` action.
   - Items are role and permission aware.

7. **User menu**
   - Profile
   - Language
   - Preferences
   - Security
   - Sign out

---

## 4. Primary Navigation Model

The default full-product navigation is intentionally short.

```text
HOME
- Home
- My Work

WORKFORCE
- People
- Time
- Leave
- Payroll

TALENT
- Recruitment
- Performance

INSIGHTS
- Reports
- AI Copilot

SYSTEM
- Administration
```

### 4.1 Navigation rules

- A section is hidden if the tenant has no license/entitlement for it.
- A section is hidden if the user has no permission to view it.
- Avoid exposing every subpage as a sidebar item.
- A module landing page can expose secondary navigation.
- Settings should live close to the module they configure, except for truly global administration.

---

## 5. Role-Based Navigation

### 5.1 Employee

```text
Home
My Profile
Attendance
Leave
Payslips
Requests / My Work
Documents
AI Copilot
```

### 5.2 Line Manager

```text
Home
My Team
Time
Leave
My Work
Selected Reports
AI Copilot
```

### 5.3 HR Operations

```text
Home
People
Time
Leave
My Work
Reports
AI Copilot
```

### 5.4 Payroll Officer

```text
Home
People (restricted fields)
Time (payroll-relevant)
Leave (payroll-relevant)
Payroll
My Work
Reports
AI Copilot
```

### 5.5 Recruiter

```text
Home
People (limited)
Recruitment
My Work
Recruitment Reports
AI Copilot
```

### 5.6 HR / Payroll Administrator

Full workforce navigation plus appropriate Administration areas.

### 5.7 System Administrator

Administration-heavy experience. Sensitive payroll/HR data access is not automatically granted merely because the user is a technical system administrator.

---

## 6. Screen Pattern Library

The mature product may contain 60+ primary routes, but they should be built from a small number of reusable UX patterns.

### Pattern P1 - Operational Dashboard

Use for Home, Payroll Overview, Recruitment Dashboard, Attendance Overview.

Standard structure:

- Page title and context
- Critical alerts / blockers
- 3-6 primary KPIs
- Work requiring action
- Trend or diagnostic charts
- Recent activity / upcoming events

Rule: Never fill a dashboard with metrics that do not lead to an action or decision.

### Pattern P2 - List / Data Grid

Use for employees, requests, documents, requisitions, candidates, payroll runs.

Standard capabilities:

- Search
- Filter bar
- Advanced filters
- Saved views
- Sorting
- Column selection
- Pagination or virtualized loading
- Bulk selection
- Bulk actions
- Export (permission controlled)
- Row actions
- Empty state
- Loading state
- Permission-aware fields

### Pattern P3 - Detail Workspace

Use for Employee, Candidate, Job Requisition, Legal Entity.

Structure:

```text
Entity Header
- identity
- status
- primary facts
- primary actions

Tabs
- Overview
- Domain-specific tabs
- Timeline / Audit where relevant
```

### Pattern P4 - Guided Wizard / Process Workspace

Use for Payroll Run, onboarding, offboarding, imports, and complex setup.

Structure:

- Process header
- Current state
- Step navigation
- Validation blockers
- Primary next action
- Save and resume
- Exit without losing progress

### Pattern P5 - Kanban / Pipeline

Use for recruitment pipeline and possibly selected workflow queues.

Rules:

- Every card move is a real domain transition.
- Drag-and-drop is convenience, not the only input method.
- Invalid transitions are blocked by backend rules.

### Pattern P6 - Calendar / Schedule

Use for leave calendar, shifts, interview scheduling, team availability.

Must support:

- Day/week/month modes where relevant
- Filters
- Team / department grouping
- Accessible list alternative

### Pattern P7 - Inbox / Exceptions Queue

Use for My Work, attendance exceptions, payroll exceptions, AI Learning Inbox.

Focus on:

- Priority
- Reason
- Owner
- Age
- Required action
- Bulk resolution when safe

### Pattern P8 - Configuration / Builder

Use for payroll components, policies, pipeline stages, templates, mappings.

Rules:

- Advanced builders require explicit save/publish behavior.
- Draft and published states where changes have operational consequences.
- Configuration changes must display impact and effective date where relevant.

---

# PART II - MODULE SCREEN INVENTORY

## 7. Home and Global Work

Estimated primary screens: **3-4**.

### HOME-001 - Home

**Purpose:** Role-aware landing workspace.

Content is composed dynamically from licensed modules and permissions.

#### HR version

- Headcount
- New hires
- Exits
- Today's attendance
- Absence summary
- Pending leave
- Expiring contracts/documents
- Payroll readiness
- Recruitment status
- Critical HR tasks

#### Payroll version

- Current payroll period
- Processed vs total employees
- Exceptions
- Missing inputs
- Variance from previous period
- Pending approvals
- Export/payment readiness

#### Recruiter version

- Open requisitions
- Open jobs
- New applicants
- Interviews today/upcoming
- Offers pending
- Hiring tasks

#### Manager version

- My team
- Present/absent/on leave today
- Pending approvals
- Upcoming team events
- Contract/document alerts in scope

#### Employee version

- Attendance status
- Leave balances
- Latest payslip
- Upcoming holiday
- Open requests
- Documents/tasks requiring attention

### HOME-002 - My Work

Universal work queue.

Types may include:

- Leave approval
- Attendance correction
- Salary/compensation change approval
- Payroll approval
- Job requisition approval
- Offer approval
- Employee lifecycle task
- AI-generated action awaiting confirmation

Views:

- Pending
- Completed
- Delegated
- Created by me

### HOME-003 - Notifications Center

Categories:

- Needs Action
- Information
- System
- AI / Insights (optional)

### HOME-004 - Global Search / Command Palette

Can be implemented as an overlay instead of a normal page.

---

## 8. People Module

Estimated primary screens: **8** plus profile tabs.

### PEO-001 - Employees

Pattern: P2 List/Data Grid.

Core functions:

- Add employee
- Search employees
- Saved views
- Advanced filters
- Bulk actions
- Import/export
- Column customization

Recommended filters:

- Legal entity
- Branch
- Department
- Team
- Position
- Manager
- Employment type
- Employee status
- Contract status
- Work location
- Join date

### PEO-002 - Employee Profile

Pattern: P3 Detail Workspace.

Recommended tabs:

1. Overview
2. Employment
3. Compensation
4. Attendance
5. Leave
6. Payroll
7. Documents
8. Performance
9. Assets
10. Timeline

Not every user sees every tab.

#### Overview tab

- Photo/avatar
- Employee number
- Name
- Position
- Department
- Manager
- Legal entity
- Branch/location
- Employment status
- Primary contact details
- Contract summary
- Leave summary
- Today's attendance
- Latest payroll summary if authorized
- Alerts
- Open tasks

#### Employment tab

- Employment periods
- Job/position history
- Department/manager assignment history
- Contract details
- Employment type
- Effective dates
- Status transitions

#### Compensation tab

- Current compensation
- Effective-dated compensation history
- Components/allowances
- Change history
- Approval state

#### Timeline tab

Unified authorized activity feed:

- Join
- Promotion
- Transfer
- Compensation change
- Contract renewal
- Leave milestones
- Warnings/actions if licensed
- Documents
- Exit

### PEO-003 - Organization Structure

Administrative hierarchy editor/viewer:

```text
Tenant
  Legal Entity
    Branch / Work Location
      Department
        Team
          Position
```

### PEO-004 - Organization Chart

Interactive reporting hierarchy visualization.

Functions:

- Search employee
- Focus on manager
- Collapse/expand teams
- Switch legal entity
- Open employee quick view

### PEO-005 - Jobs & Positions

- Job definitions
- Position slots
- Filled/vacant status
- Reporting relationships
- Headcount planning hooks for future use

### PEO-006 - Onboarding / Offboarding

Pattern: P4 Guided Process or P7 Inbox depending on view.

- Templates
- Employee onboarding instance
- Tasks by owner
- Due dates
- Documents required
- Equipment/assets
- Account/access tasks
- Exit checklist

### PEO-007 - Documents Center

Cross-employee operational view.

- Employee documents
- Document types
- Missing documents
- Expiring documents
- Expired documents
- Bulk upload / import where safe

### PEO-008 - Assets

If asset tracking is included in the product:

- Assigned assets
- Unassigned assets
- Employee assignment history
- Return status

---

## 9. Attendance & Time Module

Estimated primary screens: **7**.

### TIM-001 - Time Overview

Pattern: P1 Operational Dashboard.

Primary daily indicators:

- Present
- Absent
- Late
- On leave
- Remote/field status if supported
- Missing check-out
- Overtime
- Open exceptions

### TIM-002 - Daily Attendance

Pattern: P2 Data Grid.

Columns typically include:

- Employee
- Shift
- Scheduled time
- Check-in
- Check-out
- Worked hours
- Late minutes
- Early departure
- Overtime
- Status
- Exception count

### TIM-003 - Timesheets / Time Records

Period-based detail for employees and managers.

### TIM-004 - Shifts

- Shift definitions
- Break rules
- Grace periods
- Shift status

### TIM-005 - Schedules

Pattern: P6 Calendar/Schedule.

- Assign employees/teams to shifts
- Recurrence
- Exceptions
- Coverage gaps

### TIM-006 - Attendance Exceptions

Pattern: P7 Exceptions Queue.

Examples:

- Missing check-in
- Missing check-out
- Late arrival
- Early departure
- Unapproved overtime
- Duplicate punch
- Device/import mismatch

Actions:

- Resolve
- Correct
- Request clarification
- Ignore with reason
- Bulk action where policy permits

### TIM-007 - Devices & Imports

- Registered devices/connectors
- Import batches
- Import status
- Errors
- Mapping
- Reprocessing
- Last synchronization

---

## 10. Leave Module

Estimated primary screens: **5**.

### LEA-001 - Leave Overview

- Requests requiring action
- Employees currently on leave
- Upcoming leave
- Balance risks
- Policy alerts

### LEA-002 - Leave Requests

Pattern: P2 List + detail drawer.

Views:

- All
- Pending
- Approved
- Rejected
- Cancelled

### LEA-003 - Team Leave Calendar

Pattern: P6 Calendar.

Must support:

- Team/department filter
- Leave type filter
- Overlap visualization
- Public holidays
- Manager view

### LEA-004 - Leave Balances

- Employee
- Leave type
- Opening
- Accrued
- Used
- Pending
- Remaining
- Carryover
- Adjustments

### LEA-005 - Leave Policies

Pattern: P8 Configuration.

- Policy definition
- Eligibility
- Entitlement
- Accrual
- Carryover
- Approval requirements
- Effective dates

Statutory minimums and company enhancements should be distinguishable.

---

## 11. Payroll Module

Estimated primary screens: **10-12**.

Payroll is a high-risk operational module and should have a guided workspace rather than loose pages.

### PAY-001 - Payroll Overview

Pattern: P1 Dashboard.

- Current periods
- Readiness status
- Exceptions
- Pending approvals
- Payroll cost summary
- Period-over-period variance
- Finalized periods
- Upcoming deadlines

### PAY-002 - Payroll Periods / Runs

Pattern: P2 List.

- Period
- Legal entity
- Run type
- Status
- Employee count
- Total cost
- Created by
- Finalized by

### PAY-003 - Payroll Run Workspace

Pattern: P4 Guided Workspace.

Recommended process:

```text
1. Inputs
2. Validation
3. Calculate
4. Exceptions
5. Review
6. Approve
7. Finalize
8. Pay / Export
```

Persistent run header:

- Period
- Legal entity
- Status
- Employees ready / total
- Blocking exceptions
- Total payroll cost
- Variance from prior period
- Last calculation timestamp

#### Step 1 - Inputs

- Compensation snapshot readiness
- Attendance input
- Leave input
- Variable earnings/deductions
- Imported adjustments
- New hires/exits

#### Step 2 - Validation

- Missing data
- Invalid bank/payment data
- Missing tax/insurance data
- Conflicting employment states
- Closed/invalid dates

#### Step 3 - Calculate

- Calculation job state
- Successful/failed employee calculations
- Re-run controls

#### Step 4 - Exceptions

- Blocking vs non-blocking
- Group by cause
- Assign owner
- Resolve or recalculate

#### Step 5 - Review

- Employee result grid
- Cost totals
- Components
- Variances
- Outlier indicators

#### Step 6 - Approve

- Approval chain
- Required sign-offs
- Comments

#### Step 7 - Finalize

- Final validation
- Explicit irreversible-state warning
- Finalization authority

#### Step 8 - Pay / Export

- Bank file
- Payment batch
- Accounting export
- Payslip release

### PAY-004 - Payroll Input Review

Can exist as a step within PAY-003 and/or a focused route.

### PAY-005 - Payroll Exceptions

Pattern: P7.

### PAY-006 - Payroll Results

Pattern: P2.

Key fields:

- Employee
- Gross
- Earnings
- Deductions
- Statutory deductions
- Net
- Variance
- Status

### PAY-007 - Employee Calculation / Explain Payroll

Signature screen.

Structure:

```text
Employee + Period

Earnings
- Basic
- Allowances
- Overtime
- Bonus

Deductions
- Attendance deductions
- Social insurance
- Tax
- Other deductions

Net Pay
```

Every material line should support **Why?** or **Explain**.

Explain panel may show:

- Source input
- Rule/policy
- Rule version
- Effective date
- Formula/calculation trace
- Legal/company reference when applicable
- Approval/change source

### PAY-008 - Variance Analysis

Compare periods.

Must support:

- Total payroll movement
- Employee-level variance
- Component-level variance
- New hires
- Exits
- Salary changes
- Overtime changes
- Bonuses/commissions
- AI explanation where allowed

### PAY-009 - Payroll Approvals

Can integrate with My Work, but payroll should also expose run-specific approval state.

### PAY-010 - Payslips

- Generated
- Released
- Delivery/download status
- Reissue controls
- Employee access

### PAY-011 - Exports

- Bank/payment exports
- Accounting exports
- Export history
- Validation/errors
- Download audit

### PAY-012 - Settlements

Termination/final settlement workspace:

- Employee
- Termination reason/date
- Payroll due
- Unused leave
- Recoveries
- statutory/company settlement components
- approval
- final statement

---

## 12. Recruitment / ATS

Estimated primary screens: **9**.

### REC-001 - Recruitment Dashboard

- Open requisitions
- Open jobs
- New applications
- Pipeline conversion
- Interviews today/upcoming
- Offers pending
- Time-to-hire
- Hiring tasks

### REC-002 - Job Requisitions

Pattern: P2.

- Hiring request
- Department
- Position
- Headcount
- Requester
- Reason
- Approval state
- Target date

### REC-003 - Job Openings / Postings

- Job details
- Publishing state
- Channels
- Application counts
- Opening/closing dates

### REC-004 - Candidates

Pattern: P2.

- Search
- Skills/tags
- Source
- Current application stage
- Jobs applied to
- Consent/privacy status

### REC-005 - Recruitment Pipeline

Pattern: P5 Kanban.

Typical stages:

```text
Applied
Screening
Shortlisted
Interview
Final Interview
Offer
Hired
```

Stages are configurable within controlled limits.

### REC-006 - Candidate Profile

Pattern: P3 Detail Workspace.

Tabs:

1. Overview
2. Resume / Documents
3. Applications
4. Interviews
5. Evaluations
6. Communication
7. Offers
8. Timeline

### REC-007 - Interviews

Pattern: P6 Calendar + list.

- Interview schedule
- Participants
- meeting/location
- evaluation status

### REC-008 - Offers

- Draft
- Approval
- Version history
- Sent
- Accepted/rejected/expired
- Hire conversion readiness

### REC-009 - Careers Management

- Published jobs
- Careers portal configuration
- application form settings
- public content hooks

For on-prem customers, public careers exposure must follow the infrastructure security design and must never expose the internal database directly.

---

## 13. Performance Management

Target-state estimated primary screens: **6**.

### PER-001 - Performance Overview

### PER-002 - Review Cycles

### PER-003 - Goals / OKRs

### PER-004 - Reviews

### PER-005 - Competencies

### PER-006 - Results / Calibration

Performance is architecture-ready but may be delivered after the first operational HR/payroll releases.

---

## 14. Reports & Analytics

Estimated primary screens: **4**.

### REP-001 - Executive Dashboard

Cross-domain workforce indicators with authorization filtering.

### REP-002 - Report Library

Categories:

- People
- Attendance
- Leave
- Payroll
- Recruitment
- Performance
- Compliance

### REP-003 - Report Builder

Pattern: P8 Configuration/Builder.

Initial builder should be constrained and safe rather than a universal BI platform.

Capabilities may include:

- Select dataset
- Select fields
- Filters
- Grouping
- Sorting
- Saved report
- Export

### REP-004 - Scheduled / Exported Reports

If scheduled delivery is supported.

---

## 15. AI Copilot

AI is not a standalone chatbot feature. It is a cross-product interaction layer.

Estimated primary screens: **4**, plus a persistent side panel.

### AI-001 - Contextual Copilot Side Panel

Accessible from any authorized page.

Examples:

On Payroll Run:

> Why did total payroll increase this month?

On Employee Profile:

> Summarize this employee's attendance for the last quarter.

On Recruitment Job:

> Which shortlisted candidates best match this role and why?

Context must be explicit in the UI so the user understands what the AI is referencing.

### AI-002 - Full AI Workspace

Sections:

- Ask
- Analyze
- Explain
- Recent conversations
- Suggested insights

### AI-003 - AI Learning Center / Learning Inbox

Admin-only operational queue.

Categories:

- New questions
- Low-confidence answers
- Negative feedback
- Knowledge gaps
- Candidate FAQs
- Tool gaps

Actions:

- Approve as knowledge
- Edit and approve
- Reject
- Create internal tool requirement
- Create evaluation case
- Mark duplicate / merge intent

### AI-004 - AI Quality & Usage

- External model usage
- Token/cost metrics
- Answer source distribution
- Positive/negative feedback
- Low-confidence rate
- Tool success/failure
- Knowledge promotion rate
- Evaluation pass rate

### 15.1 AI answer routing UX

The user does not need to see internal complexity, but the product should be able to label answer provenance when useful:

- Company data
- Approved company policy
- Approved product knowledge
- Payroll calculation trace
- External general AI

For sensitive answers, show citations/references to the internal source where possible.

### 15.2 AI actions

AI can propose actions but does not bypass normal application controls.

Example:

```text
User: Increase Ahmed's transportation allowance to 2,000 EGP next month.

AI proposes:
Employee: Ahmed Hassan
Component: Transportation
Current: 1,500 EGP
New: 2,000 EGP
Effective: 01 Sep 2026
Impact: Future payroll

[Confirm] [Cancel]
```

Confirmation then invokes the same authorized backend command used by normal UI workflows.

---

## 16. Administration

Estimated primary screens: **8-10**.

### ADM-001 - Company / Tenant

- General company information
- localization defaults
- enabled product modules

### ADM-002 - Legal Entities

- Legal entity data
- statutory identifiers
- entity-level settings

### ADM-003 - Organization Administration

- branches
- work locations
- departments
- teams
- positions

### ADM-004 - Users & Roles

- users
- roles
- permission assignments
- scope assignments
- authentication/SSO hooks

Technical system-admin access must not imply HR/payroll data permission.

### ADM-005 - Global Policies

Cross-module company policies where appropriate.

### ADM-006 - Integrations

- Odoo/ERP connectors
- attendance devices
- email/notification providers
- banking connectors
- integration health
- synchronization logs

### ADM-007 - Notifications & Templates

- email templates
- in-app templates
- notification rules

### ADM-008 - AI Configuration

- AI enabled/disabled
- provider configuration
- cloud/private mode
- external model limits
- knowledge settings
- feedback settings

Secrets must never be exposed to normal frontend users after configuration.

### ADM-009 - System Operations

Depending on deployment model:

- version information
- health summary
- license status
- backup status
- update status

### ADM-010 - Audit Explorer

If exposed as a dedicated authorized administrative screen.

---

# PART III - CROSS-PRODUCT UX CAPABILITIES

## 17. Global Search

Search categories:

- Employees
- Candidates
- Requisitions/jobs
- Payroll runs
- Documents
- Reports
- Commands

Requirements:

- Permission filtered before results are returned.
- Recent searches.
- Keyboard navigation.
- Entity type labels.
- Fast open to detail workspace.
- Optional command execution for safe actions.

---

## 18. Quick Create

Role-aware examples:

- Add employee
- Request leave
- Create requisition
- Add candidate
- Start payroll run
- Upload document
- Create report

Do not show actions the user cannot execute.

---

## 19. My Work / Unified Approval Inbox

Every approval-producing module integrates with the same user work model.

Work item display should include:

- Type
- Subject/entity
- Requester
- Created date
- Due date/SLA if used
- Priority
- Summary of requested change
- Required action

Detail should open in context without forcing the user to lose their place.

---

## 20. Notifications

Design notifications as a priority system rather than a message dump.

Recommended categories:

1. Needs Action
2. Important Information
3. System
4. AI Insight (optional)

Notification rules:

- Avoid duplicates.
- Aggregate noisy events.
- Mark what requires action.
- Link directly to the affected workspace.

---

## 21. Filters, Saved Views, and Data Grids

Shared behavior across all grids is mandatory.

### Standard grid features

- Free-text search
- Quick filters
- Advanced filters
- Clear all
- Saved personal views
- Shared team views where allowed
- Column chooser
- Sort
- Density preference
- Bulk actions
- Export permissions

### URL state

Where practical, filters/sort/page state should be reflected in the URL so users can bookmark or share authorized views.

---

## 22. Drawers vs Dialogs vs Pages

### Use a drawer for

- Quick employee/candidate preview
- Reviewing an approval
- Inspecting a calculation explanation
- Viewing lightweight record details without losing the list context

### Use a modal dialog for

- Confirmations
- Small single-purpose forms
- Destructive/irreversible warnings

### Use a full page/workspace for

- Complex editing
- Payroll processes
- Employee/candidate profiles
- Configuration with many dependencies
- Reports/builders

Avoid deeply nested modals.

---

## 23. State Design

Every screen specification must account for:

- Loading
- Empty
- Partial data
- Error
- Permission denied
- Read-only
- Draft
- Pending approval
- Finalized/locked
- Archived

Sensitive modules must visually distinguish editable versus finalized records.

---

## 24. Responsive Behavior

### 24.1 Heavy desktop workflows

Desktop-first:

- Payroll run workspace
- Payroll variance
- Organization editing
- Recruitment pipeline
- Report builder
- Administration

On small screens, these may provide read-only summaries or focused actions rather than reproducing every desktop feature.

### 24.2 Mobile-priority browser workflows

Must be first-class responsive experiences:

- Home
- My Profile
- My Team
- Leave request
- Leave balance
- Attendance status
- My Work / approvals
- Payslips
- Notifications
- AI Copilot

### 24.3 Tables on mobile

Do not horizontally compress a 15-column table into unreadable content.

Use:

- Priority columns
- Row cards
- Detail drawer/page
- Horizontal scroll only when genuinely required

---

## 25. RTL / LTR Requirements

Arabic and English are architectural requirements, not final-stage styling work.

Rules:

- Entire navigation mirrors correctly.
- Directional icons mirror only when semantically directional.
- Tables remain readable in both modes.
- Numbers, currencies, dates, and formulas use locale-aware formatting.
- Mixed Arabic/English employee names and identifiers must not break layouts.
- Do not hardcode labels in components.

---

## 26. Accessibility Baseline

Even before the final visual system, frontend patterns should support:

- Keyboard navigation
- Visible focus
- Proper labels
- Semantic tables
- Accessible form errors
- Screen-reader status announcements for async actions
- Non-color-only status indicators
- Accessible alternatives to drag-and-drop

---

# PART IV - SCREEN COUNT AND RELEASE SLICES

## 27. Mature Product Screen Estimate

| Area | Approximate primary screens |
|---|---:|
| Home / My Work / Global | 3-4 |
| People | 8 |
| Attendance & Time | 7 |
| Leave | 5 |
| Payroll | 10-12 |
| Recruitment | 9 |
| Performance | 6 |
| Reports | 4 |
| AI | 4 |
| Administration | 8-10 |
| **Mature product total** | **64-69** |

This is a route/workspace estimate, not a count of every tab, drawer, dialog, or configuration sub-screen.

---

## 28. Recommended Release 1 Frontend Scope

Target approximately **32-38 primary screens/workspaces**, depending on first commercial release boundaries.

Recommended inclusion:

### Global

- Home
- My Work
- Notifications
- Search/command palette

### People

- Employees
- Employee Profile
- Organization Structure
- Org Chart
- Jobs/Positions
- Documents Center
- Basic onboarding/offboarding

### Time

- Overview
- Daily attendance
- Exceptions
- Shifts/schedules as required
- Imports/devices where customer integration requires them

### Leave

- Overview
- Requests
- Calendar
- Balances
- Policies

### Payroll

- Overview
- Runs
- Run workspace
- Exceptions
- Results
- Employee calculation/explanation
- Variance
- Payslips
- Exports
- Settlement if commercially required

### Reports

- Report library
- Essential operational reports

### Administration

- Company/legal entity
- Organization
- Users/roles
- Payroll configuration
- Integrations relevant to deployment

### AI

- Contextual Copilot shell
- Initially read-only or explain-oriented
- Full action execution only after tool/permission/evaluation maturity

---

## 29. Deferred Frontend Scope

Candidate deferred areas unless commercially required:

- Full Performance Management
- Advanced Report Builder
- Advanced Configuration Studio
- Full AI action automation
- AI Learning Center polish beyond administrator minimum
- Broad careers customization
- Advanced talent analytics
- Marketplace/plugin UX

Architecture may reserve space for them without forcing Release 1 implementation.

---

# PART V - FRONTEND TEAM CONTRACTS

## 30. Feature Folder Model

Recommended logical frontend layout:

```text
src/
  app/
    routing/
    providers/
    shell/

  design-system/
    primitives/
    forms/
    data-grid/
    feedback/
    navigation/
    overlays/
    charts/

  shared/
    auth/
    permissions/
    api/
    localization/
    formatting/
    hooks/
    utilities/

  features/
    home/
    people/
    time/
    leave/
    payroll/
    recruitment/
    performance/
    reports/
    ai/
    administration/
```

Business modules should not create private replacements for shared primitive components without a documented reason.

---

## 31. Screen Specification Template

Every primary screen should have a small specification file containing:

```text
Screen ID:
Name:
Module:
Purpose:
Primary roles:
Route:
Page pattern:
Permissions:
Data/API dependencies:
Primary actions:
Secondary actions:
Filters:
Columns / key fields:
State machine impact:
Loading state:
Empty state:
Error state:
Read-only/finalized behavior:
Responsive behavior:
RTL notes:
Analytics/audit events:
Acceptance criteria:
```

This allows frontend developers to work independently while preserving consistent behavior.

---

## 32. Frontend / Backend Contract Rules

- Frontend does not query the database.
- Frontend uses generated typed clients from OpenAPI or equivalent machine-readable contracts.
- Every mutation has an explicit command endpoint and defined error model.
- Authorization is enforced by backend; frontend hiding is convenience, not security.
- List APIs must define pagination/filter/sort behavior consistently.
- Sensitive values are returned only when authorized.
- Concurrency/version conflicts must have a consistent user-facing resolution pattern.

---

## 33. Route Naming Convention

Illustrative convention:

```text
/home
/work

/people/employees
/people/employees/:employeeId
/people/organization
/people/org-chart
/people/documents

/time/overview
/time/attendance
/time/exceptions
/time/shifts

/leave/overview
/leave/requests
/leave/calendar
/leave/balances

/payroll/overview
/payroll/runs
/payroll/runs/:runId
/payroll/payslips
/payroll/settlements

/recruitment/dashboard
/recruitment/requisitions
/recruitment/jobs
/recruitment/candidates
/recruitment/candidates/:candidateId
/recruitment/pipeline/:jobId
/recruitment/interviews
/recruitment/offers

/reports
/ai
/admin/...
```

Routes should be stable and human-readable.

---

# PART VI - VISUAL BENCHMARKING PLAN

## 34. Objective of Visual Benchmarking

The next phase will collect visual references from competitors and strong adjacent products.

The goal is **not to copy a competitor**. The goal is to identify the best interaction and information-density patterns for each page type.

For every reference captured, record:

```text
Product:
Screen / pattern:
What works:
What does not work:
What fits our product:
What conflicts with our workflow:
What we want to test:
```

---

## 35. Benchmark by Screen Pattern, Not by Brand

### Employee Profile

Collect references from modern HR/HCM products.

Focus on:

- Profile header
- Tab structure
- alerts
- history/timeline
- sensitive compensation presentation

### Payroll Run

Focus on:

- guided workflow
- readiness
- exceptions
- employee review
- variance
- finalization warnings

### Recruitment Pipeline

Collect from specialized ATS products as well as HR suites.

Focus on:

- card density
- stage movement
- candidate preview
- filters
- collaboration/evaluation

### Data Grids

Focus on:

- filters
- saved views
- bulk actions
- row density
- column customization

### Approvals / Inbox

Focus on:

- priority
- comparison before/after
- context without navigation loss
- mobile behavior

### Analytics

Focus on decision-support dashboards, not decorative charts.

### AI Copilot

Study modern enterprise copilots.

Focus on:

- context indication
- citations/provenance
- proposed action confirmation
- conversation history
- switching between Ask / Analyze / Act

---

## 36. Reference Capture Categories

Create visual-reference folders such as:

```text
Ref/
  Frontend/
    01-App-Shell/
    02-Home-Dashboards/
    03-Data-Grids/
    04-Employee-Profile/
    05-Attendance/
    06-Leave/
    07-Payroll/
    08-Recruitment/
    09-Approvals/
    10-Reports/
    11-AI-Copilot/
    12-Administration/
    13-Mobile-Responsive/
```

This is better than storing screenshots grouped only by competitor name.

---

# PART VII - ACCEPTANCE RULES FOR THE FRONTEND BLUEPRINT

## 37. Cross-Module Consistency Checklist

Before a module is considered frontend-complete:

- Uses the shared shell.
- Uses one of the approved page patterns.
- Uses shared data-grid behavior.
- Uses shared form validation patterns.
- Handles loading/empty/error/permission states.
- Supports Arabic RTL and English LTR.
- Supports required responsive behaviors.
- Respects generated API contracts.
- Hides unauthorized actions but does not rely on hiding for security.
- Uses My Work for common approvals where appropriate.
- Emits audit-relevant commands through backend APIs.
- Does not invent module-local global navigation concepts.

---

## 38. UX Definition of Done per Screen

A primary screen is not done when only the happy path renders.

Minimum Definition of Done:

1. Primary workflow works.
2. Permission behavior works.
3. Loading state exists.
4. Empty state exists.
5. API error state exists.
6. Validation messages are understandable.
7. Keyboard flow is usable.
8. Responsive behavior is defined.
9. RTL behavior is verified.
10. Finalized/read-only states are handled where relevant.
11. Acceptance tests cover critical actions.
12. No duplicate design-system primitives were created without approval.

---

# PART VIII - PRIORITY DECISIONS BEFORE VISUAL DESIGN

## 39. Decisions Already Recommended

The following decisions should be treated as the baseline unless later research produces a strong reason to change them:

1. Sidebar contains high-level modules only.
2. Home is role-aware, not separate products for each role.
3. Approvals use a universal `My Work` inbox.
4. Employee information is centered around one Employee Profile workspace.
5. Candidate information is centered around one Candidate Profile workspace.
6. Payroll uses a guided Run Workspace.
7. Recruitment uses a pipeline/Kanban where it matches domain state.
8. AI is both contextual side panel and full workspace.
9. Administration is permission-restricted and not visible to normal users.
10. Heavy operational workflows are desktop-first; employee/manager workflows are responsive-first.
11. Mature product complexity is hidden behind role and entitlement filtering.
12. Approximately eight reusable page patterns should generate the majority of screens.

---

## 40. Decisions to Make During Visual Benchmarking

The next phase should determine, through references and prototypes:

- Sidebar behavior: fixed, collapsible, compact mode.
- Top-bar density.
- Employee Profile header structure.
- Whether detail tabs remain sticky on long pages.
- Default data-grid density.
- Drawer widths and behavior.
- Payroll run stepper behavior.
- Recruitment Kanban card information density.
- Dashboard KPI density.
- AI panel width and context display.
- Mobile bottom navigation for employee/manager experiences, if any.
- How strongly admin/configuration screens differ from operational screens.

Do not decide these based on one competitor screenshot.

---

# PART IX - IMPLEMENTATION ORDER FOR FRONTEND TEAM

## 41. Foundation Slice

Build before module teams diverge:

1. Application shell
2. Routing and route guards
3. Authentication session handling
4. Permission-aware navigation
5. Localization / RTL foundation
6. Design-system primitives
7. Data grid baseline
8. Forms baseline
9. Drawer/modal/toast patterns
10. Loading/empty/error patterns
11. API client generation pipeline
12. Global error handling
13. Global search shell
14. My Work shell
15. AI panel shell

The goal is to create the common language all module developers will use.

---

## 42. Parallel Module Workstreams

After the foundation contract is stable, teams can work in parallel.

### Workstream A - People / Organization

- Employees
- Employee Profile
- Organization
- Documents
- Onboarding/offboarding

### Workstream B - Time / Leave

- Attendance
- Exceptions
- Shifts
- Leave requests
- Calendar
- Balances

### Workstream C - Payroll

- Payroll overview
- Run workspace
- employee calculations
- variance
- payslips
- exports

### Workstream D - Recruitment

- Requisitions
- jobs
- candidates
- pipeline
- interviews
- offers

### Workstream E - Platform / Admin / Reports

- My Work
- administration
- integrations
- reporting framework

### Workstream F - AI

- Copilot panel
- AI workspace
- learning center
- quality/usage

Each workstream must use the same common page patterns and design-system components.

---

## 43. Integration Milestones

Recommended frontend integration milestones:

### Milestone 1 - Shell

Users can authenticate, navigate, switch language/direction, and see permission-filtered modules.

### Milestone 2 - People Core

Employee list and profile establish the core UX language.

### Milestone 3 - Workforce Operations

Attendance, leave, and My Work share common operational patterns.

### Milestone 4 - Payroll Operational Flow

End-to-end payroll run is navigable and testable.

### Milestone 5 - Recruitment

ATS workflows integrate without changing the shared shell.

### Milestone 6 - AI Context Layer

AI can read context and answer from approved tools/knowledge.

### Milestone 7 - Visual System Refinement

After visual benchmarking, refine spacing, typography, brand, density, motion, charts, and responsive polish without rewriting the information architecture.

---

# PART X - SUMMARY

## 44. Frontend Target State

The Workforce Platform should mature into approximately **64-69 primary screens/workspaces**, but users should experience a much smaller product because navigation is filtered by role, permission, and licensed module.

The primary frontend architecture is:

```text
Shared Application Shell
        |
        +-- Home / My Work / Search / Notifications / AI
        |
        +-- Workforce
        |     +-- People
        |     +-- Time
        |     +-- Leave
        |     +-- Payroll
        |
        +-- Talent
        |     +-- Recruitment
        |     +-- Performance
        |
        +-- Insights
        |     +-- Reports
        |     +-- AI Copilot
        |
        +-- Administration
```

Most screens should be produced from eight reusable UX patterns:

1. Dashboard
2. Data Grid
3. Detail Workspace
4. Guided Wizard
5. Kanban
6. Calendar/Schedule
7. Inbox/Exceptions
8. Configuration/Builder

The design phase should now focus on **visual benchmarking of these patterns**, not random competitor screenshots.

The most important frontend product principle remains:

> **Build a fast operational workforce system where users complete work, understand why outcomes happened, and can use AI to ask, analyze, explain, and safely act - rather than a dashboard-heavy HR application.**

---

## Appendix A - Suggested Screen ID Catalog

```text
HOME-001 Home
HOME-002 My Work
HOME-003 Notifications
HOME-004 Global Search

PEO-001 Employees
PEO-002 Employee Profile
PEO-003 Organization Structure
PEO-004 Organization Chart
PEO-005 Jobs & Positions
PEO-006 Onboarding / Offboarding
PEO-007 Documents Center
PEO-008 Assets

TIM-001 Time Overview
TIM-002 Daily Attendance
TIM-003 Timesheets
TIM-004 Shifts
TIM-005 Schedules
TIM-006 Attendance Exceptions
TIM-007 Devices & Imports

LEA-001 Leave Overview
LEA-002 Leave Requests
LEA-003 Team Leave Calendar
LEA-004 Leave Balances
LEA-005 Leave Policies

PAY-001 Payroll Overview
PAY-002 Payroll Runs
PAY-003 Payroll Run Workspace
PAY-004 Payroll Input Review
PAY-005 Payroll Exceptions
PAY-006 Payroll Results
PAY-007 Employee Calculation / Explain Payroll
PAY-008 Variance Analysis
PAY-009 Payroll Approvals
PAY-010 Payslips
PAY-011 Exports
PAY-012 Settlements

REC-001 Recruitment Dashboard
REC-002 Job Requisitions
REC-003 Job Openings
REC-004 Candidates
REC-005 Recruitment Pipeline
REC-006 Candidate Profile
REC-007 Interviews
REC-008 Offers
REC-009 Careers Management

PER-001 Performance Overview
PER-002 Review Cycles
PER-003 Goals / OKRs
PER-004 Reviews
PER-005 Competencies
PER-006 Results / Calibration

REP-001 Executive Dashboard
REP-002 Report Library
REP-003 Report Builder
REP-004 Scheduled / Exported Reports

AI-001 Contextual Copilot
AI-002 AI Workspace
AI-003 AI Learning Center
AI-004 AI Quality & Usage

ADM-001 Company / Tenant
ADM-002 Legal Entities
ADM-003 Organization Administration
ADM-004 Users & Roles
ADM-005 Global Policies
ADM-006 Integrations
ADM-007 Notifications & Templates
ADM-008 AI Configuration
ADM-009 System Operations
ADM-010 Audit Explorer
```

## Appendix B - Module Owner Handoff Checklist

Before assigning a frontend module to a developer, provide:

- Module scope
- Screen IDs
- User roles
- Routes
- Permissions
- API/OpenAPI contracts or mocks
- State transitions
- Shared pattern to use per screen
- Required design-system components
- Responsive requirements
- RTL requirements
- Acceptance criteria
- Cross-module dependencies

The developer should not independently redefine navigation, shared grids, global approvals, AI behavior, or common design primitives.

