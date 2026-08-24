using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using Workforce.Modules.Attendance.Domain;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Attendance.Infrastructure;

public interface IAttendanceRepository
{
    Task RecordClockEventAsync(ClockEvent clockEvent);
    Task<IReadOnlyList<ClockEvent>> GetClockEventsAsync(TenantId tenantId, Guid employmentId, DateTime fromUtc, DateTime toUtc);
    Task<(IReadOnlyList<AttendanceDayDto> Items, int TotalCount)> GetAttendanceDaysAsync(TenantId tenantId, LegalEntityId? legalEntityId, DateOnly? fromDate, DateOnly? toDate, int? status, int page = 1, int pageSize = 50);
    Task<AttendanceDayDto?> GetAttendanceDayByIdAsync(TenantId tenantId, Guid id);
    Task<AttendanceDay?> GetAttendanceDayEntityByIdAsync(TenantId tenantId, Guid id);
    Task<AttendanceDay?> GetOrCreateAttendanceDayAsync(TenantId tenantId, LegalEntityId legalEntityId, Guid employmentId, DateOnly businessDate, string timeZoneId);
    Task SaveAttendanceDayAsync(AttendanceDay day);
    Task<(IReadOnlyList<AttendanceExceptionDto> Items, int TotalCount)> GetExceptionsQueueAsync(TenantId tenantId, int? status, int page = 1, int pageSize = 50);
    Task<bool> ResolveExceptionAsync(TenantId tenantId, Guid exceptionId, string notes, Guid resolvedByUserId);
    Task<IReadOnlyList<WorkSchedule>> GetWorkSchedulesAsync(TenantId tenantId, LegalEntityId legalEntityId);
    Task SaveWorkScheduleAsync(WorkSchedule schedule);
}

public record AttendanceDayDto(
    Guid Id,
    Guid TenantId,
    Guid LegalEntityId,
    Guid EmploymentId,
    string BusinessDate,
    string TimeZoneId,
    string Status,
    DateTime? FirstClockInUtc,
    DateTime? LastClockOutUtc,
    int ScheduledMinutes,
    int TotalWorkedMinutes,
    int LateMinutes,
    int EarlyDepartureMinutes,
    bool IsAbsent,
    uint RowVersion
);

public record AttendanceExceptionDto(
    Guid Id,
    Guid AttendanceDayId,
    Guid TenantId,
    Guid EmploymentId,
    string Type,
    string Status,
    string Details,
    string? ResolutionNotes,
    Guid? ResolvedByUserId,
    DateTime? ResolvedAtUtc,
    DateTime CreatedAtUtc
);

