# FullCalendar Major Version Architectural Decision

**Document Version:** 1.0  
**Status:** **DECISION: KEEP FullCalendar 6.1.x (Version 6.1.15)**  
**Date:** August 24, 2026  
**Owners:** Architecture / Frontend Engineering

---

## 1. Executive Decision

ZainX Workforce formally selects **FullCalendar 6.1.x (specifically locked to `6.1.15`)** as the standard scheduling baseline. We explicitly **REJECT upgrading to FullCalendar 7.x at this time**.

---

## 2. Comparative Evaluation Matrix

| Evaluation Criterion | FullCalendar 6.1.x (Locked: 6.1.15) | FullCalendar 7.x (Prerelease / Current) | ZainX Assessment |
| :--- | :--- | :--- | :--- |
| **Stability & Release Maturity** | Long-term stable (LTS), battle-tested across thousands of enterprise deployments. | Active major redesign / beta transition with rapid breaking changes. | **6.1.x is stable and production-ready.** |
| **Package Export & Module Resolution** | Standard Node/Vite ESM module exports across core and all plugins. | Breaking ESM export restructuring causes missing subpath errors (`./index.js` missing in `package.json`). | **6.1.x integrates seamlessly with Vite 8 & Vitest.** |
| **React 19 Compatibility** | Fully compatible with React 18 & 19 through `@fullcalendar/react@6.1.15`. | Unstable wrapper bindings undergoing refactoring. | **6.1.x is verified.** |
| **Open-Source License Status** | Core, DayGrid, TimeGrid, List, and Interaction plugins are **100% MIT Open Source**. | Same licensing structure, but plugin ecosystem in flux. | **6.1.x fulfills all standard view requirements under MIT.** |
| **Commercial Premium Upgrade Path** | Fully compatible with FullCalendar Scheduler v6 commercial license. | Scheduler v7 ecosystem in active migration. | **6.1.x provides a stable procurement upgrade path if needed.** |

---

## 3. Review Trigger
Re-evaluate migration to FullCalendar 7.x only after version 7.x reaches general availability (GA) LTS stability and its plugin ecosystem reaches full parity with Vite/Rollup modern subpath exports.
