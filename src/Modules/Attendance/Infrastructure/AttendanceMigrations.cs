using System.Threading.Tasks;
using Npgsql;

namespace Workforce.Modules.Attendance.Infrastructure;

public static class AttendanceMigrations
{
    public static async Task ApplyAsync(NpgsqlDataSource dataSource)
    {
        await using var cmd = dataSource.CreateCommand();
        cmd.CommandText = """
            CREATE SCHEMA IF NOT EXISTS attendance;

            -- 1. Immutable Raw Clock Events
            CREATE TABLE IF NOT EXISTS attendance.clock_events (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                employment_id UUID NOT NULL,
                type INT NOT NULL,
                source INT NOT NULL,
                captured_at_utc TIMESTAMPTZ NOT NULL,
                received_at_utc TIMESTAMPTZ NOT NULL,
                source_device_id VARCHAR(100),
                correlation_id VARCHAR(100),
                actor_user_id UUID,
                latitude DOUBLE PRECISION,
                longitude DOUBLE PRECISION
            );

            CREATE INDEX IF NOT EXISTS ix_clock_events_tenant_emp_captured
                ON attendance.clock_events (tenant_id, employment_id, captured_at_utc);

            -- 2. Work Schedules
            CREATE TABLE IF NOT EXISTS attendance.work_schedules (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                legal_entity_id UUID NOT NULL,
                code VARCHAR(50) NOT NULL,
                name_en VARCHAR(200) NOT NULL,
                name_ar VARCHAR(200) NOT NULL,
                shift_start_time TIME NOT NULL,
                shift_end_time TIME NOT NULL,
                grace_period_minutes INT NOT NULL DEFAULT 15,
                timezone_id VARCHAR(100) NOT NULL DEFAULT 'UTC',
                crosses_midnight BOOLEAN NOT NULL DEFAULT FALSE,
                effective_from DATE NOT NULL,
                effective_to DATE,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                CONSTRAINT uq_work_schedules_tenant_code UNIQUE (tenant_id, legal_entity_id, code)
            );

            -- 3. Attendance Days (Aggregate Root)
            CREATE TABLE IF NOT EXISTS attendance.attendance_days (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                legal_entity_id UUID NOT NULL,
                employment_id UUID NOT NULL,
                business_date DATE NOT NULL,
                timezone_id VARCHAR(100) NOT NULL DEFAULT 'UTC',
                status INT NOT NULL,
                scheduled_start_utc TIMESTAMPTZ,
                scheduled_end_utc TIMESTAMPTZ,
                scheduled_minutes INT NOT NULL DEFAULT 480,
                first_clock_in_utc TIMESTAMPTZ,
                last_clock_out_utc TIMESTAMPTZ,
                total_worked_minutes INT NOT NULL DEFAULT 0,
                late_minutes INT NOT NULL DEFAULT 0,
                early_departure_minutes INT NOT NULL DEFAULT 0,
                is_absent BOOLEAN NOT NULL DEFAULT FALSE,
                created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                row_version BIGINT NOT NULL DEFAULT 1,
                CONSTRAINT uq_attendance_days_emp_date UNIQUE (tenant_id, employment_id, business_date)
            );

            CREATE INDEX IF NOT EXISTS ix_attendance_days_query
                ON attendance.attendance_days (tenant_id, legal_entity_id, business_date, status);

            -- 4. Attendance Exceptions
            CREATE TABLE IF NOT EXISTS attendance.attendance_exceptions (
                id UUID PRIMARY KEY,
                attendance_day_id UUID NOT NULL REFERENCES attendance.attendance_days(id) ON DELETE CASCADE,
                tenant_id UUID NOT NULL,
                employment_id UUID NOT NULL,
                type INT NOT NULL,
                status INT NOT NULL,
                details TEXT NOT NULL,
                resolution_notes TEXT,
                resolved_by_user_id UUID,
                resolved_at_utc TIMESTAMPTZ,
                created_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            CREATE INDEX IF NOT EXISTS ix_attendance_exceptions_queue
                ON attendance.attendance_exceptions (tenant_id, status);

            -- 5. Attendance Adjustments
            CREATE TABLE IF NOT EXISTS attendance.attendance_adjustments (
                id UUID PRIMARY KEY,
                attendance_day_id UUID NOT NULL REFERENCES attendance.attendance_days(id) ON DELETE CASCADE,
                tenant_id UUID NOT NULL,
                employment_id UUID NOT NULL,
                adjusted_worked_minutes INT NOT NULL,
                reason TEXT NOT NULL,
                actor_user_id UUID NOT NULL,
                created_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                before_worked_minutes INT NOT NULL,
                after_worked_minutes INT NOT NULL,
                approval_request_id UUID
            );

            -- 6. Outbox Messages
            CREATE TABLE IF NOT EXISTS attendance.outbox_messages (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                event_type VARCHAR(200) NOT NULL,
                payload_json JSONB NOT NULL,
                occurred_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                processed_at_utc TIMESTAMPTZ
            );
        """;

        await cmd.ExecuteNonQueryAsync();
    }
}
