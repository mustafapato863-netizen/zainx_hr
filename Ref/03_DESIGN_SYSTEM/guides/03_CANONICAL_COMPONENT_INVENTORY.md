# Canonical Component Inventory

Legend:
- **P0** required before module implementation
- **P1** required for Release 1 product screens
- **P2** target-state / advanced

| Family | Component | Priority | Required variants / states |
|---|---|---:|---|
| Foundation | ThemeProvider contract | P0 | light, dark, system |
| Foundation | DirectionProvider contract | P0 | LTR, RTL |
| Foundation | DensityProvider contract | P0 | compact, standard, comfortable |
| Foundation | MotionProvider contract | P0 | full, reduced |
| Primitive | Button | P0 | primary, secondary, tertiary, ghost, danger; xs-sm-md-lg |
| Primitive | IconButton | P0 | neutral, primary, danger; tooltip |
| Primitive | Input | P0 | default, focus, error, warning, disabled, read-only |
| Primitive | NumberInput | P0 | min/max, step, formatted |
| Primitive | CurrencyInput | P0 | EGP/other, masked, negative |
| Primitive | PercentageInput | P0 | decimal, range |
| Primitive | SearchInput | P0 | clear, loading |
| Primitive | Textarea | P0 | count, resize rules |
| Primitive | Checkbox | P0 | checked, unchecked, indeterminate |
| Primitive | Radio | P0 | single/group |
| Primitive | Switch | P0 | on/off/disabled |
| Primitive | Badge | P0 | neutral/info/success/warning/danger/ai |
| Primitive | Tag | P0 | static, removable |
| Primitive | Avatar | P0 | image, initials, status |
| Primitive | AvatarGroup | P0 | overflow |
| Primitive | Tooltip | P0 | top/right/bottom/left |
| Primitive | Separator | P0 | horizontal, vertical |
| Form | Field | P0 | helper/error/warning/success |
| Form | Select | P0 | single |
| Form | SearchableSelect | P0 | async/loading/no-results |
| Form | Combobox | P0 | typed/free or constrained |
| Form | MultiSelect | P0 | chips, search |
| Form | DatePicker | P0 | single |
| Form | DateRangePicker | P1 | ranges, presets |
| Form | TimePicker | P1 | 12/24h |
| Form | EffectiveDateGroup | P0 | from/to/open-ended/overlap warning |
| Form | EmployeePicker | P1 | avatar/context/permission-filtered |
| Form | ManagerPicker | P1 | manager chain context |
| Form | OrgPicker | P1 | department/team/position |
| Form | LegalEntityPicker | P1 | tenant/legal entity |
| Form | FileUpload | P1 | browse/drop/progress/error/scan |
| Navigation | Sidebar | P0 | expanded, compact, mobile drawer |
| Navigation | Topbar | P0 | desktop |
| Navigation | MobileNav | P1 | employee/manager only |
| Navigation | Breadcrumb | P0 | LTR/RTL |
| Navigation | Tabs | P0 | underline, contained, scrollable, sticky |
| Navigation | PageHeader | P0 | list/detail/process/settings |
| Navigation | PageToolbar | P0 | filters/views/actions |
| Navigation | SectionHeader | P0 | title/count/action |
| Navigation | ContextSwitcher | P1 | tenant/legal entity |
| Navigation | CommandPalette | P1 | entities/navigation/actions |
| Navigation | QuickCreate | P1 | role-aware |
| Navigation | Pagination | P0 | page/cursor compatible |
| Overlay | Dialog | P0 | sm/md/lg |
| Overlay | ConfirmDialog | P0 | safe confirmation |
| Overlay | IrreversibleDialog | P1 | payroll/rule publish |
| Overlay | Drawer | P0 | 360/440/520/640/760 |
| Overlay | Popover | P0 | filters/date/actions |
| Overlay | DropdownMenu | P0 | icons/destructive/separators |
| Overlay | ContextMenu | P1 | row/item |
| Feedback | Alert | P0 | info/success/warning/danger/ai |
| Feedback | Banner | P0 | page-level |
| Feedback | Toast | P0 | action/progress |
| Feedback | Skeleton | P0 | card/grid/profile/workspace |
| Feedback | EmptyState | P0 | first use/no data/success |
| Feedback | NoResults | P0 | filters/search |
| Feedback | AccessDenied | P0 | branded access gate |
| Feedback | ErrorState | P0 | 400/401/404/409/429/500 |
| Feedback | OfflineState | P1 | offline/on-prem network |
| Feedback | ConflictState | P1 | optimistic concurrency |
| Feedback | FinalizedState | P1 | immutable payroll |
| Data | DataTable | P0 | semantic display |
| Data | DataGrid | P0 | full enterprise behavior |
| Data | FilterBar | P0 | quick/advanced |
| Data | FilterBuilder | P1 | AND/OR |
| Data | SavedViews | P1 | personal/shared/default |
| Data | ColumnChooser | P1 | visibility/order |
| Data | BulkActionBar | P1 | selection scope |
| Data | Money | P0 | currency/variance/masked |
| Data | SensitiveValue | P0 | masked/revealed/denied |
| Data | KPI | P0 | number/currency/percent/progress |
| Data | Timeline | P1 | employee/candidate/audit |
| Data | Tree | P1 | org/permissions |
| Data | Calendar | P1 | month/week/day/agenda |
| Data | Chart | P1 | line/bar/stacked/waterfall/sparkline |
| Enterprise | Stepper | P1 | complete/current/upcoming/blocked/warning |
| Enterprise | ComparisonDiff | P1 | before/after |
| Enterprise | WorkflowHeader | P1 | state/readiness/actions |
| Enterprise | WorkItem | P1 | approval/task |
| People | EmployeeCell | P1 | compact/standard |
| People | EmployeeHeader | P1 | profile |
| People | EmployeeQuickPreview | P1 | drawer |
| People | EmploymentSummary | P1 | current/history |
| People | AssignmentHistory | P1 | effective-dated |
| People | CompensationSummary | P1 | sensitive |
| People | CompensationHistory | P1 | effective-dated |
| People | EmployeeTimeline | P1 | filtered |
| People | OrgNode | P1 | org chart |
| Attendance | AttendanceStatus | P1 | present/absent/late/leave/exception |
| Attendance | AttendanceDayTimeline | P1 | expected vs actual |
| Attendance | PunchPair | P1 | check in/out |
| Attendance | ShiftSummary | P1 | scheduled/actual |
| Attendance | AttendanceException | P1 | blocking/warning/info |
| Attendance | CorrectionComparison | P1 | before/after |
| Attendance | DeviceHealth | P1 | healthy/warning/failed/offline |
| Leave | LeaveBalance | P1 | available/used/pending/carryover |
| Leave | LeaveRequestSummary | P1 | status/impact |
| Leave | LeaveRequestForm | P1 | balance preview |
| Leave | LeavePolicySummary | P1 | statutory/company |
| Payroll | PayrollRunHeader | P1 | editable/finalized |
| Payroll | PayrollStepper | P1 | 8-step process |
| Payroll | PayrollReadiness | P1 | spotlight-capable |
| Payroll | PayrollException | P1 | blocking/warning/info |
| Payroll | PayrollResultRow | P1 | gross/net/variance |
| Payroll | PayrollCalculationBreakdown | P1 | earnings/deductions/statutory/net |
| Payroll | PayrollLine | P1 | source/explain |
| Payroll | PayrollTrace | P1 | rules/input/formula |
| Payroll | RuleReference | P1 | version/effective/legal source |
| Payroll | PayrollVariance | P1 | amount/percent/reason |
| Payroll | PayslipStatus | P1 | generated/released/delivery |
| Payroll | PaymentBatch | P1 | created/exported/failed |
| Payroll | FinalizePayrollDialog | P1 | irreversible |
| Recruitment | CandidateCell | P1 | list |
| Recruitment | CandidateCard | P1 | kanban compact/standard |
| Recruitment | CandidateHeader | P1 | profile |
| Recruitment | CandidateQuickPreview | P1 | drawer |
| Recruitment | PipelineColumn | P1 | count/WIP/status |
| Recruitment | InterviewSchedule | P1 | time/participants |
| Recruitment | EvaluationScorecard | P1 | competency/rating/comment |
| Recruitment | OfferSummary | P1 | version/comp/start |
| Recruitment | OfferStatus | P1 | draft-to-accepted |
| Recruitment | HireConversionState | P1 | ready/in-progress/complete/error |
| Approval | ApprovalComparison | P1 | before/after |
| Approval | ApprovalChain | P1 | steps/actors/status |
| Approval | DelegationBadge | P1 | delegated-by/to |
| Admin | SettingsNavigation | P1 | grouped |
| Admin | PermissionMatrix | P1 | role x permission x scope |
| Admin | ScopePicker | P1 | self/team/dept/entity/tenant |
| Admin | IntegrationCard | P1 | provider/status/last sync |
| Admin | IntegrationHealth | P1 | healthy/warning/failed |
| Admin | SystemHealth | P1 | services/storage/worker |
| Admin | BackupStatus | P1 | last/verified/failed |
| Admin | AuditEntry | P1 | actor/action/entity/time |
| AI | AIContextBar | P1 | current page/entity |
| AI | AIContextChip | P1 | removable/source |
| AI | AISourceBadge | P1 | provenance |
| AI | AIComposer | P1 | ask/analyze/explain/act |
| AI | AIAnswer | P1 | answer/source/actions |
| AI | AIInsightCard | P1 | spotlight optional |
| AI | AIThinking | P1 | truthful context/tool stages |
| AI | AIToolExecution | P1 | running/success/error |
| AI | AIActionProposal | P1 | proposed mutation |
| AI | AIActionConfirmation | P1 | confirm/edit/cancel |
| AI | AIFeedback | P1 | rating/reason |
| AI | AILearningItem | P2 | review queue |
| AI | AIQualityMetric | P2 | quality/usage |
| Signature | BrandAssembly | P1 | login/startup |
| Signature | AccessGate | P1 | 403 |
| Signature | SpotlightEffect | P1 | allowed surfaces only |
| Signature | SuccessResolve | P1 | high-value success |
| Signature | AIContextScan | P1 | analysis progress |
