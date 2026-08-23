## ZainX Signature Login Intro / Brand Loading — Implementation Prompt

You are a **Principal Frontend Engineer, Motion Designer, and Design Systems Engineer** implementing the signature login/loading experience for **ZainX**, an enterprise Workforce / HR / Payroll / Compliance / Talent / AI platform.

Build a production-ready **ZainX Brand Assembly Loader** that becomes the visual intro for the Login experience.

This must feel like a proprietary branded product moment — not a generic spinner, not a typical SaaS loader, and not a flashy cyberpunk animation.

---

### Core idea

The ZainX logo mark should appear to **construct / draw itself intelligently**.

The experience should communicate:

**precision → connection → completion → secure workspace**

The logo starts incomplete and resolves into the exact official ZainX mark.

Do **not redesign or reinterpret the logo geometry**.

Use the official ZainX SVG/vector asset when available.

If only raster assets exist during development, create a temporary implementation shell, but keep the animation architecture ready for the final SVG. Do not manually approximate the final production logo paths.

---

# Experience sequence

Implement the animation approximately as:

```text
00 — Canvas Awake
      ↓
01 — Trace
      ↓
02 — Assembly
      ↓
03 — Energy Convergence
      ↓
04 — Brand Resolve
      ↓
05 — Login Reveal
```

## 00. Canvas Awake

Start with a deep ZainX navy/near-black background.

Do not show a blank black screen.

Introduce an extremely subtle radial atmosphere behind the future logo position.

```text
opacity: 0 → subtle
scale: 0.98 → 1
```

Keep this understated.

---

## 01. Trace

Reveal the main geometric structure of the ZainX mark using SVG path drawing.

Prefer:

```text
stroke-dasharray
stroke-dashoffset
```

or Motion SVG path animation.

The initial trace should look precise, technical, and controlled.

Think:

```text
blueprint line
→
illuminated contour
→
real brand form
```

Do not use random handwriting effects.

Do not trace every path at exactly the same time.

Stagger the important structural segments by approximately:

```text
40–80ms
```

---

## 02. Assembly

The traced pieces should begin resolving into the actual filled ZainX mark.

The visual metaphor is **pieces finding their correct position**, not exploding particles.

Use very small offsets such as:

```text
translateX: ±8–18px
translateY: ±6–14px
opacity: 0 → 1
blur: 6px → 0
```

Then converge into the exact final geometry.

The blue portion should resolve first, followed very slightly by the cyan portion.

This timing difference should be subtle.

---

## 03. Energy Convergence

Once the geometry is almost complete, introduce one controlled energy event.

Example:

```text
small blue light
       ↓
travels across one critical edge
       ↓
reaches the ZainX intersection
       ↓
cyan side activates
```

A soft horizontal light sweep may pass across the completed mark once.

The sweep must happen **one time only**.

No constant scanning loop.

No rainbow glow.

No excessive bloom.

Use ZainX blue → cyan only.

---

## 04. Brand Resolve

The mark becomes fully stable.

```text
outline → fill
glow → restrained
blur → 0
scale → 1
```

Give the final mark one extremely subtle “settle”:

```text
scale 0.985
→ 1.01
→ 1
```

This should feel almost imperceptible.

The final state must look exactly like the official ZainX logo — not like a glowing wireframe.

Under the mark, optionally show:

**Preparing your workspace…**

or:

**Securing your workspace…**

Keep the typography quiet.

---

# 05. Login Reveal

After the brand resolves, transition naturally into the Login screen.

Do not cut from loader to login.

The brand mark should become the visual anchor connecting both states.

Preferred transition:

```text
loader mark
     ↓
subtle scale-down
     ↓
translate toward final login logo position
     ↓
login content resolves around it
```

Use a shared-layout / shared-element style transition where practical.

Example:

```text
Full-screen logo
      ↓
120–160px mark
      ↓
40–56px login-brand mark
```

Then reveal:

```text
Welcome back
Email
Password
SSO
Sign in
```

with restrained stagger.

Suggested:

```text
opacity 0 → 1
y 6px → 0
stagger 35–50ms
```

Do not make the login form fly in dramatically.

---

# Login bootstrap behavior

The animation must represent **real application loading**, not fake waiting.

Integrate it with:

```text
application bootstrap
session resolution
authentication-provider initialization
tenant/bootstrap configuration
```

Recommended logic:

```text
App starts
   ↓
bootstrap/auth session starts
   ↓
if ready almost instantly
     → use shortened logo resolve

if still loading
     → run full BrandAssembly

animation completes but app still loading
     → hold the completed logo calmly

backend becomes ready
     → reveal Login / Workspace
```

**Never invent a fake percentage.**

Do not artificially keep the user waiting just to finish a beautiful animation.

If loading completes very quickly, use a shortened resolve transition instead of forcing the full sequence.

---

# Long-load state

If authentication/bootstrap takes longer than the expressive intro:

Do **not** repeat the logo-drawing animation.

Hold the completed logo.

Allowed:

* very subtle atmospheric light
* static completed mark
* quiet status copy

Avoid:

* rotating logo
* repeated tracing
* continuous neon pulse
* looping particles
* generic spinner over the logo

Optionally after a longer threshold show:

**Still preparing your workspace…**

