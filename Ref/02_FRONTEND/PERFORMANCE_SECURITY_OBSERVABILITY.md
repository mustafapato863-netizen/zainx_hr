# Performance, Security and Observability

## Performance
- route-level splitting
- lazy-load grid/chart/scheduler/editor
- server-side huge-grid operations
- virtualization for large custom lists
- bundle budgets and route chunk review

## Security
- no secrets in frontend
- no sensitive browser persistence
- backend authorization always
- sanitize rich content
- CSP/security headers at deployment
- no raw exception/stack trace
- no sensitive telemetry payloads

## Observability
Use OpenTelemetry Web and propagate correlation IDs across frontend → ASP.NET → workers/DB.
Trace high-value actions and workflow failures without logging sensitive payroll/PII values.
