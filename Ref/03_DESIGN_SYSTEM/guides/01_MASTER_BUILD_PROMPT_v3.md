# ZainX Workforce — Master Design System Build Prompt v3.0

Use this prompt with a capable design/coding model to build the canonical ZainX Workforce design system.

---

# ROLE

You are simultaneously acting as:

- Principal Product Designer
- Enterprise UX Architect
- Design Systems Lead
- Motion Designer
- Accessibility Lead
- Arabic/RTL Product Designer
- Senior React/TypeScript Engineer
- Design QA Lead

You are building a real design system for a long-lived enterprise Workforce / HR / Payroll / Compliance / Talent platform.

Do not stop at a visual showcase.

Do not output a generic admin template.

Do not output only screenshots.

Do not output a single page containing attractive components with no behavioral specification.

Your result must be a **system that another frontend team can implement without inventing missing design decisions**.

---

# 1. PRODUCT CHARACTER

ZainX Workforce must feel:

- enterprise-grade,
- trustworthy,
- operational,
- financially serious,
- compact,
- high-information-density,
- premium,
- distinctive,
- calm under normal conditions,
- visually expressive only when the moment deserves it.

Use these two non-negotiable principles:

> **The system is quiet. Important things glow.**

> **Light is an event, not decoration.**

The default UI is quiet.

Glow, spotlight, chromatic light, and expressive animation are reserved for:
- brand entry,
- app startup,
- major AI insight,
- payroll readiness,
- critical blocker,
- access/security event,
- irreversible workflow,
- high-value success,
- explicit active focus.

Do not use glow as a default card style.

---

# 2. UX PHILOSOPHY

This is an operational product.

The primary verbs are:

Approve  
Correct  
Calculate  
Review  
Resolve  
Hire  
Explain  
Export  
Configure  
Request  
Finalize  
Act

Design screens around:

1. Current context
2. Current state
3. What requires attention
4. What can be acted on
5. What changed
6. Why the result exists
7. Who can see/modify it
8. What happens next

Dashboards are secondary to work.

---

# 3. PRODUCT AREAS

The design system must support:

## Global
- App Shell
- Home
- My Work
- Notifications
- Global Search
- Quick Create
- Context Switcher
- User/Profile

## People
- Employee Directory
- Employee Profile
- Employment
- Compensation
- Organization
- Org Chart
- Jobs/Positions
- Documents
- Assets
- Onboarding/Offboarding

## Time
- Attendance Overview
- Daily Attendance
- Timesheets
- Shifts
- Schedules
- Exceptions
- Devices/Imports

## Leave
- Overview
- Requests
- Calendar
- Balances
- Policies

## Payroll
- Overview
- Periods/Runs
- Run Workspace
- Input Review
- Exceptions
- Results
- Employee Calculation
- Explanation
- Variance
- Approvals
- Payslips
- Exports
- Settlements

## Recruitment
- Dashboard
- Requisitions
- Jobs
- Candidates
- Pipeline
- Candidate Profile
- Interviews
- Offers
- Careers Management

## Reports
- Report Library
- Report Builder
- Scheduled/Exported Reports
- Executive Analytics

## AI
- Contextual Copilot
- Full AI Workspace
- AI Learning Center
- AI Quality/Usage
- AI Action Proposals

## Administration
- Company
- Legal Entities
- Organization
- Users
- Roles
- Permissions
- Policies
- Payroll Configuration
- Attendance Configuration
- Leave Configuration
- Recruitment Configuration
- Integrations
- Notifications
- AI Configuration
- System Operations
- Audit

---

# 4. HARD OUTPUT CONTRACT

Build and document ALL of the following.

## Foundations
- color primitives
- semantic color tokens
- light theme
- dark theme
- typography
- Arabic fallback
- spacing
- radius
- borders
- shadows
- elevation
- opacity
- iconography
- motion
- effects
- responsive
- RTL
- z-index
- focus
- data-viz palette
- density
- layout dimensions

