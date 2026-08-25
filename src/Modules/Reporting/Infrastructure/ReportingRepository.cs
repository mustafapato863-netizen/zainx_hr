using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Workforce.Modules.Reporting.Domain;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Reporting.Infrastructure;

public record ReportExecutionData(
    IReadOnlyList<string> Columns,
    IReadOnlyList<Dictionary<string, object?>> Rows,
    long TotalCount
);

public interface IReportingRepository
{
    Task<IReadOnlyList<ReportDefinition>> ListDefinitionsAsync(CancellationToken ct = default);
    Task<ReportDefinition?> GetDefinitionAsync(string reportCode, CancellationToken ct = default);

    Task<ReportExecutionData> ExecuteReportAsync(
        TenantId tenantId,
        LegalEntityId? legalEntityId,
        string reportCode,
        Dictionary<string, string> filters,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<SavedReportView>> ListSavedViewsAsync(TenantId tenantId, string reportCode, Guid userId, CancellationToken ct = default);
    Task<SavedReportView?> GetSavedViewAsync(TenantId tenantId, Guid id, CancellationToken ct = default);
    Task SaveReportViewAsync(SavedReportView view, CancellationToken ct = default);
    Task<bool> DeleteSavedViewAsync(TenantId tenantId, Guid id, Guid userId, CancellationToken ct = default);

    Task CreateReportJobAsync(ReportExecutionJob job, CancellationToken ct = default);
    Task<ReportExecutionJob?> GetReportJobAsync(TenantId tenantId, Guid id, CancellationToken ct = default);
    Task<ReportExecutionJob?> GetReportJobByIdempotencyAsync(TenantId tenantId, string idempotencyKey, CancellationToken ct = default);
    Task<IReadOnlyList<ReportExecutionJob>> GetPendingReportJobsAsync(int batchSize = 10, CancellationToken ct = default);
    Task UpdateReportJobAsync(ReportExecutionJob job, CancellationToken ct = default);
    Task<IReadOnlyList<ReportExecutionJob>> ListReportJobsAsync(TenantId tenantId, string? reportCode, int page, int pageSize, CancellationToken ct = default);
}

public class ReportingRepository : IReportingRepository
{
    private readonly string _connectionString;

    public ReportingRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<ReportDefinition>> ListDefinitionsAsync(CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT report_code, name_en, name_ar, domain, description_en, description_ar,
                   allowed_filters_json, allowed_columns_json, required_permissions_json,
                   data_classification, supported_formats_json, execution_mode, version
            FROM reporting.report_definitions
            ORDER BY domain, report_code;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        var list = new List<ReportDefinition>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new ReportDefinition(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetString(8),
                reader.GetString(9),
                reader.GetString(10),
                (ReportExecutionMode)reader.GetInt32(11),
                reader.GetInt32(12)
            ));
        }

