# Integrated Product Delivery Model

Every product phase is delivered as a vertical contract-aligned slice.

Do not run backend, frontend, database, design system and QA as unrelated streams.

## Standard phase structure

For every phase/module:

```text
1. Module Work Package
        ↓
2. Domain + Database contract
        ↓
3. API / Events / Permissions contract
        ↓
4. Generated frontend contracts + mocks
        ↓
5. Backend implementation      Frontend implementation
           ↘                       ↙
             Shared acceptance tests
                      ↓
6. Telemetry / security / operations
                      ↓
7. End-to-end workflow gate
```

## Work lanes

### A — Backend / Domain
- aggregates/invariants
- commands
- queries
- domain/application services
- permissions
- events
- background jobs
- idempotency
- audit hooks

### B — Database
- schema ownership
- migrations
- indexes
- constraints
- effective dating
- historical immutability
- transaction/concurrency behavior
- PostgreSQL integration tests

### C — Contracts
- REST endpoints
- optional GraphQL read model
- OpenAPI
- errors / ProblemDetails
- permissions
- events
- async job contracts
- generated clients/mocks

### D — Frontend
- routes
- server state
- URL state
- workflow orchestration
- page patterns
- responsive behavior
- permission states
- loading/error/empty/finalized behavior

### E — Design System
- required shared primitives
- enterprise components
- product components
- Storybook states
- RTL/a11y/motion specifications

### F — Quality / Security / Operations
- unit tests
- architecture tests
- DB integration tests
- contract tests
- Storybook/a11y
- Playwright
- telemetry
- security/privacy
- on-premise/deployment diagnostics

## Contract-first parallelism

Frontend does not wait for finished backend internals.

Once an endpoint/query contract is approved:

```text
OpenAPI / schema
  → generated types
  → MSW mock
  → frontend implementation
```

Backend implements the same contract in parallel.

## Gate

A phase is not complete because "the screen works" or "the API exists."

It is complete only when the end-to-end user workflow works through real contracts with permissions, errors, observability and required historical/audit behavior.
