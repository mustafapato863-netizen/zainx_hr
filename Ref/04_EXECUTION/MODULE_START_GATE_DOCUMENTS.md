# Module Start Gate — Documents Infrastructure (2C)

**Module ID:** `documents`  
**Module Name:** Workforce Documents Infrastructure  
**Phase:** Phase 2C  
**Date:** August 24, 2026  
**Status:** **IMPLEMENTATION CHECKPOINT — NOT RELEASE SEALED**
**Owner:** Core Architecture & Platform Engineering

---

## 1. Identity & Purpose
- **Business Purpose:** Provide secure, audited, on-premise and cloud-agnostic document storage for workforce records (National IDs, Passports, Employment Contracts, Diplomas, Medical Clearances).
- **Explicit Non-Goals:**
  - No public binary CDN or unauthenticated direct URL sharing.
  - No optical character recognition (OCR) or automated parsing in Phase 2.
  - No document e-signature workflow (owned by Approvals/Contracts).

---

## 2. Domain Model & Invariants
- **Aggregates:**
  - `Document` (Aggregate Root): Represents the business document entity.
    - Fields: `Id`, `TenantId`, `LegalEntityId`, `OwnerType` (`Employee`, `OrganizationUnit`), `OwnerId`, `DocumentTypeId`, `Title`, `Status` (`Active`, `Archived`, `Expired`), `ExpiryDate`, `CreatedAt`, `CreatedBy`.
  - `DocumentVersion` (Entity / Version History):
    - Fields: `Id`, `DocumentId`, `VersionNumber`, `StorageKey`, `FileName`, `FileSize`, `ContentType`, `Sha256Checksum`, `UploadedAt`, `UploadedBy`.
  - `DocumentType` (Configuration Entity):
    - Fields: `Id`, `Code`, `NameEn`, `NameAr`, `IsRequired`, `RequiresExpiryDate`, `AllowedMimeTypes`, `MaxSizeBytes`.
- **Invariants:**
  - Binary payloads are stored via `IStorageProvider` (filesystem volume / S3), never as raw BLOBs in transactional SQL tables.
  - Replacing a document creates a new `DocumentVersion` with incremented `VersionNumber` without destroying historical version records.
  - Upload validates MIME type and file size against `DocumentType` configuration before persisting.

---

## 3. Database Schema (`documents.*`)
- **Tables:**
  - `documents.document_types`
  - `documents.documents`
  - `documents.document_versions`
  - `documents.document_access_logs`

---

## 4. Operational Endpoints & Permissions
- `GET /api/v1/documents?ownerType=...&ownerId=...` (`documents.read`)
- `POST /api/v1/documents/upload` (`documents.upload`)
- `GET /api/v1/documents/{id}/download` (`documents.download`)
- `POST /api/v1/documents/{id}/replace` (`documents.replace`)
- `POST /api/v1/documents/{id}/archive` (`documents.archive`)
- `GET /api/v1/documents/types` (`documents.types.read`)
