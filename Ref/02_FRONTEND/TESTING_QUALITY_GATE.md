# Testing and Quality Gate

Required:
- TypeScript strict
- ESLint
- Nx boundary checks
- Vitest
- Testing Library
- Storybook interaction tests
- axe accessibility checks
- MSW mocks aligned with generated contracts
- Playwright critical flows
- RTL checks
- no unexpected heavy-bundle regression

Critical Payroll E2E:
calculate → exceptions → review → approve → finalize → explain → finalized immutable state → permissions/errors.
