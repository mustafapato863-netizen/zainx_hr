# Frontend Baseline Lock — Phase 0 Candidate

## Candidate baseline

```text
Node.js           24 LTS
React             19.2
TypeScript        6.0 candidate baseline
Vite              8.x
pnpm              current approved stable
Nx                23.x
Tailwind CSS      4.x
Storybook         10.x
```

The remaining architectural libraries stay as approved in the Frontend Engineering Guideline:

```text
React Aria Components
Redux Toolkit
TanStack Query v5
XState v5
TanStack Router
React Hook Form
Zod 4
OpenAPI + Orval
Optional GraphQL + GraphQL Code Generator
AG Grid Enterprise behind ZainXDataGrid
TanStack Table / Virtual
FullCalendar Scheduler behind ZainXScheduler
Apache ECharts behind ZainXChart
Motion for React
Scoped GSAP for vector brand choreography
dnd-kit
Tiptap
i18next/react-i18next/Intl
MSW
Vitest
Testing Library
Playwright
axe-core
OpenTelemetry Web
```

## Phase-0 compatibility spike

Before locking the package manifest:

1. Node 24 LTS CI runner.
2. React 19.2 minimal app.
3. TypeScript 6 strict compile.
4. Vite 8 production build.
5. Nx 23 graph/build/test/lint.
6. Storybook 10 build.
7. React Aria Button/Dialog/ComboBox smoke test.
8. TanStack Router + Query smoke test.
9. Redux Toolkit + XState smoke test.
10. AG Grid trial smoke test.
11. FullCalendar Premium trial smoke test.
12. ECharts smoke test.
13. Motion + reduced-motion smoke test.
14. Arabic RTL smoke screen.
15. Playwright Chromium/Edge smoke test.

If TypeScript 6 causes a real dependency blocker, pin latest 5.9 temporarily and create an ADR with upgrade trigger. Do not silently stay on 5.8.
