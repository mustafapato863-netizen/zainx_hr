# ZainX ADR Register v4.1

**Last Updated:** August 25, 2026 (Phase 1C Closed; Phase 2 Approved to Start)

| ADR | Decision | Required | Status |
|---|---|---|:---:|
| ADR-APP-001 | Full repo: .NET root + nested `/web` Nx/pnpm workspace | Phase 0 | **ACCEPTED** |
| ADR-BE-001 | Modular Monolith + schema-per-module | Existing canonical | **ACCEPTED** |
| ADR-BE-002 | Standard ProblemDetails/error model | Phase 0 | **ACCEPTED** |
| ADR-BE-003 | Background-job + outbox baseline | Phase 0 | **ACCEPTED** |
| ADR-FE-001 | Modular Frontend Monolith / Nx boundaries | Phase 0 | **ACCEPTED** |
| ADR-FE-002 | React Aria accessible primitive layer | Phase 0 | **ACCEPTED** |
| ADR-FE-003 | Redux Toolkit for cross-module client state | Phase 0 | **ACCEPTED** |
| ADR-FE-004 | TanStack Query as sole server-state cache | Phase 0 | **ACCEPTED** |
| ADR-FE-005 | XState for complex UI workflow orchestration | Phase 0 | **ACCEPTED** |
| ADR-FE-006 | TanStack Router for routes/shareable URL state | Phase 0 | **ACCEPTED** |
| ADR-FE-007 | REST/OpenAPI canonical commands and operational APIs | Phase 0 | **ACCEPTED** |
| ADR-FE-008 | GraphQL optional compositional-read layer | Phase 0 | **ACCEPTED** |
| ADR-FE-009 | AG Grid Enterprise behind ZainXDataGrid | Phase 1C | **ACCEPTED (With Restrictions)** |
| ADR-FE-010 | FullCalendar Scheduler behind ZainXScheduler | Phase 1C | **ACCEPTED (With Restrictions)** |
| ADR-FE-011 | ECharts behind ZainXChart | Phase 1C | **ACCEPTED (With Restrictions)** |
| ADR-FE-012 | Tailwind + Style Dictionary semantic token pipeline | Phase 1B | **ACCEPTED** |
| ADR-FE-013 | Motion for React Core; GSAP Rejected | Phase 1C | **ACCEPTED** |
| ADR-FE-014 | Browser support policy | Phase 0 | **ACCEPTED** |
| ADR-FE-015 | Frontend telemetry + sensitive-data filtering | Phase 0 | **ACCEPTED** |
| ADR-FE-016 | Long-running jobs: polling correctness, push optional | Phase 1C | **ACCEPTED** |
| ADR-SEC-001 | Browser/session storage policy | Phase 0 | **ACCEPTED** |
| ADR-SEC-002 | Sensitive-data reveal/audit policy | Before People | **ACCEPTED** |
| ADR-PAY-001 | Frontend orchestration only; backend owns payroll truth | Before Payroll | **ACCEPTED** |
| ADR-AI-001 | AI 7A read-only before 7B mutations | Before AI | **ACCEPTED** |

## Current governance state

Phase 0, Phase 1A, Phase 1B and Phase 1C are accepted as PASS. Phase 2 is approved to start under Restricted Audit Mode. This register preserves the accepted architecture decisions; it does not represent production readiness or completion of the HCM Core release gate.
