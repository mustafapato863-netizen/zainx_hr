using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Workforce.Modules.Recruitment.Domain;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Recruitment.Infrastructure;

public class RecruitmentRepository : IRecruitmentRepository
{
    private readonly string _connectionString;

    public RecruitmentRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    private async Task<NpgsqlConnection> CreateOpenConnectionAsync(CancellationToken ct)
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }

    // ========================================================================
    // 1. REQUISITIONS
    // ========================================================================

    public async Task CreateRequisitionAsync(JobRequisition req, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        INSERT INTO recruitment.job_requisitions (
            id, tenant_id, legal_entity_id, organization_unit_id, position_id, location_id,
            hiring_manager_id, recruiter_id, requisition_number, title_en, title_ar,
            openings_count, employment_type, pipeline_id, pipeline_version, status,
            approval_request_id, requisition_reason, target_start_date, opened_at_utc,
            closed_at_utc, created_at_utc, row_version
        ) VALUES (
            @id, @tenant_id, @legal_entity_id, @organization_unit_id, @position_id, @location_id,
            @hiring_manager_id, @recruiter_id, @requisition_number, @title_en, @title_ar,
            @openings_count, @employment_type, @pipeline_id, @pipeline_version, @status,
            @approval_request_id, @requisition_reason, @target_start_date, @opened_at_utc,
            @closed_at_utc, @created_at_utc, @row_version
        );
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", req.Id);
        cmd.Parameters.AddWithValue("tenant_id", req.TenantId.Value);
        cmd.Parameters.AddWithValue("legal_entity_id", req.LegalEntityId.Value);
        cmd.Parameters.AddWithValue("organization_unit_id", req.OrganizationUnitId);
        cmd.Parameters.AddWithValue("position_id", (object?)req.PositionId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("location_id", (object?)req.LocationId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("hiring_manager_id", req.HiringManagerId);
        cmd.Parameters.AddWithValue("recruiter_id", req.RecruiterId);
        cmd.Parameters.AddWithValue("requisition_number", req.RequisitionNumber);
        cmd.Parameters.AddWithValue("title_en", req.TitleEn);
        cmd.Parameters.AddWithValue("title_ar", req.TitleAr);
        cmd.Parameters.AddWithValue("openings_count", req.OpeningsCount);
        cmd.Parameters.AddWithValue("employment_type", req.EmploymentType);
        cmd.Parameters.AddWithValue("pipeline_id", req.PipelineId);
        cmd.Parameters.AddWithValue("pipeline_version", req.PipelineVersion);
        cmd.Parameters.AddWithValue("status", (int)req.Status);
        cmd.Parameters.AddWithValue("approval_request_id", (object?)req.ApprovalRequestId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("requisition_reason", (object?)req.RequisitionReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("target_start_date", (object?)req.TargetStartDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("opened_at_utc", (object?)req.OpenedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("closed_at_utc", (object?)req.ClosedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("created_at_utc", req.CreatedAtUtc);
        cmd.Parameters.AddWithValue("row_version", (long)req.RowVersion);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateRequisitionAsync(JobRequisition req, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        UPDATE recruitment.job_requisitions SET
            organization_unit_id = @organization_unit_id,
            position_id = @position_id,
            location_id = @location_id,
            hiring_manager_id = @hiring_manager_id,
            recruiter_id = @recruiter_id,
            title_en = @title_en,
            title_ar = @title_ar,
            openings_count = @openings_count,
            employment_type = @employment_type,
            status = @status,
            approval_request_id = @approval_request_id,
            requisition_reason = @requisition_reason,
            target_start_date = @target_start_date,
            opened_at_utc = @opened_at_utc,
            closed_at_utc = @closed_at_utc,
            row_version = @row_version
        WHERE id = @id AND tenant_id = @tenant_id;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", req.Id);
        cmd.Parameters.AddWithValue("tenant_id", req.TenantId.Value);
        cmd.Parameters.AddWithValue("organization_unit_id", req.OrganizationUnitId);
        cmd.Parameters.AddWithValue("position_id", (object?)req.PositionId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("location_id", (object?)req.LocationId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("hiring_manager_id", req.HiringManagerId);
        cmd.Parameters.AddWithValue("recruiter_id", req.RecruiterId);
        cmd.Parameters.AddWithValue("title_en", req.TitleEn);
        cmd.Parameters.AddWithValue("title_ar", req.TitleAr);
        cmd.Parameters.AddWithValue("openings_count", req.OpeningsCount);
        cmd.Parameters.AddWithValue("employment_type", req.EmploymentType);
        cmd.Parameters.AddWithValue("status", (int)req.Status);
        cmd.Parameters.AddWithValue("approval_request_id", (object?)req.ApprovalRequestId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("requisition_reason", (object?)req.RequisitionReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("target_start_date", (object?)req.TargetStartDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("opened_at_utc", (object?)req.OpenedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("closed_at_utc", (object?)req.ClosedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("row_version", (long)req.RowVersion);

        var rows = await cmd.ExecuteNonQueryAsync(ct);
        if (rows == 0)
        {
            throw new InvalidOperationException($"Failed to update Requisition '{req.Id}'. Not found or tenant mismatch.");
        }
    }

    public async Task<JobRequisition?> GetRequisitionByIdAsync(TenantId tenantId, Guid requisitionId, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, tenant_id, legal_entity_id, organization_unit_id, position_id, location_id,
               hiring_manager_id, recruiter_id, requisition_number, title_en, title_ar,
               openings_count, employment_type, pipeline_id, pipeline_version, status,
               approval_request_id, requisition_reason, target_start_date, opened_at_utc,
               closed_at_utc, created_at_utc, row_version
        FROM recruitment.job_requisitions
        WHERE id = @id AND tenant_id = @tenant_id;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", requisitionId);
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return MapJobRequisition(reader);
    }

    public async Task<PagedRecruitmentResult<JobRequisition>> QueryRequisitionsAsync(
        TenantId tenantId,
        LegalEntityId? legalEntityId,
        RequisitionStatus? status,
        string? search,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);

        var whereClause = "WHERE tenant_id = @tenant_id";
        if (legalEntityId != null) whereClause += " AND legal_entity_id = @legal_entity_id";
        if (status.HasValue) whereClause += " AND status = @status";
        if (!string.IsNullOrWhiteSpace(search))
            whereClause += " AND (title_en ILIKE @search OR title_ar ILIKE @search OR requisition_number ILIKE @search)";

        var countSql = $"SELECT COUNT(*) FROM recruitment.job_requisitions {whereClause};";
        await using var countCmd = new NpgsqlCommand(countSql, conn);
        countCmd.Parameters.AddWithValue("tenant_id", tenantId.Value);
        if (legalEntityId != null) countCmd.Parameters.AddWithValue("legal_entity_id", legalEntityId.Value);
        if (status.HasValue) countCmd.Parameters.AddWithValue("status", (int)status.Value);
        if (!string.IsNullOrWhiteSpace(search)) countCmd.Parameters.AddWithValue("search", $"%{search.Trim()}%");

        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));

        var querySql = $"""
        SELECT id, tenant_id, legal_entity_id, organization_unit_id, position_id, location_id,
               hiring_manager_id, recruiter_id, requisition_number, title_en, title_ar,
               openings_count, employment_type, pipeline_id, pipeline_version, status,
               approval_request_id, requisition_reason, target_start_date, opened_at_utc,
               closed_at_utc, created_at_utc, row_version
        FROM recruitment.job_requisitions
        {whereClause}
        ORDER BY created_at_utc DESC
        LIMIT @limit OFFSET @offset;
        """;

        await using var queryCmd = new NpgsqlCommand(querySql, conn);
        queryCmd.Parameters.AddWithValue("tenant_id", tenantId.Value);
        if (legalEntityId != null) queryCmd.Parameters.AddWithValue("legal_entity_id", legalEntityId.Value);
        if (status.HasValue) queryCmd.Parameters.AddWithValue("status", (int)status.Value);
        if (!string.IsNullOrWhiteSpace(search)) queryCmd.Parameters.AddWithValue("search", $"%{search.Trim()}%");
        queryCmd.Parameters.AddWithValue("limit", pageSize);
        queryCmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);

        var list = new List<JobRequisition>();
        await using var reader = await queryCmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(MapJobRequisition(reader));
        }

        return new PagedRecruitmentResult<JobRequisition>(list, total, page, pageSize);
    }

    private static JobRequisition MapJobRequisition(NpgsqlDataReader reader)
    {
        return JobRequisition.Reconstitute(
            reader.GetGuid(0),
            new TenantId(reader.GetGuid(1)),
            new LegalEntityId(reader.GetGuid(2)),
            reader.GetGuid(3),
            reader.IsDBNull(4) ? null : reader.GetGuid(4),
            reader.IsDBNull(5) ? null : reader.GetGuid(5),
            reader.GetGuid(6),
            reader.GetGuid(7),
            reader.GetString(8),
            reader.GetString(9),
            reader.GetString(10),
            reader.GetInt32(11),
            reader.GetString(12),
            reader.GetGuid(13),
            reader.GetInt32(14),
            (RequisitionStatus)reader.GetInt32(15),
            reader.IsDBNull(16) ? null : reader.GetGuid(16),
            reader.IsDBNull(17) ? null : reader.GetString(17),
            reader.IsDBNull(18) ? null : DateOnly.FromDateTime(reader.GetDateTime(18)),
            reader.IsDBNull(19) ? null : reader.GetDateTime(19),
            reader.IsDBNull(20) ? null : reader.GetDateTime(20),
            reader.GetDateTime(21),
            (uint)reader.GetInt64(22)
        );
    }

    // ========================================================================
    // 2. PIPELINES
    // ========================================================================

    public async Task CreatePipelineAsync(RecruitmentPipeline pipeline, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        INSERT INTO recruitment.pipelines (id, tenant_id, code, name_en, name_ar, is_active, created_at_utc, row_version)
        VALUES (@id, @tenant_id, @code, @name_en, @name_ar, @is_active, @created_at_utc, @row_version);
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", pipeline.Id);
        cmd.Parameters.AddWithValue("tenant_id", pipeline.TenantId.Value);
        cmd.Parameters.AddWithValue("code", pipeline.Code);
        cmd.Parameters.AddWithValue("name_en", pipeline.NameEn);
        cmd.Parameters.AddWithValue("name_ar", pipeline.NameAr);
        cmd.Parameters.AddWithValue("is_active", pipeline.IsActive);
        cmd.Parameters.AddWithValue("created_at_utc", pipeline.CreatedAtUtc);
        cmd.Parameters.AddWithValue("row_version", (long)pipeline.RowVersion);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task CreatePipelineVersionAsync(RecruitmentPipelineVersion version, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        const string versionSql = """
        INSERT INTO recruitment.pipeline_versions (id, pipeline_id, version_number, is_immutable, created_at_utc)
        VALUES (@id, @pipeline_id, @version_number, @is_immutable, @created_at_utc);
        """;
        await using var vCmd = new NpgsqlCommand(versionSql, conn, tx);
        vCmd.Parameters.AddWithValue("id", version.Id);
        vCmd.Parameters.AddWithValue("pipeline_id", version.PipelineId);
        vCmd.Parameters.AddWithValue("version_number", version.VersionNumber);
        vCmd.Parameters.AddWithValue("is_immutable", version.IsImmutable);
        vCmd.Parameters.AddWithValue("created_at_utc", version.CreatedAtUtc);
        await vCmd.ExecuteNonQueryAsync(ct);

        foreach (var stage in version.Stages)
        {
            const string stageSql = """
            INSERT INTO recruitment.pipeline_stages (id, pipeline_version_id, stage_order, code, name_en, name_ar, stage_kind)
            VALUES (@id, @pipeline_version_id, @stage_order, @code, @name_en, @name_ar, @stage_kind);
            """;
            await using var sCmd = new NpgsqlCommand(stageSql, conn, tx);
            sCmd.Parameters.AddWithValue("id", stage.Id);
            sCmd.Parameters.AddWithValue("pipeline_version_id", stage.PipelineVersionId);
            sCmd.Parameters.AddWithValue("stage_order", stage.StageOrder);
            sCmd.Parameters.AddWithValue("code", stage.Code);
            sCmd.Parameters.AddWithValue("name_en", stage.NameEn);
            sCmd.Parameters.AddWithValue("name_ar", stage.NameAr);
            sCmd.Parameters.AddWithValue("stage_kind", (int)stage.StageKind);
            await sCmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task<RecruitmentPipeline?> GetPipelineWithVersionsAsync(TenantId tenantId, Guid pipelineId, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string pSql = "SELECT id, tenant_id, code, name_en, name_ar, is_active, created_at_utc, row_version FROM recruitment.pipelines WHERE id = @id AND tenant_id = @tenant_id;";
        await using var cmd = new NpgsqlCommand(pSql, conn);
        cmd.Parameters.AddWithValue("id", pipelineId);
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        var pipeline = RecruitmentPipeline.Reconstitute(
            reader.GetGuid(0),
            new TenantId(reader.GetGuid(1)),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetBoolean(5),
            reader.GetDateTime(6),
            (uint)reader.GetInt64(7)
        );
        await reader.CloseAsync();

        // Load versions
        const string vSql = "SELECT id, pipeline_id, version_number, is_immutable, created_at_utc FROM recruitment.pipeline_versions WHERE pipeline_id = @pipeline_id ORDER BY version_number ASC;";
        await using var vCmd = new NpgsqlCommand(vSql, conn);
        vCmd.Parameters.AddWithValue("pipeline_id", pipelineId);

        await using var vReader = await vCmd.ExecuteReaderAsync(ct);
        while (await vReader.ReadAsync(ct))
        {
            var v = RecruitmentPipelineVersion.Reconstitute(
                vReader.GetGuid(0),
                vReader.GetGuid(1),
                vReader.GetInt32(2),
                vReader.GetBoolean(3),
                vReader.GetDateTime(4)
            );
            pipeline.AddVersion(v);
        }

        return pipeline;
    }

    public async Task<RecruitmentPipelineVersion?> GetPipelineVersionWithStagesAsync(Guid pipelineVersionId, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string vSql = "SELECT id, pipeline_id, version_number, is_immutable, created_at_utc FROM recruitment.pipeline_versions WHERE id = @id;";
        await using var cmd = new NpgsqlCommand(vSql, conn);
        cmd.Parameters.AddWithValue("id", pipelineVersionId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        var version = RecruitmentPipelineVersion.Reconstitute(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetInt32(2),
            reader.GetBoolean(3),
            reader.GetDateTime(4)
        );
        await reader.CloseAsync();

        // Load stages
        const string sSql = "SELECT id, pipeline_version_id, stage_order, code, name_en, name_ar, stage_kind FROM recruitment.pipeline_stages WHERE pipeline_version_id = @vid ORDER BY stage_order ASC;";
        await using var sCmd = new NpgsqlCommand(sSql, conn);
        sCmd.Parameters.AddWithValue("vid", pipelineVersionId);

        await using var sReader = await sCmd.ExecuteReaderAsync(ct);
        while (await sReader.ReadAsync(ct))
        {
            var stage = RecruitmentStage.Reconstitute(
                sReader.GetGuid(0),
                sReader.GetGuid(1),
                sReader.GetInt32(2),
                sReader.GetString(3),
                sReader.GetString(4),
                sReader.GetString(5),
                (StageKind)sReader.GetInt32(6)
            );
            version.AddStage(stage);
        }

        return version;
    }

    public async Task<RecruitmentPipelineVersion?> GetDefaultPipelineVersionAsync(TenantId tenantId, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT pv.id
        FROM recruitment.pipelines p
        JOIN recruitment.pipeline_versions pv ON p.id = pv.pipeline_id
        WHERE (p.tenant_id = @tenant_id OR p.code = 'STANDARD') AND p.is_active = TRUE
        ORDER BY p.code = 'STANDARD' ASC, pv.version_number DESC
        LIMIT 1;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);

        var versionIdObj = await cmd.ExecuteScalarAsync(ct);
        if (versionIdObj == null || versionIdObj is DBNull) return null;

        return await GetPipelineVersionWithStagesAsync((Guid)versionIdObj, ct);
    }

    // ========================================================================
    // 3. CANDIDATES & DUPLICATE DETECTION
    // ========================================================================

    public async Task CreateCandidateAsync(Candidate c, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        INSERT INTO recruitment.candidates (
            id, tenant_id, first_name_en, last_name_en, first_name_ar, last_name_ar,
            email, phone_number, location, headline, source, resume_document_id,
            skills_json, normalized_email_hash, normalized_phone_hash, created_at_utc, row_version
        ) VALUES (
            @id, @tenant_id, @first_name_en, @last_name_en, @first_name_ar, @last_name_ar,
            @email, @phone_number, @location, @headline, @source, @resume_document_id,
            @skills_json::jsonb, @normalized_email_hash, @normalized_phone_hash, @created_at_utc, @row_version
        );
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", c.Id);
        cmd.Parameters.AddWithValue("tenant_id", c.TenantId.Value);
        cmd.Parameters.AddWithValue("first_name_en", c.FirstNameEn);
        cmd.Parameters.AddWithValue("last_name_en", c.LastNameEn);
        cmd.Parameters.AddWithValue("first_name_ar", c.FirstNameAr);
        cmd.Parameters.AddWithValue("last_name_ar", c.LastNameAr);
        cmd.Parameters.AddWithValue("email", c.Email);
        cmd.Parameters.AddWithValue("phone_number", c.PhoneNumber);
        cmd.Parameters.AddWithValue("location", (object?)c.Location ?? DBNull.Value);
        cmd.Parameters.AddWithValue("headline", (object?)c.Headline ?? DBNull.Value);
        cmd.Parameters.AddWithValue("source", (object?)c.Source ?? DBNull.Value);
        cmd.Parameters.AddWithValue("resume_document_id", (object?)c.ResumeDocumentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("skills_json", c.SkillsJson ?? "[]");
        cmd.Parameters.AddWithValue("normalized_email_hash", c.NormalizedEmailHash);
        cmd.Parameters.AddWithValue("normalized_phone_hash", c.NormalizedPhoneHash);
        cmd.Parameters.AddWithValue("created_at_utc", c.CreatedAtUtc);
        cmd.Parameters.AddWithValue("row_version", (long)c.RowVersion);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateCandidateAsync(Candidate c, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        UPDATE recruitment.candidates SET
            first_name_en = @first_name_en,
            last_name_en = @last_name_en,
            first_name_ar = @first_name_ar,
            last_name_ar = @last_name_ar,
            email = @email,
            phone_number = @phone_number,
            location = @location,
            headline = @headline,
            source = @source,
            resume_document_id = @resume_document_id,
            skills_json = @skills_json::jsonb,
            normalized_email_hash = @normalized_email_hash,
            normalized_phone_hash = @normalized_phone_hash,
            row_version = @row_version
        WHERE id = @id AND tenant_id = @tenant_id;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", c.Id);
        cmd.Parameters.AddWithValue("tenant_id", c.TenantId.Value);
        cmd.Parameters.AddWithValue("first_name_en", c.FirstNameEn);
        cmd.Parameters.AddWithValue("last_name_en", c.LastNameEn);
        cmd.Parameters.AddWithValue("first_name_ar", c.FirstNameAr);
        cmd.Parameters.AddWithValue("last_name_ar", c.LastNameAr);
        cmd.Parameters.AddWithValue("email", c.Email);
        cmd.Parameters.AddWithValue("phone_number", c.PhoneNumber);
        cmd.Parameters.AddWithValue("location", (object?)c.Location ?? DBNull.Value);
        cmd.Parameters.AddWithValue("headline", (object?)c.Headline ?? DBNull.Value);
        cmd.Parameters.AddWithValue("source", (object?)c.Source ?? DBNull.Value);
        cmd.Parameters.AddWithValue("resume_document_id", (object?)c.ResumeDocumentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("skills_json", c.SkillsJson ?? "[]");
        cmd.Parameters.AddWithValue("normalized_email_hash", c.NormalizedEmailHash);
        cmd.Parameters.AddWithValue("normalized_phone_hash", c.NormalizedPhoneHash);
        cmd.Parameters.AddWithValue("row_version", (long)c.RowVersion);

        var rows = await cmd.ExecuteNonQueryAsync(ct);
        if (rows == 0)
        {
            throw new InvalidOperationException($"Failed to update Candidate '{c.Id}'. Not found or tenant mismatch.");
        }
    }

    public async Task<Candidate?> GetCandidateByIdAsync(TenantId tenantId, Guid candidateId, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, tenant_id, first_name_en, last_name_en, first_name_ar, last_name_ar,
               email, phone_number, location, headline, source, resume_document_id,
               skills_json, normalized_email_hash, normalized_phone_hash, created_at_utc, row_version
        FROM recruitment.candidates
        WHERE id = @id AND tenant_id = @tenant_id;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", candidateId);
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return MapCandidate(reader);
    }

    public async Task<PagedRecruitmentResult<Candidate>> QueryCandidatesAsync(
        TenantId tenantId,
        string? search,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);

        var whereClause = "WHERE tenant_id = @tenant_id";
        if (!string.IsNullOrWhiteSpace(search))
        {
            whereClause += " AND (first_name_en ILIKE @search OR last_name_en ILIKE @search OR first_name_ar ILIKE @search OR last_name_ar ILIKE @search OR email ILIKE @search OR phone_number ILIKE @search)";
        }

        var countSql = $"SELECT COUNT(*) FROM recruitment.candidates {whereClause};";
        await using var countCmd = new NpgsqlCommand(countSql, conn);
        countCmd.Parameters.AddWithValue("tenant_id", tenantId.Value);
        if (!string.IsNullOrWhiteSpace(search)) countCmd.Parameters.AddWithValue("search", $"%{search.Trim()}%");

        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));

        var querySql = $"""
        SELECT id, tenant_id, first_name_en, last_name_en, first_name_ar, last_name_ar,
               email, phone_number, location, headline, source, resume_document_id,
               skills_json, normalized_email_hash, normalized_phone_hash, created_at_utc, row_version
        FROM recruitment.candidates
        {whereClause}
        ORDER BY created_at_utc DESC
        LIMIT @limit OFFSET @offset;
        """;

        await using var queryCmd = new NpgsqlCommand(querySql, conn);
        queryCmd.Parameters.AddWithValue("tenant_id", tenantId.Value);
        if (!string.IsNullOrWhiteSpace(search)) queryCmd.Parameters.AddWithValue("search", $"%{search.Trim()}%");
        queryCmd.Parameters.AddWithValue("limit", pageSize);
        queryCmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);

        var list = new List<Candidate>();
        await using var reader = await queryCmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(MapCandidate(reader));
        }

        return new PagedRecruitmentResult<Candidate>(list, total, page, pageSize);
    }

    public async Task<IReadOnlyList<DuplicateCandidateMatchDto>> FindPotentialDuplicatesAsync(
        TenantId tenantId,
        string email,
        string phoneNumber,
        Guid? excludeCandidateId = null,
        CancellationToken ct = default)
    {
        var emailHash = Candidate.ComputeNormalizedEmailHash(email);
        var phoneHash = Candidate.ComputeNormalizedPhoneHash(phoneNumber);

        if (string.IsNullOrEmpty(emailHash) && string.IsNullOrEmpty(phoneHash))
            return Array.Empty<DuplicateCandidateMatchDto>();

        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, first_name_en, last_name_en, email, phone_number,
               CASE 
                   WHEN normalized_email_hash = @email_hash AND normalized_phone_hash = @phone_hash THEN 'EXACT_EMAIL_AND_PHONE'
                   WHEN normalized_email_hash = @email_hash THEN 'EMAIL_MATCH'
                   WHEN normalized_phone_hash = @phone_hash THEN 'PHONE_MATCH'
                   ELSE 'UNKNOWN'
               END AS match_type
        FROM recruitment.candidates
        WHERE tenant_id = @tenant_id
          AND (@exclude_id IS NULL OR id <> @exclude_id)
          AND (
              (normalized_email_hash = @email_hash AND @email_hash <> '')
              OR
              (normalized_phone_hash = @phone_hash AND @phone_hash <> '')
          );
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);
        cmd.Parameters.AddWithValue("email_hash", emailHash);
        cmd.Parameters.AddWithValue("phone_hash", phoneHash);
        cmd.Parameters.AddWithValue("exclude_id", (object?)excludeCandidateId ?? DBNull.Value);

        var list = new List<DuplicateCandidateMatchDto>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new DuplicateCandidateMatchDto(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5)
            ));
        }

        return list;
    }

    private static Candidate MapCandidate(NpgsqlDataReader reader)
    {
        return Candidate.Reconstitute(
            reader.GetGuid(0),
            new TenantId(reader.GetGuid(1)),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetString(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetString(9),
            reader.IsDBNull(10) ? null : reader.GetString(10),
            reader.IsDBNull(11) ? null : reader.GetGuid(11),
            reader.GetString(12),
            reader.GetString(13),
            reader.GetString(14),
            reader.GetDateTime(15),
            (uint)reader.GetInt64(16)
        );
    }

    // ========================================================================
    // 4. APPLICATIONS
    // ========================================================================

    public async Task CreateApplicationAsync(Application app, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        const string sql = """
        INSERT INTO recruitment.applications (
            id, tenant_id, legal_entity_id, requisition_id, candidate_id, pipeline_version_id,
            current_stage_id, status, source, applied_at_utc, disposed_at_utc, disposition_reason,
            disposition_note, hired_person_id, hired_employment_id, hired_at_utc, row_version
        ) VALUES (
            @id, @tenant_id, @legal_entity_id, @requisition_id, @candidate_id, @pipeline_version_id,
            @current_stage_id, @status, @source, @applied_at_utc, @disposed_at_utc, @disposition_reason,
            @disposition_note, @hired_person_id, @hired_employment_id, @hired_at_utc, @row_version
        );
        """;

        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("id", app.Id);
        cmd.Parameters.AddWithValue("tenant_id", app.TenantId.Value);
        cmd.Parameters.AddWithValue("legal_entity_id", app.LegalEntityId.Value);
        cmd.Parameters.AddWithValue("requisition_id", app.RequisitionId);
        cmd.Parameters.AddWithValue("candidate_id", app.CandidateId);
        cmd.Parameters.AddWithValue("pipeline_version_id", app.PipelineVersionId);
        cmd.Parameters.AddWithValue("current_stage_id", app.CurrentStageId);
        cmd.Parameters.AddWithValue("status", (int)app.Status);
        cmd.Parameters.AddWithValue("source", (object?)app.Source ?? DBNull.Value);
        cmd.Parameters.AddWithValue("applied_at_utc", app.AppliedAtUtc);
        cmd.Parameters.AddWithValue("disposed_at_utc", (object?)app.DisposedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("disposition_reason", (object?)app.DispositionReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("disposition_note", (object?)app.DispositionNote ?? DBNull.Value);
        cmd.Parameters.AddWithValue("hired_person_id", (object?)app.HiredPersonId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("hired_employment_id", (object?)app.HiredEmploymentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("hired_at_utc", (object?)app.HiredAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("row_version", (long)app.RowVersion);

        await cmd.ExecuteNonQueryAsync(ct);

        // Insert stage history
        foreach (var history in app.StageHistory)
        {
            const string hSql = """
            INSERT INTO recruitment.application_stage_history (
                id, application_id, from_stage_id, to_stage_id, changed_by_user_id, changed_at_utc, reason, idempotency_key
            ) VALUES (
                @id, @application_id, @from_stage_id, @to_stage_id, @changed_by_user_id, @changed_at_utc, @reason, @idempotency_key
            ) ON CONFLICT (id) DO NOTHING;
            """;
            await using var hCmd = new NpgsqlCommand(hSql, conn, tx);
            hCmd.Parameters.AddWithValue("id", history.Id);
            hCmd.Parameters.AddWithValue("application_id", history.ApplicationId);
            hCmd.Parameters.AddWithValue("from_stage_id", (object?)history.FromStageId ?? DBNull.Value);
            hCmd.Parameters.AddWithValue("to_stage_id", history.ToStageId);
            hCmd.Parameters.AddWithValue("changed_by_user_id", history.ChangedByUserId);
            hCmd.Parameters.AddWithValue("changed_at_utc", history.ChangedAtUtc);
            hCmd.Parameters.AddWithValue("reason", (object?)history.Reason ?? DBNull.Value);
            hCmd.Parameters.AddWithValue("idempotency_key", (object?)history.IdempotencyKey ?? DBNull.Value);
            await hCmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task UpdateApplicationAsync(Application app, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        const string sql = """
        UPDATE recruitment.applications SET
            current_stage_id = @current_stage_id,
            status = @status,
            disposed_at_utc = @disposed_at_utc,
            disposition_reason = @disposition_reason,
            disposition_note = @disposition_note,
            hired_person_id = @hired_person_id,
            hired_employment_id = @hired_employment_id,
            hired_at_utc = @hired_at_utc,
            row_version = @row_version
        WHERE id = @id AND tenant_id = @tenant_id;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("id", app.Id);
        cmd.Parameters.AddWithValue("tenant_id", app.TenantId.Value);
        cmd.Parameters.AddWithValue("current_stage_id", app.CurrentStageId);
        cmd.Parameters.AddWithValue("status", (int)app.Status);
        cmd.Parameters.AddWithValue("disposed_at_utc", (object?)app.DisposedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("disposition_reason", (object?)app.DispositionReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("disposition_note", (object?)app.DispositionNote ?? DBNull.Value);
        cmd.Parameters.AddWithValue("hired_person_id", (object?)app.HiredPersonId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("hired_employment_id", (object?)app.HiredEmploymentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("hired_at_utc", (object?)app.HiredAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("row_version", (long)app.RowVersion);

        var rows = await cmd.ExecuteNonQueryAsync(ct);
        if (rows == 0)
        {
            throw new InvalidOperationException($"Failed to update Application '{app.Id}'. Not found or tenant mismatch.");
        }

        // Upsert any new history entries
        foreach (var history in app.StageHistory)
        {
            const string hSql = """
            INSERT INTO recruitment.application_stage_history (
                id, application_id, from_stage_id, to_stage_id, changed_by_user_id, changed_at_utc, reason, idempotency_key
            ) VALUES (
                @id, @application_id, @from_stage_id, @to_stage_id, @changed_by_user_id, @changed_at_utc, @reason, @idempotency_key
            ) ON CONFLICT (id) DO NOTHING;
            """;
            await using var hCmd = new NpgsqlCommand(hSql, conn, tx);
            hCmd.Parameters.AddWithValue("id", history.Id);
            hCmd.Parameters.AddWithValue("application_id", history.ApplicationId);
            hCmd.Parameters.AddWithValue("from_stage_id", (object?)history.FromStageId ?? DBNull.Value);
            hCmd.Parameters.AddWithValue("to_stage_id", history.ToStageId);
            hCmd.Parameters.AddWithValue("changed_by_user_id", history.ChangedByUserId);
            hCmd.Parameters.AddWithValue("changed_at_utc", history.ChangedAtUtc);
            hCmd.Parameters.AddWithValue("reason", (object?)history.Reason ?? DBNull.Value);
            hCmd.Parameters.AddWithValue("idempotency_key", (object?)history.IdempotencyKey ?? DBNull.Value);
            await hCmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task<Application?> GetApplicationByIdAsync(TenantId tenantId, Guid applicationId, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, tenant_id, legal_entity_id, requisition_id, candidate_id, pipeline_version_id,
               current_stage_id, status, source, applied_at_utc, disposed_at_utc, disposition_reason,
               disposition_note, hired_person_id, hired_employment_id, hired_at_utc, row_version
        FROM recruitment.applications
        WHERE id = @id AND tenant_id = @tenant_id;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", applicationId);
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        var app = MapApplication(reader);
        await reader.CloseAsync();

        // Load stage history
        const string hSql = "SELECT id, application_id, from_stage_id, to_stage_id, changed_by_user_id, changed_at_utc, reason, idempotency_key FROM recruitment.application_stage_history WHERE application_id = @app_id ORDER BY changed_at_utc ASC;";
        await using var hCmd = new NpgsqlCommand(hSql, conn);
        hCmd.Parameters.AddWithValue("app_id", applicationId);

        var historyList = new List<ApplicationStageHistory>();
        await using var hReader = await hCmd.ExecuteReaderAsync(ct);
        while (await hReader.ReadAsync(ct))
        {
            historyList.Add(new ApplicationStageHistory(
                hReader.GetGuid(0),
                hReader.GetGuid(1),
                hReader.IsDBNull(2) ? null : hReader.GetGuid(2),
                hReader.GetGuid(3),
                hReader.GetGuid(4),
                hReader.GetDateTime(5),
                hReader.IsDBNull(6) ? null : hReader.GetString(6),
                hReader.IsDBNull(7) ? null : hReader.GetString(7)
            ));
        }

        return Application.Reconstitute(
            app.Id,
            app.TenantId,
            app.LegalEntityId,
            app.RequisitionId,
            app.CandidateId,
            app.PipelineVersionId,
            app.CurrentStageId,
            app.Status,
            app.Source,
            app.AppliedAtUtc,
            app.DisposedAtUtc,
            app.DispositionReason,
            app.DispositionNote,
            app.HiredPersonId,
            app.HiredEmploymentId,
            app.HiredAtUtc,
            app.RowVersion,
            historyList
        );
    }

    public async Task<Application?> GetActiveApplicationForCandidateAsync(TenantId tenantId, Guid requisitionId, Guid candidateId, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, tenant_id, legal_entity_id, requisition_id, candidate_id, pipeline_version_id,
               current_stage_id, status, source, applied_at_utc, disposed_at_utc, disposition_reason,
               disposition_note, hired_person_id, hired_employment_id, hired_at_utc, row_version
        FROM recruitment.applications
        WHERE tenant_id = @tenant_id AND requisition_id = @requisition_id AND candidate_id = @candidate_id AND status = 1;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);
        cmd.Parameters.AddWithValue("requisition_id", requisitionId);
        cmd.Parameters.AddWithValue("candidate_id", candidateId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return MapApplication(reader);
    }

    public async Task<PagedRecruitmentResult<Application>> QueryApplicationsAsync(
        TenantId tenantId,
        Guid? requisitionId,
        Guid? candidateId,
        Guid? stageId,
        ApplicationStatus? status,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);

        var whereClause = "WHERE tenant_id = @tenant_id";
        if (requisitionId.HasValue) whereClause += " AND requisition_id = @requisition_id";
        if (candidateId.HasValue) whereClause += " AND candidate_id = @candidate_id";
        if (stageId.HasValue) whereClause += " AND current_stage_id = @stage_id";
        if (status.HasValue) whereClause += " AND status = @status";

        var countSql = $"SELECT COUNT(*) FROM recruitment.applications {whereClause};";
        await using var countCmd = new NpgsqlCommand(countSql, conn);
        countCmd.Parameters.AddWithValue("tenant_id", tenantId.Value);
        if (requisitionId.HasValue) countCmd.Parameters.AddWithValue("requisition_id", requisitionId.Value);
        if (candidateId.HasValue) countCmd.Parameters.AddWithValue("candidate_id", candidateId.Value);
        if (stageId.HasValue) countCmd.Parameters.AddWithValue("stage_id", stageId.Value);
        if (status.HasValue) countCmd.Parameters.AddWithValue("status", (int)status.Value);

        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));

        var querySql = $"""
        SELECT id, tenant_id, legal_entity_id, requisition_id, candidate_id, pipeline_version_id,
               current_stage_id, status, source, applied_at_utc, disposed_at_utc, disposition_reason,
               disposition_note, hired_person_id, hired_employment_id, hired_at_utc, row_version
        FROM recruitment.applications
        {whereClause}
        ORDER BY applied_at_utc DESC
        LIMIT @limit OFFSET @offset;
        """;

        await using var queryCmd = new NpgsqlCommand(querySql, conn);
        queryCmd.Parameters.AddWithValue("tenant_id", tenantId.Value);
        if (requisitionId.HasValue) queryCmd.Parameters.AddWithValue("requisition_id", requisitionId.Value);
        if (candidateId.HasValue) queryCmd.Parameters.AddWithValue("candidate_id", candidateId.Value);
        if (stageId.HasValue) queryCmd.Parameters.AddWithValue("stage_id", stageId.Value);
        if (status.HasValue) queryCmd.Parameters.AddWithValue("status", (int)status.Value);
        queryCmd.Parameters.AddWithValue("limit", pageSize);
        queryCmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);

        var list = new List<Application>();
        await using var reader = await queryCmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(MapApplication(reader));
        }

        return new PagedRecruitmentResult<Application>(list, total, page, pageSize);
    }

    public async Task<IReadOnlyList<Application>> GetPipelineBoardApplicationsAsync(TenantId tenantId, Guid requisitionId, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, tenant_id, legal_entity_id, requisition_id, candidate_id, pipeline_version_id,
               current_stage_id, status, source, applied_at_utc, disposed_at_utc, disposition_reason,
               disposition_note, hired_person_id, hired_employment_id, hired_at_utc, row_version
        FROM recruitment.applications
        WHERE tenant_id = @tenant_id AND requisition_id = @requisition_id
        ORDER BY applied_at_utc DESC;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);
        cmd.Parameters.AddWithValue("requisition_id", requisitionId);

        var list = new List<Application>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(MapApplication(reader));
        }

        return list;
    }

    private static Application MapApplication(NpgsqlDataReader reader)
    {
        return Application.Reconstitute(
            reader.GetGuid(0),
            new TenantId(reader.GetGuid(1)),
            new LegalEntityId(reader.GetGuid(2)),
            reader.GetGuid(3),
            reader.GetGuid(4),
            reader.GetGuid(5),
            reader.GetGuid(6),
            (ApplicationStatus)reader.GetInt32(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.GetDateTime(9),
            reader.IsDBNull(10) ? null : reader.GetDateTime(10),
            reader.IsDBNull(11) ? null : reader.GetString(11),
            reader.IsDBNull(12) ? null : reader.GetString(12),
            reader.IsDBNull(13) ? null : reader.GetGuid(13),
            reader.IsDBNull(14) ? null : reader.GetGuid(14),
            reader.IsDBNull(15) ? null : reader.GetDateTime(15),
            (uint)reader.GetInt64(16)
        );
    }

    // ========================================================================
    // 5. INTERVIEWS & SCORECARDS
    // ========================================================================

    public async Task CreateInterviewAsync(Interview interview, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        const string sql = """
        INSERT INTO recruitment.interviews (
            id, tenant_id, application_id, stage_id, title, interview_type, scheduled_start_utc,
            scheduled_end_utc, timezone, location_or_meeting_url, status, interview_kit_json,
            created_at_utc, row_version
        ) VALUES (
            @id, @tenant_id, @application_id, @stage_id, @title, @interview_type, @scheduled_start_utc,
            @scheduled_end_utc, @timezone, @location_or_meeting_url, @status, @interview_kit_json::jsonb,
            @created_at_utc, @row_version
        );
        """;

        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("id", interview.Id);
        cmd.Parameters.AddWithValue("tenant_id", interview.TenantId.Value);
        cmd.Parameters.AddWithValue("application_id", interview.ApplicationId);
        cmd.Parameters.AddWithValue("stage_id", interview.StageId);
        cmd.Parameters.AddWithValue("title", interview.Title);
        cmd.Parameters.AddWithValue("interview_type", (int)interview.InterviewType);
        cmd.Parameters.AddWithValue("scheduled_start_utc", interview.ScheduledStartUtc);
        cmd.Parameters.AddWithValue("scheduled_end_utc", interview.ScheduledEndUtc);
        cmd.Parameters.AddWithValue("timezone", interview.Timezone);
        cmd.Parameters.AddWithValue("location_or_meeting_url", (object?)interview.LocationOrMeetingUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("status", (int)interview.Status);
        cmd.Parameters.AddWithValue("interview_kit_json", interview.InterviewKitJson ?? "{}");
        cmd.Parameters.AddWithValue("created_at_utc", interview.CreatedAtUtc);
        cmd.Parameters.AddWithValue("row_version", (long)interview.RowVersion);

        await cmd.ExecuteNonQueryAsync(ct);

        foreach (var p in interview.Participants)
        {
            const string pSql = """
            INSERT INTO recruitment.interview_participants (id, interview_id, interviewer_user_id, role, is_required)
            VALUES (@id, @interview_id, @interviewer_user_id, @role, @is_required)
            ON CONFLICT (interview_id, interviewer_user_id) DO UPDATE SET role = EXCLUDED.role, is_required = EXCLUDED.is_required;
            """;
            await using var pCmd = new NpgsqlCommand(pSql, conn, tx);
            pCmd.Parameters.AddWithValue("id", p.Id);
            pCmd.Parameters.AddWithValue("interview_id", p.InterviewId);
            pCmd.Parameters.AddWithValue("interviewer_user_id", p.InterviewerUserId);
            pCmd.Parameters.AddWithValue("role", (int)p.Role);
            pCmd.Parameters.AddWithValue("is_required", p.IsRequired);
            await pCmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task UpdateInterviewAsync(Interview interview, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        const string sql = """
        UPDATE recruitment.interviews SET
            scheduled_start_utc = @scheduled_start_utc,
            scheduled_end_utc = @scheduled_end_utc,
            timezone = @timezone,
            location_or_meeting_url = @location_or_meeting_url,
            status = @status,
            interview_kit_json = @interview_kit_json::jsonb,
            row_version = @row_version
        WHERE id = @id AND tenant_id = @tenant_id;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("id", interview.Id);
        cmd.Parameters.AddWithValue("tenant_id", interview.TenantId.Value);
        cmd.Parameters.AddWithValue("scheduled_start_utc", interview.ScheduledStartUtc);
        cmd.Parameters.AddWithValue("scheduled_end_utc", interview.ScheduledEndUtc);
        cmd.Parameters.AddWithValue("timezone", interview.Timezone);
        cmd.Parameters.AddWithValue("location_or_meeting_url", (object?)interview.LocationOrMeetingUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("status", (int)interview.Status);
        cmd.Parameters.AddWithValue("interview_kit_json", interview.InterviewKitJson ?? "{}");
        cmd.Parameters.AddWithValue("row_version", (long)interview.RowVersion);

        var rows = await cmd.ExecuteNonQueryAsync(ct);
        if (rows == 0)
        {
            throw new InvalidOperationException($"Failed to update Interview '{interview.Id}'. Not found or tenant mismatch.");
        }

        foreach (var p in interview.Participants)
        {
            const string pSql = """
            INSERT INTO recruitment.interview_participants (id, interview_id, interviewer_user_id, role, is_required)
            VALUES (@id, @interview_id, @interviewer_user_id, @role, @is_required)
            ON CONFLICT (interview_id, interviewer_user_id) DO UPDATE SET role = EXCLUDED.role, is_required = EXCLUDED.is_required;
            """;
            await using var pCmd = new NpgsqlCommand(pSql, conn, tx);
            pCmd.Parameters.AddWithValue("id", p.Id);
            pCmd.Parameters.AddWithValue("interview_id", p.InterviewId);
            pCmd.Parameters.AddWithValue("interviewer_user_id", p.InterviewerUserId);
            pCmd.Parameters.AddWithValue("role", (int)p.Role);
            pCmd.Parameters.AddWithValue("is_required", p.IsRequired);
            await pCmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task<Interview?> GetInterviewByIdAsync(TenantId tenantId, Guid interviewId, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, tenant_id, application_id, stage_id, title, interview_type, scheduled_start_utc,
               scheduled_end_utc, timezone, location_or_meeting_url, status, interview_kit_json,
               created_at_utc, row_version
        FROM recruitment.interviews
        WHERE id = @id AND tenant_id = @tenant_id;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", interviewId);
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        var interview = MapInterview(reader);
        await reader.CloseAsync();

        // Load participants
        const string pSql = "SELECT id, interview_id, interviewer_user_id, role, is_required FROM recruitment.interview_participants WHERE interview_id = @iid;";
        await using var pCmd = new NpgsqlCommand(pSql, conn);
        pCmd.Parameters.AddWithValue("iid", interviewId);

        var participants = new List<InterviewParticipant>();
        await using var pReader = await pCmd.ExecuteReaderAsync(ct);
        while (await pReader.ReadAsync(ct))
        {
            participants.Add(new InterviewParticipant(
                pReader.GetGuid(0),
                pReader.GetGuid(1),
                pReader.GetGuid(2),
                (InterviewerRole)pReader.GetInt32(3),
                pReader.GetBoolean(4)
            ));
        }
        await pReader.CloseAsync();

        // Load scorecards
        var scorecards = await GetScorecardsForInterviewAsync(interviewId, ct);

        return Interview.Reconstitute(
            interview.Id,
            interview.TenantId,
            interview.ApplicationId,
            interview.StageId,
            interview.Title,
            interview.InterviewType,
            interview.ScheduledStartUtc,
            interview.ScheduledEndUtc,
            interview.Timezone,
            interview.LocationOrMeetingUrl,
            interview.Status,
            interview.InterviewKitJson,
            interview.CreatedAtUtc,
            interview.RowVersion,
            participants,
            scorecards
        );
    }

    public async Task<IReadOnlyList<Interview>> GetInterviewsForApplicationAsync(TenantId tenantId, Guid applicationId, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, tenant_id, application_id, stage_id, title, interview_type, scheduled_start_utc,
               scheduled_end_utc, timezone, location_or_meeting_url, status, interview_kit_json,
               created_at_utc, row_version
        FROM recruitment.interviews
        WHERE tenant_id = @tenant_id AND application_id = @application_id
        ORDER BY scheduled_start_utc ASC;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);
        cmd.Parameters.AddWithValue("application_id", applicationId);

        var list = new List<Interview>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(MapInterview(reader));
        }

        return list;
    }

    public async Task<IReadOnlyList<Interview>> QueryInterviewsAsync(
        TenantId tenantId,
        DateTime startUtc,
        DateTime endUtc,
        Guid? interviewerUserId = null,
        CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);

        var sql = """
        SELECT i.id, i.tenant_id, i.application_id, i.stage_id, i.title, i.interview_type, i.scheduled_start_utc,
               i.scheduled_end_utc, i.timezone, i.location_or_meeting_url, i.status, i.interview_kit_json,
               i.created_at_utc, i.row_version
        FROM recruitment.interviews i
        """;

        if (interviewerUserId.HasValue)
        {
            sql += " JOIN recruitment.interview_participants ip ON i.id = ip.interview_id AND ip.interviewer_user_id = @user_id";
        }

        sql += """
        WHERE i.tenant_id = @tenant_id 
          AND i.scheduled_start_utc >= @start_utc 
          AND i.scheduled_end_utc <= @end_utc
        ORDER BY i.scheduled_start_utc ASC;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);
        cmd.Parameters.AddWithValue("start_utc", startUtc);
        cmd.Parameters.AddWithValue("end_utc", endUtc);
        if (interviewerUserId.HasValue)
        {
            cmd.Parameters.AddWithValue("user_id", interviewerUserId.Value);
        }

        var list = new List<Interview>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(MapInterview(reader));
        }

        return list;
    }

    public async Task SaveScorecardSubmissionAsync(ScorecardSubmission sc, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        INSERT INTO recruitment.scorecard_submissions (
            id, interview_id, application_id, interviewer_user_id, ratings_json, strengths,
            concerns, recommendation, is_finalized, submitted_at_utc, row_version
        ) VALUES (
            @id, @interview_id, @application_id, @interviewer_user_id, @ratings_json::jsonb, @strengths,
            @concerns, @recommendation, @is_finalized, @submitted_at_utc, @row_version
        )
        ON CONFLICT (interview_id, interviewer_user_id) DO UPDATE SET
            ratings_json = EXCLUDED.ratings_json,
            strengths = EXCLUDED.strengths,
            concerns = EXCLUDED.concerns,
            recommendation = EXCLUDED.recommendation,
            is_finalized = EXCLUDED.is_finalized,
            submitted_at_utc = EXCLUDED.submitted_at_utc,
            row_version = recruitment.scorecard_submissions.row_version + 1;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", sc.Id);
        cmd.Parameters.AddWithValue("interview_id", sc.InterviewId);
        cmd.Parameters.AddWithValue("application_id", sc.ApplicationId);
        cmd.Parameters.AddWithValue("interviewer_user_id", sc.InterviewerUserId);
        cmd.Parameters.AddWithValue("ratings_json", sc.RatingsJson ?? "{}");
        cmd.Parameters.AddWithValue("strengths", (object?)sc.Strengths ?? DBNull.Value);
        cmd.Parameters.AddWithValue("concerns", (object?)sc.Concerns ?? DBNull.Value);
        cmd.Parameters.AddWithValue("recommendation", (int)sc.Recommendation);
        cmd.Parameters.AddWithValue("is_finalized", sc.IsFinalized);
        cmd.Parameters.AddWithValue("submitted_at_utc", sc.SubmittedAtUtc);
        cmd.Parameters.AddWithValue("row_version", (long)sc.RowVersion);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<ScorecardSubmission>> GetScorecardsForInterviewAsync(Guid interviewId, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, interview_id, application_id, interviewer_user_id, ratings_json, strengths,
               concerns, recommendation, is_finalized, submitted_at_utc, row_version
        FROM recruitment.scorecard_submissions
        WHERE interview_id = @interview_id;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("interview_id", interviewId);

        var list = new List<ScorecardSubmission>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(ScorecardSubmission.Reconstitute(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetGuid(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                (ScorecardRecommendation)reader.GetInt32(7),
                reader.GetBoolean(8),
                reader.GetDateTime(9),
                (uint)reader.GetInt64(10)
            ));
        }

        return list;
    }

    private static Interview MapInterview(NpgsqlDataReader reader)
    {
        return Interview.Reconstitute(
            reader.GetGuid(0),
            new TenantId(reader.GetGuid(1)),
            reader.GetGuid(2),
            reader.GetGuid(3),
            reader.GetString(4),
            (InterviewType)reader.GetInt32(5),
            reader.GetDateTime(6),
            reader.GetDateTime(7),
            reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetString(9),
            (InterviewStatus)reader.GetInt32(10),
            reader.GetString(11),
            reader.GetDateTime(12),
            (uint)reader.GetInt64(13)
        );
    }

    // ========================================================================
    // 6. OFFERS
    // ========================================================================

    public async Task CreateOfferAsync(Offer offer, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        INSERT INTO recruitment.offers (
            id, tenant_id, legal_entity_id, application_id, candidate_id, offer_version_number,
            title_en, title_ar, proposed_start_date, base_salary_monthly, currency,
            allowances_json, conditions_note, status, approval_request_id, issued_at_utc,
            accepted_at_utc, expiry_date, offer_document_id, created_at_utc, row_version
        ) VALUES (
            @id, @tenant_id, @legal_entity_id, @application_id, @candidate_id, @offer_version_number,
            @title_en, @title_ar, @proposed_start_date, @base_salary_monthly, @currency,
            @allowances_json::jsonb, @conditions_note, @status, @approval_request_id, @issued_at_utc,
            @accepted_at_utc, @expiry_date, @offer_document_id, @created_at_utc, @row_version
        );
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", offer.Id);
        cmd.Parameters.AddWithValue("tenant_id", offer.TenantId.Value);
        cmd.Parameters.AddWithValue("legal_entity_id", offer.LegalEntityId.Value);
        cmd.Parameters.AddWithValue("application_id", offer.ApplicationId);
        cmd.Parameters.AddWithValue("candidate_id", offer.CandidateId);
        cmd.Parameters.AddWithValue("offer_version_number", offer.OfferVersionNumber);
        cmd.Parameters.AddWithValue("title_en", offer.TitleEn);
        cmd.Parameters.AddWithValue("title_ar", offer.TitleAr);
        cmd.Parameters.AddWithValue("proposed_start_date", offer.ProposedStartDate);
        cmd.Parameters.AddWithValue("base_salary_monthly", offer.BaseSalaryMonthly);
        cmd.Parameters.AddWithValue("currency", offer.Currency);
        cmd.Parameters.AddWithValue("allowances_json", offer.AllowancesJson ?? "[]");
        cmd.Parameters.AddWithValue("conditions_note", (object?)offer.ConditionsNote ?? DBNull.Value);
        cmd.Parameters.AddWithValue("status", (int)offer.Status);
        cmd.Parameters.AddWithValue("approval_request_id", (object?)offer.ApprovalRequestId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("issued_at_utc", (object?)offer.IssuedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("accepted_at_utc", (object?)offer.AcceptedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("expiry_date", (object?)offer.ExpiryDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("offer_document_id", (object?)offer.OfferDocumentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("created_at_utc", offer.CreatedAtUtc);
        cmd.Parameters.AddWithValue("row_version", (long)offer.RowVersion);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateOfferAsync(Offer offer, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        UPDATE recruitment.offers SET
            title_en = @title_en,
            title_ar = @title_ar,
            proposed_start_date = @proposed_start_date,
            base_salary_monthly = @base_salary_monthly,
            currency = @currency,
            allowances_json = @allowances_json::jsonb,
            conditions_note = @conditions_note,
            status = @status,
            approval_request_id = @approval_request_id,
            issued_at_utc = @issued_at_utc,
            accepted_at_utc = @accepted_at_utc,
            expiry_date = @expiry_date,
            offer_document_id = @offer_document_id,
            row_version = @row_version
        WHERE id = @id AND tenant_id = @tenant_id;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", offer.Id);
        cmd.Parameters.AddWithValue("tenant_id", offer.TenantId.Value);
        cmd.Parameters.AddWithValue("title_en", offer.TitleEn);
        cmd.Parameters.AddWithValue("title_ar", offer.TitleAr);
        cmd.Parameters.AddWithValue("proposed_start_date", offer.ProposedStartDate);
        cmd.Parameters.AddWithValue("base_salary_monthly", offer.BaseSalaryMonthly);
        cmd.Parameters.AddWithValue("currency", offer.Currency);
        cmd.Parameters.AddWithValue("allowances_json", offer.AllowancesJson ?? "[]");
        cmd.Parameters.AddWithValue("conditions_note", (object?)offer.ConditionsNote ?? DBNull.Value);
        cmd.Parameters.AddWithValue("status", (int)offer.Status);
        cmd.Parameters.AddWithValue("approval_request_id", (object?)offer.ApprovalRequestId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("issued_at_utc", (object?)offer.IssuedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("accepted_at_utc", (object?)offer.AcceptedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("expiry_date", (object?)offer.ExpiryDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("offer_document_id", (object?)offer.OfferDocumentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("row_version", (long)offer.RowVersion);

        var rows = await cmd.ExecuteNonQueryAsync(ct);
        if (rows == 0)
        {
            throw new InvalidOperationException($"Failed to update Offer '{offer.Id}'. Not found or tenant mismatch.");
        }
    }

    public async Task<Offer?> GetOfferByIdAsync(TenantId tenantId, Guid offerId, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, tenant_id, legal_entity_id, application_id, candidate_id, offer_version_number,
               title_en, title_ar, proposed_start_date, base_salary_monthly, currency,
               allowances_json, conditions_note, status, approval_request_id, issued_at_utc,
               accepted_at_utc, expiry_date, offer_document_id, created_at_utc, row_version
        FROM recruitment.offers
        WHERE id = @id AND tenant_id = @tenant_id;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", offerId);
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return MapOffer(reader);
    }

    public async Task<IReadOnlyList<Offer>> GetOffersForApplicationAsync(TenantId tenantId, Guid applicationId, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, tenant_id, legal_entity_id, application_id, candidate_id, offer_version_number,
               title_en, title_ar, proposed_start_date, base_salary_monthly, currency,
               allowances_json, conditions_note, status, approval_request_id, issued_at_utc,
               accepted_at_utc, expiry_date, offer_document_id, created_at_utc, row_version
        FROM recruitment.offers
        WHERE tenant_id = @tenant_id AND application_id = @application_id
        ORDER BY offer_version_number DESC;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);
        cmd.Parameters.AddWithValue("application_id", applicationId);

        var list = new List<Offer>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(MapOffer(reader));
        }

        return list;
    }

    public async Task<Offer?> GetLatestOfferForApplicationAsync(TenantId tenantId, Guid applicationId, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, tenant_id, legal_entity_id, application_id, candidate_id, offer_version_number,
               title_en, title_ar, proposed_start_date, base_salary_monthly, currency,
               allowances_json, conditions_note, status, approval_request_id, issued_at_utc,
               accepted_at_utc, expiry_date, offer_document_id, created_at_utc, row_version
        FROM recruitment.offers
        WHERE tenant_id = @tenant_id AND application_id = @application_id
        ORDER BY offer_version_number DESC
        LIMIT 1;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);
        cmd.Parameters.AddWithValue("application_id", applicationId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return MapOffer(reader);
    }

    private static Offer MapOffer(NpgsqlDataReader reader)
    {
        return Offer.Reconstitute(
            reader.GetGuid(0),
            new TenantId(reader.GetGuid(1)),
            new LegalEntityId(reader.GetGuid(2)),
            reader.GetGuid(3),
            reader.GetGuid(4),
            reader.GetInt32(5),
            reader.GetString(6),
            reader.GetString(7),
            DateOnly.FromDateTime(reader.GetDateTime(8)),
            reader.GetDecimal(9),
            reader.GetString(10),
            reader.IsDBNull(11) ? null : reader.GetString(11),
            reader.IsDBNull(12) ? null : reader.GetString(12),
            (OfferStatus)reader.GetInt32(13),
            reader.IsDBNull(14) ? null : reader.GetGuid(14),
            reader.IsDBNull(15) ? null : reader.GetDateTime(15),
            reader.IsDBNull(16) ? null : reader.GetDateTime(16),
            reader.IsDBNull(17) ? null : DateOnly.FromDateTime(reader.GetDateTime(17)),
            reader.IsDBNull(18) ? null : reader.GetGuid(18),
            reader.GetDateTime(19),
            (uint)reader.GetInt64(20)
        );
    }

    // ========================================================================
    // 7. OUTBOX
    // ========================================================================

    public async Task SaveOutboxMessageAsync(TenantId tenantId, string eventType, object payload, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        INSERT INTO recruitment.outbox_messages (id, tenant_id, event_type, payload_json, occurred_at_utc, processed_at_utc)
        VALUES (@id, @tenant_id, @event_type, @payload_json::jsonb, @occurred_at_utc, NULL);
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);
        cmd.Parameters.AddWithValue("event_type", eventType);
        cmd.Parameters.AddWithValue("payload_json", JsonSerializer.Serialize(payload));
        cmd.Parameters.AddWithValue("occurred_at_utc", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync(ct);
    }
}
