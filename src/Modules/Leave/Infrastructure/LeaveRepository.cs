using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using Workforce.Modules.Leave.Domain;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Leave.Infrastructure;

public interface ILeaveRepository
{
    Task<IReadOnlyList<LeaveTypeDto>> GetLeaveTypesAsync(TenantId tenantId, LegalEntityId legalEntityId);
    Task SaveLeaveTypeAsync(LeaveType leaveType);
    Task<IReadOnlyList<LeaveBalanceDto>> GetLeaveBalancesAsync(TenantId tenantId, Guid employmentId, int year);
    Task<LeaveBalance?> GetLeaveBalanceEntityAsync(TenantId tenantId, Guid employmentId, Guid leaveTypeId, int year);
    Task<LeaveBalance> GetOrCreateLeaveBalanceAsync(TenantId tenantId, Guid employmentId, Guid leaveTypeId, int year, decimal defaultEntitledDays);
    Task SaveLeaveBalanceAsync(LeaveBalance balance);
    Task<(IReadOnlyList<LeaveRequestDto> Items, int TotalCount)> GetLeaveRequestsAsync(TenantId tenantId, LegalEntityId? legalEntityId, Guid? employmentId, int? status, int page = 1, int pageSize = 50);
    Task<LeaveRequestDto?> GetLeaveRequestByIdAsync(TenantId tenantId, Guid id);
    Task<LeaveRequest?> GetLeaveRequestEntityByIdAsync(TenantId tenantId, Guid id);
    Task SaveLeaveRequestAsync(LeaveRequest request);
}

public record LeaveTypeDto(
    Guid Id,
    Guid TenantId,
    Guid LegalEntityId,
    string Code,
    string NameEn,
    string NameAr,
    string Category,
    bool IsPaid,
    bool RequiresAttachment,
    bool AllowHalfDay,
    bool IsActive
);

public record LeaveBalanceDto(
    Guid Id,
    Guid TenantId,
    Guid EmploymentId,
    Guid LeaveTypeId,
    string LeaveTypeCode,
    string LeaveTypeNameEn,
    string LeaveTypeNameAr,
    int Year,
    decimal EntitledDays,
    decimal AccruedDays,
    decimal UsedDays,
    decimal PendingDays,
    decimal AvailableDays,
    uint RowVersion
);

public record LeaveRequestDto(
    Guid Id,
    Guid TenantId,
    Guid LegalEntityId,
    Guid EmploymentId,
    Guid LeaveTypeId,
    string LeaveTypeCode,
    string LeaveTypeNameEn,
    string LeaveTypeNameAr,
    string StartDate,
    string EndDate,
    decimal DurationDays,
    int DurationMinutes,
    string Status,
    string Reason,
    Guid? AttachmentDocumentId,
    Guid? ApprovalRequestId,
    string? RejectionReason,
    DateTime CreatedAt,
    uint RowVersion
);

