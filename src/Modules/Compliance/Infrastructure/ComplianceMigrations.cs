using System.Threading.Tasks;
using Npgsql;

namespace Workforce.Modules.Compliance.Infrastructure;

public static class ComplianceMigrations
{
    public static async Task ApplyAsync(NpgsqlDataSource dataSource)
    {
        await using var cmd = dataSource.CreateCommand("""
            CREATE SCHEMA IF NOT EXISTS compliance;

            -- 1. Statutory Rules
            CREATE TABLE IF NOT EXISTS compliance.statutory_rules (
                id UUID PRIMARY KEY,
                jurisdiction INT NOT NULL,
                category INT NOT NULL,
                code VARCHAR(100) NOT NULL UNIQUE,
                name_en VARCHAR(200) NOT NULL,
                name_ar VARCHAR(200) NOT NULL,
                source_reference_law TEXT NOT NULL,
                is_verified BOOLEAN NOT NULL DEFAULT TRUE,
                created_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            -- 2. Statutory Rule Versions (Effective-Dated)
            CREATE TABLE IF NOT EXISTS compliance.statutory_rule_versions (
                id UUID PRIMARY KEY,
                rule_id UUID NOT NULL REFERENCES compliance.statutory_rules(id) ON DELETE CASCADE,
                version_number INT NOT NULL,
                effective_from DATE NOT NULL,
                effective_to DATE,
                parameters_json JSONB NOT NULL DEFAULT '{}',
                calculation_strategy_name VARCHAR(150) NOT NULL,
                status INT NOT NULL DEFAULT 1,
                created_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                CONSTRAINT uq_rule_version UNIQUE (rule_id, version_number)
            );

            -- 3. Compliance Validations
            CREATE TABLE IF NOT EXISTS compliance.compliance_validations (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                payroll_run_id UUID NOT NULL,
                employment_id UUID NOT NULL,
                rule_version_id UUID NOT NULL REFERENCES compliance.statutory_rule_versions(id) ON DELETE RESTRICT,
                is_passed BOOLEAN NOT NULL,
                severity VARCHAR(50) NOT NULL DEFAULT 'INFO',
                message TEXT NOT NULL,
                evaluated_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            CREATE INDEX IF NOT EXISTS ix_compliance_validations_query 
                ON compliance.compliance_validations (tenant_id, payroll_run_id, is_passed);

            -- 4. Outbox Messages
            CREATE TABLE IF NOT EXISTS compliance.outbox_messages (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                event_type VARCHAR(200) NOT NULL,
                payload_json JSONB NOT NULL,
                occurred_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                processed_at_utc TIMESTAMPTZ
            );
        """);

        await cmd.ExecuteNonQueryAsync();
    }
}
