using System.Threading.Tasks;
using Npgsql;

namespace Workforce.Modules.Leave.Infrastructure;

public static class LeaveMigrations
{
    public static async Task ApplyAsync(NpgsqlDataSource dataSource)
    {
        await using var cmd = dataSource.CreateCommand();
        cmd.CommandText = """
            CREATE SCHEMA IF NOT EXISTS leave;

            -- btree_gist extension for PostgreSQL exclusion constraint
            CREATE EXTENSION IF NOT EXISTS btree_gist;

            -- 1. Leave Types
            CREATE TABLE IF NOT EXISTS leave.leave_types (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                legal_entity_id UUID NOT NULL,
                code VARCHAR(50) NOT NULL,
                name_en VARCHAR(200) NOT NULL,
                name_ar VARCHAR(200) NOT NULL,
                category INT NOT NULL,
                is_paid BOOLEAN NOT NULL DEFAULT TRUE,
                requires_attachment BOOLEAN NOT NULL DEFAULT FALSE,
                allow_half_day BOOLEAN NOT NULL DEFAULT TRUE,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                CONSTRAINT uq_leave_types_tenant_code UNIQUE (tenant_id, legal_entity_id, code)
            );

            -- 2. Leave Policies
            CREATE TABLE IF NOT EXISTS leave.leave_policies (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                leave_type_id UUID NOT NULL REFERENCES leave.leave_types(id) ON DELETE CASCADE,
                accrual_rate_per_year NUMERIC(6, 2) NOT NULL DEFAULT 21.00,
                max_carry_forward_days NUMERIC(6, 2) NOT NULL DEFAULT 5.00,
                probation_wait_days INT NOT NULL DEFAULT 90,
                effective_from DATE NOT NULL,
                effective_to DATE,
                policy_version INT NOT NULL DEFAULT 1
            );

            -- 3. Leave Balances
            CREATE TABLE IF NOT EXISTS leave.leave_balances (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                employment_id UUID NOT NULL,
                leave_type_id UUID NOT NULL REFERENCES leave.leave_types(id) ON DELETE CASCADE,
                year INT NOT NULL,
                entitled_days NUMERIC(6, 2) NOT NULL DEFAULT 0.00,
                accrued_days NUMERIC(6, 2) NOT NULL DEFAULT 0.00,
                used_days NUMERIC(6, 2) NOT NULL DEFAULT 0.00,
                pending_days NUMERIC(6, 2) NOT NULL DEFAULT 0.00,
                updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                row_version BIGINT NOT NULL DEFAULT 1,
                CONSTRAINT uq_leave_balances_emp_type_year UNIQUE (tenant_id, employment_id, leave_type_id, year)
            );

            -- 4. Leave Requests (with Non-Overlapping Exclusion Constraint)
            CREATE TABLE IF NOT EXISTS leave.leave_requests (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                legal_entity_id UUID NOT NULL,
                employment_id UUID NOT NULL,
                leave_type_id UUID NOT NULL REFERENCES leave.leave_types(id) ON DELETE RESTRICT,
                start_date DATE NOT NULL,
                end_date DATE NOT NULL,
                duration_days NUMERIC(6, 2) NOT NULL,
                duration_minutes INT NOT NULL,
                status INT NOT NULL,
                reason TEXT NOT NULL,
                attachment_document_id UUID,
                approval_request_id UUID,
                rejection_reason TEXT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                row_version BIGINT NOT NULL DEFAULT 1,
                CONSTRAINT chk_leave_request_dates CHECK (end_date >= start_date),
                CONSTRAINT ex_leave_request_no_overlap EXCLUDE USING gist (
                    employment_id WITH =,
                    daterange(start_date, end_date, '[]') WITH &&
                ) WHERE (status IN (2, 3, 4))
            );

            CREATE INDEX IF NOT EXISTS ix_leave_requests_query
                ON leave.leave_requests (tenant_id, legal_entity_id, employment_id, status);

            -- 4b. Auditable balance transactions. Mutable balance projections remain
            -- query-optimized state; workflow reservations, approvals, releases,
            -- and cancellations are recorded here as the authoritative trail.
            -- Future accrual/adjustment writers must use the same transaction contract.
            CREATE TABLE IF NOT EXISTS leave.leave_transactions (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                legal_entity_id UUID NOT NULL,
                employment_id UUID NOT NULL,
                leave_type_id UUID NOT NULL REFERENCES leave.leave_types(id) ON DELETE RESTRICT,
                leave_request_id UUID NULL REFERENCES leave.leave_requests(id) ON DELETE SET NULL,
                balance_year INT NOT NULL DEFAULT 0,
                transaction_type VARCHAR(60) NOT NULL,
                transaction_days NUMERIC(6, 2) NOT NULL,
                used_days_before NUMERIC(6, 2) NOT NULL,
                used_days_after NUMERIC(6, 2) NOT NULL,
                pending_days_before NUMERIC(6, 2) NOT NULL,
                pending_days_after NUMERIC(6, 2) NOT NULL,
                actor_user_id UUID NULL,
                reason TEXT NOT NULL DEFAULT '',
                occurred_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            ALTER TABLE leave.leave_transactions
                ADD COLUMN IF NOT EXISTS balance_year INT NOT NULL DEFAULT 0;

            DROP INDEX IF EXISTS leave.uq_leave_transactions_request_type;
            CREATE UNIQUE INDEX IF NOT EXISTS uq_leave_transactions_request_type_year
                ON leave.leave_transactions (leave_request_id, transaction_type, balance_year)
                WHERE leave_request_id IS NOT NULL;
            CREATE INDEX IF NOT EXISTS ix_leave_transactions_scope
                ON leave.leave_transactions (tenant_id, legal_entity_id, employment_id, occurred_at_utc DESC);

            -- 5. Outbox Messages
            CREATE TABLE IF NOT EXISTS leave.outbox_messages (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                event_type VARCHAR(200) NOT NULL,
                payload_json JSONB NOT NULL,
                occurred_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                processed_at_utc TIMESTAMPTZ
            );

            -- 6. Inbox Processed Messages (Consumer Idempotency Key)
            CREATE TABLE IF NOT EXISTS leave.inbox_processed_messages (
                message_id UUID PRIMARY KEY,
                event_type VARCHAR(200) NOT NULL,
                processed_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
        """;

        await cmd.ExecuteNonQueryAsync();
    }
}
