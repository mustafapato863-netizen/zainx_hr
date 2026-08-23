# AG Grid Enterprise — Phase 2 License Gate & Capability Boundary

**Document Version:** 1.0  
**Status:** Mandatory Governance Gate for Phase 2  
**Date:** August 24, 2026  
**Owners:** Architecture / Product Engineering / Procurement

---

## 1. Mandatory Phase 2 License Gate

> [!IMPORTANT]
> **PHASE 2 PRODUCT SPECIFICATION RULE:**  
> Commercial procurement of AG Grid Enterprise licenses has NOT yet occurred.  
> **Until an official commercial Enterprise license is procured and approved, Phase 2 (People & Foundational HR) and subsequent feature modules MUST NOT depend on Enterprise-only capabilities as a blocking product or functional requirement.**

Feature modules (`@zainx/people`, `@zainx/payroll`, etc.) may consume the design system wrapper [`ZainXDataGrid`](file:///D:/Projects/ZainX_HR/web/packages/design-system/src/components/ZainXDataGrid/ZainXDataGrid.tsx), but all product screens in Phase 2 must be fully functional using only **COMMUNITY-SAFE** capabilities.

---

## 2. AG Grid 33.1.x Capability Classification

Verified against `ag-grid-community@33.1.1` and `ag-grid-enterprise@33.1.1`:

### A. COMMUNITY-SAFE Capabilities (Approved for Phase 2 Production Use)
These features operate natively in the free/open-source AG Grid Community engine:
1. **High-Performance DOM Virtualization:** Up to 100,000 rows with 60 FPS scrolling and low memory footprint.
2. **Column Pinning:** Freezing columns on the left or right (essential for employee ID, name, action columns).
3. **Column Sorting:** Multi-column sort with shift-click, custom comparators.
4. **Column Resizing & Reordering:** Dragging headers, auto-size to fit, column min/max constraints.
5. **Column Filtering:** Simple text filter, number filter, date filter.
6. **Row Selection:** Single-row selection, multi-row checkbox selection.
7. **Custom Cell Renderers:** React custom cell renderers ([`Money`](file:///D:/Projects/ZainX_HR/web/packages/design-system/src/components/Money/Money.tsx), [`Badge`](file:///D:/Projects/ZainX_HR/web/packages/design-system/src/components/Badge/Badge.tsx), [`SensitiveValue`](file:///D:/Projects/ZainX_HR/web/packages/design-system/src/components/SensitiveValue/SensitiveValue.tsx)).
8. **Client-Side Pagination:** Standard page size controls and page switching.
9. **CSV Export:** Client-side CSV generation and download.
10. **RTL & Logical Layout:** Full right-to-left Hebrew/Arabic layout mirroring.

### B. ENTERPRISE-ONLY Capabilities (Gated by Commercial License)
These features require a valid commercial license key and must NOT be required by Phase 2 deliverables:
1. **Excel (.xlsx) Export with Styles:** Native formatting, multi-tab workbooks, formula preservation.
2. **Master-Detail Hierarchy:** Nested expandable sub-grids for line items and dependent data.
3. **Server-Side Row Model (SSRM):** Dynamic lazy-loaded infinite chunking and server-side grouping.
4. **Range Selection & Fill Handle:** Excel-like cell bounding box selections and formula dragging.
5. **Clipboard Cut/Copy/Paste:** Inter-cell clipboard operations with OS paste integration.
6. **Row Grouping & Aggregations (Pivot Mode):** Dynamic drag-and-drop column grouping and aggregate footers.
7. **Enterprise Tool Panels & Menus:** Column side drawer, column chooser panel, filter panel.
8. **Status Bar Aggregations:** Bottom status bar with Sum, Min, Max, Average counts.

---

## 3. License Key Handling & CI/CD Guardrails

1. **No Hardcoded Keys:**
   - No trial, developer, or production license key may ever be committed to the Git repository or hardcoded into client source files.
2. **Environment Variable Injection:**
   - When procured, the production key is injected via `VITE_AG_GRID_LICENSE_KEY` during deployment or Docker container startup.
3. **Graceful Degraded Mode:**
   - In the absence of a license key, `ZainXDataGrid` operates in Community mode in development/evaluation without throwing unhandled exceptions or breaking application functionality.
