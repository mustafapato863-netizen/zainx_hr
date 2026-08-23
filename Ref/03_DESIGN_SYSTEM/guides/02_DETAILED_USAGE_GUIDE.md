# ZainX Workforce Design System — Detailed Usage Guide

This guide explains how product designers, frontend developers, and AI coding/design agents should use the system.

---

# 1. Decision order

When designing a new screen, never begin with a component.

Use this order:

1. Identify the user and permission scope.
2. Identify the business object/context.
3. Identify the primary verb/action.
4. Identify the workflow state.
5. Select one of the eight page patterns.
6. Define data density.
7. Select shared enterprise components.
8. Add product components.
9. Apply semantic statuses.
10. Add signature visual effects only if the moment qualifies.
11. Define loading/error/empty/permission/finalized states.
12. Verify responsive/RTL/accessibility.
13. Only then polish.

---

# 2. Visual hierarchy rule

Every viewport should have one clear visual hierarchy.

Recommended priority:

**1. Critical blocker or current process state**  
**2. Primary entity/context**  
**3. Primary action**  
**4. Work/data**  
**5. Secondary metrics**  
**6. Metadata**

Avoid multiple equal hero cards.

---

# 3. Card selection

## Utility Card
Use when the card is simply grouping content.

Examples:
- employee contact summary,
- integration metadata,
- policy detail,
- secondary KPI.

Do not add glow.

## Emphasis Card
Use when content needs attention but is not the main focal moment.

Examples:
- expiring contract,
- attendance exception count,
- pending approval.

Use:
- semantic edge,
- 3–6% surface tint,
- stronger icon,
- no large bloom.

## Spotlight Card
Use only when the information should become the focal point.

Examples:
- payroll readiness,
- major AI insight,
- critical compliance blocker,
- login brand moment.

Use:
- soft radial light,
- local luminous edge,
- optional fine-pointer tracking,
- stable static fallback.

---

# 4. Action hierarchy

A screen should normally have:
- one primary action,
- zero to three secondary visible actions,
- remaining actions in More/menu.

Examples:

Employee Directory:
Primary = Add Employee  
Secondary = Import / Export  
More = configuration-adjacent actions

Payroll Run:
Primary changes by step:
Load Inputs → Validate → Calculate → Review → Finalize

Do not keep multiple conflicting primary buttons.

---

# 5. Dangerous / irreversible actions

Examples:
- Finalize payroll
- Publish compliance rule
- Delete/anonymize candidate data
- Disable user
- destructive bulk update

Use:
- explicit consequence,
- affected entity count,
- current state,
- result state,
- actor/authority if useful,
- clear cancel.

Do not use generic:
“Are you sure?”

---

# 6. Data density

Use compact layout when the user's main task is scanning/comparison.

Examples:
- employee directory,
- payroll results,
- attendance exceptions,
- candidates,
- audit logs.

Use standard layout when understanding/editing a single entity.

Examples:
- employee profile,
- candidate profile,
- settings.

Use comfortable spacing for mobile employee/manager flows.

---

# 7. Drawers vs pages

Use a drawer when the user should preserve context.

Good:
- employee quick preview from grid,
- approval review,
- calculation explanation,
- candidate preview.

Use a full page/workspace when:
- editing is complex,
- the user must compare many data groups,
- the workflow has several steps,
- history/context matters.

Avoid nested drawers.

---

# 8. Tables and grids

Choose DataTable for simple display.

Choose DataGrid when users need:
- views,
- filters,
- sorting,
- bulk actions,
- column customization,
- large datasets,
- permission-sensitive fields.

For mobile, convert the most important records into row-card summaries or dedicated detail flows.

---

# 9. Payroll design guidance

Payroll is not a normal HR module.

Treat it like financial operations software.

Prioritize:
- exact numbers,
- traceability,
- readiness,
- exceptions,
- variance,
- finalization state,
- history,
- approvals.

Avoid:
- decorative charts,
- large employee photos,
- soft lifestyle HR visuals,
- casual language.

Money should use tabular numerals.

Finalized state should feel materially different from editable state.

---

# 10. AI design guidance

AI is a contextual layer, not a separate decorative chatbot.

Always make clear:
- what context is active,
- what source was used,
- whether a tool ran,
- whether the answer is policy/data/external AI,
- whether an action is only proposed or actually executed.

Use the signature AI mark.

Avoid sparkle overload.

Use spotlight only for important insights/action moments.

---

# 11. Permission design guidance

Permission behavior has four visual outcomes:

1. Hidden entirely — user should not know feature exists.
2. Visible but disabled — only when understanding availability is useful.
3. Read-only — user can inspect but not mutate.
4. Access denied — user followed a valid link but lacks permission.

Never expose sensitive values and rely on blur as authorization.

---

# 12. Loading guidance

Loading should preserve layout.

### Good
- table-shaped skeleton,
- employee profile-shaped skeleton,
- progress with real payroll stages.

### Bad
- blank page with centered spinner,
- fake 0–100% counter,
- “AI is thinking” with invented steps.

---

# 13. Empty-state taxonomy

## First use
Explain what this feature will contain + primary setup action.

## No data
Neutral explanation.

## No results
Show active search/filter context + clear action.

## Success empty
Use for queues such as:
“No payroll exceptions.”

This may use a very brief success resolve.

## Permission empty
Do not call it “empty.” Use restricted-access treatment.

---

# 14. RTL checklist

For every new component verify:
- logical margin/padding,
- icon direction,
- text alignment,
- tab order,
- menu alignment,
- drawer direction,
- breadcrumb,
- stepper,
- table,
- charts,
- date input,
- currency,
- mixed text.

Never implement RTL as a final CSS patch.

---

# 15. Mobile guidance

Do not reproduce desktop complexity.

Employee/manager mobile should optimize for:
- quick status,
- one primary action,
- simple approvals,
- leave,
- attendance,
- payslip,
- notifications,
- AI.

Payroll/admin mobile may be read-oriented or focused-action rather than full parity.

---

# 16. Signature-motion trigger policy

Motion is allowed when at least one is true:

- entering/leaving the product,
- a major state changed,
- a high-risk workflow completed,
- access/security boundary was evaluated,
- AI changed context or produced a major insight,
- progress would otherwise be ambiguous.

If none are true, use standard micro motion.

---

# 17. Motion replay policy

Brand motion:
once per entry/session context; optional replay only in design-system docs.

Success motion:
once per successful state transition.

Access gate:
once on state entry.

Spotlight:
persistent but subtle only in allowed cards.

Do not loop expressive motion indefinitely.

---

# 18. Design review questions

Before approving a screen:

- What is the primary job?
- Is the context obvious?
- Is there one clear primary action?
- Are blockers obvious?
- Does the user understand why a sensitive result exists?
- Is dense data still scannable?
- Are empty/error/loading states designed?
- Are permissions represented correctly?
- Does RTL remain coherent?
- Does mobile have a deliberate layout?
- Does motion explain anything?
- Is glow used because it matters, or because it looks cool?
- Would the screen still look like ZainX if the brand logo disappeared?

If the last answer is no, the system lacks distinctive structural language.
