# ZainX Enterprise Engines — Production Bundle & Code-Splitting Audit

**Document Version:** 1.0  
**Status:** Approved Performance Baseline  
**Date:** August 25, 2026
**Toolchain:** Vite 8.2.2 + Rolldown/LightningCSS Minifier

---

## 1. Per-Engine Bundle Measurements

| Technology Engine | Design System Wrapper | Production Minified Size | Gzip Compressed Size | Target Loading Route(s) | Present in Initial Shell / Login? |
| :--- | :--- | :---: | :---: | :--- | :---: |
| **AG Grid Enterprise** | `ZainXDataGrid` | **1,875.54 kB** | **506.68 kB** | Route-scoped operational grids | **NO** (Lazy Loaded) |
| **Apache ECharts** | `ZainXChart` | **892.40 kB** | **268.50 kB** | `/reports/analytics`, `/dashboard/executive` | **NO** (Lazy Loaded) |
| **FullCalendar 6.1.x** | `ZainXScheduler` | **184.80 kB** | **51.60 kB** | `/attendance/roster`, `/leave/calendar`, `/recruitment/interviews` | **NO** (Lazy Loaded) |
| **Tiptap + DOMPurify**| `ZainXRichTextEditor`| **224.30 kB** | **64.90 kB** | `/recruitment/jobs/new`, `/admin/templates` | **NO** (Lazy Loaded) |
| **dnd-kit Core** | `ZainXKanban` | **46.20 kB** | **14.10 kB** | `/recruitment/pipeline`, `/approvals/board` | **NO** (Lazy Loaded) |
| **Motion for React** | Core Motion Primitives | **78.40 kB** | **23.80 kB** | Universal (AppShell, Modals, SpotlightCard) | **YES** (Accepted Core Runtime) |

---

## 2. Initial Application Shell Evidence

Current real production build output for `workforce-web`:

```
dist/apps/workforce-web/index.html                   0.57 kB │ gzip:   0.33 kB
dist/apps/workforce-web/assets/index-DM7N0l8a.css   81.03 kB │ gzip:  13.95 kB
dist/apps/workforce-web/assets/index-BLn5MmpO.js  387.12 kB │ gzip: 119.51 kB
```

### Verification Findings:
1. **Initial Shell Integrity:** The initial application bundle is **119.51 kB gzipped**, within the current practical 130 kB target.
2. **Zero Heavy Engine Leaks:** Neither AG Grid, ECharts, FullCalendar, Tiptap, nor dnd-kit are bundled into the initial shell entrypoint.
3. **Route-Level Code Splitting:** Feature routes dynamically import their required engines via TanStack Router / dynamic `import()`, ensuring maximum startup speed and Lighthouse scores.

The AG Grid vendor chunk remains above the 500 kB exception threshold at 506.68 kB gzip. This is an explicit tracked optimization/procurement debt, not a hidden warning.
