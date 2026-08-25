using Npgsql;
using Workforce.Modules.Documents.Application;
using Workforce.Modules.Documents.Domain;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Documents.Infrastructure;

public class DocumentsRepository
{
    private readonly string _connectionString;

    public DocumentsRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<DocumentTypeDto?> GetDocumentTypeAsync(Guid documentTypeId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id, code, name_en, name_ar, is_required, requires_expiry_date, allowed_mime_types, max_size_bytes
            FROM documents.document_types
            WHERE id = @id;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", documentTypeId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return new DocumentTypeDto
        {
            Id = reader.GetGuid(0),
            Code = reader.GetString(1),
            NameEn = reader.GetString(2),
            NameAr = reader.GetString(3),
            IsRequired = reader.GetBoolean(4),
            RequiresExpiryDate = reader.GetBoolean(5),
            AllowedMimeTypes = reader.GetString(6),
            MaxSizeBytes = reader.GetInt64(7)
        };
    }

    public async Task<IReadOnlyList<DocumentTypeDto>> ListDocumentTypesAsync(CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id, code, name_en, name_ar, is_required, requires_expiry_date, allowed_mime_types, max_size_bytes
            FROM documents.document_types
            ORDER BY name_en ASC;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        var list = new List<DocumentTypeDto>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new DocumentTypeDto
            {
                Id = reader.GetGuid(0),
                Code = reader.GetString(1),
                NameEn = reader.GetString(2),
                NameAr = reader.GetString(3),
                IsRequired = reader.GetBoolean(4),
                RequiresExpiryDate = reader.GetBoolean(5),
                AllowedMimeTypes = reader.GetString(6),
                MaxSizeBytes = reader.GetInt64(7)
            });
        }
        return list;
    }

    public async Task<IReadOnlyList<DocumentSummaryDto>> ListDocumentsAsync(
        TenantId tenantId,
        string ownerType,
        Guid ownerId,
        LegalEntityId? legalEntityId = null,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var sql = @"
            SELECT 
                d.id, d.tenant_id, d.legal_entity_id, d.owner_type, d.owner_id, d.document_type_id,
                dt.code, dt.name_en, dt.name_ar, d.title, d.status, d.expiry_date, d.created_at,
                v.version_number, v.file_name, v.file_size, v.content_type
            FROM documents.documents d
            INNER JOIN documents.document_types dt ON d.document_type_id = dt.id
            LEFT JOIN LATERAL (
                SELECT version_number, file_name, file_size, content_type
                FROM documents.document_versions
                WHERE document_id = d.id
                ORDER BY version_number DESC
                LIMIT 1
            ) v ON TRUE
            WHERE d.tenant_id = @tenantId AND d.owner_type = @ownerType AND d.owner_id = @ownerId
        ";

        if (legalEntityId.HasValue)
        {
            sql += " AND d.legal_entity_id = @legalEntityId";
        }

        sql += " ORDER BY d.created_at DESC;";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        cmd.Parameters.AddWithValue("ownerType", ownerType);
        cmd.Parameters.AddWithValue("ownerId", ownerId);
        if (legalEntityId.HasValue) cmd.Parameters.AddWithValue("legalEntityId", legalEntityId.Value.Value);

        var list = new List<DocumentSummaryDto>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new DocumentSummaryDto
            {
                Id = reader.GetGuid(0),
                TenantId = reader.GetGuid(1).ToString(),
                LegalEntityId = reader.GetGuid(2).ToString(),
                OwnerType = reader.GetString(3),
                OwnerId = reader.GetGuid(4),
                DocumentTypeId = reader.GetGuid(5),
                DocumentTypeCode = reader.GetString(6),
                DocumentTypeNameEn = reader.GetString(7),
                DocumentTypeNameAr = reader.GetString(8),
                Title = reader.GetString(9),
                Status = ((DocumentStatus)reader.GetInt32(10)).ToString(),
                ExpiryDate = reader.IsDBNull(11) ? null : DateOnly.FromDateTime(reader.GetDateTime(11)).ToString("yyyy-MM-dd"),
                CreatedAt = reader.GetDateTime(12).ToString("o"),
                LatestVersionNumber = reader.IsDBNull(13) ? 1 : reader.GetInt32(13),
                LatestFileName = reader.IsDBNull(14) ? "" : reader.GetString(14),
                LatestFileSize = reader.IsDBNull(15) ? 0 : reader.GetInt64(15),
                LatestContentType = reader.IsDBNull(16) ? "" : reader.GetString(16)
            });
        }
        return list;
    }

    public async Task<DocumentDetailDto?> GetDocumentDetailsAsync(
        Guid documentId,
        TenantId tenantId,
        LegalEntityId? legalEntityId = null,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var docSql = @"
            SELECT 
                d.id, d.tenant_id, d.legal_entity_id, d.owner_type, d.owner_id, d.document_type_id,
                dt.code, dt.name_en, dt.name_ar, d.title, d.status, d.expiry_date, d.created_at
            FROM documents.documents d
            INNER JOIN documents.document_types dt ON d.document_type_id = dt.id
            WHERE d.id = @id AND d.tenant_id = @tenantId
        ";

        if (legalEntityId.HasValue)
        {
            docSql += " AND d.legal_entity_id = @legalEntityId";
        }

        await using var docCmd = new NpgsqlCommand(docSql, conn);
        docCmd.Parameters.AddWithValue("id", documentId);
        docCmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        if (legalEntityId.HasValue) docCmd.Parameters.AddWithValue("legalEntityId", legalEntityId.Value.Value);

        DocumentDetailDto? detail = null;
        await using (var reader = await docCmd.ExecuteReaderAsync(ct))
        {
            if (await reader.ReadAsync(ct))
            {
                detail = new DocumentDetailDto
                {
                    Id = reader.GetGuid(0),
                    TenantId = reader.GetGuid(1).ToString(),
                    LegalEntityId = reader.GetGuid(2).ToString(),
                    OwnerType = reader.GetString(3),
                    OwnerId = reader.GetGuid(4),
                    DocumentTypeId = reader.GetGuid(5),
                    DocumentTypeCode = reader.GetString(6),
                    DocumentTypeNameEn = reader.GetString(7),
                    DocumentTypeNameAr = reader.GetString(8),
                    Title = reader.GetString(9),
                    Status = ((DocumentStatus)reader.GetInt32(10)).ToString(),
                    ExpiryDate = reader.IsDBNull(11) ? null : DateOnly.FromDateTime(reader.GetDateTime(11)).ToString("yyyy-MM-dd"),
                    CreatedAt = reader.GetDateTime(12).ToString("o")
                };
            }
        }

        if (detail == null) return null;

        // Fetch all versions history
        const string verSql = @"
            SELECT id, version_number, file_name, file_size, content_type, sha256_checksum, uploaded_at, uploaded_by
            FROM documents.document_versions
            WHERE document_id = @docId
            ORDER BY version_number ASC;
        ";

        await using var verCmd = new NpgsqlCommand(verSql, conn);
        verCmd.Parameters.AddWithValue("docId", documentId);

        await using (var verReader = await verCmd.ExecuteReaderAsync(ct))
        {
            while (await verReader.ReadAsync(ct))
            {
                var ver = new DocumentVersionDto
                {
                    Id = verReader.GetGuid(0),
                    DocumentId = documentId,
                    VersionNumber = verReader.GetInt32(1),
                    FileName = verReader.GetString(2),
                    FileSize = verReader.GetInt64(3),
                    ContentType = verReader.GetString(4),
                    Sha256Checksum = verReader.GetString(5),
                    UploadedAt = verReader.GetDateTime(6).ToString("o"),
                    UploadedBy = verReader.GetGuid(7)
                };
                detail.Versions.Add(ver);
                detail.LatestVersionNumber = ver.VersionNumber;
                detail.LatestFileName = ver.FileName;
                detail.LatestFileSize = ver.FileSize;
                detail.LatestContentType = ver.ContentType;
            }
        }

        return detail;
    }

    public async Task<string?> GetStorageKeyForDownloadAsync(
        Guid documentId,
        TenantId tenantId,
        LegalEntityId? legalEntityId = null,
        int? versionNumber = null,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var sql = @"
            SELECT v.storage_key
            FROM documents.documents d
            INNER JOIN documents.document_versions v ON d.id = v.document_id
            WHERE d.id = @id AND d.tenant_id = @tenantId
        ";

        if (legalEntityId.HasValue)
        {
            sql += " AND d.legal_entity_id = @legalEntityId";
        }

        if (versionNumber.HasValue)
        {
            sql += " AND v.version_number = @versionNumber";
        }
        else
        {
            sql += " ORDER BY v.version_number DESC LIMIT 1";
        }

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", documentId);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        if (legalEntityId.HasValue) cmd.Parameters.AddWithValue("legalEntityId", legalEntityId.Value.Value);
        if (versionNumber.HasValue) cmd.Parameters.AddWithValue("versionNumber", versionNumber.Value);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result?.ToString();
    }

    public async Task<IReadOnlyList<DocumentSummaryDto>> ListExpiringDocumentsAsync(
        TenantId tenantId,
        LegalEntityId legalEntityId,
        DateOnly fromDate,
        DateOnly untilDate,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT
                d.id, d.tenant_id, d.legal_entity_id, d.owner_type, d.owner_id, d.document_type_id,
                dt.code, dt.name_en, dt.name_ar, d.title, d.status, d.expiry_date, d.created_at,
                v.version_number, v.file_name, v.file_size, v.content_type
            FROM documents.documents d
            INNER JOIN documents.document_types dt ON d.document_type_id = dt.id
            LEFT JOIN LATERAL (
                SELECT version_number, file_name, file_size, content_type
                FROM documents.document_versions
                WHERE document_id = d.id
                ORDER BY version_number DESC
                LIMIT 1
            ) v ON TRUE
            WHERE d.tenant_id = @tenantId
              AND d.legal_entity_id = @legalEntityId
              AND d.status = @active
              AND d.expiry_date >= @fromDate
              AND d.expiry_date <= @untilDate
            ORDER BY d.expiry_date ASC, d.created_at ASC
            LIMIT @limit OFFSET @offset;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        cmd.Parameters.AddWithValue("legalEntityId", legalEntityId.Value);
        cmd.Parameters.AddWithValue("active", (int)DocumentStatus.Active);
        cmd.Parameters.AddWithValue("fromDate", fromDate.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue("untilDate", untilDate.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue("limit", Math.Clamp(pageSize, 1, 100));
        cmd.Parameters.AddWithValue("offset", (Math.Max(1, page) - 1) * Math.Clamp(pageSize, 1, 100));

        var list = new List<DocumentSummaryDto>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new DocumentSummaryDto
            {
                Id = reader.GetGuid(0),
                TenantId = reader.GetGuid(1).ToString(),
                LegalEntityId = reader.GetGuid(2).ToString(),
                OwnerType = reader.GetString(3),
                OwnerId = reader.GetGuid(4),
                DocumentTypeId = reader.GetGuid(5),
                DocumentTypeCode = reader.GetString(6),
                DocumentTypeNameEn = reader.GetString(7),
                DocumentTypeNameAr = reader.GetString(8),
                Title = reader.GetString(9),
                Status = ((DocumentStatus)reader.GetInt32(10)).ToString(),
                ExpiryDate = reader.IsDBNull(11) ? null : DateOnly.FromDateTime(reader.GetDateTime(11)).ToString("yyyy-MM-dd"),
                CreatedAt = reader.GetDateTime(12).ToString("o"),
                LatestVersionNumber = reader.IsDBNull(13) ? 0 : reader.GetInt32(13),
                LatestFileName = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
                LatestFileSize = reader.IsDBNull(15) ? 0 : reader.GetInt64(15),
                LatestContentType = reader.IsDBNull(16) ? string.Empty : reader.GetString(16)
            });
        }

        return list;
    }

    public async Task<bool> ArchiveDocumentAsync(
        Guid documentId,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        const string updateSql = @"
            UPDATE documents.documents
            SET status = @archived
            WHERE id = @id AND tenant_id = @tenantId AND legal_entity_id = @legalEntityId AND status = @active;
        ";
        await using var update = new NpgsqlCommand(updateSql, conn, tx);
        update.Parameters.AddWithValue("archived", (int)DocumentStatus.Archived);
        update.Parameters.AddWithValue("active", (int)DocumentStatus.Active);
        update.Parameters.AddWithValue("id", documentId);
        update.Parameters.AddWithValue("tenantId", tenantId.Value);
        update.Parameters.AddWithValue("legalEntityId", legalEntityId.Value);
        var changed = await update.ExecuteNonQueryAsync(ct);
        if (changed == 0)
        {
            await tx.RollbackAsync(ct);
            return false;
        }

        const string logSql = @"
            INSERT INTO documents.document_access_logs
                (id, tenant_id, legal_entity_id, document_id, actor_user_id, action)
            VALUES (@id, @tenantId, @legalEntityId, @documentId, @actorUserId, @action);
        ";
        await using var log = new NpgsqlCommand(logSql, conn, tx);
        log.Parameters.AddWithValue("id", Guid.NewGuid());
        log.Parameters.AddWithValue("tenantId", tenantId.Value);
        log.Parameters.AddWithValue("legalEntityId", legalEntityId.Value);
        log.Parameters.AddWithValue("documentId", documentId);
        log.Parameters.AddWithValue("actorUserId", actorUserId);
        log.Parameters.AddWithValue("action", "archive");
        await log.ExecuteNonQueryAsync(ct);

        await tx.CommitAsync(ct);
        return true;
    }

    public async Task RecordAccessAsync(
        Guid documentId,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid actorUserId,
        string action,
        int? versionNumber = null,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            INSERT INTO documents.document_access_logs
                (id, tenant_id, legal_entity_id, document_id, version_number, actor_user_id, action)
            VALUES (@id, @tenantId, @legalEntityId, @documentId, @versionNumber, @actorUserId, @action);
        ";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        cmd.Parameters.AddWithValue("legalEntityId", legalEntityId.Value);
        cmd.Parameters.AddWithValue("documentId", documentId);
        cmd.Parameters.AddWithValue("versionNumber", (object?)versionNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("actorUserId", actorUserId);
        cmd.Parameters.AddWithValue("action", action);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task CreateDocumentWithInitialVersionAsync(Document doc, DocumentVersion initialVersion, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            const string docSql = @"
                INSERT INTO documents.documents (
                    id, tenant_id, legal_entity_id, owner_type, owner_id, document_type_id, title, status, expiry_date, created_at, created_by
                ) VALUES (
                    @id, @tenantId, @legalEntityId, @ownerType, @ownerId, @docTypeId, @title, @status, @expiryDate, @createdAt, @createdBy
                );
            ";
            await using var docCmd = new NpgsqlCommand(docSql, conn, tx);
            docCmd.Parameters.AddWithValue("id", doc.Id);
            docCmd.Parameters.AddWithValue("tenantId", doc.TenantId.Value);
            docCmd.Parameters.AddWithValue("legalEntityId", doc.LegalEntityId.Value);
            docCmd.Parameters.AddWithValue("ownerType", doc.OwnerType);
            docCmd.Parameters.AddWithValue("ownerId", doc.OwnerId);
            docCmd.Parameters.AddWithValue("docTypeId", doc.DocumentTypeId);
            docCmd.Parameters.AddWithValue("title", doc.Title);
            docCmd.Parameters.AddWithValue("status", (int)doc.Status);
            docCmd.Parameters.AddWithValue("expiryDate", doc.ExpiryDate.HasValue ? doc.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
            docCmd.Parameters.AddWithValue("createdAt", doc.CreatedAt);
            docCmd.Parameters.AddWithValue("createdBy", doc.CreatedBy);
            await docCmd.ExecuteNonQueryAsync(ct);

            const string verSql = @"
                INSERT INTO documents.document_versions (
                    id, document_id, version_number, storage_key, file_name, file_size, content_type, sha256_checksum, uploaded_at, uploaded_by
                ) VALUES (
                    @id, @docId, @verNo, @storageKey, @fileName, @fileSize, @contentType, @sha, @uploadedAt, @uploadedBy
                );
            ";
            await using var verCmd = new NpgsqlCommand(verSql, conn, tx);
            verCmd.Parameters.AddWithValue("id", initialVersion.Id);
            verCmd.Parameters.AddWithValue("docId", initialVersion.DocumentId);
            verCmd.Parameters.AddWithValue("verNo", initialVersion.VersionNumber);
            verCmd.Parameters.AddWithValue("storageKey", initialVersion.StorageKey);
            verCmd.Parameters.AddWithValue("fileName", initialVersion.FileName);
            verCmd.Parameters.AddWithValue("fileSize", initialVersion.FileSize);
            verCmd.Parameters.AddWithValue("contentType", initialVersion.ContentType);
            verCmd.Parameters.AddWithValue("sha", initialVersion.Sha256Checksum);
            verCmd.Parameters.AddWithValue("uploadedAt", initialVersion.UploadedAt);
            verCmd.Parameters.AddWithValue("uploadedBy", initialVersion.UploadedBy);
            await verCmd.ExecuteNonQueryAsync(ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<int> AddDocumentVersionAsync(Guid documentId, TenantId tenantId, DocumentVersion newVersion, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            // Verify document belongs to tenant and get current max version
            const string verifySql = @"
                SELECT MAX(version_number)
                FROM documents.document_versions v
                INNER JOIN documents.documents d ON v.document_id = d.id
                WHERE d.id = @docId AND d.tenant_id = @tenantId;
            ";
            await using var verifyCmd = new NpgsqlCommand(verifySql, conn, tx);
            verifyCmd.Parameters.AddWithValue("docId", documentId);
            verifyCmd.Parameters.AddWithValue("tenantId", tenantId.Value);

            var maxVerObj = await verifyCmd.ExecuteScalarAsync(ct);
            if (maxVerObj == null || maxVerObj == DBNull.Value)
            {
                await tx.RollbackAsync(ct);
                throw new InvalidOperationException($"Document '{documentId}' not found for tenant '{tenantId}'.");
            }

            var nextVersionNumber = Convert.ToInt32(maxVerObj) + 1;

            const string insertVerSql = @"
                INSERT INTO documents.document_versions (
                    id, document_id, version_number, storage_key, file_name, file_size, content_type, sha256_checksum, uploaded_at, uploaded_by
                ) VALUES (
                    @id, @docId, @verNo, @storageKey, @fileName, @fileSize, @contentType, @sha, @uploadedAt, @uploadedBy
                );
            ";
            await using var verCmd = new NpgsqlCommand(insertVerSql, conn, tx);
            verCmd.Parameters.AddWithValue("id", newVersion.Id);
            verCmd.Parameters.AddWithValue("docId", documentId);
            verCmd.Parameters.AddWithValue("verNo", nextVersionNumber);
            verCmd.Parameters.AddWithValue("storageKey", newVersion.StorageKey);
            verCmd.Parameters.AddWithValue("fileName", newVersion.FileName);
            verCmd.Parameters.AddWithValue("fileSize", newVersion.FileSize);
            verCmd.Parameters.AddWithValue("contentType", newVersion.ContentType);
            verCmd.Parameters.AddWithValue("sha", newVersion.Sha256Checksum);
            verCmd.Parameters.AddWithValue("uploadedAt", newVersion.UploadedAt);
            verCmd.Parameters.AddWithValue("uploadedBy", newVersion.UploadedBy);
            await verCmd.ExecuteNonQueryAsync(ct);

            await tx.CommitAsync(ct);
            return nextVersionNumber;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
