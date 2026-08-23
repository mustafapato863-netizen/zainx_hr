# Stack Decision Matrix

| Concern | Chosen | Avoid as parallel baseline | Why |
|---|---|---|---|
| Global state | Redux Toolkit | Zustand | multi-team governance, actions, listeners, DevTools |
| Server state | TanStack Query | RTK Query / Apollo cache | one remote-data cache strategy |
| Workflow state | XState | ad-hoc booleans | explicit valid transitions |
| URL state | TanStack Router | Redux filters | typed deep-linkable state |
| Forms | React Hook Form + Zod | global form store | correct ownership |
| Accessible primitives | React Aria | visual framework lock-in | style-free accessible behavior |
| Heavy grid | AG Grid Enterprise | raw grid in modules | enterprise features behind wrapper |
| Lightweight table | TanStack Table | heavy grid everywhere | lower complexity |
| REST | OpenAPI + Orval | handwritten clients | generated contract |
| GraphQL | optional reads + Codegen | GraphQL everywhere | use for composition only |
| Styling | Tailwind 4 + semantic tokens | raw palette utilities | DS ownership |
| Motion | Motion | mixed animation engines | coherent product motion |
| Brand motion | scoped GSAP | GSAP everywhere | specialist SVG only |
| Charts | ECharts | per-module chart libs | shared analytics language |
| Scheduling | FullCalendar Scheduler | custom scheduler | mature resource scheduling |
| Docs | Storybook | screenshot-only docs | executable specification |
| E2E | Playwright | manual-only QA | repeatable critical workflows |
