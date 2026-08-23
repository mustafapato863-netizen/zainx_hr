# ADR-FE-010 — Resource Scheduling Architecture & FullCalendar Wrapper

**Status:** Accepted (With Restrictions)  
**Date:** 2026-08-24  
**Owners:** Architecture / Frontend / Product Operations

## Context
Attendance shift planning, employee leave calendars, and recruitment interview scheduling require monthly, weekly, daily, and list-based visual schedule layouts with full RTL Arabic support and keyboard accessibility.

## Decision
1. Encapsulate FullCalendar behind `@zainx/design-system` (`ZainXScheduler`).
2. Standardize deliberately on **FullCalendar 6.1.x (locked to `6.1.15`)** under MIT open-source license (`@fullcalendar/core`, `@fullcalendar/react`, `@fullcalendar/daygrid`, `@fullcalendar/timegrid`, `@fullcalendar/list`, `@fullcalendar/interaction`). FullCalendar 7.x is rejected at this time due to prerelease ESM breaking changes.
3. Provide mandatory accessible command/form alternatives (e.g. "Add Event", "Edit Event" buttons) so that drag-and-drop is never the sole interaction mechanism.
4. Defer FullCalendar Premium (Resource Timeline view) until commercial requirements are finalized.

## Alternatives Considered
- **Custom CSS Grid + TanStack Virtual Calendar:** High maintenance overhead for recurring events, multi-day spans, and timezone calculations.

## Consequences
- **Positive:** Reliable scheduling engine with standard open-source MIT license for all core views, encapsulated behind accessible ZainX UI.
- **Negative:** Resource timeline views require separate commercial license if needed in future phases.

## Security & A11y Impact
- Full keyboard focusable controls, ARIA live region title announcements, and complete RTL layout mirroring.

## On-Premise Impact
- 100% locally bundled; zero CDN calls.