## Primitive Components
- Button
- IconButton
- LinkButton
- Input
- NumberInput
- CurrencyInput
- PercentageInput
- SearchInput
- PasswordInput
- Textarea
- Checkbox
- Radio
- Switch
- SegmentedControl
- Toggle
- Badge
- Tag
- Avatar
- AvatarGroup
- Separator
- Tooltip
- Progress
- Spinner

## Form Components
- Field
- FormSection
- Select
- SearchableSelect
- Combobox
- MultiSelect
- TagInput
- DatePicker
- DateRangePicker
- TimePicker
- EmployeePicker
- ManagerPicker
- LegalEntityPicker
- DepartmentPicker
- PositionPicker
- FileUpload
- EffectiveDateFieldGroup
- ValidationSummary

## Navigation
- Sidebar
- MobileNav
- Topbar
- Breadcrumb
- Tabs
- ContextSwitcher
- CommandPalette
- QuickCreate
- UserMenu
- Pagination
- PageHeader
- PageToolbar
- SectionHeader

## Data Display
- DataTable
- DataGrid
- GridCell
- EmployeeCell
- Money
- SensitiveValue
- KPI
- Card
- EmphasisCard
- SpotlightCard
- DescriptionList
- Timeline
- ActivityFeed
- Tree
- OrgNode
- Calendar
- Charts

## Feedback
- Alert
- Banner
- Callout
- Toast
- Skeleton
- EmptyState
- NoResults
- ErrorState
- AccessDenied
- OfflineState
- MaintenanceState
- PartialDataWarning
- ConflictState
- LockedState
- FinalizedState
- SuccessMoment

## Overlays
- Dialog
- ConfirmDialog
- DestructiveDialog
- IrreversibleDialog
- Drawer
- Popover
- DropdownMenu
- ContextMenu
- CommandOverlay

## Enterprise Components
- FilterBar
- QuickFilter
- FilterBuilder
- SavedViews
- ColumnChooser
- SortBuilder
- BulkActionBar
- DensitySwitcher
- ExportMenu
- Stepper
- WorkflowHeader
- ComparisonDiff

## Product Components
Implement every component listed later in this prompt.

---

# 5. COMPONENT SPECIFICATION CONTRACT

For EVERY component provide:

1. Name
2. Category
3. Purpose
4. When to use
5. When not to use
6. Anatomy
7. Required props
8. Optional props
9. Variants
10. Sizes
11. Visual states
12. Interaction states
13. Loading state
14. Empty state if relevant
15. Error state if relevant
16. Read-only state
17. Permission-limited state
18. Locked/finalized state if relevant
19. Icons
20. Keyboard behavior
21. Focus behavior
22. Screen-reader semantics
23. Motion
24. Reduced-motion behavior
25. Responsive behavior
26. RTL behavior
27. Localization concerns
28. Sensitive-data concerns
29. Audit implications
30. Example usage
31. Do / Don't
32. Acceptance criteria
33. Recommended React TypeScript API

Do not call a component complete until these are documented.

---

# 6. TOKEN ARCHITECTURE

Never let application components consume raw colors directly unless the value is purely illustrative.

Use layers:

```text
Primitive
→ Semantic
→ Component
→ State
```

Example:

```text
color.neutral.100
→ surface.subtle
→ button.secondary.background
→ button.secondary.hover.background
```

Required semantic groups:

## Surfaces
canvas  
sidebar  
topbar  
panel  
panel-subtle  
raised  
card  
card-hover  
input  
selected  
floating  
tooltip  
overlay  
spotlight

## Text
primary  
secondary  
tertiary  
disabled  
inverse  
link  
danger  
success  
warning  
ai  
sensitive

## Border
subtle  
default  
strong  
selected  
focus  
disabled  
danger  
warning  
success  
ai  
luminous

## Actions
primary  
secondary  
tertiary  
danger  
success  
ghost

For each action:
default  
hover  
pressed  
focus  
disabled  
loading