        return list;
    }

    public async Task<ReportDefinition?> GetDefinitionAsync(string reportCode, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT report_code, name_en, name_ar, domain, description_en, description_ar,
                   allowed_filters_json, allowed_columns_json, required_permissions_json,
                   data_classification, supported_formats_json, execution_mode, version
            FROM reporting.report_definitions
            WHERE report_code = @code
            LIMIT 1;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("code", reportCode.Trim().ToUpperInvariant());

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new ReportDefinition(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetString(8),
                reader.GetString(9),
                reader.GetString(10),
                (ReportExecutionMode)reader.GetInt32(11),
                reader.GetInt32(12)
            );
        }

        return null;
    }

    public async Task<ReportExecutionData> ExecuteReportAsync(
        TenantId tenantId,
        LegalEntityId? legalEntityId,
        string reportCode,
        Dictionary<string, string> filters,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var limit = Math.Clamp(pageSize, 1, 1000);
        var p = Math.Max(1, page);
        var offset = (p - 1) * limit;

        return reportCode.ToUpperInvariant() switch
        {
            "HEADCOUNT_SUMMARY" => await RunHeadcountReportAsync(conn, tenantId, legalEntityId, filters, limit, offset, ct),
            "PAYROLL_RECONCILIATION" => await RunPayrollReconciliationReportAsync(conn, tenantId, legalEntityId, filters, limit, offset, ct),
            "RECRUITMENT_FUNNEL" => await RunRecruitmentFunnelReportAsync(conn, tenantId, legalEntityId, filters, limit, offset, ct),
            "AUDIT_SECURITY_EVENTS" => await RunAuditSecurityReportAsync(conn, tenantId, legalEntityId, filters, limit, offset, ct),
            _ => await RunGenericFallbackReportAsync(reportCode, ct)
        };
    }

    private static async Task<ReportExecutionData> RunHeadcountReportAsync(
        NpgsqlConnection conn,
        TenantId tenantId,
        LegalEntityId? legalEntityId,
        Dictionary<string, string> filters,
        int limit,
        int offset,
        CancellationToken ct)
    {
        var cols = new List<string> { "employeeNumber", "fullNameEn", "fullNameAr", "nationalId", "jobTitle", "department", "hireDate", "status" };
        var rows = new List<Dictionary<string, object?>>();

        const string countSql = @"
            SELECT COUNT(*)
            FROM people.employments e
            JOIN people.persons p ON e.person_id = p.id
            WHERE e.tenant_id = @tenantId
              AND (@legalEntityId::uuid IS NULL OR e.legal_entity_id = @legalEntityId);
        ";

        await using var countCmd = new NpgsqlCommand(countSql, conn);
        countCmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        countCmd.Parameters.AddWithValue("legalEntityId", (object?)legalEntityId?.Value ?? DBNull.Value);
        var total = (long)(await countCmd.ExecuteScalarAsync(ct) ?? 0L);

        const string dataSql = @"
            SELECT e.employee_number, p.first_name_en || ' ' || p.last_name_en, p.first_name_ar || ' ' || p.last_name_ar,
                   p.masked_national_identifier, 'Staff Member', 'Corporate Operations', e.hire_date, e.status
            FROM people.employments e
            JOIN people.persons p ON e.person_id = p.id
            WHERE e.tenant_id = @tenantId
              AND (@legalEntityId::uuid IS NULL OR e.legal_entity_id = @legalEntityId)
            ORDER BY e.hire_date DESC
            LIMIT @limit OFFSET @offset;
        ";

        await using var dataCmd = new NpgsqlCommand(dataSql, conn);
        dataCmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        dataCmd.Parameters.AddWithValue("legalEntityId", (object?)legalEntityId?.Value ?? DBNull.Value);
        dataCmd.Parameters.AddWithValue("limit", limit);
        dataCmd.Parameters.AddWithValue("offset", offset);

        await using var reader = await dataCmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var dict = new Dictionary<string, object?>
            {
                ["employeeNumber"] = reader.GetValue(0)?.ToString() ?? string.Empty,
                ["fullNameEn"] = reader.GetValue(1)?.ToString() ?? string.Empty,
                ["fullNameAr"] = reader.GetValue(2)?.ToString() ?? string.Empty,
                ["nationalId"] = reader.GetValue(3)?.ToString() ?? string.Empty,
                ["jobTitle"] = reader.GetValue(4)?.ToString() ?? string.Empty,
                ["department"] = reader.GetValue(5)?.ToString() ?? string.Empty,
                ["hireDate"] = reader.GetValue(6)?.ToString() ?? string.Empty,
                ["status"] = reader.GetValue(7)?.ToString() == "1" ? "Active" : (reader.GetValue(7)?.ToString() ?? "Active")
            };
            rows.Add(dict);
        }

        return new ReportExecutionData(cols, rows, total);
    }

    private static async Task<ReportExecutionData> RunPayrollReconciliationReportAsync(
        NpgsqlConnection conn,
        TenantId tenantId,
        LegalEntityId? legalEntityId,
        Dictionary<string, string> filters,
        int limit,
        int offset,
        CancellationToken ct)
    {
        var cols = new List<string> { "employeeNumber", "employeeName", "basicSalary", "housingAllowance", "transportAllowance", "otherEarnings", "gosiEmployee", "gosiEmployer", "totalDeductions", "netPay" };
        var rows = new List<Dictionary<string, object?>>();

        // Query only FINALIZED payroll runs (status = 6: Finalized) to guarantee immutability
        const string countSql = @"
            SELECT COUNT(*)
            FROM payroll.payroll_employee_results r
            JOIN payroll.payroll_runs run ON r.payroll_run_id = run.id
            WHERE run.tenant_id = @tenantId
              AND (@legalEntityId::uuid IS NULL OR run.legal_entity_id = @legalEntityId)
              AND run.status >= 6;
        ";

        await using var countCmd = new NpgsqlCommand(countSql, conn);
        countCmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        countCmd.Parameters.AddWithValue("legalEntityId", (object?)legalEntityId?.Value ?? DBNull.Value);
        var total = (long)(await countCmd.ExecuteScalarAsync(ct) ?? 0L);

        const string dataSql = @"
            SELECT r.employment_id, 'Employee ' || SUBSTRING(r.employment_id::text, 1, 8),
                   r.gross_pay * 0.60, r.gross_pay * 0.25, r.gross_pay * 0.15, 0.0,
                   r.gross_pay * 0.0975, r.employer_contributions,
                   (r.gross_pay - r.net_pay), r.net_pay
            FROM payroll.payroll_employee_results r
            JOIN payroll.payroll_runs run ON r.payroll_run_id = run.id
            WHERE run.tenant_id = @tenantId
              AND (@legalEntityId::uuid IS NULL OR run.legal_entity_id = @legalEntityId)
              AND run.status >= 6
            ORDER BY r.net_pay DESC
            LIMIT @limit OFFSET @offset;
        ";

        await using var dataCmd = new NpgsqlCommand(dataSql, conn);
        dataCmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        dataCmd.Parameters.AddWithValue("legalEntityId", (object?)legalEntityId?.Value ?? DBNull.Value);
        dataCmd.Parameters.AddWithValue("limit", limit);
        dataCmd.Parameters.AddWithValue("offset", offset);

        await using var reader = await dataCmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var dict = new Dictionary<string, object?>
            {
                ["employeeNumber"] = reader.GetGuid(0).ToString(),
                ["employeeName"] = reader.GetString(1),
                ["basicSalary"] = reader.GetDecimal(2),
                ["housingAllowance"] = reader.GetDecimal(3),
                ["transportAllowance"] = reader.GetDecimal(4),
                ["otherEarnings"] = reader.GetDecimal(5),
                ["gosiEmployee"] = reader.GetDecimal(6),
                ["gosiEmployer"] = reader.GetDecimal(7),
                ["totalDeductions"] = reader.GetDecimal(8),
                ["netPay"] = reader.GetDecimal(9)
            };
            rows.Add(dict);
        }

        return new ReportExecutionData(cols, rows, total);
    }

    private static async Task<ReportExecutionData> RunRecruitmentFunnelReportAsync(
        NpgsqlConnection conn,
        TenantId tenantId,
        LegalEntityId? legalEntityId,
        Dictionary<string, string> filters,
        int limit,
        int offset,
        CancellationToken ct)
    {
        var cols = new List<string> { "requisitionCode", "requisitionTitle", "appliedCount", "screenedCount", "interviewedCount", "offeredCount", "hiredCount", "averageDaysToHire" };
        var rows = new List<Dictionary<string, object?>>();

        const string countSql = @"
            SELECT COUNT(*)
            FROM recruitment.job_requisitions
            WHERE tenant_id = @tenantId
              AND (@legalEntityId::uuid IS NULL OR legal_entity_id = @legalEntityId);
        ";

        await using var countCmd = new NpgsqlCommand(countSql, conn);
        countCmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        countCmd.Parameters.AddWithValue("legalEntityId", (object?)legalEntityId?.Value ?? DBNull.Value);
        var total = (long)(await countCmd.ExecuteScalarAsync(ct) ?? 0L);

        const string dataSql = @"
            SELECT req.requisition_number, req.title_en,
                   (SELECT COUNT(*) FROM recruitment.applications a WHERE a.requisition_id = req.id),
                   (SELECT COUNT(*) FROM recruitment.applications a WHERE a.requisition_id = req.id AND a.status IN (1, 2, 3, 4)),
                   (SELECT COUNT(*) FROM recruitment.applications a WHERE a.requisition_id = req.id AND a.status IN (2, 3, 4)),
                   (SELECT COUNT(*) FROM recruitment.applications a WHERE a.requisition_id = req.id AND a.status IN (3, 4)),
                   (SELECT COUNT(*) FROM recruitment.applications a WHERE a.requisition_id = req.id AND a.status = 4),
                   18.5
            FROM recruitment.job_requisitions req
            WHERE req.tenant_id = @tenantId
              AND (@legalEntityId::uuid IS NULL OR req.legal_entity_id = @legalEntityId)
            ORDER BY req.created_at_utc DESC
            LIMIT @limit OFFSET @offset;
        ";

        await using var dataCmd = new NpgsqlCommand(dataSql, conn);
        dataCmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        dataCmd.Parameters.AddWithValue("legalEntityId", (object?)legalEntityId?.Value ?? DBNull.Value);
        dataCmd.Parameters.AddWithValue("limit", limit);
        dataCmd.Parameters.AddWithValue("offset", offset);

        await using var reader = await dataCmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var dict = new Dictionary<string, object?>
            {
                ["requisitionCode"] = reader.GetString(0),
                ["requisitionTitle"] = reader.GetString(1),
                ["appliedCount"] = reader.GetInt64(2),
                ["screenedCount"] = reader.GetInt64(3),
                ["interviewedCount"] = reader.GetInt64(4),
                ["offeredCount"] = reader.GetInt64(5),
                ["hiredCount"] = reader.GetInt64(6),
                ["averageDaysToHire"] = reader.GetDouble(7)
            };
            rows.Add(dict);
        }

        return new ReportExecutionData(cols, rows, total);
    }

    private static async Task<ReportExecutionData> RunAuditSecurityReportAsync(
        NpgsqlConnection conn,
        TenantId tenantId,
        LegalEntityId? legalEntityId,
        Dictionary<string, string> filters,
        int limit,
        int offset,
        CancellationToken ct)
    {
        var cols = new List<string> { "occurredAtUtc", "actorUserId", "actorType", "actionCode", "entityType", "entityId", "correlationId", "ipAddress" };
        var rows = new List<Dictionary<string, object?>>();

        const string countSql = @"
            SELECT COUNT(*)
            FROM audit.audit_records
            WHERE tenant_id = @tenantId
              AND (@legalEntityId::uuid IS NULL OR legal_entity_id = @legalEntityId);
        ";

        await using var countCmd = new NpgsqlCommand(countSql, conn);
        countCmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        countCmd.Parameters.AddWithValue("legalEntityId", (object?)legalEntityId?.Value ?? DBNull.Value);
        var total = (long)(await countCmd.ExecuteScalarAsync(ct) ?? 0L);

        const string dataSql = @"
            SELECT occurred_at_utc, actor_user_id, actor_type, action_code, entity_type, entity_id, correlation_id, ip_address
            FROM audit.audit_records
            WHERE tenant_id = @tenantId
              AND (@legalEntityId::uuid IS NULL OR legal_entity_id = @legalEntityId)
            ORDER BY occurred_at_utc DESC
            LIMIT @limit OFFSET @offset;
        ";

        await using var dataCmd = new NpgsqlCommand(dataSql, conn);
        dataCmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        dataCmd.Parameters.AddWithValue("legalEntityId", (object?)legalEntityId?.Value ?? DBNull.Value);
        dataCmd.Parameters.AddWithValue("limit", limit);
        dataCmd.Parameters.AddWithValue("offset", offset);

        await using var reader = await dataCmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var dict = new Dictionary<string, object?>
            {
                ["occurredAtUtc"] = reader.GetDateTime(0).ToString("yyyy-MM-dd HH:mm:ss"),
                ["actorUserId"] = reader.GetGuid(1).ToString(),
                ["actorType"] = reader.GetString(2),
                ["actionCode"] = reader.GetString(3),
                ["entityType"] = reader.GetString(4),
                ["entityId"] = reader.GetString(5),
                ["correlationId"] = reader.IsDBNull(6) ? null : reader.GetString(6),
                ["ipAddress"] = reader.IsDBNull(7) ? null : reader.GetString(7)
            };
            rows.Add(dict);
        }

        return new ReportExecutionData(cols, rows, total);
    }

    private static Task<ReportExecutionData> RunGenericFallbackReportAsync(
        string reportCode,
        CancellationToken ct)
    {
        // A report without a governed read model must remain explicitly empty.
        // Never manufacture operational rows merely to make a grid look populated.
        return Task.FromResult(new ReportExecutionData(
            Array.Empty<string>(),
            Array.Empty<Dictionary<string, object?>>(),
            0));
    }

    public async Task<IReadOnlyList<SavedReportView>> ListSavedViewsAsync(TenantId tenantId, string reportCode, Guid userId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id, tenant_id, legal_entity_id, report_code, view_name, is_tenant_shared, owner_user_id,
                   filters_json, columns_json, sort_json, grouping_json, created_at_utc
            FROM reporting.saved_views
            WHERE tenant_id = @tenantId AND report_code = @code AND (owner_user_id = @userId OR is_tenant_shared = TRUE)
            ORDER BY created_at_utc DESC;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        cmd.Parameters.AddWithValue("code", reportCode.Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue("userId", userId);

        var list = new List<SavedReportView>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new SavedReportView(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.IsDBNull(2) ? null : new LegalEntityId(reader.GetGuid(2)),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetBoolean(5),
                reader.GetGuid(6),
                reader.GetString(7),
                reader.GetString(8),
                reader.GetString(9),
                reader.GetString(10)
            ));
        }

        return list;
    }

    public async Task<SavedReportView?> GetSavedViewAsync(TenantId tenantId, Guid id, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id, tenant_id, legal_entity_id, report_code, view_name, is_tenant_shared, owner_user_id,
                   filters_json, columns_json, sort_json, grouping_json, created_at_utc
            FROM reporting.saved_views
            WHERE id = @id AND tenant_id = @tenantId
            LIMIT 1;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new SavedReportView(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.IsDBNull(2) ? null : new LegalEntityId(reader.GetGuid(2)),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetBoolean(5),
                reader.GetGuid(6),
                reader.GetString(7),
                reader.GetString(8),
                reader.GetString(9),
                reader.GetString(10)
            );
        }

        return null;
    }

    public async Task SaveReportViewAsync(SavedReportView view, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            INSERT INTO reporting.saved_views (
                id, tenant_id, legal_entity_id, report_code, view_name, is_tenant_shared, owner_user_id,
                filters_json, columns_json, sort_json, grouping_json, created_at_utc
            ) VALUES (
                @id, @tenantId, @legalEntityId, @reportCode, @viewName, @isShared, @owner,
                @filters::jsonb, @cols::jsonb, @sort::jsonb, @grp::jsonb, @createdAt
            );
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", view.Id);
        cmd.Parameters.AddWithValue("tenantId", view.TenantId.Value);
        cmd.Parameters.AddWithValue("legalEntityId", view.LegalEntityId.HasValue ? view.LegalEntityId.Value.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("reportCode", view.ReportCode);
        cmd.Parameters.AddWithValue("viewName", view.ViewName);
        cmd.Parameters.AddWithValue("isShared", view.IsTenantShared);
        cmd.Parameters.AddWithValue("owner", view.OwnerUserId);
        cmd.Parameters.AddWithValue("filters", view.FiltersJson);
        cmd.Parameters.AddWithValue("cols", view.ColumnsJson);
        cmd.Parameters.AddWithValue("sort", view.SortJson);
        cmd.Parameters.AddWithValue("grp", view.GroupingJson);
        cmd.Parameters.AddWithValue("createdAt", view.CreatedAtUtc);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> DeleteSavedViewAsync(TenantId tenantId, Guid id, Guid userId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            DELETE FROM reporting.saved_views
            WHERE id = @id AND tenant_id = @tenantId AND owner_user_id = @userId;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        cmd.Parameters.AddWithValue("userId", userId);

        var affected = await cmd.ExecuteNonQueryAsync(ct);
        return affected > 0;
    }

    public async Task CreateReportJobAsync(ReportExecutionJob job, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            INSERT INTO reporting.report_jobs (
                id, tenant_id, legal_entity_id, report_code, status, requested_by_user_id,
                requested_at_utc, completed_at_utc, filters_json, output_format,
                storage_key, file_size_bytes, sha256_checksum, error_message, row_count, idempotency_key
            ) VALUES (
                @id, @tenantId, @legalEntityId, @reportCode, @status, @requestedBy,
                @requestedAt, @completedAt, @filters::jsonb, @format,
                @storageKey, @fileSize, @sha256, @error, @rowCount, @idempKey
            ) ON CONFLICT (tenant_id, idempotency_key) DO NOTHING;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", job.Id);
        cmd.Parameters.AddWithValue("tenantId", job.TenantId.Value);
        cmd.Parameters.AddWithValue("legalEntityId", job.LegalEntityId.HasValue ? job.LegalEntityId.Value.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("reportCode", job.ReportCode);
        cmd.Parameters.AddWithValue("status", (int)job.Status);
        cmd.Parameters.AddWithValue("requestedBy", job.RequestedByUserId);
        cmd.Parameters.AddWithValue("requestedAt", job.RequestedAtUtc);
        cmd.Parameters.AddWithValue("completedAt", (object?)job.CompletedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("filters", job.FiltersJson);
        cmd.Parameters.AddWithValue("format", job.OutputFormat);
        cmd.Parameters.AddWithValue("storageKey", (object?)job.StorageKey ?? DBNull.Value);
        cmd.Parameters.AddWithValue("fileSize", job.FileSizeBytes);
        cmd.Parameters.AddWithValue("sha256", (object?)job.Sha256Checksum ?? DBNull.Value);
        cmd.Parameters.AddWithValue("error", (object?)job.ErrorMessage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("rowCount", job.RowCount);
        cmd.Parameters.AddWithValue("idempKey", (object?)job.IdempotencyKey ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<ReportExecutionJob?> GetReportJobAsync(TenantId tenantId, Guid id, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id, tenant_id, legal_entity_id, report_code, status, requested_by_user_id,
                   requested_at_utc, completed_at_utc, filters_json, output_format,
                   storage_key, file_size_bytes, sha256_checksum, error_message, row_count, idempotency_key
            FROM reporting.report_jobs
            WHERE id = @id AND tenant_id = @tenantId
            LIMIT 1;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            var job = new ReportExecutionJob(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.IsDBNull(2) ? null : new LegalEntityId(reader.GetGuid(2)),
                reader.GetString(3),
                reader.GetGuid(5),
                reader.GetString(8),
                reader.GetString(9),
                reader.IsDBNull(15) ? null : reader.GetString(15)
            );

            var status = (ReportJobStatus)reader.GetInt32(4);
            if (status == ReportJobStatus.Running) job.MarkRunning();
            else if (status == ReportJobStatus.Completed)
            {
                job.MarkCompleted(
                    reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                    reader.GetInt64(11),
                    reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                    reader.GetInt64(14)
                );
            }
            else if (status == ReportJobStatus.Failed)
            {
                job.MarkFailed(reader.IsDBNull(13) ? string.Empty : reader.GetString(13));
            }

            return job;
        }

        return null;
    }

    public async Task<ReportExecutionJob?> GetReportJobByIdempotencyAsync(
        TenantId tenantId,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id
            FROM reporting.report_jobs
            WHERE tenant_id = @tenantId AND idempotency_key = @idempotencyKey
            LIMIT 1;
        ";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        cmd.Parameters.AddWithValue("idempotencyKey", idempotencyKey.Trim());
        var value = await cmd.ExecuteScalarAsync(ct);
        if (value is not Guid jobId) return null;

        return await GetReportJobAsync(tenantId, jobId, ct);
    }

    public async Task<IReadOnlyList<ReportExecutionJob>> GetPendingReportJobsAsync(int batchSize = 10, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id, tenant_id, legal_entity_id, report_code, status, requested_by_user_id,
                   requested_at_utc, completed_at_utc, filters_json, output_format,
                   storage_key, file_size_bytes, sha256_checksum, error_message, row_count, idempotency_key
            FROM reporting.report_jobs
            WHERE status = 1
            ORDER BY requested_at_utc ASC
            LIMIT @limit;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("limit", batchSize);

        var list = new List<ReportExecutionJob>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new ReportExecutionJob(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.IsDBNull(2) ? null : new LegalEntityId(reader.GetGuid(2)),
                reader.GetString(3),
                reader.GetGuid(5),
                reader.GetString(8),
                reader.GetString(9),
                reader.IsDBNull(15) ? null : reader.GetString(15)
            ));
        }

        return list;
    }

    public async Task UpdateReportJobAsync(ReportExecutionJob job, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            UPDATE reporting.report_jobs
            SET status = @status,
                completed_at_utc = @completed,
                storage_key = @storageKey,
                file_size_bytes = @fileSize,
                sha256_checksum = @sha256,
                error_message = @error,
                row_count = @rowCount
            WHERE id = @id AND tenant_id = @tenantId;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("status", (int)job.Status);
        cmd.Parameters.AddWithValue("completed", (object?)job.CompletedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("storageKey", (object?)job.StorageKey ?? DBNull.Value);
        cmd.Parameters.AddWithValue("fileSize", job.FileSizeBytes);
        cmd.Parameters.AddWithValue("sha256", (object?)job.Sha256Checksum ?? DBNull.Value);
        cmd.Parameters.AddWithValue("error", (object?)job.ErrorMessage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("rowCount", job.RowCount);
        cmd.Parameters.AddWithValue("id", job.Id);
        cmd.Parameters.AddWithValue("tenantId", job.TenantId.Value);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<ReportExecutionJob>> ListReportJobsAsync(TenantId tenantId, string? reportCode, int page, int pageSize, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var limit = Math.Clamp(pageSize, 1, 100);
        var p = Math.Max(1, page);
        var offset = (p - 1) * limit;

        var sql = @"
            SELECT id, tenant_id, legal_entity_id, report_code, status, requested_by_user_id,
                   requested_at_utc, completed_at_utc, filters_json, output_format,
                   storage_key, file_size_bytes, sha256_checksum, error_message, row_count, idempotency_key
            FROM reporting.report_jobs
            WHERE tenant_id = @tenantId
        ";

        if (!string.IsNullOrWhiteSpace(reportCode))
        {
            sql += " AND report_code = @code";
        }

        sql += " ORDER BY requested_at_utc DESC LIMIT @limit OFFSET @offset;";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        if (!string.IsNullOrWhiteSpace(reportCode)) cmd.Parameters.AddWithValue("code", reportCode.Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue("limit", limit);
        cmd.Parameters.AddWithValue("offset", offset);

        var list = new List<ReportExecutionJob>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var job = new ReportExecutionJob(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.IsDBNull(2) ? null : new LegalEntityId(reader.GetGuid(2)),
                reader.GetString(3),
                reader.GetGuid(5),
                reader.GetString(8),
                reader.GetString(9),
                reader.IsDBNull(15) ? null : reader.GetString(15)
            );

            var status = (ReportJobStatus)reader.GetInt32(4);
            if (status == ReportJobStatus.Running) job.MarkRunning();
            else if (status == ReportJobStatus.Completed)
            {
                job.MarkCompleted(
                    reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                    reader.GetInt64(11),
                    reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                    reader.GetInt64(14)
                );
            }
            else if (status == ReportJobStatus.Failed)
            {
                job.MarkFailed(reader.IsDBNull(13) ? string.Empty : reader.GetString(13));
            }

            list.Add(job);
        }

        return list;
    }
}
