# ZainX Project Risk Register v4.1

| ID | Risk | Severity | Mitigation |
|---|---|---:|---|
| R-001 | Frontend accidentally becomes payroll authority | Critical | ADR-PAY-001, backend contracts, Phase-4 gate |
| R-002 | Statutory values hard-coded in UI/prompts | Critical | versioned Compliance rules + SME validation |
| R-003 | Feature teams start before DS P0 | High | Design System P0 hard gate |
| R-004 | Backend/frontend repo topology diverges | High | nested `/web` canonical structure |
| R-005 | AI agents use stale/conflicting prompts | High | v4.1 corrected missions only |
| R-006 | AI UI fabricates reasoning/progress | High | truthful tool/activity states only |
| R-007 | Long-running operations lack reliable status/retry | High | standard job contract; polling baseline |
| R-008 | Commercial grid/scheduler licenses discovered late | High | Phase 1C + procurement register |
| R-009 | Toolchain incompatibility | Medium | Phase-0 compatibility lock |
| R-010 | RTL/accessibility retrofitted late | High | DS/CI gates from Phase 1 |
| R-011 | Sensitive data leaks to storage/telemetry | Critical | storage/security/telemetry ADRs |
| R-012 | Browser becomes cross-module integration layer | High | purpose-built backend read models / optional GraphQL |
| R-013 | ATS optimistic stage move diverges from server | Medium | command authority + rollback |
| R-014 | On-premise push channel fails | Medium | polling defines correctness |
| R-015 | On-premise deployment issues found only at release | High | Docker/no-CDN/reverse-proxy smoke from Phase 0 |
| R-016 | Module starts without commands/permissions/state contract | High | Module Start Gate |
| R-017 | Brand motion blocked by raster-only assets | Low | vector asset gate; provisional animation |
