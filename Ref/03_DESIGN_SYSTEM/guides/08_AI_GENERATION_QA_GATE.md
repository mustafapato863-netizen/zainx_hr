# AI Generation & Design QA Gate

Any AI-generated ZainX screen/component must pass this gate.

## Reject immediately if
- it looks like a generic shadcn dashboard,
- the first answer is a 4-card KPI grid,
- it uses giant rounded cards everywhere,
- it uses purple gradients as the AI identity,
- it uses excessive glass,
- it uses 3D tilt in payroll/admin/compliance,
- it hides primary actions on hover,
- it ignores RTL,
- it has no loading/error/permission state,
- it invents a new component that duplicates the system,
- it uses glow without semantic reason,
- it turns an operational page into a marketing landing page.

## Required questions
1. Which page pattern is used?
2. What is the primary user job?
3. What is the main entity/context?
4. What permission state is assumed?
5. What is the primary action?
6. What can block completion?
7. What changes in finalized/read-only state?
8. How does it work in RTL?
9. How does it work on required mobile viewport?
10. Does keyboard access exist?
11. Does motion explain state?
12. Is spotlight within effect budget?
13. Are sensitive values protected?
14. Are source/provenance labels present for AI?
15. Does it use shared components rather than inventing replacements?

## Visual consistency check
- same button hierarchy,
- same field height,
- same status semantics,
- same grid density,
- same drawer behavior,
- same focus,
- same border/elevation language,
- same icon family,
- same motion tokens.

## Final score
Approve only if all P0 requirements pass and no critical anti-pattern remains.
