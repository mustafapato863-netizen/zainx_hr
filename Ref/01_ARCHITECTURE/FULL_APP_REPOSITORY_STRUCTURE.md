# Recommended Full Application Repository Structure

This resolves the conflict between the canonical .NET repository in the Engineering Blueprint and the standalone Nx frontend structure.

```text
zainx-workforce/
│
├── src/                                  # Backend — canonical .NET modular monolith
│   ├── Workforce.Host.Api/
│   ├── Workforce.Host.Worker/
│   ├── Workforce.SharedKernel/
│   ├── Workforce.BuildingBlocks/
│   └── Modules/
│       ├── Tenancy/
│       ├── Identity/
│       ├── Organization/
│       ├── People/
│       ├── Documents/
│       ├── Attendance/
│       ├── Leave/
│       ├── Approvals/
│       ├── Payroll/
│       ├── Compliance/
│       ├── Settlement/
│       ├── Recruitment/
│       ├── Reporting/
│       ├── Integrations/
│       ├── Notifications/
│       ├── Audit/
│       └── Ai/
│
├── web/                                  # Frontend — Nx modular frontend monolith
│   ├── apps/
│   │   ├── workforce-web/
│   │   ├── design-system-docs/
│   │   └── e2e/
│   │
│   ├── packages/
│   │   ├── platform/
│   │   │   ├── auth/
│   │   │   ├── session/
│   │   │   ├── permissions/
│   │   │   ├── tenancy/
│   │   │   ├── entitlements/
│   │   │   ├── shell/
│   │   │   ├── routing/
│   │   │   ├── i18n/
│   │   │   ├── errors/
│   │   │   ├── telemetry/
│   │   │   ├── feature-flags/
│   │   │   └── storage/
│   │   │
│   │   ├── design-system/
│   │   │   ├── tokens/
│   │   │   ├── icons/
│   │   │   ├── primitives/
│   │   │   ├── forms/
│   │   │   ├── navigation/
│   │   │   ├── data/
│   │   │   ├── overlays/
│   │   │   ├── feedback/
│   │   │   ├── enterprise/
│   │   │   ├── motion/
│   │   │   └── signature/
│   │   │
│   │   ├── contracts/
│   │   │   ├── rest-generated/
│   │   │   ├── graphql-generated/
│   │   │   ├── permissions/
│   │   │   ├── errors/
│   │   │   └── shared/
│   │   │
│   │   ├── people/
│   │   ├── attendance/
│   │   ├── leave/
│   │   ├── payroll/
│   │   ├── recruitment/
│   │   ├── approvals/
│   │   ├── reports/
│   │   ├── administration/
│   │   └── ai/
│   │
│   ├── tooling/
│   │   ├── eslint/
│   │   ├── generators/
│   │   ├── nx/
│   │   ├── test-utils/
│   │   ├── mock-data/
│   │   └── scripts/
│   │
│   ├── package.json
│   ├── pnpm-workspace.yaml
│   ├── nx.json
│   ├── tsconfig.base.json
│   └── vite/shared configs
│
├── tests/
│   ├── Architecture.Tests/
│   ├── EndToEnd.Tests/
│   └── Modules/
│
├── deploy/
│   ├── docker/
│   ├── nginx/
│   ├── migrations/
│   ├── backup/
│   └── offline-update/
│
├── docs/
│   ├── adr/
│   │   ├── backend/
│   │   └── frontend/
│   ├── modules/
│   ├── api/
│   ├── operations/
│   └── architecture/
│
├── Ref/                                  # Source reference / prototypes only
│
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── Workforce.slnx
└── README.md
```

## Why this structure

- preserves the Engineering Blueprint's full-product repo
- gives frontend its own clean Nx/pnpm workspace
- supports independent backend/frontend CI caching
- keeps on-prem deployment artifacts beside the product
- avoids pretending the frontend is a separate product/repository
- avoids adopting root-level Nx control over .NET unless a future ADR explicitly chooses that

## Frontend feature package rule

Frontend packages are **experience ownership boundaries**, not database ownership boundaries.

They consume approved backend module contracts.