## Status
success  
warning  
danger  
info  
pending  
draft  
locked  
finalized  
archived  
ai

---

# 7. LIGHT AND DARK THEMES

Both themes must be first-class.

Do not design dark first and mechanically invert.

### Light
- calm neutral canvas,
- white/near-white working surfaces,
- subtle cool borders,
- strong dark text,
- spotlight expressed using low-opacity tint and chromatic edge.

### Dark
- avoid absolute black,
- distinguish canvas/panel/card using small luminance steps,
- preserve strong text contrast,
- glow can be slightly more visible than light theme but still restrained.

Every semantic token must have a light and dark mapping.

---

# 8. DENSITY

Support:

Compact  
Standard  
Comfortable

Recommended:
- admin/payroll/data grid defaults: Compact or Standard
- employee self-service: Standard
- touch/mobile: Comfortable touch targets even when visual density is compact.

Do not reduce tap targets below accessible mobile sizes.

---

# 9. ICONOGRAPHY

Use Lucide-style line icons as the functional base.

Stroke:
1.75px standard.

Sizes:
12 / 14 / 16 / 18 / 20 / 24.

No random mixed icon libraries.

Create a registry containing:
- semantic name,
- Lucide icon,
- default size,
- whether RTL mirroring is required,
- whether animation is permitted.

## Core navigation mapping

Home → House  
My Work → Inbox or ListChecks  
People → Users  
Time → Clock3  
Leave → CalendarDays  
Payroll → WalletCards or ReceiptText  
Recruitment → BriefcaseBusiness  
Performance → Target  
Reports → ChartNoAxesCombined  
AI → custom ZainX Intelligence Mark  
Administration → Settings2

## Key system icons

Search → Search  
Filter → SlidersHorizontal  
Advanced Filter → ListFilter  
Sort → ArrowUpDown  
Columns → Columns3  
Export → Download  
Import → Upload  
Refresh → RefreshCw  
Add → Plus  
Edit → Pencil  
Delete → Trash2  
Archive → Archive  
More → Ellipsis  
Copy → Copy  
External → ExternalLink  
Lock → Lock  
Finalized → ShieldCheck  
Warning → TriangleAlert  
Error → CircleX  
Success → CircleCheck  
Info → Info  
Pending → Clock  
Draft → FilePenLine

## Directional icon rule

Mirror in RTL only if semantic direction changes:
- arrows,
- chevrons,
- back/forward.

Do not mirror:
- search,
- user,
- calendar,
- status,
- settings,
- document.

---

# 10. CUSTOM AI MARK

Do not use Sparkles as the main AI identity.

Use a custom ZainX Intelligence Mark derived from brand geometry.

It should communicate:
- connected context,
- convergence,
- reasoning,
- action.

States:
idle  
context-attached  
thinking  
tool-running  
answer-ready  
action-proposed  
confirmed  
blocked

Until a vector brand source exists, use the provided brand mark as static artwork and keep path-animation APIs prepared for SVG replacement.

---

# 11. MOTION TOKENS

instant 80ms  
micro 140ms  
standard 220ms  
context 320ms  
expressive 640ms  
brand 900ms

Easing:
productive  
enter  
exit  
expressive

Routine work must stay fast.

Expressive motion must never delay the user's action.

---

# 12. SIGNATURE EFFECT SYSTEM

## Spotlight

Allowed on:
- payroll readiness,
- main AI insight,
- critical operational summary,
- login,
- brand moment,
- major success.

Recommended baseline:
- radius: 280–420px desktop
- radius: static 220–320px touch
- surface tint: 3–8%
- border illumination alpha: 10–24%
- bloom opacity: extremely low
- no visible blur over text.

Pointer-follow only on fine pointers.

On touch:
use static radial placement.

Reduced motion:
no tracking; static highlight only.

## Luminous Edge

Use a 1px local edge/border fragment.

Do not glow the whole card by default.

## Neon

Allowed:
- AI-specific moment,
- payroll completion,
- active brand moment,
- focused spotlight,
- security/access boundary.

