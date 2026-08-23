# Responsive, RTL, Accessibility & Localization Guide

## Responsive responsibility

### Desktop-first
Payroll Run  
Payroll Variance  
Administration  
Report Builder  
Recruitment Pipeline  
Complex configuration

### Responsive-first
Employee Home  
Manager Home  
My Team  
Attendance status  
Leave  
Payslips  
Approvals  
Notifications  
AI Copilot

## Mobile rule
Do not compress desktop grids into unreadable columns.

Prefer:
priority information,
row cards,
focused action views,
detail page/drawer,
local horizontal scroll only where genuinely required.

---

# RTL

Use CSS logical properties.

Bad:
`margin-left`

Prefer:
`margin-inline-start`

Test:
- Arabic full names,
- Arabic + English names,
- employee numbers,
- email,
- EGP,
- decimals,
- dates,
- percentage,
- formula snippets.

### Directional icons
Mirror:
arrows,
chevrons,
back/forward.

Do not mirror:
search,
calendar,
settings,
status,
user,
file.

### Tables
Do not blindly reverse every business column.

Define a logical content order for Arabic. Numeric columns remain visually scannable.

### Drawers
Contextual drawer can open from logical end by default; validate per workflow.

### Stepper
Sequence direction mirrors while numbers/status remain correct.

---

# Accessibility component contracts

## Tabs
Arrow navigation, Home/End, active tab semantics.

## Menu
Arrow navigation, Escape, typeahead where appropriate.

## Dialog
Focus trap, initial focus, restore focus, Escape unless blocked by high-risk confirmation policy.

## Drawer
Same focus expectations as dialog if modal; non-modal drawer must preserve page navigation semantics.

## Combobox
ARIA combobox/listbox semantics.

## Data Grid
Semantic table/grid depending on interactivity.
Keyboard focus strategy must be documented.
Bulk selection must have accessible label.

## Kanban
Drag-and-drop cannot be the only stage-change mechanism.
Provide menu/button move actions.

## Calendar
Provide accessible agenda/list alternative.

## Toast
Use polite/assertive live region according to severity.
Do not move keyboard focus to toast automatically.

## AI streaming
Do not announce every streamed token.
Announce completion or meaningful state changes.

## Progress
Expose real status text and value when known.

## Error
Associate input error to field programmatically.

## Status
Never use color alone.

---

# Contrast / focus

Target WCAG AA minimum.

Focus ring:
visible in light and dark.

Do not remove outline without replacement.

High-risk actions should remain distinguishable in high contrast.

---

# Localization

All labels come from localization keys.

Formatting layer handles:
- dates,
- time,
- currency,
- percentages,
- pluralization,
- numbers.

Do not format currency manually inside components.

Money component receives structured value + currency.
