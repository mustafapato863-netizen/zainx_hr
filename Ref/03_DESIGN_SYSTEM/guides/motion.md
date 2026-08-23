# Signature Motion System

## Motion principles

- **Motion explains state.**
- **Light is an event, not decoration.**
- Productive interactions are fast; expressive motion is reserved for boundaries and outcomes.
- Never delay a user action to show animation.
- All non-essential motion respects `prefers-reduced-motion`.

## Timing tokens

| Token | Duration | Use |
|---|---:|---|
| instant | 80ms | pressed/active feedback |
| micro | 140ms | hover, checkbox, small state change |
| standard | 220ms | menu, popover, tab indicator |
| context | 320ms | drawer, AI panel, workspace detail transition |
| expressive | 640ms | permission gate, completion reveal |
| brand | 900ms | login/app launch brand assembly |

## Easing

- `productive`: cubic-bezier(0.2, 0, 0, 1)
- `enter`: cubic-bezier(0.16, 1, 0.3, 1)
- `exit`: cubic-bezier(0.4, 0, 1, 1)
- `expressive`: cubic-bezier(0.2, 0.8, 0.2, 1)

## Brand entry sequence

Target: **800–1000ms**, first load/login only.

1. 0–240ms — logo geometry/strokes appear from low opacity and short offset.
2. 180–520ms — pieces assemble into the brand mark.
3. 460–720ms — one restrained light sweep crosses the mark.
4. 650–900ms — shell fades/slides in while the logo settles.

Do not block authentication work. If app data is still loading, transition immediately to skeleton/progress state after the brand animation.

## Logout sequence

Target: **450–650ms**.

1. Workspace controls fade/soften.
2. Main canvas contracts slightly or resolves toward the brand anchor.
3. Mark becomes the last stable element.
4. Login screen appears.

No fake waiting; if logout completes instantly, animation stays within this timing budget.

## Loading hierarchy

### App bootstrap
Use compact brand mark animation once, then skeletons.

### Route/module data load
Use structural skeletons preserving layout. Avoid centered spinners for full pages.

### Long-running operations
Expose real steps or job state. Payroll example:

`Inputs → Rules → Calculation → Validation → Results`

### AI thinking
Use a context scan rather than generic bouncing dots:

`Current page → authorized sources/tools → analysis → answer`

The scan can illuminate source chips sequentially once, then settle into a low-motion progress state.

## Permission / 403 motion

Use a short **access gate** animation:

- Boundary line draws in.
- A scan passes once.
- Gate resolves to locked/restricted state.
- Then motion stops.

Never use playful shaking or cartoon locks for sensitive payroll/HR access denial.

## Success motion

For high-value completion only:

- 180–260ms local illumination.
- Check/state glyph resolves.
- Optional short line sweep through the final status label.

Examples: Payroll finalized, export generated, approval completed.

## Spotlight motion

Pointer-follow spotlight is allowed only on Spotlight Cards and only on fine-pointer devices. On touch devices it becomes a static radial highlight. The spotlight must not obscure text or exceed the card boundary.

## Reduced-motion behavior

When `prefers-reduced-motion: reduce` or the user disables motion:

- Replace assembly with a 120ms fade.
- Remove cursor-follow effects.
- Remove looping shimmer.
- Keep state changes immediate and clear.
- Preserve progress text and accessible announcements.