Forbidden:
- normal settings cards,
- regular employee rows,
- every KPI,
- ordinary forms,
- every table row.

## Success Resolve

400–700ms:
1. state changes,
2. local edge illuminates,
3. check/shield resolves,
4. light fades to stable status.

No confetti in payroll/compliance.

## Access Gate

300–600ms:
1. structural boundary appears,
2. one scan passes,
3. locked state resolves,
4. motion stops.

Tone:
serious, premium, not playful.

## AI Context Scan

Show only truthful stages:
Context  
Authorized data/tool  
Policy/rule  
Answer

Never simulate a source or tool that was not actually used.

---

# 13. BRAND LOGIN / STARTUP

Build a distinct login/startup experience.

### Initial frame
- calm canvas,
- ZainX brand mark,
- one subtle localized light field,
- login surface.

### Brand entry
1. vector segments appear,
2. segments assemble,
3. brief localized light convergence,
4. stable mark,
5. auth surface becomes primary.

### Successful login
1. form state resolves,
2. authentication success is acknowledged,
3. mark becomes transitional anchor,
4. application shell appears,
5. active workspace loads via real skeleton.

Do not add fake loading.

### Logout
1. active content quiets,
2. session closes,
3. mark becomes last stable brand element,
4. login state returns.

---

# 14. BUTTON SYSTEM

Variants:
Primary  
Secondary  
Tertiary  
Ghost  
Danger  
Success-rare  
Icon  
Split

Sizes:
xs / sm / md / lg

States:
default  
hover  
pressed  
focus  
disabled  
loading  
permission-disabled

Rules:
- no width shift when loading,
- icon-only buttons always have accessible name/tooltip,
- dangerous action never appears visually equivalent to safe primary action,
- do not make every action primary.

---

# 15. FIELD SYSTEM

All fields share one field wrapper.

Anatomy:
Label  
Required marker  
Control  
Prefix/Suffix  
Helper text  
Validation text  
Optional counter

States:
default  
hover  
focus  
filled  
read-only  
disabled  
error  
warning  
success  
loading

Never hide validation only in color.

---

# 16. ENTERPRISE PICKERS

Build:
EmployeePicker  
ManagerPicker  
DepartmentPicker  
PositionPicker  
LegalEntityPicker

They require:
- search,
- entity icon/avatar,
- primary label,
- secondary context,
- recent items where useful,
- permission filtering,
- empty/no-results,
- keyboard navigation,
- virtualization compatibility.

---

# 17. DATE / EFFECTIVE-DATE SYSTEM

Effective dating is important in ZainX.

Build:
DatePicker  
DateRangePicker  
EffectiveDateGroup

EffectiveDateGroup should support:
- effective from,
- optional effective to,
- overlap warning,
- future-date indicator,
- historical/read-only state,
- policy/rule effective period.

---

# 18. DATA GRID — HARD REQUIREMENT

DataGrid is one of the canonical components.

Required features:

Search  
Quick filters  
Advanced filters  
Saved views  
Sorting  
Multi-sort  
Column chooser  
Resize  
Reorder  
Pinning  
Grouping  
Totals  
Pagination  
Cursor pagination support  
Virtualization compatibility  
Bulk selection  
Bulk action bar  
Export  
Row actions  
Expandable rows  
Quick preview  
Density  
Keyboard navigation  
Empty  
No results  
Loading  
Error  
Retry  
Permission-hidden columns  
Sensitive cells  
Read-only/finalized rows

## Grid row heights
Compact 36  
Standard 44  
Comfortable 52

## Money
Right aligned in LTR; locale-logical alignment in RTL.
Use tabular numerals.

## Selection
Clearly separate:
current page selected  
all matching records selected

Never imply all records are selected when only visible rows are selected.

---

# 19. FILTER MODEL

Filters use:

Field  
Operator  
Value

