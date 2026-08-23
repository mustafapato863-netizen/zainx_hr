# Module Work Package Template

## 1. Module Identity
- Module:
- Owner:
- Backup reviewer:
- Contract version:
- Schema:
- Related ADRs:

## 2. Purpose
Describe the business capability this module owns.

## 3. Non-Goals
List responsibilities that belong to other modules.

## 4. Aggregate Roots and Invariants
- Aggregate:
- State machine:
- Non-negotiable invariants:

## 5. Database Ownership
| Table | PK | FK / logical references | Critical constraints | Lifecycle |
| --- | --- | --- | --- | --- |

## 6. Public Commands
| Command | Input | Output | Permission | Idempotency |
| --- | --- | --- | --- | --- |

## 7. Public Queries
| Query | Input | Output | Permission/scope | Caching |
| --- | --- | --- | --- | --- |

## 8. Events Published
| Event | Version | Trigger | Consumers | Durable? |
| --- | --- | --- | --- | --- |

## 9. Contracts Consumed
| Producer module | Contract | Why required | Failure behavior |
| --- | --- | --- | --- |

## 10. Permissions
List `<module>.<resource>.<action>` permissions and data scopes.

## 11. Background Jobs
List schedule/trigger, idempotency key, retry behavior and observability.

## 12. API / OpenAPI Examples
Add request/response examples and stable error codes.

## 13. Test Plan
- Unit:
- PostgreSQL integration:
- Contract:
- Authorization:
- Concurrency:
- Golden/eval tests if applicable:

## 14. Migration Plan
Describe schema migrations, data backfill and compatibility.

## 15. Security / Privacy
Describe sensitive fields, masking, audit and retention.

## 16. Definition of Done
- [ ] Contract agreed
- [ ] Migrations included
- [ ] Integration tests pass
- [ ] Tenant isolation tested
- [ ] Permissions tested
- [ ] README updated
- [ ] Observability added
- [ ] Consumer fixtures/mocks published

## 17. Deferred Decisions / Known Limitations