public class LeaveRepository : ILeaveRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public LeaveRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<IReadOnlyList<LeaveTypeDto>> GetLeaveTypesAsync(TenantId tenantId, LegalEntityId legalEntityId)
    {
        var list = new List<LeaveTypeDto>();
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            SELECT id, tenant_id, legal_entity_id, code, name_en, name_ar, category,
                   is_paid, requires_attachment, allow_half_day, is_active
            FROM leave.leave_types
            WHERE tenant_id = $1 AND legal_entity_id = $2
            ORDER BY code ASC;
        """;
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(legalEntityId.Value);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new LeaveTypeDto(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                ((LeaveCategory)reader.GetInt32(6)).ToString(),
                reader.GetBoolean(7),
                reader.GetBoolean(8),
                reader.GetBoolean(9),
                reader.GetBoolean(10)
            ));
        }

        return list;
    }

    public async Task SaveLeaveTypeAsync(LeaveType leaveType)
    {
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            INSERT INTO leave.leave_types (
                id, tenant_id, legal_entity_id, code, name_en, name_ar, category,
                is_paid, requires_attachment, allow_half_day, is_active
            ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11)
            ON CONFLICT (tenant_id, legal_entity_id, code) DO UPDATE SET
                name_en = EXCLUDED.name_en,
                name_ar = EXCLUDED.name_ar,
                category = EXCLUDED.category,
                is_paid = EXCLUDED.is_paid,
                requires_attachment = EXCLUDED.requires_attachment,
                allow_half_day = EXCLUDED.allow_half_day,
                is_active = EXCLUDED.is_active;
        """;
        cmd.Parameters.AddWithValue(leaveType.Id);
        cmd.Parameters.AddWithValue(leaveType.TenantId.Value);
        cmd.Parameters.AddWithValue(leaveType.LegalEntityId.Value);
        cmd.Parameters.AddWithValue(leaveType.Code);
        cmd.Parameters.AddWithValue(leaveType.NameEn);
        cmd.Parameters.AddWithValue(leaveType.NameAr);
        cmd.Parameters.AddWithValue((int)leaveType.Category);
        cmd.Parameters.AddWithValue(leaveType.IsPaid);
        cmd.Parameters.AddWithValue(leaveType.RequiresAttachment);
        cmd.Parameters.AddWithValue(leaveType.AllowHalfDay);
        cmd.Parameters.AddWithValue(leaveType.IsActive);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<LeaveBalanceDto>> GetLeaveBalancesAsync(TenantId tenantId, Guid employmentId, int year)
    {
        var list = new List<LeaveBalanceDto>();
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            SELECT b.id, b.tenant_id, b.employment_id, b.leave_type_id, t.code, t.name_en, t.name_ar,
                   b.year, b.entitled_days, b.accrued_days, b.used_days, b.pending_days,
                   (b.accrued_days + b.entitled_days - b.used_days - b.pending_days) AS available_days,
                   b.row_version
            FROM leave.leave_balances b
            JOIN leave.leave_types t ON b.leave_type_id = t.id
            WHERE b.tenant_id = $1 AND b.employment_id = $2 AND b.year = $3
            ORDER BY t.code ASC;
        """;
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(employmentId);
        cmd.Parameters.AddWithValue(year);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new LeaveBalanceDto(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetGuid(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetInt32(7),
                reader.GetDecimal(8),
                reader.GetDecimal(9),
                reader.GetDecimal(10),
                reader.GetDecimal(11),
                reader.GetDecimal(12),
                (uint)reader.GetInt64(13)
            ));
        }

        return list;
    }

    public async Task<LeaveBalance?> GetLeaveBalanceEntityAsync(TenantId tenantId, Guid employmentId, Guid leaveTypeId, int year)
    {
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            SELECT id, tenant_id, employment_id, leave_type_id, year, entitled_days, accrued_days, used_days, pending_days
            FROM leave.leave_balances
            WHERE tenant_id = $1 AND employment_id = $2 AND leave_type_id = $3 AND year = $4;
        """;
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(employmentId);
        cmd.Parameters.AddWithValue(leaveTypeId);
        cmd.Parameters.AddWithValue(year);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new LeaveBalance(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.GetGuid(2),
                reader.GetGuid(3),
                reader.GetInt32(4),
                reader.GetDecimal(5),
                reader.GetDecimal(6),
                reader.GetDecimal(7),
                reader.GetDecimal(8)
            );
        }

        return null;
    }

    public async Task<LeaveBalance> GetOrCreateLeaveBalanceAsync(
        TenantId tenantId, Guid employmentId, Guid leaveTypeId, int year, decimal defaultEntitledDays)
    {
        var existing = await GetLeaveBalanceEntityAsync(tenantId, employmentId, leaveTypeId, year);
        if (existing != null) return existing;

        var newBalance = new LeaveBalance(Guid.NewGuid(), tenantId, employmentId, leaveTypeId, year, defaultEntitledDays);
        await SaveLeaveBalanceAsync(newBalance);
        return newBalance;
    }

    public async Task SaveLeaveBalanceAsync(LeaveBalance balance)
    {
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            INSERT INTO leave.leave_balances (
                id, tenant_id, employment_id, leave_type_id, year,
                entitled_days, accrued_days, used_days, pending_days, updated_at, row_version
            ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, CURRENT_TIMESTAMP, $10)
            ON CONFLICT (tenant_id, employment_id, leave_type_id, year) DO UPDATE SET
                entitled_days = EXCLUDED.entitled_days,
                accrued_days = EXCLUDED.accrued_days,
                used_days = EXCLUDED.used_days,
                pending_days = EXCLUDED.pending_days,
                updated_at = CURRENT_TIMESTAMP,
                row_version = leave.leave_balances.row_version + 1;
        """;
        cmd.Parameters.AddWithValue(balance.Id);
        cmd.Parameters.AddWithValue(balance.TenantId.Value);
        cmd.Parameters.AddWithValue(balance.EmploymentId);
        cmd.Parameters.AddWithValue(balance.LeaveTypeId);
        cmd.Parameters.AddWithValue(balance.Year);
        cmd.Parameters.AddWithValue(balance.EntitledDays);
        cmd.Parameters.AddWithValue(balance.AccruedDays);
        cmd.Parameters.AddWithValue(balance.UsedDays);
        cmd.Parameters.AddWithValue(balance.PendingDays);
        cmd.Parameters.AddWithValue((long)balance.RowVersion);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<(IReadOnlyList<LeaveRequestDto> Items, int TotalCount)> GetLeaveRequestsAsync(
        TenantId tenantId, LegalEntityId? legalEntityId, Guid? employmentId, int? status, int page = 1, int pageSize = 50)
    {
        var list = new List<LeaveRequestDto>();
        var offset = (Math.Max(1, page) - 1) * pageSize;

        await using var countCmd = _dataSource.CreateCommand();
        countCmd.CommandText = """
            SELECT COUNT(*) FROM leave.leave_requests
            WHERE tenant_id = $1
              AND ($2::uuid IS NULL OR legal_entity_id = $2)
              AND ($3::uuid IS NULL OR employment_id = $3)
              AND ($4::int IS NULL OR status = $4);
        """;
        countCmd.Parameters.AddWithValue(tenantId.Value);
        countCmd.Parameters.AddWithValue((object?)legalEntityId?.Value ?? DBNull.Value);
        countCmd.Parameters.AddWithValue((object?)employmentId ?? DBNull.Value);
        countCmd.Parameters.AddWithValue((object?)status ?? DBNull.Value);

        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            SELECT r.id, r.tenant_id, r.legal_entity_id, r.employment_id, r.leave_type_id,
                   t.code, t.name_en, t.name_ar, r.start_date, r.end_date, r.duration_days,
                   r.duration_minutes, r.status, r.reason, r.attachment_document_id,
                   r.approval_request_id, r.rejection_reason, r.created_at, r.row_version
            FROM leave.leave_requests r
            JOIN leave.leave_types t ON r.leave_type_id = t.id
            WHERE r.tenant_id = $1
              AND ($2::uuid IS NULL OR r.legal_entity_id = $2)
              AND ($3::uuid IS NULL OR r.employment_id = $3)
              AND ($4::int IS NULL OR r.status = $4)
            ORDER BY r.created_at DESC
            LIMIT $5 OFFSET $6;
        """;
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue((object?)legalEntityId?.Value ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)employmentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)status ?? DBNull.Value);
        cmd.Parameters.AddWithValue(pageSize);
        cmd.Parameters.AddWithValue(offset);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new LeaveRequestDto(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetGuid(3),
                reader.GetGuid(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetFieldValue<DateOnly>(8).ToString("yyyy-MM-dd"),
                reader.GetFieldValue<DateOnly>(9).ToString("yyyy-MM-dd"),
                reader.GetDecimal(10),
                reader.GetInt32(11),
                ((LeaveRequestStatus)reader.GetInt32(12)).ToString(),
                reader.GetString(13),
                reader.IsDBNull(14) ? null : reader.GetGuid(14),
                reader.IsDBNull(15) ? null : reader.GetGuid(15),
                reader.IsDBNull(16) ? null : reader.GetString(16),
                reader.GetDateTime(17),
                (uint)reader.GetInt64(18)
            ));
        }

        return (list, total);
    }

    public async Task<LeaveRequestDto?> GetLeaveRequestByIdAsync(TenantId tenantId, Guid id)
    {
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            SELECT r.id, r.tenant_id, r.legal_entity_id, r.employment_id, r.leave_type_id,
                   t.code, t.name_en, t.name_ar, r.start_date, r.end_date, r.duration_days,
                   r.duration_minutes, r.status, r.reason, r.attachment_document_id,
                   r.approval_request_id, r.rejection_reason, r.created_at, r.row_version
            FROM leave.leave_requests r
            JOIN leave.leave_types t ON r.leave_type_id = t.id
            WHERE r.tenant_id = $1 AND r.id = $2;
        """;
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new LeaveRequestDto(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetGuid(3),
                reader.GetGuid(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetFieldValue<DateOnly>(8).ToString("yyyy-MM-dd"),
                reader.GetFieldValue<DateOnly>(9).ToString("yyyy-MM-dd"),
                reader.GetDecimal(10),
                reader.GetInt32(11),
                ((LeaveRequestStatus)reader.GetInt32(12)).ToString(),
                reader.GetString(13),
                reader.IsDBNull(14) ? null : reader.GetGuid(14),
                reader.IsDBNull(15) ? null : reader.GetGuid(15),
                reader.IsDBNull(16) ? null : reader.GetString(16),
                reader.GetDateTime(17),
                (uint)reader.GetInt64(18)
            );
        }

        return null;
    }

    public async Task<LeaveRequest?> GetLeaveRequestEntityByIdAsync(TenantId tenantId, Guid id)
    {
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            SELECT id, tenant_id, legal_entity_id, employment_id, leave_type_id,
                   start_date, end_date, duration_days, reason, attachment_document_id
            FROM leave.leave_requests
            WHERE tenant_id = $1 AND id = $2;
        """;
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new LeaveRequest(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                new LegalEntityId(reader.GetGuid(2)),
                reader.GetGuid(3),
                reader.GetGuid(4),
                reader.GetFieldValue<DateOnly>(5),
                reader.GetFieldValue<DateOnly>(6),
                reader.GetDecimal(7),
                reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetGuid(9)
            );
        }

        return null;
    }

    public async Task SaveLeaveRequestAsync(LeaveRequest request)
    {
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            INSERT INTO leave.leave_requests (
                id, tenant_id, legal_entity_id, employment_id, leave_type_id,
                start_date, end_date, duration_days, duration_minutes, status, reason,
                attachment_document_id, approval_request_id, rejection_reason,
                created_at, updated_at, row_version
            ) VALUES (
                $1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14,
                CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, $15
            )
            ON CONFLICT (id) DO UPDATE SET
                status = EXCLUDED.status,
                approval_request_id = EXCLUDED.approval_request_id,
                rejection_reason = EXCLUDED.rejection_reason,
                updated_at = CURRENT_TIMESTAMP,
                row_version = leave.leave_requests.row_version + 1;
        """;
        cmd.Parameters.AddWithValue(request.Id);
        cmd.Parameters.AddWithValue(request.TenantId.Value);
        cmd.Parameters.AddWithValue(request.LegalEntityId.Value);
        cmd.Parameters.AddWithValue(request.EmploymentId);
        cmd.Parameters.AddWithValue(request.LeaveTypeId);
        cmd.Parameters.AddWithValue(request.StartDate);
        cmd.Parameters.AddWithValue(request.EndDate);
        cmd.Parameters.AddWithValue(request.DurationDays);
        cmd.Parameters.AddWithValue(request.DurationMinutes);
        cmd.Parameters.AddWithValue((int)request.Status);
        cmd.Parameters.AddWithValue(request.Reason);
        cmd.Parameters.AddWithValue((object?)request.AttachmentDocumentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)request.ApprovalRequestId ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)request.RejectionReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue((long)request.RowVersion);

        await cmd.ExecuteNonQueryAsync();
    }
}
