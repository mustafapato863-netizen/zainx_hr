# Module Start Gate — People Core (2B)

**Module ID:** `people`  
**Module Name:** People Core & Workforce Master Data  
**Phase:** Phase 2B  
**Date:** August 24, 2026  
**Status:** **IMPLEMENTATION CHECKPOINT — NOT RELEASE SEALED**
**Owner:** Core Architecture & Platform Engineering

---

## 1. Identity & Purpose
- **Business Purpose:** Provide the authoritative employee master-data foundation, separating human identity (`Person`) from legal employment relationships (`Employment`) and temporal assignments (`EmploymentAssignment`), with full effective-dating, state lifecycle management, and sensitive PII authorization.
- **Explicit Non-Goals:**
  - No payroll salary calculation, allowances, tax, deductions, or payment file generation.
  - No leave balance calculation or vacation accruals.
  - No attendance biometric processing or timesheets.
  - No candidate interview feedback or applicant tracking (owned by Recruitment).

---

## 2. Domain Aggregates & Invariants
- **Aggregates:**
  - `Person` (Aggregate Root): Represents the unique individual human.
    - Fields: `Id`, `TenantId`, `FirstNameEn`, `LastNameEn`, `FirstNameAr`, `LastNameAr`, `DateOfBirth`, `Gender`, `Nationality`, `NationalIdentifier` (Sensitive PII), `PrimaryEmail`, `PhoneNumber`.
  - `Employment` (Aggregate Root): Represents the legal employment contract with a specific `LegalEntityId`.
    - Fields: `Id`, `TenantId`, `PersonId`, `LegalEntityId`, `EmployeeNumber`, `HireDate`, `ProbationEndDate`, `TerminationDate`, `Status` (`Draft`, `Active`, `Inactive`, `Terminated`), `ConcurrencyVersion`.
  - `EmploymentAssignment` (Entity / Effective-Dated Child):
    - Fields: `Id`, `EmploymentId`, `OrganizationUnitId`, `PositionId`, `LocationId`, `ManagerEmploymentId`, `JobTitleEn`, `JobTitleAr`, `EffectiveFrom`, `EffectiveTo`, `IsCurrent`.
- **Invariants:**
  - `EmployeeNumber` must be unique per `LegalEntityId`.
  - A person may have multiple historical employments, but only one `Active` employment per legal entity at any point in time.
  - `EmploymentAssignment` periods for a given employment must never overlap.
  - State machine transitions:
    - `Draft` $\rightarrow$ `Active` (Hire / Onboard)
    - `Active` $\rightarrow$ `Inactive` (Suspend / Extended Leave)
    - `Inactive` $\rightarrow$ `Active` (Reactivate)
    - `Active` or `Inactive` $\rightarrow$ `Terminated` (Final termination with reason code)
  - Optimistic concurrency: Enforced via `ConcurrencyVersion` (409 Conflict ProblemDetails on clash).

---

## 3. Database Schema (`people.*`)
- **Tables:**
  - `people.persons`
  - `people.employments`
  - `people.employment_assignments`
  - `people.sensitive_pii_audit`
- **Indexes:**
  - Unique index on `(tenant_id, legal_entity_id, employee_number)` on `people.employments`.
  - B-Tree index on `(employment_id, is_current)` on `people.employment_assignments`.
  - Full-text search / trigram index on `(first_name_en, last_name_en, first_name_ar, last_name_ar)` for rapid operational directory search.

---

## 4. Sensitive PII Security & Temporary Reveal
- Sensitive fields (`national_identifier`, `date_of_birth`) are masked by default in directory and profile queries (`"109********"`).
- Temporary reveal endpoint: `POST /api/v1/people/employees/{id}/reveal-sensitive` checks `people.employee.reveal_pii` and records immutable audit log.

---

## 5. Frontend & Design System Reuse
- **Employee Directory (`P2`):** Dense operational grid using `ZainXDataGrid` (Community-Safe mode), `FilterBar`, `SavedViews`, `DensitySwitcher`, `ColumnChooser`, `BulkActionBar`, `Pagination`.
- **Employee Detail Workspace (`P3`):** `PageHeader` with status treatments, sub-tabs (**Overview**, **Employment**, **Organization**, **Documents**, **History**).
- **Forms:** React Hook Form + Zod for client interactions; server owns domain rules.
