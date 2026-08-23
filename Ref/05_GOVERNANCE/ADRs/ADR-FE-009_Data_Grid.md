# ADR-FE-009 — Data Grid Architecture & AG Grid Encapsulation

**Status:** Accepted (With Commercial Restrictions)  
**Date:** 2026-08-24  
**Owners:** Architecture / Frontend / Procurement

## Context
ZainX Workforce operational screens (Payroll processing sheets, attendance registers, statutory employee rosters) require high-performance rendering of 10,000+ rows, multi-column sorting/filtering, column pinning, range selections, and export capabilities.

## Decision
1. Adopt AG Grid behind the `@zainx/design-system` component wrapper `ZainXDataGrid`.
2. Prohibit direct imports of `ag-grid-*` from feature packages; enforce via ESLint `no-restricted-imports`.
3. Use AG Grid Community features as default baseline; require commercial license procurement approval before enabling Enterprise-only capabilities in production releases.
4. For lightweight tables (<1,000 rows without grid-level manipulation), consume the standard semantic `Table` primitive.

## Alternatives Considered
- **TanStack Table + TanStack Virtual:** Headless and open source, but requires custom development of pinning, column resize handles, filtering menus, and Excel export, significantly increasing engineering cost.

## Consequences
- **Positive:** Standardized 60 FPS virtualization, robust enterprise grid capabilities, encapsulated behind a single reusable design system component.
- **Negative:** Commercial license cost for developer seats and production distribution.

## Security / Privacy Impact
- Sensitive values inside cells must be rendered using `SensitiveValue` composition.
- Grid must not log row data to external telemetry.

## On-Premise Impact
- Zero external CDN dependencies; all CSS and web fonts bundled locally.
- License key injected via environment variable without external verification network calls.

## Review Trigger
- Inability to procure commercial licenses or severe breaking API changes in future AG Grid major releases.
