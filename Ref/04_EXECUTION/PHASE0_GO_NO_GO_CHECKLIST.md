# Phase 0 — Go / No-Go Checklist v4.1

## Start decision
**GO**

## Repository / toolchain
- [ ] Create full-product repo structure
- [ ] Create `/web` pnpm/Nx workspace
- [ ] Lock Node 24 LTS
- [ ] Verify TypeScript 6 compatibility; temporary 5.9 fallback only by ADR
- [ ] Lock compatible Vite/Nx/Storybook/Tailwind versions
- [ ] Commit lockfiles
- [ ] Configure Nx boundaries
- [ ] Configure ESLint / Prettier / TypeScript strict

## Backend foundation
- [ ] .NET solution/module shells
- [ ] API Host
- [ ] Worker Host
- [ ] SharedKernel/BuildingBlocks conventions
- [ ] ProblemDetails
- [ ] configuration/secrets convention
- [ ] OpenTelemetry baseline
- [ ] architecture-test project
- [ ] background-job abstraction
- [ ] outbox baseline decision

## Database
- [ ] PostgreSQL 18 dev environment
- [ ] migration infrastructure
- [ ] schema-per-module convention
- [ ] integration-test DB strategy
- [ ] backup/restore smoke skeleton

## Contracts
- [ ] OpenAPI generation
- [ ] Orval generation
- [ ] standard errors
- [ ] permission ID convention
- [ ] long-running job contract accepted
- [ ] generated client smoke test

## Frontend
- [ ] workforce-web app
- [ ] design-system-docs app
- [ ] e2e app
- [ ] shell route
- [ ] base providers
- [ ] test harness
- [ ] no-CDN production asset policy

## CI / Quality
- [ ] backend build/test
- [ ] frontend lint/typecheck/test/build
- [ ] Storybook build smoke
- [ ] Playwright smoke
- [ ] dependency/security scanning
- [ ] artifact/package output

## Deployment / on-premise
- [ ] Docker Compose local stack
- [ ] Nginx/reverse-proxy reference
- [ ] offline asset smoke
- [ ] environment config strategy
- [ ] health/readiness endpoints

## Governance
- [ ] Accept P0 ADRs
- [ ] Browser support ADR
- [ ] frontend telemetry/privacy ADR
- [ ] storage/session ADR
- [ ] AG Grid commercial evaluation
- [ ] FullCalendar commercial evaluation
- [ ] GSAP licensing review if retained
- [ ] font asset/license owner
- [ ] dependency/license register owner

## Exit gate

Phase 0 is complete only when a clean CI environment can:

1. build backend and frontend,
2. start PostgreSQL/API/web using the dev deployment path,
3. call a health endpoint,
4. generate the OpenAPI frontend client,
5. run backend/frontend tests,
6. build Storybook,
7. run a Playwright smoke test,
8. propagate a correlation ID through a sample request.