Operators:
equals  
not equals  
contains  
not contains  
starts with  
greater than  
less than  
between  
before  
after  
is empty  
is not empty  
is any of  
is none of

Support AND/OR groups.

The UI should keep common filters simple and hide advanced logic until requested.

---

# 20. SAVED VIEWS

Support:
personal  
shared  
default  
temporary/unsaved

Actions:
save  
save as  
rename  
duplicate  
share  
set default  
delete

Show unsaved-view changes clearly.

---

# 21. APP SHELL

Sidebar is high-level only.

Sections:

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

Features:
- permission filtering,
- entitlement filtering,
- compact mode,
- collapsed tooltips,
- badges,
- active state,
- mobile drawer mode.

Do not expose every subpage.

---

# 22. TOPBAR

Keep quiet.

Contains as appropriate:
- context/breadcrumb,
- legal entity/company context,
- search,
- quick create,
- My Work,
- notifications,
- AI,
- user menu.

Avoid seven equally prominent controls.

---

# 23. COMMAND PALETTE

Ctrl/Cmd + K.

Search:
Employees  
Candidates  
Jobs  
Payroll Runs  
Reports  
Documents  
Navigation  
Commands

Keyboard-first.

Return only authorized records.

---

# 24. PAGE HEADER

Variants:
List  
Detail  
Process  
Settings  
Report

Anatomy:
Breadcrumb  
Title  
Description/context  
Status  
Primary action  
Secondary actions  
Optional metadata

---

# 25. STATUS SYSTEM

Every status uses:
icon + label + semantic treatment.

Do not use color only.

Generic:
Active  
Inactive  
Draft  
Pending  
Approved  
Rejected  
Cancelled  
Completed  
Archived  
Locked

Payroll:
Draft  
Inputs Loaded  
Calculated  
Under Review  
Approved  
Finalized  
Outputs Published

Recruitment:
Applied  
Screening  
Shortlisted  
Interview  
Final Interview  
Offer  
Hired  
Rejected  
Withdrawn

Attendance:
Present  
Absent  
Late  
On Leave  
Incomplete  
Exception

---

# 26. MONEY AND SENSITIVE VALUES

Money component:
- amount,
- currency,
- locale,
- compact format,
- variance,
- sign,
- masked.

SensitiveValue:
Masked  
Visible  
Permission denied

Never reveal salary/bank/national ID on hover.

Reveal requires explicit control and permission.

---

# 27. CARD HIERARCHY

Utility:
neutral, no glow.

Emphasis:
semantic tint + stronger edge.

Spotlight:
rare focal point.

Recommended visual budget:
one dominant Spotlight treatment per major viewport region.
Do not place multiple competing glow cards together.

---

# 28. LOADING SYSTEM

Never use one generic spinner everywhere.

App:
brand entry then real skeleton.

Page:
structural skeleton.

Grid:
column-aware skeleton rows.

Drawer:
local skeleton.

Payroll:
real processing state.

AI:
context/tool/source progress when known.

File:
upload progress + scan/validation status.

---

# 29. ERROR / PERMISSION STATES

Build:
400  
401  
403  
404  
409  
429  
500  
Maintenance  
Offline

Each includes:
- human explanation,
- recovery action,
- reference ID when useful,
- no technical stack trace.

403 uses Access Gate.

409 uses conflict-resolution pattern.

---

# 30. PEOPLE PRODUCT COMPONENTS

Build:

EmployeeHeader  
EmployeeCell  
EmployeeAvatar  
EmployeeStatus  
EmployeeQuickPreview  
EmployeeSummary  
EmploymentSummary  
AssignmentHistory  
EmploymentPeriod  
ManagerRelation  
CompensationSummary  
CompensationHistory  
DocumentExpirySummary  
EmployeeTimeline  
OrgNode  
PositionStatus  
AssetAssignment

EmployeeHeader:
avatar  
name  
employee number  
position  
department  
manager  
legal entity  
status  
primary actions

EmployeeQuickPreview:
identity  
position  
manager  
contact  
status  
today attendance  
leave status  
open alerts

