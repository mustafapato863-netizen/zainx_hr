using System.Threading.Tasks;
using Npgsql;

namespace Workforce.Modules.Payroll.Infrastructure;

public static class PayrollMigrations
{
    public static async Task ApplyAsync(NpgsqlDataSource dataSource)
    {
        await using var cmd = dataSource.CreateCommand("""
            CREATE SCHEMA IF NOT EXISTS payroll;

            -- 1. Payroll Periods
            CREATE TABLE IF NOT EXISTS payroll.payroll_periods (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                legal_entity_id UUID NOT NULL,
                code VARCHAR(100) NOT NULL,
                period_start DATE NOT NULL,
                period_end DATE NOT NULL,
                payment_date DATE NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                CONSTRAINT uq_payroll_period_code UNIQUE (tenant_id, legal_entity_id, code),
                CONSTRAINT chk_payroll_period_dates CHECK (period_end >= period_start)
            );

            -- 2. Payroll Runs
            CREATE TABLE IF NOT EXISTS payroll.payroll_runs (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                legal_entity_id UUID NOT NULL,
                period_id UUID NOT NULL REFERENCES payroll.payroll_periods(id) ON DELETE RESTRICT,
                code VARCHAR(100) NOT NULL,
                status INT NOT NULL DEFAULT 1,
                currency VARCHAR(10) NOT NULL DEFAULT 'EGP',
                total_gross NUMERIC(14, 4) NOT NULL DEFAULT 0.0000,
                total_net NUMERIC(14, 4) NOT NULL DEFAULT 0.0000,
                total_employer_contributions NUMERIC(14, 4) NOT NULL DEFAULT 0.0000,
                employee_count INT NOT NULL DEFAULT 0,
                reproducibility_hash VARCHAR(128) NOT NULL DEFAULT '',
                approval_request_id UUID,
                finalized_at_utc TIMESTAMPTZ,
                finalized_by_user_id UUID,
                created_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                row_version BIGINT NOT NULL DEFAULT 1,
                CONSTRAINT uq_payroll_run_code UNIQUE (tenant_id, legal_entity_id, code)
            );

            CREATE INDEX IF NOT EXISTS ix_payroll_runs_query
                ON payroll.payroll_runs (tenant_id, legal_entity_id, period_id, status);

            -- 3. Payroll Input Snapshots (Immutable inputs used in calculation)
            CREATE TABLE IF NOT EXISTS payroll.payroll_input_snapshots (
                id UUID PRIMARY KEY,
                payroll_run_id UUID NOT NULL REFERENCES payroll.payroll_runs(id) ON DELETE CASCADE,
                employment_id UUID NOT NULL,
                base_salary_monthly NUMERIC(14, 4) NOT NULL,
                allowances_json JSONB NOT NULL DEFAULT '[]',
                scheduled_days INT NOT NULL,
                verified_worked_minutes INT NOT NULL,
                approved_absence_days NUMERIC(6, 2) NOT NULL DEFAULT 0.00,
                approved_leave_days NUMERIC(6, 2) NOT NULL DEFAULT 0.00,
                unpaid_leave_days NUMERIC(6, 2) NOT NULL DEFAULT 0.00,
                captured_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                CONSTRAINT uq_input_snapshot UNIQUE (payroll_run_id, employment_id)
            );

            -- 4. Payroll Employee Results
            CREATE TABLE IF NOT EXISTS payroll.payroll_employee_results (
                id UUID PRIMARY KEY,
                payroll_run_id UUID NOT NULL REFERENCES payroll.payroll_runs(id) ON DELETE CASCADE,
                employment_id UUID NOT NULL,
                gross_pay NUMERIC(14, 4) NOT NULL,
                net_pay NUMERIC(14, 4) NOT NULL,
                total_earnings NUMERIC(14, 4) NOT NULL,
                total_deductions NUMERIC(14, 4) NOT NULL,
                employer_contributions NUMERIC(14, 4) NOT NULL DEFAULT 0.0000,
                row_version BIGINT NOT NULL DEFAULT 1,
                CONSTRAINT uq_employee_result UNIQUE (payroll_run_id, employment_id)
            );

            CREATE INDEX IF NOT EXISTS ix_employee_results_run
                ON payroll.payroll_employee_results (payroll_run_id, employment_id);

            -- 5. Calculation Traces (Business explainability lines)
            CREATE TABLE IF NOT EXISTS payroll.calculation_traces (
                id UUID PRIMARY KEY,
                employee_result_id UUID NOT NULL REFERENCES payroll.payroll_employee_results(id) ON DELETE CASCADE,
                step_order INT NOT NULL,
                rule_reference VARCHAR(150) NOT NULL,
                description TEXT NOT NULL,
                formula_applied TEXT NOT NULL,
                input_values_json JSONB NOT NULL DEFAULT '{}',
                intermediate_amount NUMERIC(14, 4) NOT NULL,
                rounding_delta NUMERIC(14, 4) NOT NULL DEFAULT 0.0000,
                final_amount NUMERIC(14, 4) NOT NULL
            );

            -- 6. Payroll Lines (Itemized components)
            CREATE TABLE IF NOT EXISTS payroll.payroll_lines (
                id UUID PRIMARY KEY,
                employee_result_id UUID NOT NULL REFERENCES payroll.payroll_employee_results(id) ON DELETE CASCADE,
                component_code VARCHAR(100) NOT NULL,
                name_en VARCHAR(200) NOT NULL,
                name_ar VARCHAR(200) NOT NULL,
                category INT NOT NULL,
                amount NUMERIC(14, 4) NOT NULL,
                calculation_type INT NOT NULL,
                rate NUMERIC(14, 4) NOT NULL DEFAULT 0.0000,
                hours_or_days NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
                trace_id UUID REFERENCES payroll.calculation_traces(id) ON DELETE SET NULL
            );

            -- 7. Payroll Exceptions
            CREATE TABLE IF NOT EXISTS payroll.payroll_exceptions (
                id UUID PRIMARY KEY,
                payroll_run_id UUID NOT NULL REFERENCES payroll.payroll_runs(id) ON DELETE CASCADE,
                employment_id UUID NOT NULL,
                severity INT NOT NULL,
                category VARCHAR(100) NOT NULL,
                reason TEXT NOT NULL,
                resolution_guidance TEXT NOT NULL,
                status INT NOT NULL DEFAULT 1,
                resolved_by_user_id UUID,
                resolution_note TEXT,
                created_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            CREATE INDEX IF NOT EXISTS ix_payroll_exceptions_query
                ON payroll.payroll_exceptions (payroll_run_id, severity, status);

            -- 8. Outbox Messages
            CREATE TABLE IF NOT EXISTS payroll.outbox_messages (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                event_type VARCHAR(200) NOT NULL,
                payload_json JSONB NOT NULL,
                occurred_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                processed_at_utc TIMESTAMPTZ
            );

            -- 9. Inbox Processed Messages (Consumer Idempotency)
            CREATE TABLE IF NOT EXISTS payroll.inbox_processed_messages (
                message_id UUID PRIMARY KEY,
                event_type VARCHAR(200) NOT NULL,
                processed_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
        """);

        await cmd.ExecuteNonQueryAsync();
    }
}
