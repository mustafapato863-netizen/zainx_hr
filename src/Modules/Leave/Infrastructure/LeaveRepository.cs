using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;
using Workforce.Modules.Leave.Application.Contracts;
using Workforce.Modules.Leave.Domain;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Leave.Infrastructure;

public interface ILeaveRepository
{
    Task<IReadOnlyList<LeaveTypeDto>> GetLeaveTypesAsync(TenantId tenantId, LegalEntityId legalEntityId);
    Task SaveLeaveTypeAsync(LeaveType leaveType);
    Task<IReadOnlyList<LeaveBalanceDto>> GetLeaveBalancesAsync(TenantId tenantId, Guid employmentId, int year, LegalEntityId? legalEntityId = null);
    Task<LeaveBalance?> GetLeaveBalanceEntityAsync(TenantId tenantId, Guid employmentId, Guid leaveTypeId, int year);
    Task<LeaveBalance> GetOrCreateLeaveBalanceAsync(TenantId tenantId, Guid employmentId, Guid leaveTypeId, int year, decimal defaultEntitledDays);
    Task SaveLeaveBalanceAsync(LeaveBalance balance);
    Task<(IReadOnlyList<LeaveRequestDto> Items, int TotalCount)> GetLeaveRequestsAsync(TenantId tenantId, LegalEntityId? legalEntityId, Guid? employmentId, int? status, int page = 1, int pageSize = 50);
    Task<LeaveRequestDto?> GetLeaveRequestByIdAsync(TenantId tenantId, Guid id, LegalEntityId? legalEntityId = null);
    Task<LeaveRequest?> GetLeaveRequestEntityByIdAsync(TenantId tenantId, Guid id, LegalEntityId? legalEntityId = null);
    Task SaveLeaveRequestAsync(LeaveRequest request);
    Task SaveSubmittedLeaveRequestAsync(LeaveRequest request, Guid? actorUserId = null, CancellationToken ct = default);
    Task<LeaveApprovalApplicationResult> ApplyApprovalDecisionAsync(ApplyLeaveApprovalDecisionCommand command, CancellationToken ct = default);
    Task<LeaveCancellationRepositoryResult> CancelApprovedLeaveRequestAsync(
        TenantId tenantId,
        Guid actorUserId,
        Guid requestId,
        uint expectedRowVersion,
        LegalEntityId? legalEntityId,
        CancellationToken ct = default);
    Task<LeaveApprovalApplicationResult> ApplyApprovalCancellationAsync(
        ApplyLeaveApprovalCancellationCommand command,
        CancellationToken ct = default);
}

public enum LeaveCancellationRepositoryOutcome
{
    Applied = 1,
    AlreadyCancelled = 2,
    NotFound = 3,
    PendingApprovalRequiresWorkflow = 4,
    InvalidState = 5
}