Compensation:
sensitive by default according to permission.

---

# 31. ATTENDANCE COMPONENTS

AttendanceStatus  
AttendanceSummary  
AttendanceDayTimeline  
PunchPair  
ShiftSummary  
WorkedHours  
LateIndicator  
OvertimeIndicator  
AttendanceException  
CorrectionComparison  
ScheduleCell  
DeviceHealth  
AttendanceImportRun

AttendanceDayTimeline:
scheduled start/end  
actual check-in/out  
breaks  
overtime  
exceptions

Use clear expected-vs-actual encoding.

---

# 32. LEAVE COMPONENTS

LeaveBalance  
LeaveBalanceBreakdown  
LeaveRequestSummary  
LeaveRequestForm  
LeaveImpactPreview  
LeaveStatus  
TeamLeaveCell  
LeavePolicySummary  
LeavePolicyEffectivePeriod

LeaveBalance:
Opening  
Accrued  
Used  
Pending  
Remaining  
Carryover  
Adjustments

Avoid decorative circular charts as default.

---

# 33. PAYROLL COMPONENTS

PayrollPeriodCard  
PayrollRunHeader  
PayrollRunStatus  
PayrollReadiness  
PayrollStepper  
PayrollInputSource  
PayrollException  
PayrollResultRow  
PayrollCalculationBreakdown  
PayrollLine  
PayrollTrace  
RuleReference  
PayrollVariance  
VarianceReason  
PayrollApprovalState  
PayslipStatus  
PaymentBatch  
ExportRun  
SettlementSummary  
FinalizePayrollDialog

## Payroll Run process
1 Inputs
2 Validation
3 Calculate
4 Exceptions
5 Review
6 Approve
7 Finalize
8 Pay / Export

Persistent header:
Period  
Legal Entity  
Status  
Ready/Total  
Blocking Exceptions  
Total Cost  
Variance  
Last Calculated

## Payroll line
Label  
Amount  
Category  
Source  
Explain action

Expanded:
Input  
Rule  
Rule version  
Effective date  
Formula  
Legal reference  
Company policy  
Audit source

## Finalized
No Edit.
Allowed:
View  
Export  
Create Adjustment

---

# 34. RECRUITMENT COMPONENTS

CandidateCell  
CandidateCard  
CandidateHeader  
CandidateQuickPreview  
PipelineColumn  
ApplicationStage  
StageAge  
InterviewSchedule  
InterviewParticipant  
EvaluationScorecard  
OfferSummary  
OfferStatus  
RequisitionSummary  
JobOpeningSummary  
HireConversionState

CandidateCard must remain compact.

Show:
name  
role  
stage  
source  
rating  
next event  
age in stage

Do not place full resume content on Kanban cards.

Provide keyboard/non-drag move action.

---

# 35. APPROVAL COMPONENTS

WorkItem  
WorkItemPreview  
ApprovalComparison  
ApprovalChain  
ApprovalStep  
DelegationBadge  
PriorityIndicator  
Age/SLA

ApprovalComparison:
Before  
→  
After

Used for:
salary  
allowance  
employee change  
requisition  
offer  
policy change

---

# 36. ADMIN COMPONENTS

SettingsNavigation  
SettingsSection  
PermissionMatrix  
RoleSummary  
ScopePicker  
ConfigStatus  
DraftPublishedState  
EffectiveDateSummary  
IntegrationCard  
IntegrationHealth  
SyncRun  
SystemHealth  
BackupStatus  
UpdateStatus  
AuditEntry

Admin can be more technical, but must remain the same product.

---

# 37. AI COMPONENTS

AIContextBar  
AIContextChip  
AISourceBadge  
AIComposer  
AIAnswer  
AIInsightCard  
AIThinking  
AIToolExecution  
AIActionProposal  
AIActionConfirmation  
AIFeedback  
AILearningItem  
AIQualityMetric  
AIUsageBudget

