# ZainX Workforce — START HERE v4.1

## Decision

**We are ready to start implementation.**

Start **Phase 0** now.

The architecture is sufficiently locked. Remaining unknowns are implementation spikes, licenses, environment choices and module-level contracts — not reasons to reopen the product architecture.

## Canonical reading path

### Product / architecture
1. `../01_ARCHITECTURE/workforce_platform_engineering_blueprint_v2.0.md`
2. `../01_ARCHITECTURE/workforce_platform_frontend_ux_blueprint_v1.0.md`
3. `../01_ARCHITECTURE/FULL_APP_REPOSITORY_STRUCTURE.md`
4. `../01_ARCHITECTURE/BACKEND_FRONTEND_MODULE_MAP.md`

### Frontend platform
5. `../02_FRONTEND/ZAINX_FRONTEND_ENGINEERING_GUIDELINE_v3.1.md`
6. `../02_FRONTEND/FRONTEND_BASELINE_LOCK.md`
7. `../02_FRONTEND/STACK_DECISION_MATRIX.md`
8. `../02_FRONTEND/STATE_OWNERSHIP_REFERENCE.md`

### Design system
9. `../03_DESIGN_SYSTEM/README.md`
10. `../03_DESIGN_SYSTEM/guides/01_MASTER_BUILD_PROMPT_v3.md`
11. `../03_DESIGN_SYSTEM/guides/03_CANONICAL_COMPONENT_INVENTORY.md`

### Execution
12. `../04_EXECUTION/INTEGRATED_DELIVERY_MODEL.md`
13. `../04_EXECUTION/EXECUTION_ROADMAP_v4.1.md`
14. `../04_EXECUTION/MODULE_START_GATE.md`
15. `../04_EXECUTION/LONG_RUNNING_OPERATION_CONTRACT.md`
16. `../04_EXECUTION/DESIGN_SYSTEM_P0_GATE.md`
17. `../04_EXECUTION/AI_RELEASE_MODEL_7A_7B.md`
18. `../04_EXECUTION/PHASE0_GO_NO_GO_CHECKLIST.md`

### Governance
19. `../05_GOVERNANCE/ADR_REGISTER.md`
20. `../05_GOVERNANCE/DEPENDENCY_LICENSE_REGISTER.md`
21. `../05_GOVERNANCE/RISK_REGISTER.md`

## Hard architectural boundaries

### Payroll
Frontend never becomes the payroll engine.

```text
Frontend command
  → Backend Payroll application service
  → Compliance rule version
  → Calculation
  → Snapshot / trace / result
  → Frontend explanation
```

No hard-coded statutory percentages in frontend code, prompts or design-system demos presented as authoritative.

### AI
AI uses approved backend tools and normal authorization.

The UI may show truthful operational status such as:

```text
Context attached
→ Authorized source requested
→ Tool running
→ Tool completed
→ Answer ready
```

It must not fabricate internal reasoning or hidden chain-of-thought.

### Repository
The frontend is under `/web`; it does not replace the full-product repository.

### Quality
Accessibility, RTL, security, observability and error states are continuous requirements, not Phase-8-only work.

## Ready state

- Architecture: Ready
- Backend baseline: Ready
- Frontend platform direction: Ready
- UX/IA: Ready
- Design-system direction: Ready
- Phase 0: GO
- Feature implementation: starts after Platform Kernel + Design System P0 gates