public class AttendanceRepository : IAttendanceRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public AttendanceRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task RecordClockEventAsync(ClockEvent clockEvent)
    {
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            INSERT INTO attendance.clock_events (
                id, tenant_id, employment_id, type, source, captured_at_utc, received_at_utc,
                source_device_id, correlation_id, actor_user_id, latitude, longitude
            ) VALUES (
                $1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12
            );
        """;
        cmd.Parameters.AddWithValue(clockEvent.Id);
        cmd.Parameters.AddWithValue(clockEvent.TenantId.Value);
        cmd.Parameters.AddWithValue(clockEvent.EmploymentId);
        cmd.Parameters.AddWithValue((int)clockEvent.Type);
        cmd.Parameters.AddWithValue((int)clockEvent.Source);
        cmd.Parameters.AddWithValue(clockEvent.CapturedAtUtc);
        cmd.Parameters.AddWithValue(clockEvent.ReceivedAtUtc);
        cmd.Parameters.AddWithValue((object?)clockEvent.SourceDeviceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)clockEvent.CorrelationId ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)clockEvent.ActorUserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)clockEvent.Latitude ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)clockEvent.Longitude ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<ClockEvent>> GetClockEventsAsync(TenantId tenantId, Guid employmentId, DateTime fromUtc, DateTime toUtc)
    {
        var list = new List<ClockEvent>();
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            SELECT id, tenant_id, employment_id, type, source, captured_at_utc, received_at_utc,
                   source_device_id, correlation_id, actor_user_id, latitude, longitude
            FROM attendance.clock_events
            WHERE tenant_id = $1 AND employment_id = $2 AND captured_at_utc >= $3 AND captured_at_utc <= $4
            ORDER BY captured_at_utc ASC;
        """;
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(employmentId);
        cmd.Parameters.AddWithValue(fromUtc);
        cmd.Parameters.AddWithValue(toUtc);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new ClockEvent(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.GetGuid(2),
                (ClockType)reader.GetInt32(3),
                (ClockSource)reader.GetInt32(4),
                reader.GetDateTime(5),
                reader.GetDateTime(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetGuid(9),
                reader.IsDBNull(10) ? null : reader.GetDouble(10),
                reader.IsDBNull(11) ? null : reader.GetDouble(11)
            ));
        }

        return list;
    }

    public async Task<(IReadOnlyList<AttendanceDayDto> Items, int TotalCount)> GetAttendanceDaysAsync(
        TenantId tenantId, LegalEntityId? legalEntityId, DateOnly? fromDate, DateOnly? toDate, int? status, int page = 1, int pageSize = 50)
    {
        var list = new List<AttendanceDayDto>();
        var from = fromDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var to = toDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var offset = (Math.Max(1, page) - 1) * pageSize;

        await using var countCmd = _dataSource.CreateCommand();
        countCmd.CommandText = """
            SELECT COUNT(*) FROM attendance.attendance_days
            WHERE tenant_id = $1
              AND ($2::uuid IS NULL OR legal_entity_id = $2)
              AND business_date >= $3 AND business_date <= $4
              AND ($5::int IS NULL OR status = $5);
        """;
        countCmd.Parameters.AddWithValue(tenantId.Value);
        countCmd.Parameters.AddWithValue((object?)legalEntityId?.Value ?? DBNull.Value);
        countCmd.Parameters.AddWithValue(from);
        countCmd.Parameters.AddWithValue(to);
        countCmd.Parameters.AddWithValue((object?)status ?? DBNull.Value);

        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            SELECT id, tenant_id, legal_entity_id, employment_id, business_date, timezone_id, status,
                   first_clock_in_utc, last_clock_out_utc, scheduled_minutes, total_worked_minutes,
                   late_minutes, early_departure_minutes, is_absent, row_version
            FROM attendance.attendance_days
            WHERE tenant_id = $1
              AND ($2::uuid IS NULL OR legal_entity_id = $2)
              AND business_date >= $3 AND business_date <= $4
              AND ($5::int IS NULL OR status = $5)
            ORDER BY business_date DESC, created_at DESC
            LIMIT $6 OFFSET $7;
        """;
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue((object?)legalEntityId?.Value ?? DBNull.Value);
        cmd.Parameters.AddWithValue(from);
        cmd.Parameters.AddWithValue(to);
        cmd.Parameters.AddWithValue((object?)status ?? DBNull.Value);
        cmd.Parameters.AddWithValue(pageSize);
        cmd.Parameters.AddWithValue(offset);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new AttendanceDayDto(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetGuid(3),
                reader.GetFieldValue<DateOnly>(4).ToString("yyyy-MM-dd"),
                reader.GetString(5),
                ((AttendanceStatus)reader.GetInt32(6)).ToString(),
                reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                reader.GetInt32(9),
                reader.GetInt32(10),
                reader.GetInt32(11),
                reader.GetInt32(12),
                reader.GetBoolean(13),
                (uint)reader.GetInt64(14)
            ));
        }

        return (list, total);
    }

    public async Task<AttendanceDayDto?> GetAttendanceDayByIdAsync(TenantId tenantId, Guid id)
    {
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            SELECT id, tenant_id, legal_entity_id, employment_id, business_date, timezone_id, status,
                   first_clock_in_utc, last_clock_out_utc, scheduled_minutes, total_worked_minutes,
                   late_minutes, early_departure_minutes, is_absent, row_version
            FROM attendance.attendance_days
            WHERE tenant_id = $1 AND id = $2;
        """;
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new AttendanceDayDto(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetGuid(3),
                reader.GetFieldValue<DateOnly>(4).ToString("yyyy-MM-dd"),
                reader.GetString(5),
                ((AttendanceStatus)reader.GetInt32(6)).ToString(),
                reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                reader.GetInt32(9),
                reader.GetInt32(10),
                reader.GetInt32(11),
                reader.GetInt32(12),
                reader.GetBoolean(13),
                (uint)reader.GetInt64(14)
            );
        }

        return null;
    }

    public async Task<AttendanceDay?> GetAttendanceDayEntityByIdAsync(TenantId tenantId, Guid id)
    {
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            SELECT id, tenant_id, legal_entity_id, employment_id, business_date, timezone_id, scheduled_minutes
            FROM attendance.attendance_days
            WHERE tenant_id = $1 AND id = $2;
        """;
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new AttendanceDay(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                new LegalEntityId(reader.GetGuid(2)),
                reader.GetGuid(3),
                reader.GetFieldValue<DateOnly>(4),
                reader.GetString(5),
                reader.GetInt32(6)
            );
        }

        return null;
    }

    public async Task<AttendanceDay?> GetOrCreateAttendanceDayAsync(
        TenantId tenantId, LegalEntityId legalEntityId, Guid employmentId, DateOnly businessDate, string timeZoneId)
    {
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            SELECT id, tenant_id, legal_entity_id, employment_id, business_date, timezone_id, scheduled_minutes
            FROM attendance.attendance_days
            WHERE tenant_id = $1 AND employment_id = $2 AND business_date = $3;
        """;
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(employmentId);
        cmd.Parameters.AddWithValue(businessDate);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new AttendanceDay(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                new LegalEntityId(reader.GetGuid(2)),
                reader.GetGuid(3),
                reader.GetFieldValue<DateOnly>(4),
                reader.GetString(5),
                reader.GetInt32(6)
            );
        }

        // Create new
        return new AttendanceDay(Guid.NewGuid(), tenantId, legalEntityId, employmentId, businessDate, timeZoneId);
    }

    public async Task SaveAttendanceDayAsync(AttendanceDay day)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();

        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO attendance.attendance_days (
                id, tenant_id, legal_entity_id, employment_id, business_date, timezone_id, status,
                scheduled_start_utc, scheduled_end_utc, scheduled_minutes, first_clock_in_utc, last_clock_out_utc,
                total_worked_minutes, late_minutes, early_departure_minutes, is_absent, updated_at, row_version
            ) VALUES (
                $1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14, $15, $16, CURRENT_TIMESTAMP, $17
            )
            ON CONFLICT (tenant_id, employment_id, business_date) DO UPDATE SET
                status = EXCLUDED.status,
                scheduled_minutes = EXCLUDED.scheduled_minutes,
                first_clock_in_utc = EXCLUDED.first_clock_in_utc,
                last_clock_out_utc = EXCLUDED.last_clock_out_utc,
                total_worked_minutes = EXCLUDED.total_worked_minutes,
                late_minutes = EXCLUDED.late_minutes,
                early_departure_minutes = EXCLUDED.early_departure_minutes,
                is_absent = EXCLUDED.is_absent,
                updated_at = CURRENT_TIMESTAMP,
                row_version = attendance.attendance_days.row_version + 1;
        """;
        cmd.Parameters.AddWithValue(day.Id);
        cmd.Parameters.AddWithValue(day.TenantId.Value);
        cmd.Parameters.AddWithValue(day.LegalEntityId.Value);
        cmd.Parameters.AddWithValue(day.EmploymentId);
        cmd.Parameters.AddWithValue(day.BusinessDate);
        cmd.Parameters.AddWithValue(day.TimeZoneId);
        cmd.Parameters.AddWithValue((int)day.Status);
        cmd.Parameters.AddWithValue((object?)day.ScheduledStartUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)day.ScheduledEndUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue(day.ScheduledMinutes);
        cmd.Parameters.AddWithValue((object?)day.FirstClockInUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)day.LastClockOutUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue(day.TotalWorkedMinutes);
        cmd.Parameters.AddWithValue(day.LateMinutes);
        cmd.Parameters.AddWithValue(day.EarlyDepartureMinutes);
        cmd.Parameters.AddWithValue(day.IsAbsent);
        cmd.Parameters.AddWithValue((long)day.RowVersion);

        await cmd.ExecuteNonQueryAsync();

        // Insert new adjustments if any
        foreach (var adj in day.Adjustments)
        {
            await using var adjCmd = conn.CreateCommand();
            adjCmd.Transaction = tx;
            adjCmd.CommandText = """
                INSERT INTO attendance.attendance_adjustments (
                    id, attendance_day_id, tenant_id, employment_id, adjusted_worked_minutes,
                    reason, actor_user_id, created_at_utc, before_worked_minutes, after_worked_minutes, approval_request_id
                ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11)
                ON CONFLICT (id) DO NOTHING;
            """;
            adjCmd.Parameters.AddWithValue(adj.Id);
            adjCmd.Parameters.AddWithValue(adj.AttendanceDayId);
            adjCmd.Parameters.AddWithValue(adj.TenantId.Value);
            adjCmd.Parameters.AddWithValue(adj.EmploymentId);
            adjCmd.Parameters.AddWithValue(adj.AdjustedWorkedMinutes);
            adjCmd.Parameters.AddWithValue(adj.Reason);
            adjCmd.Parameters.AddWithValue(adj.ActorUserId);
            adjCmd.Parameters.AddWithValue(adj.CreatedAtUtc);
            adjCmd.Parameters.AddWithValue(adj.BeforeWorkedMinutes);
            adjCmd.Parameters.AddWithValue(adj.AfterWorkedMinutes);
            adjCmd.Parameters.AddWithValue((object?)adj.ApprovalRequestId ?? DBNull.Value);
            await adjCmd.ExecuteNonQueryAsync();
        }

        // Insert new exceptions if any
        foreach (var ex in day.Exceptions)
        {
            await using var exCmd = conn.CreateCommand();
            exCmd.Transaction = tx;
            exCmd.CommandText = """
                INSERT INTO attendance.attendance_exceptions (
                    id, attendance_day_id, tenant_id, employment_id, type, status, details,
                    resolution_notes, resolved_by_user_id, resolved_at_utc, created_at_utc
                ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11)
                ON CONFLICT (id) DO UPDATE SET
                    status = EXCLUDED.status,
                    resolution_notes = EXCLUDED.resolution_notes,
                    resolved_by_user_id = EXCLUDED.resolved_by_user_id,
                    resolved_at_utc = EXCLUDED.resolved_at_utc;
            """;
            exCmd.Parameters.AddWithValue(ex.Id);
            exCmd.Parameters.AddWithValue(ex.AttendanceDayId);
            exCmd.Parameters.AddWithValue(ex.TenantId.Value);
            exCmd.Parameters.AddWithValue(ex.EmploymentId);
            exCmd.Parameters.AddWithValue((int)ex.Type);
            exCmd.Parameters.AddWithValue((int)ex.Status);
            exCmd.Parameters.AddWithValue(ex.Details);
            exCmd.Parameters.AddWithValue((object?)ex.ResolutionNotes ?? DBNull.Value);
            exCmd.Parameters.AddWithValue((object?)ex.ResolvedByUserId ?? DBNull.Value);
            exCmd.Parameters.AddWithValue((object?)ex.ResolvedAtUtc ?? DBNull.Value);
            exCmd.Parameters.AddWithValue(ex.CreatedAtUtc);
            await exCmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
    }

    public async Task<(IReadOnlyList<AttendanceExceptionDto> Items, int TotalCount)> GetExceptionsQueueAsync(
        TenantId tenantId, int? status, int page = 1, int pageSize = 50)
    {
        var list = new List<AttendanceExceptionDto>();
        var offset = (Math.Max(1, page) - 1) * pageSize;

        await using var countCmd = _dataSource.CreateCommand();
        countCmd.CommandText = """
            SELECT COUNT(*) FROM attendance.attendance_exceptions
            WHERE tenant_id = $1 AND ($2::int IS NULL OR status = $2);
        """;
        countCmd.Parameters.AddWithValue(tenantId.Value);
        countCmd.Parameters.AddWithValue((object?)status ?? DBNull.Value);

        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            SELECT id, attendance_day_id, tenant_id, employment_id, type, status, details,
                   resolution_notes, resolved_by_user_id, resolved_at_utc, created_at_utc
            FROM attendance.attendance_exceptions
            WHERE tenant_id = $1 AND ($2::int IS NULL OR status = $2)
            ORDER BY created_at_utc DESC
            LIMIT $3 OFFSET $4;
        """;
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue((object?)status ?? DBNull.Value);
        cmd.Parameters.AddWithValue(pageSize);
        cmd.Parameters.AddWithValue(offset);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new AttendanceExceptionDto(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetGuid(3),
                ((AttendanceExceptionType)reader.GetInt32(4)).ToString(),
                ((AttendanceExceptionStatus)reader.GetInt32(5)).ToString(),
                reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetGuid(8),
                reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                reader.GetDateTime(10)
            ));
        }

        return (list, total);
    }

    public async Task<bool> ResolveExceptionAsync(TenantId tenantId, Guid exceptionId, string notes, Guid resolvedByUserId)
    {
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            UPDATE attendance.attendance_exceptions
            SET status = $1, resolution_notes = $2, resolved_by_user_id = $3, resolved_at_utc = CURRENT_TIMESTAMP
            WHERE tenant_id = $4 AND id = $5;
        """;
        cmd.Parameters.AddWithValue((int)AttendanceExceptionStatus.Resolved);
        cmd.Parameters.AddWithValue(notes.Trim());
        cmd.Parameters.AddWithValue(resolvedByUserId);
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(exceptionId);

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<IReadOnlyList<WorkSchedule>> GetWorkSchedulesAsync(TenantId tenantId, LegalEntityId legalEntityId)
    {
        var list = new List<WorkSchedule>();
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            SELECT id, tenant_id, legal_entity_id, code, name_en, name_ar, shift_start_time,
                   shift_end_time, grace_period_minutes, timezone_id, effective_from, effective_to, is_active
            FROM attendance.work_schedules
            WHERE tenant_id = $1 AND legal_entity_id = $2
            ORDER BY code ASC;
        """;
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(legalEntityId.Value);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new WorkSchedule(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                new LegalEntityId(reader.GetGuid(2)),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetFieldValue<TimeOnly>(6),
                reader.GetFieldValue<TimeOnly>(7),
                reader.GetInt32(8),
                reader.GetString(9),
                new EffectivePeriod(reader.GetFieldValue<DateOnly>(10), reader.IsDBNull(11) ? null : reader.GetFieldValue<DateOnly>(11)),
                reader.GetBoolean(12)
            ));
        }

        return list;
    }

    public async Task SaveWorkScheduleAsync(WorkSchedule schedule)
    {
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            INSERT INTO attendance.work_schedules (
                id, tenant_id, legal_entity_id, code, name_en, name_ar, shift_start_time,
                shift_end_time, grace_period_minutes, timezone_id, crosses_midnight,
                effective_from, effective_to, is_active
            ) VALUES (
                $1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14
            )
            ON CONFLICT (tenant_id, legal_entity_id, code) DO UPDATE SET
                name_en = EXCLUDED.name_en,
                name_ar = EXCLUDED.name_ar,
                shift_start_time = EXCLUDED.shift_start_time,
                shift_end_time = EXCLUDED.shift_end_time,
                grace_period_minutes = EXCLUDED.grace_period_minutes,
                timezone_id = EXCLUDED.timezone_id,
                crosses_midnight = EXCLUDED.crosses_midnight,
                effective_from = EXCLUDED.effective_from,
                effective_to = EXCLUDED.effective_to,
                is_active = EXCLUDED.is_active;
        """;
        cmd.Parameters.AddWithValue(schedule.Id);
        cmd.Parameters.AddWithValue(schedule.TenantId.Value);
        cmd.Parameters.AddWithValue(schedule.LegalEntityId.Value);
        cmd.Parameters.AddWithValue(schedule.Code);
        cmd.Parameters.AddWithValue(schedule.NameEn);
        cmd.Parameters.AddWithValue(schedule.NameAr);
        cmd.Parameters.AddWithValue(schedule.ShiftStartTime);
        cmd.Parameters.AddWithValue(schedule.ShiftEndTime);
        cmd.Parameters.AddWithValue(schedule.GracePeriodMinutes);
        cmd.Parameters.AddWithValue(schedule.TimeZoneId);
        cmd.Parameters.AddWithValue(schedule.CrossesMidnight);
        cmd.Parameters.AddWithValue(schedule.EffectivePeriod.EffectiveFrom);
        cmd.Parameters.AddWithValue((object?)schedule.EffectivePeriod.EffectiveTo ?? DBNull.Value);
        cmd.Parameters.AddWithValue(schedule.IsActive);

        await cmd.ExecuteNonQueryAsync();
    }
}