## AI source labels
Company Data  
Company Policy  
Product Knowledge  
Payroll Trace  
External AI

External AI must never be visually presented as authoritative business truth.

## AI Action Proposal
Always show:
what  
who/what entity  
current value  
proposed value  
effective date  
impact  
permission/risk  
confirm/edit/cancel

Sensitive actions may require approval.

---

# 38. PAGE PATTERNS

Every page must instantiate one of these:

P1 Operational Dashboard  
P2 Data Grid / List  
P3 Detail Workspace  
P4 Guided Process  
P5 Kanban / Pipeline  
P6 Calendar / Schedule  
P7 Inbox / Exceptions  
P8 Configuration / Builder

Do not invent new top-level patterns without a reason.

---

# 39. DASHBOARD PATTERN

Structure:
PageHeader  
Critical blockers  
3–6 meaningful KPIs  
Work requiring action  
Diagnostic trend  
Recent/upcoming work

Do not produce a 12-card KPI grid.

---

# 40. DETAIL WORKSPACE

Structure:
EntityHeader  
Status/context  
Primary actions  
Sticky/contained tabs as appropriate  
Main content  
Optional contextual drawer  
Timeline/audit

Used for:
Employee  
Candidate  
Requisition  
Legal Entity

---

# 41. GUIDED PROCESS

Structure:
ProcessHeader  
State  
Stepper  
Blockers  
Current step content  
Primary next action  
Save/resume  
Exit

Used for:
Payroll  
Imports  
Onboarding  
Offboarding  
complex configuration.

---

# 42. INBOX / EXCEPTIONS

Structure:
View/filter controls  
Priority list/grid  
Age/owner/reason  
Preview  
Required actions  
Safe bulk action

Used for:
My Work  
Attendance Exceptions  
Payroll Exceptions  
AI Learning Inbox

---

# 43. RESPONSIVE RULES

Desktop-first:
Payroll  
Admin  
Report Builder  
Recruitment Pipeline  
Complex configuration

Responsive-first:
Employee Home  
Manager Home  
My Team  
Leave  
Attendance status  
Payslips  
Approvals  
Notifications  
AI

On mobile:
do not squeeze 15-column grids.

Use:
priority columns,
row cards,
detail page/drawer,
horizontal scroll only where unavoidable.

---

# 44. RTL RULES

Arabic and English are both first-class.

Use logical CSS properties.

Test:
mixed Arabic/Latin names,
EGP,
employee IDs,
emails,
dates,
formulas,
tables,
charts,
steppers,
drawers,
breadcrumbs,
timelines,
command palette.

Directional icons mirror semantically.

Tab order follows visual/logical direction.

---

# 45. ACCESSIBILITY

Target WCAG AA.

Every component must define:
keyboard navigation,
focus,
screen-reader name/role/state,
errors,
status announcements,
non-color semantics,
touch target,
reduced motion.

Kanban drag must have non-drag alternative.

AI streaming must not spam live regions.

Grid navigation must remain usable with keyboard.

---

# 46. IMPLEMENTATION REQUIREMENTS

Target:
React + TypeScript + Vite.

Use:
semantic tokens,
CSS variables,
composable APIs,
headless behavior where useful,
typed props,
forward refs where appropriate,
controlled/uncontrolled state only where justified.

Do not couple a primitive to payroll/HR logic.

Product components may compose primitives and enterprise components.

---

# 47. REACT API REQUIREMENT

For each component propose a typed API.

Example shape:

```ts
type StatusTone =
  | "neutral"
  | "info"
  | "success"
  | "warning"
  | "danger"
  | "ai";

interface StatusBadgeProps {
  label: string;
  tone: StatusTone;
  icon?: ReactNode;
  size?: "sm" | "md";
}
```

Do not produce `any`.

Do not expose implementation-only visual props when a semantic prop can exist.

Bad:
`color="#34D399"`

Good:
`tone="success"`

---

# 48. DOCUMENTATION SITE REQUIREMENT

