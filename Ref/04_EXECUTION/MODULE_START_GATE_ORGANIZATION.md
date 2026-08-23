# Module Start Gate — Organization Foundation (2A)

**Module ID:** `organization`  
**Module Name:** Organization Foundation  
**Phase:** Phase 2A  
**Date:** August 24, 2026  
**Status:** **READY FOR IMPLEMENTATION**  
**Owner:** Core Architecture & Platform Engineering

---

## 1. Identity & Purpose
- **Business Purpose:** Establish the structural foundation of the organization (Departments, Divisions, Sections, Locations, Positions, Managerial Hierarchies) required as master data for workforce assignment and reporting.
- **Explicit Non-Goals:**
  - No attendance scheduling or shift configuration.
  - No leave policy rules or approvals.
  - No payroll cost center calculations or GL account posting.
  - No recruitment requisitions or job posting management.

---

## 2. Domain Model & Invariants
- **Aggregates:**
  - `OrganizationUnit` (Aggregate Root): Represents an organizational entity (Company, Division, Department, Section).
  - `Position` (Entity / Sub-Aggregate): Represents an approved job seat/position.
  - `Location` (Aggregate Root): Represents a physical/legal work facility.
- **Value Objects:**
  - `OrganizationUnitId`, `PositionId`, `LocationId`, `TenantId`, `LegalEntityId`.
  - `EffectivePeriod` (`EffectiveFrom: DateOnly`, `EffectiveTo: DateOnly?`).
- **Invariants:**
  - An OrganizationUnit must belong to a valid `TenantId` and `LegalEntityId`.
  - Cyclic parent-child relationships in the organizational hierarchy are strictly prohibited.
  - Code must be unique within the tenant and legal entity.
  - Deactivating a unit does not delete historical assignments.

---

## 3. Database Schema (`organization.*`)
- **Schema Owner:** `organization`
- **Tables:**
  - `organization.organization_units`: `id`, `tenant_id`, `legal_entity_id`, `code`, `name_en`, `name_ar`, `type`, `parent_unit_id`, `manager_position_id`, `is_active`, `effective_from`, `effective_to`, `created_at`, `updated_at`, `row_version`.
  - `organization.positions`: `id`, `tenant_id`, `legal_entity_id`, `organization_unit_id`, `job_code`, `title_en`, `title_ar`, `grade`, `is_active`, `created_at`, `updated_at`.
  - `organization.locations`: `id`, `tenant_id`, `legal_entity_id`, `code`, `name_en`, `name_ar`, `country`, `city`, `address`, `is_active`, `created_at`.
- **Constraints & Indexes:**
  - Primary keys on `id`.
  - Unique index on `(tenant_id, legal_entity_id, code)` for units and positions.
  - B-Tree indexes on `parent_unit_id`, `organization_unit_id`, `is_active`.

---

## 4. Commands, Queries & Permissions
- **Commands:**
  - `CreateOrganizationUnitCommand` (`organization.unit.create`)
  - `UpdateOrganizationUnitCommand` (`organization.unit.update`)
  - `DeactivateOrganizationUnitCommand` (`organization.unit.deactivate`)
  - `CreateLocationCommand` (`organization.location.create`)
  - `CreatePositionCommand` (`organization.position.create`)
- **Queries:**
  - `GetOrganizationUnitsQuery` (`organization.unit.read`) — returns flattened and hierarchical tree representations.
  - `GetOrganizationUnitByIdQuery` (`organization.unit.read`)
  - `GetLocationsQuery` (`organization.location.read`)
  - `GetPositionsQuery` (`organization.position.read`)

---

## 5. API Contracts & Errors
- `GET /api/v1/organization/units`
- `GET /api/v1/organization/units/{id}`
- `POST /api/v1/organization/units`
- `PUT /api/v1/organization/units/{id}`
- `POST /api/v1/organization/units/{id}/deactivate`
- `GET /api/v1/organization/locations`
- `POST /api/v1/organization/locations`
- Standard ProblemDetails for validation (400), not found (404), optimistic concurrency conflicts (409).