public sealed record LeaveCancellationRepositoryResult(
    LeaveCancellationRepositoryOutcome Outcome,
    Guid RequestId,
    LeaveRequestStatus? CurrentStatus,
    uint NewRowVersion,
    string Message,
    bool IsConcurrencyConflict);

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

    public async Task<IReadOnlyList<LeaveBalanceDto>> GetLeaveBalancesAsync(TenantId tenantId, Guid employmentId, int year, LegalEntityId? legalEntityId = null)
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
              AND ($4::uuid IS NULL OR t.legal_entity_id = $4)
            ORDER BY t.code ASC;
        """;
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(employmentId);
        cmd.Parameters.AddWithValue(year);
        cmd.Parameters.AddWithValue((object?)legalEntityId?.Value ?? DBNull.Value);

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
            SELECT id, tenant_id, employment_id, leave_type_id, year, entitled_days, accrued_days, used_days, pending_days, updated_at, row_version
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
            return LeaveBalance.Rehydrate(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.GetGuid(2),
                reader.GetGuid(3),
                reader.GetInt32(4),
                reader.GetDecimal(5),
                reader.GetDecimal(6),
                reader.GetDecimal(7),
                reader.GetDecimal(8),
                reader.GetDateTime(9),
                (uint)reader.GetInt64(10)
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

    public async Task<LeaveRequestDto?> GetLeaveRequestByIdAsync(TenantId tenantId, Guid id, LegalEntityId? legalEntityId = null)
    {
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            SELECT r.id, r.tenant_id, r.legal_entity_id, r.employment_id, r.leave_type_id,
                   t.code, t.name_en, t.name_ar, r.start_date, r.end_date, r.duration_days,
                   r.duration_minutes, r.status, r.reason, r.attachment_document_id,
                   r.approval_request_id, r.rejection_reason, r.created_at, r.row_version
            FROM leave.leave_requests r
            JOIN leave.leave_types t ON r.leave_type_id = t.id
            WHERE r.tenant_id = $1 AND r.id = $2
              AND ($3::uuid IS NULL OR r.legal_entity_id = $3);
        """;
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue((object?)legalEntityId?.Value ?? DBNull.Value);

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

    public async Task<LeaveRequest?> GetLeaveRequestEntityByIdAsync(TenantId tenantId, Guid id, LegalEntityId? legalEntityId = null)
    {
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            SELECT id, tenant_id, legal_entity_id, employment_id, leave_type_id,
                   start_date, end_date, duration_days, reason, attachment_document_id,
                   status, approval_request_id, rejection_reason, created_at, updated_at, row_version
            FROM leave.leave_requests
            WHERE tenant_id = $1 AND id = $2
              AND ($3::uuid IS NULL OR legal_entity_id = $3);
        """;
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue((object?)legalEntityId?.Value ?? DBNull.Value);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return LeaveRequest.Rehydrate(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                new LegalEntityId(reader.GetGuid(2)),
                reader.GetGuid(3),
                reader.GetGuid(4),
                reader.GetFieldValue<DateOnly>(5),
                reader.GetFieldValue<DateOnly>(6),
                reader.GetDecimal(7),
                reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetGuid(9),
                (LeaveRequestStatus)reader.GetInt32(10),
                reader.IsDBNull(11) ? null : reader.GetGuid(11),
                reader.IsDBNull(12) ? null : reader.GetString(12),
                reader.GetDateTime(13),
                reader.GetDateTime(14),
                (uint)reader.GetInt64(15));
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

    public async Task SaveSubmittedLeaveRequestAsync(LeaveRequest request, Guid? actorUserId = null, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            await using var balanceCmd = conn.CreateCommand();
            balanceCmd.Transaction = tx;
            balanceCmd.CommandText = """
                SELECT b.id, b.tenant_id, b.employment_id, b.leave_type_id, b.year,
                       b.entitled_days, b.accrued_days, b.used_days, b.pending_days,
                       b.updated_at, b.row_version
                FROM leave.leave_balances b
                INNER JOIN leave.leave_types t ON t.id = b.leave_type_id
                WHERE b.tenant_id = $1
                  AND b.employment_id = $2
                  AND b.leave_type_id = $3
                  AND b.year = $4
                  AND t.tenant_id = $1
                  AND t.legal_entity_id = $5
                  AND t.is_active = TRUE
                FOR UPDATE OF b;
            """;
            balanceCmd.Parameters.AddWithValue(request.TenantId.Value);
            balanceCmd.Parameters.AddWithValue(request.EmploymentId);
            balanceCmd.Parameters.AddWithValue(request.LeaveTypeId);
            balanceCmd.Parameters.AddWithValue(request.StartDate.Year);
            balanceCmd.Parameters.AddWithValue(request.LegalEntityId.Value);

            await using var balanceReader = await balanceCmd.ExecuteReaderAsync(ct);
            if (!await balanceReader.ReadAsync(ct))
            {
                throw new InvalidOperationException("A configured leave balance is required before submitting a leave request.");
            }

            var balance = LeaveBalance.Rehydrate(
                balanceReader.GetGuid(0),
                new TenantId(balanceReader.GetGuid(1)),
                balanceReader.GetGuid(2),
                balanceReader.GetGuid(3),
                balanceReader.GetInt32(4),
                balanceReader.GetDecimal(5),
                balanceReader.GetDecimal(6),
                balanceReader.GetDecimal(7),
                balanceReader.GetDecimal(8),
                balanceReader.GetDateTime(9),
                (uint)balanceReader.GetInt64(10));
            var expectedBalanceRowVersion = balance.RowVersion;
            var usedDaysBefore = balance.UsedDays;
            var pendingDaysBefore = balance.PendingDays;
            balance.ReservePendingDays(request.DurationDays, expectedBalanceRowVersion);
            await balanceReader.DisposeAsync();

            await using var requestCmd = conn.CreateCommand();
            requestCmd.Transaction = tx;
            requestCmd.CommandText = """
                INSERT INTO leave.leave_requests (
                    id, tenant_id, legal_entity_id, employment_id, leave_type_id,
                    start_date, end_date, duration_days, duration_minutes, status, reason,
                    attachment_document_id, approval_request_id, rejection_reason,
                    created_at, updated_at, row_version
                ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14,
                          CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, $15);
            """;
            requestCmd.Parameters.AddWithValue(request.Id);
            requestCmd.Parameters.AddWithValue(request.TenantId.Value);
            requestCmd.Parameters.AddWithValue(request.LegalEntityId.Value);
            requestCmd.Parameters.AddWithValue(request.EmploymentId);
            requestCmd.Parameters.AddWithValue(request.LeaveTypeId);
            requestCmd.Parameters.AddWithValue(request.StartDate);
            requestCmd.Parameters.AddWithValue(request.EndDate);
            requestCmd.Parameters.AddWithValue(request.DurationDays);
            requestCmd.Parameters.AddWithValue(request.DurationMinutes);
            requestCmd.Parameters.AddWithValue((int)request.Status);
            requestCmd.Parameters.AddWithValue(request.Reason);
            requestCmd.Parameters.AddWithValue((object?)request.AttachmentDocumentId ?? DBNull.Value);
            requestCmd.Parameters.AddWithValue((object?)request.ApprovalRequestId ?? DBNull.Value);
            requestCmd.Parameters.AddWithValue((object?)request.RejectionReason ?? DBNull.Value);
            requestCmd.Parameters.AddWithValue((long)request.RowVersion);
            await requestCmd.ExecuteNonQueryAsync(ct);

            await using var updateBalance = conn.CreateCommand();
            updateBalance.Transaction = tx;
            updateBalance.CommandText = """
                UPDATE leave.leave_balances
                SET pending_days = $1, updated_at = CURRENT_TIMESTAMP, row_version = row_version + 1
                WHERE id = $2 AND row_version = $3;
            """;
            updateBalance.Parameters.AddWithValue(balance.PendingDays);
            updateBalance.Parameters.AddWithValue(balance.Id);
            updateBalance.Parameters.AddWithValue((long)expectedBalanceRowVersion);
            if (await updateBalance.ExecuteNonQueryAsync(ct) != 1)
                throw new InvalidOperationException("Optimistic concurrency conflict on leave balance.");

            await InsertBalanceTransactionAsync(
                conn,
                tx,
                request.TenantId.Value,
                request.LegalEntityId.Value,
                request.EmploymentId,
                request.LeaveTypeId,
                request.Id,
                "ReservePending",
                request.DurationDays,
                usedDaysBefore,
                balance.UsedDays,
                pendingDaysBefore,
                balance.PendingDays,
                actorUserId,
                "Leave request submitted for approval.",
                ct);

            await InsertOutboxMessageAsync(
                conn,
                tx,
                request.TenantId.Value,
                "LeaveRequestSubmitted",
                request.Id,
                new { requestId = request.Id, approvalRequestId = request.ApprovalRequestId, request.EmploymentId, request.LeaveTypeId, request.DurationDays },
                ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<LeaveApprovalApplicationResult> ApplyApprovalDecisionAsync(
        ApplyLeaveApprovalDecisionCommand command,
        CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            await using var requestCmd = conn.CreateCommand();
            requestCmd.Transaction = tx;
            requestCmd.CommandText = """
                SELECT id, tenant_id, legal_entity_id, employment_id, leave_type_id,
                       start_date, end_date, duration_days, reason, attachment_document_id,
                       status, approval_request_id, rejection_reason, created_at, updated_at, row_version
                FROM leave.leave_requests
                WHERE tenant_id = $1 AND legal_entity_id = $2 AND id = $3
                FOR UPDATE;
            """;
            requestCmd.Parameters.AddWithValue(command.TenantId.Value);
            requestCmd.Parameters.AddWithValue(command.LegalEntityId.Value);
            requestCmd.Parameters.AddWithValue(command.RequestId);

            await using var requestReader = await requestCmd.ExecuteReaderAsync(ct);
            if (!await requestReader.ReadAsync(ct))
            {
                await tx.RollbackAsync(ct);
                return LeaveApprovalApplicationResult.NotFound;
            }

            var request = LeaveRequest.Rehydrate(
                requestReader.GetGuid(0),
                new TenantId(requestReader.GetGuid(1)),
                new LegalEntityId(requestReader.GetGuid(2)),
                requestReader.GetGuid(3),
                requestReader.GetGuid(4),
                requestReader.GetFieldValue<DateOnly>(5),
                requestReader.GetFieldValue<DateOnly>(6),
                requestReader.GetDecimal(7),
                requestReader.GetString(8),
                requestReader.IsDBNull(9) ? null : requestReader.GetGuid(9),
                (LeaveRequestStatus)requestReader.GetInt32(10),
                requestReader.IsDBNull(11) ? null : requestReader.GetGuid(11),
                requestReader.IsDBNull(12) ? null : requestReader.GetString(12),
                requestReader.GetDateTime(13),
                requestReader.GetDateTime(14),
                (uint)requestReader.GetInt64(15));
            await requestReader.DisposeAsync();

            if (request.ApprovalRequestId != command.ApprovalRequestId)
                throw new InvalidOperationException("Approval request does not belong to the leave request.");

            if (command.Decision == LeaveApprovalDecision.Approved && request.Status == LeaveRequestStatus.Approved ||
                command.Decision == LeaveApprovalDecision.Rejected && request.Status == LeaveRequestStatus.Rejected)
            {
                await tx.CommitAsync(ct);
                return LeaveApprovalApplicationResult.AlreadyApplied;
            }

            if (request.Status != LeaveRequestStatus.PendingApproval && request.Status != LeaveRequestStatus.Submitted)
                throw new InvalidOperationException($"Leave request is not awaiting approval; current status is '{request.Status}'.");

            await using var balanceCmd = conn.CreateCommand();
            balanceCmd.Transaction = tx;
            balanceCmd.CommandText = """
                SELECT id, tenant_id, employment_id, leave_type_id, year,
                       entitled_days, accrued_days, used_days, pending_days, updated_at, row_version
                FROM leave.leave_balances
                WHERE tenant_id = $1 AND employment_id = $2 AND leave_type_id = $3 AND year = $4
                FOR UPDATE;
            """;
            balanceCmd.Parameters.AddWithValue(command.TenantId.Value);
            balanceCmd.Parameters.AddWithValue(request.EmploymentId);
            balanceCmd.Parameters.AddWithValue(request.LeaveTypeId);
            balanceCmd.Parameters.AddWithValue(request.StartDate.Year);

            await using var balanceReader = await balanceCmd.ExecuteReaderAsync(ct);
            if (!await balanceReader.ReadAsync(ct))
                throw new InvalidOperationException("The configured leave balance no longer exists.");

            var balance = LeaveBalance.Rehydrate(
                balanceReader.GetGuid(0),
                new TenantId(balanceReader.GetGuid(1)),
                balanceReader.GetGuid(2),
                balanceReader.GetGuid(3),
                balanceReader.GetInt32(4),
                balanceReader.GetDecimal(5),
                balanceReader.GetDecimal(6),
                balanceReader.GetDecimal(7),
                balanceReader.GetDecimal(8),
                balanceReader.GetDateTime(9),
                (uint)balanceReader.GetInt64(10));
            await balanceReader.DisposeAsync();

            var requestRowVersion = request.RowVersion;
            var balanceRowVersion = balance.RowVersion;
            var usedDaysBefore = balance.UsedDays;
            var pendingDaysBefore = balance.PendingDays;
            var transactionType = command.Decision == LeaveApprovalDecision.Approved
                ? "Approve"
                : "RejectRelease";
            if (command.Decision == LeaveApprovalDecision.Approved)
            {
                request.Approve(requestRowVersion);
                balance.ConfirmApprovedDays(request.DurationDays, balanceRowVersion);
            }
            else
            {
                request.Reject(command.Reason ?? "Rejected by approver.", requestRowVersion);
                balance.ReleasePendingDays(request.DurationDays, balanceRowVersion);
            }

            await using var updateRequest = conn.CreateCommand();
            updateRequest.Transaction = tx;
            updateRequest.CommandText = """
                UPDATE leave.leave_requests
                SET status = $1, rejection_reason = $2, updated_at = CURRENT_TIMESTAMP, row_version = row_version + 1
                WHERE id = $3 AND tenant_id = $4 AND row_version = $5;
            """;
            updateRequest.Parameters.AddWithValue((int)request.Status);
            updateRequest.Parameters.AddWithValue((object?)request.RejectionReason ?? DBNull.Value);
            updateRequest.Parameters.AddWithValue(request.Id);
            updateRequest.Parameters.AddWithValue(request.TenantId.Value);
            updateRequest.Parameters.AddWithValue((long)requestRowVersion);
            if (await updateRequest.ExecuteNonQueryAsync(ct) != 1)
                throw new InvalidOperationException("Optimistic concurrency conflict on leave request.");

            await using var updateBalance = conn.CreateCommand();
            updateBalance.Transaction = tx;
            updateBalance.CommandText = """
                UPDATE leave.leave_balances
                SET used_days = $1, pending_days = $2, updated_at = CURRENT_TIMESTAMP, row_version = row_version + 1
                WHERE id = $3 AND row_version = $4;
            """;
            updateBalance.Parameters.AddWithValue(balance.UsedDays);
            updateBalance.Parameters.AddWithValue(balance.PendingDays);
            updateBalance.Parameters.AddWithValue(balance.Id);
            updateBalance.Parameters.AddWithValue((long)balanceRowVersion);
            if (await updateBalance.ExecuteNonQueryAsync(ct) != 1)
                throw new InvalidOperationException("Optimistic concurrency conflict on leave balance.");

            await InsertBalanceTransactionAsync(
                conn,
                tx,
                request.TenantId.Value,
                request.LegalEntityId.Value,
                request.EmploymentId,
                request.LeaveTypeId,
                request.Id,
                transactionType,
                request.DurationDays,
                usedDaysBefore,
                balance.UsedDays,
                pendingDaysBefore,
                balance.PendingDays,
                command.ActorUserId,
                command.Reason ?? (command.Decision == LeaveApprovalDecision.Approved
                    ? "Leave approved."
                    : "Leave rejected; pending balance released."),
                ct);

            await InsertOutboxMessageAsync(
                conn,
                tx,
                command.TenantId.Value,
                command.Decision == LeaveApprovalDecision.Approved ? "LeaveApproved" : "LeaveRejected",
                request.Id,
                new { requestId = request.Id, approvalRequestId = command.ApprovalRequestId, actorUserId = command.ActorUserId, reason = command.Reason },
                ct);

            await tx.CommitAsync(ct);
            return LeaveApprovalApplicationResult.Applied;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<LeaveCancellationRepositoryResult> CancelApprovedLeaveRequestAsync(
        TenantId tenantId,
        Guid actorUserId,
        Guid requestId,
        uint expectedRowVersion,
        LegalEntityId? legalEntityId,
        CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            await using var requestCmd = conn.CreateCommand();
            requestCmd.Transaction = tx;
            requestCmd.CommandText = """
                SELECT id, tenant_id, legal_entity_id, employment_id, leave_type_id,
                       start_date, end_date, duration_days, reason, attachment_document_id,
                       status, approval_request_id, rejection_reason, created_at, updated_at, row_version
                FROM leave.leave_requests
                WHERE tenant_id = $1 AND id = $2
                  AND ($3::uuid IS NULL OR legal_entity_id = $3)
                FOR UPDATE;
            """;
            requestCmd.Parameters.AddWithValue(tenantId.Value);
            requestCmd.Parameters.AddWithValue(requestId);
            requestCmd.Parameters.AddWithValue((object?)legalEntityId?.Value ?? DBNull.Value);

            await using var requestReader = await requestCmd.ExecuteReaderAsync(ct);
            if (!await requestReader.ReadAsync(ct))
            {
                await tx.RollbackAsync(ct);
                return new LeaveCancellationRepositoryResult(
                    LeaveCancellationRepositoryOutcome.NotFound,
                    requestId,
                    null,
                    0,
                    "Leave request not found or access denied.",
                    false);
            }

            var request = LeaveRequest.Rehydrate(
                requestReader.GetGuid(0),
                new TenantId(requestReader.GetGuid(1)),
                new LegalEntityId(requestReader.GetGuid(2)),
                requestReader.GetGuid(3),
                requestReader.GetGuid(4),
                requestReader.GetFieldValue<DateOnly>(5),
                requestReader.GetFieldValue<DateOnly>(6),
                requestReader.GetDecimal(7),
                requestReader.GetString(8),
                requestReader.IsDBNull(9) ? null : requestReader.GetGuid(9),
                (LeaveRequestStatus)requestReader.GetInt32(10),
                requestReader.IsDBNull(11) ? null : requestReader.GetGuid(11),
                requestReader.IsDBNull(12) ? null : requestReader.GetString(12),
                requestReader.GetDateTime(13),
                requestReader.GetDateTime(14),
                (uint)requestReader.GetInt64(15));
            await requestReader.DisposeAsync();

            if (request.RowVersion != expectedRowVersion)
            {
                await tx.RollbackAsync(ct);
                return new LeaveCancellationRepositoryResult(
                    LeaveCancellationRepositoryOutcome.InvalidState,
                    request.Id,
                    request.Status,
                    request.RowVersion,
                    "Concurrency conflict: leave request was updated by another process.",
                    true);
            }

            if (request.Status == LeaveRequestStatus.Cancelled)
            {
                await tx.CommitAsync(ct);
                return new LeaveCancellationRepositoryResult(
                    LeaveCancellationRepositoryOutcome.AlreadyCancelled,
                    request.Id,
                    request.Status,
                    request.RowVersion,
                    "Leave request is already cancelled.",
                    false);
            }

            if (request.Status is LeaveRequestStatus.PendingApproval or LeaveRequestStatus.Submitted)
            {
                await tx.CommitAsync(ct);
                return new LeaveCancellationRepositoryResult(
                    LeaveCancellationRepositoryOutcome.PendingApprovalRequiresWorkflow,
                    request.Id,
                    request.Status,
                    request.RowVersion,
                    "Cancel the linked approval workflow before cancelling a pending leave request.",
                    false);
            }

            if (request.Status != LeaveRequestStatus.Approved)
            {
                await tx.CommitAsync(ct);
                return new LeaveCancellationRepositoryResult(
                    LeaveCancellationRepositoryOutcome.InvalidState,
                    request.Id,
                    request.Status,
                    request.RowVersion,
                    $"Leave request cannot be cancelled from '{request.Status}'.",
                    false);
            }

            await using var balanceCmd = conn.CreateCommand();
            balanceCmd.Transaction = tx;
            balanceCmd.CommandText = """
                SELECT id, tenant_id, employment_id, leave_type_id, year,
                       entitled_days, accrued_days, used_days, pending_days, updated_at, row_version
                FROM leave.leave_balances
                WHERE tenant_id = $1 AND employment_id = $2 AND leave_type_id = $3 AND year = $4
                FOR UPDATE;
            """;
            balanceCmd.Parameters.AddWithValue(tenantId.Value);
            balanceCmd.Parameters.AddWithValue(request.EmploymentId);
            balanceCmd.Parameters.AddWithValue(request.LeaveTypeId);
            balanceCmd.Parameters.AddWithValue(request.StartDate.Year);

            await using var balanceReader = await balanceCmd.ExecuteReaderAsync(ct);
            if (!await balanceReader.ReadAsync(ct))
                throw new InvalidOperationException("The configured leave balance no longer exists.");

            var balance = LeaveBalance.Rehydrate(
                balanceReader.GetGuid(0),
                new TenantId(balanceReader.GetGuid(1)),
                balanceReader.GetGuid(2),
                balanceReader.GetGuid(3),
                balanceReader.GetInt32(4),
                balanceReader.GetDecimal(5),
                balanceReader.GetDecimal(6),
                balanceReader.GetDecimal(7),
                balanceReader.GetDecimal(8),
                balanceReader.GetDateTime(9),
                (uint)balanceReader.GetInt64(10));
            await balanceReader.DisposeAsync();

            var balanceRowVersion = balance.RowVersion;
            var usedDaysBefore = balance.UsedDays;
            var pendingDaysBefore = balance.PendingDays;
            request.Cancel(request.RowVersion);
            balance.CancelApprovedDays(request.DurationDays, balanceRowVersion);

            await using var updateRequest = conn.CreateCommand();
            updateRequest.Transaction = tx;
            updateRequest.CommandText = """
                UPDATE leave.leave_requests
                SET status = $1, updated_at = CURRENT_TIMESTAMP, row_version = row_version + 1
                WHERE id = $2 AND tenant_id = $3 AND row_version = $4;
            """;
            updateRequest.Parameters.AddWithValue((int)request.Status);
            updateRequest.Parameters.AddWithValue(request.Id);
            updateRequest.Parameters.AddWithValue(request.TenantId.Value);
            updateRequest.Parameters.AddWithValue((long)expectedRowVersion);
            if (await updateRequest.ExecuteNonQueryAsync(ct) != 1)
                throw new InvalidOperationException("Optimistic concurrency conflict on leave request.");

            await using var updateBalance = conn.CreateCommand();
            updateBalance.Transaction = tx;
            updateBalance.CommandText = """
                UPDATE leave.leave_balances
                SET used_days = $1, pending_days = $2, updated_at = CURRENT_TIMESTAMP, row_version = row_version + 1
                WHERE id = $3 AND row_version = $4;
            """;
            updateBalance.Parameters.AddWithValue(balance.UsedDays);
            updateBalance.Parameters.AddWithValue(balance.PendingDays);
            updateBalance.Parameters.AddWithValue(balance.Id);
            updateBalance.Parameters.AddWithValue((long)balanceRowVersion);
            if (await updateBalance.ExecuteNonQueryAsync(ct) != 1)
                throw new InvalidOperationException("Optimistic concurrency conflict on leave balance.");

            await InsertBalanceTransactionAsync(
                conn,
                tx,
                request.TenantId.Value,
                request.LegalEntityId.Value,
                request.EmploymentId,
                request.LeaveTypeId,
                request.Id,
                "CancelApproved",
                request.DurationDays,
                usedDaysBefore,
                balance.UsedDays,
                pendingDaysBefore,
                balance.PendingDays,
                actorUserId,
                "Approved leave cancelled; used balance reversed.",
                ct);

            await InsertOutboxMessageAsync(
                conn,
                tx,
                request.TenantId.Value,
                "LeaveCancelled",
                request.Id,
                new { requestId = request.Id, actorUserId, reason = "Approved leave cancelled." },
                ct);

            await tx.CommitAsync(ct);
            return new LeaveCancellationRepositoryResult(
                LeaveCancellationRepositoryOutcome.Applied,
                request.Id,
                request.Status,
                request.RowVersion,
                "Leave request cancelled successfully.",
                false);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<LeaveApprovalApplicationResult> ApplyApprovalCancellationAsync(
        ApplyLeaveApprovalCancellationCommand command,
        CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            await using var requestCmd = conn.CreateCommand();
            requestCmd.Transaction = tx;
            requestCmd.CommandText = """
                SELECT id, tenant_id, legal_entity_id, employment_id, leave_type_id,
                       start_date, end_date, duration_days, reason, attachment_document_id,
                       status, approval_request_id, rejection_reason, created_at, updated_at, row_version
                FROM leave.leave_requests
                WHERE tenant_id = $1 AND legal_entity_id = $2 AND approval_request_id = $3
                FOR UPDATE;
            """;
            requestCmd.Parameters.AddWithValue(command.TenantId.Value);
            requestCmd.Parameters.AddWithValue(command.LegalEntityId.Value);
            requestCmd.Parameters.AddWithValue(command.ApprovalRequestId);

            await using var requestReader = await requestCmd.ExecuteReaderAsync(ct);
            if (!await requestReader.ReadAsync(ct))
            {
                await tx.RollbackAsync(ct);
                return LeaveApprovalApplicationResult.NotFound;
            }

            var request = LeaveRequest.Rehydrate(
                requestReader.GetGuid(0),
                new TenantId(requestReader.GetGuid(1)),
                new LegalEntityId(requestReader.GetGuid(2)),
                requestReader.GetGuid(3),
                requestReader.GetGuid(4),
                requestReader.GetFieldValue<DateOnly>(5),
                requestReader.GetFieldValue<DateOnly>(6),
                requestReader.GetDecimal(7),
                requestReader.GetString(8),
                requestReader.IsDBNull(9) ? null : requestReader.GetGuid(9),
                (LeaveRequestStatus)requestReader.GetInt32(10),
                requestReader.IsDBNull(11) ? null : requestReader.GetGuid(11),
                requestReader.IsDBNull(12) ? null : requestReader.GetString(12),
                requestReader.GetDateTime(13),
                requestReader.GetDateTime(14),
                (uint)requestReader.GetInt64(15));
            await requestReader.DisposeAsync();

            if (request.ApprovalRequestId != command.ApprovalRequestId)
                throw new InvalidOperationException("Approval request does not belong to the leave request.");

            if (request.Status == LeaveRequestStatus.Cancelled)
            {
                await tx.CommitAsync(ct);
                return LeaveApprovalApplicationResult.AlreadyApplied;
            }

            if (request.Status is not (LeaveRequestStatus.PendingApproval or LeaveRequestStatus.Submitted))
                throw new InvalidOperationException($"Leave request cannot be cancelled from '{request.Status}'.");

            await using var balanceCmd = conn.CreateCommand();
            balanceCmd.Transaction = tx;
            balanceCmd.CommandText = """
                SELECT id, tenant_id, employment_id, leave_type_id, year,
                       entitled_days, accrued_days, used_days, pending_days, updated_at, row_version
                FROM leave.leave_balances
                WHERE tenant_id = $1 AND employment_id = $2 AND leave_type_id = $3 AND year = $4
                FOR UPDATE;
            """;
            balanceCmd.Parameters.AddWithValue(command.TenantId.Value);
            balanceCmd.Parameters.AddWithValue(request.EmploymentId);
            balanceCmd.Parameters.AddWithValue(request.LeaveTypeId);
            balanceCmd.Parameters.AddWithValue(request.StartDate.Year);

            await using var balanceReader = await balanceCmd.ExecuteReaderAsync(ct);
            if (!await balanceReader.ReadAsync(ct))
                throw new InvalidOperationException("The configured leave balance no longer exists.");

            var balance = LeaveBalance.Rehydrate(
                balanceReader.GetGuid(0),
                new TenantId(balanceReader.GetGuid(1)),
                balanceReader.GetGuid(2),
                balanceReader.GetGuid(3),
                balanceReader.GetInt32(4),
                balanceReader.GetDecimal(5),
                balanceReader.GetDecimal(6),
                balanceReader.GetDecimal(7),
                balanceReader.GetDecimal(8),
                balanceReader.GetDateTime(9),
                (uint)balanceReader.GetInt64(10));
            await balanceReader.DisposeAsync();

            var balanceRowVersion = balance.RowVersion;
            var usedDaysBefore = balance.UsedDays;
            var pendingDaysBefore = balance.PendingDays;
            request.Cancel(request.RowVersion);
            balance.ReleasePendingDays(request.DurationDays, balanceRowVersion);

            await using var updateRequest = conn.CreateCommand();
            updateRequest.Transaction = tx;
            updateRequest.CommandText = """
                UPDATE leave.leave_requests
                SET status = $1, updated_at = CURRENT_TIMESTAMP, row_version = row_version + 1
                WHERE id = $2 AND tenant_id = $3 AND row_version = $4;
            """;
            updateRequest.Parameters.AddWithValue((int)request.Status);
            updateRequest.Parameters.AddWithValue(request.Id);
            updateRequest.Parameters.AddWithValue(request.TenantId.Value);
            updateRequest.Parameters.AddWithValue((long)(request.RowVersion - 1));
            if (await updateRequest.ExecuteNonQueryAsync(ct) != 1)
                throw new InvalidOperationException("Optimistic concurrency conflict on leave request.");

            await using var updateBalance = conn.CreateCommand();
            updateBalance.Transaction = tx;
            updateBalance.CommandText = """
                UPDATE leave.leave_balances
                SET used_days = $1, pending_days = $2, updated_at = CURRENT_TIMESTAMP, row_version = $3
                WHERE id = $4 AND row_version = $5;
            """;
            updateBalance.Parameters.AddWithValue(balance.UsedDays);
            updateBalance.Parameters.AddWithValue(balance.PendingDays);
            updateBalance.Parameters.AddWithValue((long)balance.RowVersion);
            updateBalance.Parameters.AddWithValue(balance.Id);
            updateBalance.Parameters.AddWithValue((long)balanceRowVersion);
            if (await updateBalance.ExecuteNonQueryAsync(ct) != 1)
                throw new InvalidOperationException("Optimistic concurrency conflict on leave balance.");

            await InsertBalanceTransactionAsync(
                conn,
                tx,
                request.TenantId.Value,
                request.LegalEntityId.Value,
                request.EmploymentId,
                request.LeaveTypeId,
                request.Id,
                "CancelPending",
                request.DurationDays,
                usedDaysBefore,
                balance.UsedDays,
                pendingDaysBefore,
                balance.PendingDays,
                command.ActorUserId,
                command.Reason ?? "Pending leave request cancelled by requester.",
                ct);

            await InsertOutboxMessageAsync(
                conn,
                tx,
                request.TenantId.Value,
                "LeaveCancelled",
                request.Id,
                new { requestId = request.Id, approvalRequestId = command.ApprovalRequestId, actorUserId = command.ActorUserId, reason = command.Reason },
                ct);

            await tx.CommitAsync(ct);
            return LeaveApprovalApplicationResult.Applied;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static async Task InsertBalanceTransactionAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        Guid tenantId,
        Guid legalEntityId,
        Guid employmentId,
        Guid leaveTypeId,
        Guid? leaveRequestId,
        string transactionType,
        decimal transactionDays,
        decimal usedDaysBefore,
        decimal usedDaysAfter,
        decimal pendingDaysBefore,
        decimal pendingDaysAfter,
        Guid? actorUserId,
        string reason,
        CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO leave.leave_transactions (
                id, tenant_id, legal_entity_id, employment_id, leave_type_id, leave_request_id,
                transaction_type, transaction_days, used_days_before, used_days_after,
                pending_days_before, pending_days_after, actor_user_id, reason
            ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14);
        """;
        cmd.Parameters.AddWithValue(Guid.NewGuid());
        cmd.Parameters.AddWithValue(tenantId);
        cmd.Parameters.AddWithValue(legalEntityId);
        cmd.Parameters.AddWithValue(employmentId);
        cmd.Parameters.AddWithValue(leaveTypeId);
        cmd.Parameters.AddWithValue((object?)leaveRequestId ?? DBNull.Value);
        cmd.Parameters.AddWithValue(transactionType);
        cmd.Parameters.AddWithValue(transactionDays);
        cmd.Parameters.AddWithValue(usedDaysBefore);
        cmd.Parameters.AddWithValue(usedDaysAfter);
        cmd.Parameters.AddWithValue(pendingDaysBefore);
        cmd.Parameters.AddWithValue(pendingDaysAfter);
        cmd.Parameters.AddWithValue((object?)actorUserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue(reason);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task InsertOutboxMessageAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        Guid tenantId,
        string eventType,
        Guid aggregateId,
        object payload,
        CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO leave.outbox_messages (id, tenant_id, event_type, payload_json, occurred_at_utc)
            VALUES ($1, $2, $3, $4::jsonb, CURRENT_TIMESTAMP);
        """;
        cmd.Parameters.AddWithValue(Guid.NewGuid());
        cmd.Parameters.AddWithValue(tenantId);
        cmd.Parameters.AddWithValue(eventType);
        cmd.Parameters.AddWithValue(JsonSerializer.Serialize(new { aggregateId, payload }));
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