Build documentation sections:

Foundations  
Controls  
Forms  
Navigation  
Data  
Feedback  
Overlays  
Patterns  
People  
Attendance  
Leave  
Payroll  
Recruitment  
Approvals  
Reports  
Administration  
AI  
Signature Motion  
Accessibility/RTL  
Implementation

Every component page includes:
anatomy,
variants,
states,
usage,
code/API,
do/don't,
RTL,
a11y,
responsive,
motion.

---

# 49. REQUIRED STRESS-TEST SCREENS

Build high-fidelity functional references for:

01 Employee Directory  
02 Employee Profile  
03 Payroll Run  
04 Payroll Explanation  
05 Recruitment Pipeline  
06 Candidate Profile  
07 My Work  
08 Attendance Exceptions  
09 Leave Calendar  
10 Roles & Permissions  
11 AI Copilot on Payroll Context  
12 Login / Startup  
13 Access Denied

Also create:
- mobile Employee Home,
- mobile Leave Request,
- mobile Approval,
- Arabic RTL Employee Profile,
- Arabic RTL Payroll summary.

No design system approval before these screens work.

---

# 50. QA MATRIX

Every major component and stress screen must be tested across:

Theme:
Light / Dark

Direction:
LTR / RTL

Density:
Compact / Standard / Comfortable

Motion:
Full / Reduced

Viewport:
Desktop / laptop / tablet / mobile as applicable

State:
Default / Loading / Empty / Error / Permission / Read-only / Finalized

Data:
Short / Long / Arabic / mixed script / large numbers / missing optional fields

---

# 51. ANTI-GENERIC-AI RULES

Reject output if it resembles:
- default shadcn admin template,
- generic purple AI dashboard,
- Linear clone,
- bento marketing page,
- glassmorphism demo,
- cyberpunk neon UI,
- huge rounded cards,
- excessive empty whitespace,
- sparkle icon everywhere,
- gradients everywhere,
- decorative charts,
- hover-only essential actions.

Do not use:
`rounded-2xl + huge shadow + gradient`
as the universal component formula.

---

# 52. REFERENCE RESPONSIBILITY

Learn behavior from:
Linear  
Attio  
Stripe  
Rippling  
Deel  
Ashby  
Intercom  
Carbon

Use 21st.dev selectively for:
spotlight,
controlled glow,
micro-interactions,
auth composition,
motion primitives.

Do not copy any product's visual identity.

---

# 53. DELIVERY PHASES

Do not attempt everything as one unstructured page.

## Phase 1 — Audit & Tokens
Semantic tokens, themes, typography, icon registry, motion/effects.

## Phase 2 — Primitives
Buttons, fields, inputs, selections, badges, avatars, tooltip.

## Phase 3 — Navigation + Overlays
Shell, sidebar, topbar, tabs, menus, command palette, dialog, drawer.

## Phase 4 — Enterprise Data
DataGrid, filters, saved views, bulk actions, pagination, timeline, calendar.

## Phase 5 — Product Components
People, Attendance, Leave, Payroll, Recruitment, Approval, Admin, AI.

## Phase 6 — Signature
Brand motion, spotlight, access gate, success, AI context scan.

## Phase 7 — Stress Screens
Build all required screens.

## Phase 8 — QA
Test matrix and fix inconsistencies.

Do not declare completion before Phase 8.

---

# 54. DEFINITION OF DONE

The design system is complete only when:

- all semantic tokens exist,
- light/dark work,
- RTL/LTR work,
- reduced motion works,
- all component families are documented,
- product components are documented,
- typed APIs exist,
- all stress-test screens exist,
- grid behaves consistently,
- permissions are represented,
- sensitive values are safe,
- finalized states are explicit,
- keyboard flows work,
- mobile employee/manager flows work,
- AI provenance is visible,
- motion is distinctive but restrained,
- no screen looks like a generic generated SaaS template.

Final quality statement:

> **Operationally serious. Visually memorable. Intelligently restrained.**
