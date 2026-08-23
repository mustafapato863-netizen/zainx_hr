# AG Grid Enterprise — Technical & Commercial Decision Document

**Document Version:** 1.0  
**Phase:** Phase 1C Enterprise Spikes  
**Date:** August 24, 2026  
**Status:** **ACCEPT WITH RESTRICTIONS** (Technical Baseline Accepted; Commercial Procurement Gate Prior to Production Release)

---

## 1. Executive Summary

ZainX Workforce evaluated `ag-grid-enterprise@33.1.1` and `ag-grid-react@33.1.1` against large operational datasets (10,000+ synthetic rows) to determine technical suitability, performance, accessibility, RTL behavior, and commercial licensing implications.

**Decision:**
- **Technical Architecture:** **ACCEPT**. AG Grid is encapsulated strictly behind `@zainx/design-system` (`ZainXDataGrid`). Direct imports from feature packages (`@zainx/people`, `@zainx/payroll`, etc.) are prohibited and enforced by ESLint boundaries.
- **Commercial Status:** **ACCEPT WITH RESTRICTIONS**. The engine operates under evaluation/trial status during development. Commercial license procurement is required before production deployment. No feature module may depend on unapproved Enterprise-only APIs without explicit commercial approval.

---

## 2. Requirement Matrix: Community vs. Enterprise

| Operational Requirement | AG Grid Community | AG Grid Enterprise | ZainX Assessment |
| :--- | :---: | :---: | :--- |
| **High row count virtualization (10k+ rows)** | YES | YES | Standard DOM virtualization is robust in Community. |
| **Column sorting, filtering, resizing** | YES | YES | Fully supported in Community. |
| **Row selection (single/multi-select)** | YES | YES | Community supports multi-row checkbox selection. |
| **Column Pinning (Left/Right Freeze)** | YES | YES | Critical for large employee/payroll tables; Community provides pinning. |
| **Custom Cell Renderers (Money, Status, SensitiveValue)** | YES | YES | Fully supported via React custom cell renderers in Community. |
| **Excel Export (.xlsx with formatting)** | NO | YES | Enterprise feature; CSV export is available in Community. |
| **Master-Detail Nested Views** | NO | YES | Used for hierarchical salary breakdowns and multi-level approvals. |
| **Server-side Row Model / Infinite Scroll** | NO | YES | Required for 100k+ historical audit and ledger logs. |
| **Range Selection & Clipboard Integration** | NO | YES | Required for power-user payroll grid editing. |
| **Row Grouping & Aggregations** | NO | YES | Required for organizational headcount and department rollups. |

---

## 3. Alternative Evaluation: TanStack Table + TanStack Virtual

| Evaluation Criterion | AG Grid Enterprise | TanStack Table + TanStack Virtual |
| :--- | :--- | :--- |
| **Developer Velocity & Out-of-the-Box Features** | High (Integrated UI, column menus, pinning, filters) | Medium (Headless; requires building all UI, menus, resize handles, and pinning logic) |
| **Virtualization Performance (10k+ rows)** | Extremely High (60 FPS smooth scrolling, C++ canvas/optimized DOM) | High (Good with `@tanstack/react-virtual`, but requires custom CSS layout optimization) |
| **Complex Enterprise Workflows (Group/Aggregate/Excel)** | Native built-in engine | Requires custom implementation or external libraries |
| **Licensing Cost** | Commercial (Per developer seat + support) | 100% Open-Source (MIT) |
| **Vendor Lock-in Risk** | Medium (Mitigated by `ZainXDataGrid` wrapper) | Zero |

**Conclusion:** For lightweight product tables (<1,000 rows), ZainX uses the headless `Table` / TanStack Table primitive. For heavy operational grids (Payroll calculation sheets, Attendance logs, Organization-wide rosters), AG Grid behind `ZainXDataGrid` provides superior performance and enterprise capabilities.

---

## 4. Licensing, CI/CD, and On-Premise Deployment Model

1. **Licensing Model:**
   - Per-developer seat license required for engineers actively developing grid configurations.
   - Production deployment license is royalty-free / included per application instance depending on procurement tier.
2. **License Key Management:**
   - License key must be injected via runtime environment variable (`VITE_AG_GRID_LICENSE_KEY`) or initialized in application bootstrap (`LicenseManager.setLicenseKey(...)`).
   - The key must NEVER be committed to Git repository or hardcoded in client source files.
3. **CI / CD Pipelines:**
   - Build agents compile against the evaluation bundle without requiring an active license key. Watermark suppression is active when the valid key is supplied.
4. **Air-Gapped / On-Premise Environments:**
   - AG Grid does NOT contact external cloud license servers at runtime.
   - All assets and styling (`ag-grid.css`, `ag-theme-alpine.css`) are bundled locally; zero external CDN runtime calls.

---

## 5. Architectural Guardrails & Enforced Rules

1. **Strict Encapsulation:** Only `@zainx/design-system` may import `ag-grid-enterprise`, `ag-grid-community`, or `ag-grid-react`.
2. **ESLint Boundary Rule:** Lint checks actively block feature packages (`@zainx/payroll`, `@zainx/people`, etc.) from importing AG Grid packages directly.
3. **Graceful Fallback:** If commercial procurement is delayed, `ZainXDataGrid` can fall back to Community features without breaking core operational screens.