---

# Login submission

Do not replay the entire intro when the user clicks Sign In.

During authentication:

```text
Sign in button
→ local pending state
```

If authentication succeeds:

use a short **Brand Resolve Success** transition:

```text
Login UI quiets
      ↓
ZainX mark becomes focal
      ↓
localized luminous edge
      ↓
application shell resolves
```

Target:

```text
320–550ms
```

Do not use confetti.

---

# Error behavior

Authentication failure must not distort the logo.

The ZainX brand remains stable.

Error feedback belongs to the login form:

```text
invalid credentials
expired session
network error
SSO failure
```

Never turn the logo red or shake the entire screen.

---

# Visual direction

Use:

```text
Deep navy / near-black canvas
ZainX blue
ZainX cyan
localized radial light
subtle luminous edge
controlled depth
soft atmospheric gradient
```

Avoid:

```text
generic purple AI gradients
rainbow neon
huge glassmorphism cards
noisy particles
excessive blur
cyberpunk aesthetics
gaming-style animation
large glowing text
fake holographic UI
```

The target feeling is:

**Enterprise + Intelligent + Precise + Secure + Premium.**

---

# Timing system

Use the ZainX motion tokens:

```text
Instant      80ms
Micro       140ms
Standard    220ms
Context     320ms
Expressive  640ms
Brand       900ms
```

Approximate initial sequence:

```text
Canvas              0–140ms
Trace             100–520ms
Assembly          300–680ms
Energy event      600–820ms
Brand resolve     760–950ms
Login reveal      850–1150ms
```

These are guidelines, not mandatory artificial delays.

---

# Easing

Prefer ZainX easings:

```ts
productive = [0.2, 0, 0, 1]
enter      = [0.16, 1, 0.3, 1]
exit       = [0.4, 0, 1, 1]
expressive = [0.2, 0.8, 0.2, 1]
```

Avoid springy/bouncy motion for authentication.

---

# Technical stack

Implement using the approved ZainX frontend stack:

```text
React 19.2
TypeScript strict
Tailwind CSS 4
Motion for React
ZainX semantic tokens
```

Use **Motion for React** for:

* sequence orchestration
* opacity
* transforms
* shared layout transition
* logo component staging
* login reveal

Use CSS for:

* gradients
* glow
* static atmospheric light
* micro transitions

Use **GSAP only if necessary** for complex SVG path choreography that Motion cannot implement cleanly.

GSAP must remain isolated inside:

```text
design-system/signature/BrandAssembly
```

Do not introduce GSAP across ordinary application UI.

---

# Required component architecture

Build reusable components:

```text
BrandAssembly
├── BrandTrace
├── BrandSegments
├── BrandEnergySweep
├── BrandResolve
└── BrandStable

LoginBootstrap
├── BrandAssembly
├── LoadingStatus
└── LoginReveal

BrandResolveSuccess
ReducedMotionBrand
```

Suggested state:

```ts
type BrandAssemblyState =
  | "idle"
  | "trace"
  | "assembling"
  | "resolving"
  | "stable"
  | "exit";
```

Keep this state local to the signature component unless integration genuinely requires a broader workflow.

---

# Accessibility

`prefers-reduced-motion` is mandatory.

Reduced-motion version:

```text
background fade
→ logo opacity 0 → 1
→ login appears
```

No drawing, orbiting, sweeping, or large transforms.

The animation itself is decorative and should not become noisy screen-reader content.

Loading status can expose one accessible status message.

Do not announce every animation phase.

---

# Responsive behavior

Desktop:

```text
mark ~140–180px
```

Tablet:

```text
~120–150px
```

Mobile:

```text
~88–112px
```

Keep the animation centered and preserve the exact logo aspect ratio.

Never crop the mark.

---

# RTL

The ZainX logo animation itself must remain geometrically identical in RTL.

**Do not mirror the brand mark.**

Login content changes direction/layout normally for Arabic.

---

# Quality bar

The result should feel closer to a premium OS/application boot moment than a website spinner.

References should be understood conceptually:

* precision of Stripe
* restraint of Linear
* polish of Apple system transitions
* premium enterprise character of ZainX

Do not copy their visuals.

---

# Final acceptance criteria

The implementation is approved only if:

1. The exact official ZainX mark is preserved.
2. The logo appears to construct/draw itself meaningfully.
3. Animation integrates with real login/bootstrap loading.
4. There is no fake loading percentage.
5. Animation does not artificially delay a ready application.
6. The full animation does not loop.
7. Long loading settles into a calm stable brand state.
8. Login appears as a continuation of the intro rather than a separate page.
9. Reduced-motion works correctly.
10. RTL does not mirror the logo.
11. Desktop/mobile behavior is polished.
12. No generic purple-AI / cyberpunk visual language appears.
13. Performance remains smooth at approximately 60fps on supported hardware.
14. Signature implementation is isolated and reusable.

**Core design principle:**

> **The logo does not spin while the application loads. The product assembles its identity while the workspace becomes ready.**

And:

> **The system is quiet. Important things glow.**

One additional requirement I would put at the top of the coding task: **use the real ZainX SVG, not the existing PNG, before finalizing `BrandAssembly`.** That will let us draw the real paths instead of faking the animation around a raster image.
