# ZainX — Source of Truth v4.1

## Architecture decisions
1. Approved ADR for the exact decision
2. Engineering Blueprint v2.x
3. Frontend Engineering Guideline v3.x for frontend implementation decisions
4. Frontend UX/IA Blueprint
5. Design System contracts
6. Module Work Package
7. Implementation code
8. Ticket/chat/comment

## Runtime contract truth
1. Versioned OpenAPI / GraphQL / module contract
2. Generated client/contracts
3. Backend implementation tests
4. Database migrations for physical schema truth

## Important distinction

The Design System may define how a field behaves visually, but it cannot define an API DTO.

A database migration may define a column physically, but frontend code must not use it as an API contract.

An execution roadmap may schedule a feature, but it cannot create a new payroll law, domain state or permission not present in approved architecture/contracts.

## Prototype status

Files under `03_DESIGN_SYSTEM/reference_prototype/` are visual and interaction references only.

They are not production code and cannot override:
- generated contracts
- accessibility rules
- RTL rules
- frontend engineering guideline
- approved product/domain behavior
