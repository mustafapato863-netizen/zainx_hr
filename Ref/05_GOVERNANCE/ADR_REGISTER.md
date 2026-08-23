# ZainX ADR Register v4.1

| ADR | Decision | Required |
|---|---|---|
| ADR-APP-001 | Full repo: .NET root + nested `/web` Nx/pnpm workspace | Phase 0 |
| ADR-BE-001 | Modular Monolith + schema-per-module | Existing canonical |
| ADR-BE-002 | Standard ProblemDetails/error model | Phase 0 |
| ADR-BE-003 | Background-job + outbox baseline | Phase 0 |
| ADR-FE-001 | Modular Frontend Monolith / Nx boundaries | Phase 0 |
| ADR-FE-002 | React Aria accessible primitive layer | Phase 0 |
| ADR-FE-003 | Redux Toolkit for cross-module client state | Phase 0 |
| ADR-FE-004 | TanStack Query as sole server-state cache | Phase 0 |
| ADR-FE-005 | XState for complex UI workflow orchestration | Phase 0 |
| ADR-FE-006 | TanStack Router for routes/shareable URL state | Phase 0 |
| ADR-FE-007 | REST/OpenAPI canonical commands and operational APIs | Phase 0 |
| ADR-FE-008 | GraphQL optional compositional-read layer | Phase 0 |
| ADR-FE-009 | AG Grid Enterprise behind ZainXDataGrid | Phase 1C/license |
| ADR-FE-010 | FullCalendar Scheduler behind ZainXScheduler | Phase 1C/license |
| ADR-FE-011 | ECharts behind ZainXChart | Phase 1C |
| ADR-FE-012 | Tailwind + Style Dictionary semantic token pipeline | Phase 1B |
| ADR-FE-013 | Motion core; GSAP brand-only | Phase 1C |
| ADR-FE-014 | Browser support policy | Phase 0 |
| ADR-FE-015 | Frontend telemetry + sensitive-data filtering | Phase 0 |
| ADR-FE-016 | Long-running jobs: polling correctness, push optional | Phase 0/1 |
| ADR-SEC-001 | Browser/session storage policy | Phase 0 |
| ADR-SEC-002 | Sensitive-data reveal/audit policy | Before People sensitive fields |
| ADR-PAY-001 | Frontend orchestration only; backend owns payroll truth | Before Payroll |
| ADR-AI-001 | AI 7A read-only before 7B mutations | Before production AI |
