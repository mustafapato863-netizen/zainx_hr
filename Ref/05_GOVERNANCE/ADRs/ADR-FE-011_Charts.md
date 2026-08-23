# ADR-FE-011 — Enterprise Analytics & Apache ECharts Encapsulation

**Status:** Accepted (With Restrictions)  
**Date:** 2026-08-24  
**Owners:** Architecture / Frontend / Analytics

## Context
Executive dashboards, payroll variance reporting, and headcount trends require rich vector visualizations (Area, Line, Bar, Stacked Bar, Donut, and Time Series) supporting dark/light themes, responsive resizing, and RTL Arabic orientation.

## Decision
1. Standardize on Apache ECharts (`echarts@6.1.0`, `echarts-for-react@3.0.2`) behind `@zainx/design-system` (`ZainXChart`).
2. Enforce strict Accessibility Mandate: Charts must NEVER be the sole representation of business information. `ZainXChart` automatically provides a toggleable accessible semantic data table alternative and screen-reader summaries.
3. Apply semantic color tokens (`primary`, `surface`, `border`, `text-primary`) dynamically based on active theme and RTL context.

## Alternatives Considered
- **Recharts / Chart.js:** Lacks mature SVG high-performance rendering for dense time-series and multi-axis alignment.

## Consequences
- **Positive:** Rich visualization capabilities under Apache 2.0 open-source license with built-in accessibility fallbacks.
- **Negative:** Substantial bundle size (~300KB minified), requiring route-level code splitting so that charts are loaded only on analytics/report routes.

## On-Premise & Offline Impact
- Runs entirely in-browser; zero external web font or script dependencies.
