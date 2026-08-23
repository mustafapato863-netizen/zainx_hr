# AI Mission — Phase 4: Payroll + Compliance + Settlement

Critical boundary:
**Frontend must never implement payroll/statutory calculation truth.**

Backend owns:
rule versions, calculations, rounding, snapshots, trace, finalization, historical reproducibility and outputs.

Frontend owns:
guided orchestration, job status, readiness, exceptions, results, explanation, variance, approval/finalization commands and output status.

Use:
- canonical Payroll module work package
- `../LONG_RUNNING_OPERATION_CONTRACT.md`
- Payroll section of `../EXECUTION_ROADMAP_v4.1.md`

Do not hard-code statutory rates in frontend code or prompts.

Exit only when a real finalized run is immutable, reproducible and explainable.
