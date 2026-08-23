# ZainX Workforce — Canonical Reference v4.1

**Status:** Ready for implementation kickoff  
**Date:** 2026-08-23  
**Start decision:** **GO — begin Phase 0**

v4.1 is the cleaned and execution-ready canonical package. It upgrades v4.0 by integrating backend, database, contracts, frontend, design system, testing, security/operations, and AI delivery into one plan.

## Start order

1. `00_START/START_HERE.md`
2. `00_START/SOURCE_OF_TRUTH.md`
3. `01_ARCHITECTURE/workforce_platform_engineering_blueprint_v2.0.md`
4. `01_ARCHITECTURE/workforce_platform_frontend_ux_blueprint_v1.0.md`
5. `01_ARCHITECTURE/FULL_APP_REPOSITORY_STRUCTURE.md`
6. `01_ARCHITECTURE/BACKEND_FRONTEND_MODULE_MAP.md`
7. `02_FRONTEND/ZAINX_FRONTEND_ENGINEERING_GUIDELINE_v3.1.md`
8. `03_DESIGN_SYSTEM/README.md`
9. `04_EXECUTION/INTEGRATED_DELIVERY_MODEL.md`
10. `04_EXECUTION/EXECUTION_ROADMAP_v4.1.md`
11. `04_EXECUTION/MODULE_START_GATE.md`
12. `04_EXECUTION/PHASE0_GO_NO_GO_CHECKLIST.md`
13. `05_GOVERNANCE/ADR_REGISTER.md`
14. `04_EXECUTION/AI_Missions/00_Phase_0_Foundation.md`
15. `04_EXECUTION/AI_Missions/ZainX_Signature_Login_Intro_Prompt.md`

## What is locked

- Full product repository with .NET backend at root and Nx/pnpm frontend under `/web`.
- Modular Monolith backend and Modular Frontend Monolith.
- Backend owns payroll/compliance calculation truth, snapshots, rule evaluation, rounding, finalization and historical reproducibility.
- Frontend owns interaction, orchestration, presentation, server-produced trace/explanation, guarded commands and responsive UX.
- REST/OpenAPI is canonical for commands and operational APIs; GraphQL is optional for genuine compositional reads.
- State ownership is split deliberately: TanStack Query / Redux Toolkit / XState / Router / RHF / React local state.
- Design System P0 is a hard gate before feature teams scale.
- Long-running operations use a common job contract with polling as correctness baseline; push is an optional accelerator.
- AI ships in two governed product stages: **7A Read/Analyze/Explain**, then **7B Proposed/Confirmed Actions**.
- RTL, accessibility, permissions, sensitive-data protection, error states, observability and on-premise constraints apply from the beginning.

## Execution rule

Do not ask an AI agent to "build the whole product" from the root prompt.

Run the phases in order, and do not start a module until its Module Start Gate is complete.

> **Stop redesigning the architecture. Start building the platform foundation.**
