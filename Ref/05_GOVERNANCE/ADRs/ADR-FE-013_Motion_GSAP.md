# ADR-FE-013 — Motion Architecture & GSAP Evaluation

**Status:** Accepted (Motion for React) / Rejected (GSAP)  
**Date:** 2026-08-24  
**Owners:** Architecture / Frontend / UX Design

## Context
ZainX requires coherent micro-interactions, layout transitions, and BrandAssembly startup orchestration while guaranteeing strict accessibility and `prefers-reduced-motion` compliance under React 19.

## Decision
1. Standardize on **Motion for React (`motion@12.43.0`, current web lockfile baseline)** as the platform-wide motion and animation engine.
2. **Reject GSAP dependency (`gsap`)**:
   - **Canonical Rationale:** Motion for React already satisfies all approved ZainX UI motion specifications and current `BrandAssembly` requirements (SVG path morphing, glow effects, staggered entrance, and timeline sequencing).
   - Adding GSAP would introduce a redundant second animation runtime, unnecessary bundle weight (~60kB+ minified), additional API surface for developers to learn, and increased long-term maintenance overhead without a demonstrated business or technical requirement.
3. **Enforce Accessibility:** Mandatory reduced-motion fallbacks (`motion-reduce:animate-none` / instant opacity transitions) across all motion components.

## Review Trigger
Re-evaluate GSAP only after official ZainX SVG brand assets are finalized and only if a specific required SVG choreography cannot be implemented cleanly and performantly using Motion for React.

## Consequences
- **Positive:** Single, unified animation engine with native React 19 concurrent mode integration, zero secondary runtime overhead, and optimal tree-shaking.
- **Negative:** None for current product scope.
