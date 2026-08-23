# Signature Effects & Motion — Detailed Specification

## 1. Effect budget

A typical viewport should contain:
- unlimited neutral utility surfaces,
- several semantic emphasis states,
- usually no more than **one dominant Spotlight**,
- no more than **two visible luminous accents** competing above the fold.

If everything glows, nothing is important.

---

# 2. Spotlight Card

## Anatomy
Base surface  
Subtle border  
Radial light layer  
Optional local luminous border layer  
Content  
Optional pointer position variable

## Desktop
Recommended radial radius: 300–420px.

## Touch
Static radius: 240–320px.

## Light theme
Tint: 3–6% perceived strength.

## Dark theme
Tint: 4–9% perceived strength.

## Border
Default luminous alpha: 0.10–0.18.  
Focused/high-value state: up to ~0.24.

## Behavior
Fine pointer:
spotlight follows pointer with no laggy spring.

Touch:
static radial placement.

Keyboard:
focus state must not depend on pointer spotlight.

Reduced motion:
static highlight.

## Do
Use for payroll readiness, AI insight, critical state, login brand moment.

## Don't
Use for every settings card or every KPI.

---

# 3. Luminous Edge

A thin localized edge, not a giant outer glow.

Use:
- selected high-value state,
- successful finalization,
- AI insight,
- access boundary.

Avoid:
- full neon rectangle on ordinary UI.

---

# 4. Login Brand Assembly

Duration target:
800–1000ms.

Sequence:
0–220ms — mark segments appear.  
160–500ms — geometry assembles.  
440–680ms — controlled light convergence.  
620–900ms — shell/auth surface resolves.

Rules:
- no fake waiting,
- if authentication is already complete, transition at the next valid state,
- if data is still loading, switch to structural skeleton,
- reduced motion = 120ms opacity transition.

---

# 5. Logout

Target:
450–650ms.

Sequence:
1. active workspace loses emphasis,
2. system chrome softens,
3. brand mark becomes visual anchor,
4. session boundary completes,
5. login surface resolves.

Do not delay a security logout to finish animation.

---

# 6. App Bootstrap

Use brand assembly once.

After that:
show real application structure with skeletons.

Do not loop the logo.

---

# 7. Payroll Calculation Motion

Never pretend calculation steps.

If backend exposes real stages, show:

Inputs  
Rules  
Calculation  
Validation  
Results

Each stage:
pending / running / complete / warning / failed.

Progress is informational, not entertainment.

When processing is long:
show timestamps/status details.

---

# 8. Payroll Finalize Success

Use only after backend confirms finalization.

400–700ms:
- status transitions,
- local border/light resolves,
- ShieldCheck appears,
- total/status settles into immutable state.

No confetti.

---

# 9. Access Gate

Target:
300–600ms.

Visual:
- geometric boundary derived from brand/system geometry,
- one scan,
- locked center,
- stable restricted state.

Copy:
Access restricted  
reason  
optional permission scope  
recovery action.

Tone:
serious and premium.

---

# 10. AI Context Scan

The UI may show:
Current page  
Authorized sources  
Tool execution  
Answer

Only show stages actually known.

Example:
Payroll Run: Aug 2026  
→ Payroll Trace  
→ Attendance Snapshot  
→ Compliance Rule vX  
→ Explanation

Do not show invented “reasoning” steps.

---

# 11. AI Answer Reveal

Routine answer:
simple progressive content reveal; no glow required.

Major insight:
AI Spotlight Card allowed.

Action proposal:
use emphasis + clear confirmation controls, not magical animation.

---

# 12. Skeletons

Skeletons should match:
- actual column count,
- avatar position,
- text geometry,
- header hierarchy.

Avoid random blocks.

Animation:
subtle shimmer or opacity wave.
Reduced motion:
static skeleton.

---

# 13. Hover / press

Hover:
120–160ms.

Press:
80ms.

Do not translate cards several pixels or create 3D tilt for finance/HR operational UI.

Maximum routine movement should be almost imperceptible.

---

# 14. Drawer

Open:
~280–340ms.

Close:
~200–260ms.

Primary content should not perform dramatic parallax.

Focus must move correctly.

---

# 15. Dialog

Use subtle opacity + scale.

No bouncing.

Irreversible dialogs should feel stable.

---

# 16. Toast

Enter:
160–220ms.

Exit:
140–200ms.

Actionable toasts stay long enough to interact.

Errors should not disappear before they can be read.

---

# 17. Reduced motion

When reduced:
- no cursor-follow,
- no scan movement,
- no looping shimmer,
- no large transforms,
- all state changes remain explicit,
- progress text remains.

Motion is never the only communication channel.
