# Long-Running Operation Contract

Applies to:
- payroll calculation
- payslip/output generation
- imports
- report exports
- integration sync
- large settlement operations
- AI tool execution where asynchronous
- long administrative jobs

## Principle

> **Push improves responsiveness; polling defines correctness.**

The application must remain correct if WebSocket/SSE/SignalR is unavailable in an on-premise environment.

## Command pattern

Example:

```http
POST /api/payroll/runs/{runId}/calculate
Idempotency-Key: ...
```

Possible response:

```http
202 Accepted
Location: /api/jobs/{jobId}
```

```json
{
  "jobId": "job_...",
  "operation": "payroll.calculate",
  "entityId": "run_...",
  "status": "queued"
}
```

## Standard job states

```text
queued
running
completed
completed_with_warnings
failed
cancelled
```

Domain entities keep their own states. A job state never replaces the payroll-run domain state.

## Job query shape

Conceptual:

```json
{
  "jobId": "job_123",
  "operation": "payroll.calculate",
  "status": "running",
  "progress": {
    "kind": "indeterminate",
    "current": null,
    "total": null,
    "messageKey": "payroll.calculation.running"
  },
  "startedAt": "...",
  "updatedAt": "...",
  "completedAt": null,
  "warnings": [],
  "error": null,
  "correlationId": "..."
}
```

If the backend genuinely knows determinate progress:

```json
"progress": {
  "kind": "determinate",
  "current": 320,
  "total": 480,
  "unit": "employees"
}
```

Never fabricate percentages.

## Frontend behavior

TanStack Query owns job polling/server state.

XState may coordinate the local workflow around the remote job.

On completion:
- refetch authoritative business entity
- invalidate relevant queries
- display result/warnings
- never infer final business status solely from the job status

## Push enhancement

Optional transport:
- SignalR
- SSE
- other approved push channel

Push notifies the browser that a job changed.

The browser still reconciles with the canonical REST query.

## Retry / idempotency

High-value commands define idempotency semantics.

Do not let double-click/retry create duplicate payroll calculations, payment batches or exports.

## Security

A user may query only jobs within their authorized tenant/scope.

Job payloads should not leak sensitive payroll/personnel content.

## Observability

Every job exposes/propagates a correlation ID.

Frontend traces:
command requested → job accepted → polling/push → final entity refetch.
