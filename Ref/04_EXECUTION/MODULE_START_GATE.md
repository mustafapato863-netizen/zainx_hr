# Module Start Gate

A product module/vertical slice cannot begin implementation until this gate is satisfied.

Use `06_TEMPLATES/developer_module_work_package_template.md`.

## Required before coding

### Identity
- Module ID/name
- business purpose
- explicit non-goals
- owner

### Domain
- aggregates
- invariants
- state machines
- effective-dating rules
- immutable states
- money/date semantics

### Database
- schema owner
- tables
- constraints
- indexes
- migration plan
- deletion/retention behavior

### Commands
For each command:
- name
- permission
- input
- validation
- domain effect
- idempotency
- audit requirement
- synchronous vs async

### Queries
- purpose
- permission/scope
- result contract
- pagination/filtering
- sensitive fields
- read-model owner

### Events
- emitted
- consumed
- outbox requirement
- idempotent handlers

### Permissions
- permission IDs
- tenant/legal-entity/org scope
- sensitive read permissions

### API / Contracts
- REST paths
- OpenAPI shape
- GraphQL read only if justified
- ProblemDetails/error codes
- concurrency behavior
- long-running job contract where applicable

### Frontend
- canonical page pattern
- routes
- state ownership
- required DS components
- loading/error/empty/read-only/finalized
- responsive responsibility
- RTL
- a11y

### Tests
- unit
- PostgreSQL integration
- contract
- API
- permission
- E2E
- golden/regression tests if financial/compliance

### Operations
- telemetry
- correlation IDs
- jobs
- alerts
- support diagnostics
- on-premise considerations

## Gate decision

Only after this document is reviewed:
**READY FOR IMPLEMENTATION**

If critical domain/contract fields are unknown:
**NOT READY**
