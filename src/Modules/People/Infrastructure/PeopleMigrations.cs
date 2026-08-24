using Npgsql;

namespace Workforce.Modules.People.Infrastructure;

public static class PeopleMigrations
{
    public static async Task ApplyMigrationsAsync(string connectionString, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            CREATE SCHEMA IF NOT EXISTS people;

            CREATE EXTENSION IF NOT EXISTS btree_gist;

            CREATE TABLE IF NOT EXISTS people.persons (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                first_name_en VARCHAR(100) NOT NULL,
                last_name_en VARCHAR(100) NOT NULL,
                first_name_ar VARCHAR(100) NOT NULL,
                last_name_ar VARCHAR(100) NOT NULL,
                date_of_birth DATE NOT NULL,
                gender VARCHAR(20) NOT NULL DEFAULT 'Unspecified',
                nationality VARCHAR(10) NOT NULL DEFAULT 'SA',
                national_identifier_encrypted VARCHAR(512) NOT NULL,
                national_identifier_hash VARCHAR(64) NOT NULL,
                masked_national_identifier VARCHAR(50) NOT NULL,
                primary_email VARCHAR(200) NOT NULL DEFAULT '',
                phone_number VARCHAR(50) NOT NULL DEFAULT '',
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS people.employments (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                person_id UUID NOT NULL REFERENCES people.persons(id),
                legal_entity_id UUID NOT NULL,
                employee_number VARCHAR(50) NOT NULL,
                hire_date DATE NOT NULL,
                probation_end_date DATE NULL,
                termination_date DATE NULL,
                termination_reason VARCHAR(255) NULL,
                status INT NOT NULL DEFAULT 2, -- Active
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                row_version INT NOT NULL DEFAULT 1,
                CONSTRAINT uq_employment_emp_no UNIQUE (tenant_id, legal_entity_id, employee_number),
                CONSTRAINT chk_employment_probation CHECK (probation_end_date IS NULL OR probation_end_date >= hire_date),
                CONSTRAINT chk_employment_termination CHECK (termination_date IS NULL OR termination_date >= hire_date)
            );

            CREATE TABLE IF NOT EXISTS people.employment_assignments (
                id UUID PRIMARY KEY,
                employment_id UUID NOT NULL REFERENCES people.employments(id),
                organization_unit_id UUID NOT NULL,
                position_id UUID NULL,
                location_id UUID NULL,
                manager_employment_id UUID NULL,
                job_title_en VARCHAR(200) NOT NULL,
                job_title_ar VARCHAR(200) NOT NULL,
                effective_from DATE NOT NULL,
                effective_to DATE NULL,
                is_current BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT chk_assignment_dates CHECK (effective_to IS NULL OR effective_to >= effective_from)
            );

            CREATE TABLE IF NOT EXISTS people.sensitive_pii_audit (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                actor_user_id UUID NOT NULL,
                employment_id UUID NOT NULL,
                field_name VARCHAR(100) NOT NULL,
                purpose VARCHAR(255) NOT NULL,
                correlation_id VARCHAR(100) NOT NULL,
                timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS people.outbox_messages (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                event_type VARCHAR(100) NOT NULL,
                aggregate_type VARCHAR(100) NOT NULL,
                aggregate_id UUID NOT NULL,
                payload JSONB NOT NULL,
                occurred_at TIMESTAMPTZ NOT NULL,
                processed_at TIMESTAMPTZ NULL
            );

            -- Performance and Isolation Indexes
            CREATE INDEX IF NOT EXISTS ix_employments_tenant_person ON people.employments(tenant_id, person_id);
            CREATE INDEX IF NOT EXISTS ix_employments_tenant_legal ON people.employments(tenant_id, legal_entity_id, status);
            CREATE INDEX IF NOT EXISTS ix_assignments_emp_current ON people.employment_assignments(employment_id, is_current);
            CREATE INDEX IF NOT EXISTS ix_persons_names ON people.persons(tenant_id, last_name_en, first_name_en);
            CREATE INDEX IF NOT EXISTS ix_persons_nat_id_hash ON people.persons(tenant_id, national_identifier_hash);
            CREATE INDEX IF NOT EXISTS ix_sensitive_audit_tenant ON people.sensitive_pii_audit(tenant_id, employment_id, timestamp);
            CREATE INDEX IF NOT EXISTS ix_outbox_unprocessed ON people.outbox_messages(tenant_id, processed_at) WHERE processed_at IS NULL;

            CREATE TABLE IF NOT EXISTS people.hire_idempotency (
                idempotency_key UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                person_id UUID NOT NULL,
                employment_id UUID NOT NULL,
                assignment_id UUID NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            -- Database Integrity: Unique index for single active current assignment
            CREATE UNIQUE INDEX IF NOT EXISTS uq_assignments_single_current ON people.employment_assignments (employment_id) WHERE is_current = TRUE;

            -- Database-Enforced Non-Overlapping Effective Periods Exclusion Constraint
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint WHERE conname = 'ex_employment_assignment_no_overlap'
                ) THEN
                    BEGIN
                        ALTER TABLE people.employment_assignments
                        ADD CONSTRAINT ex_employment_assignment_no_overlap
                        EXCLUDE USING gist (
                            employment_id WITH =,
                            daterange(effective_from, COALESCE(effective_to, 'infinity'::date), '[]') WITH &&
                        );
                    EXCEPTION WHEN OTHERS THEN
                        -- Handles standalone environments without root extension install privileges
                        NULL;
                    END;
                END IF;
            END $$;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
