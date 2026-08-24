using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace Workforce.Modules.Audit.Infrastructure;

public static class AuditMigrations
{
    public static async Task ApplyMigrationsAsync(string connectionString, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            CREATE SCHEMA IF NOT EXISTS audit;

            CREATE TABLE IF NOT EXISTS audit.audit_records (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                legal_entity_id UUID NULL,
                actor_user_id UUID NOT NULL,
                actor_type VARCHAR(50) NOT NULL DEFAULT 'User',
                action_code VARCHAR(100) NOT NULL,
                entity_type VARCHAR(100) NOT NULL,
                entity_id VARCHAR(255) NOT NULL,
                occurred_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                correlation_id VARCHAR(100) NULL,
                ip_address VARCHAR(45) NULL,
                user_agent VARCHAR(500) NULL,
                reason_code VARCHAR(100) NULL,
                changes_before_json JSONB NULL,
                changes_after_json JSONB NULL,
                safe_metadata_json JSONB NULL,
                data_classification VARCHAR(50) NOT NULL DEFAULT 'Internal'
            );

            CREATE INDEX IF NOT EXISTS ix_audit_tenant_occurred ON audit.audit_records (tenant_id, occurred_at_utc DESC);
            CREATE INDEX IF NOT EXISTS ix_audit_tenant_actor ON audit.audit_records (tenant_id, actor_user_id);
            CREATE INDEX IF NOT EXISTS ix_audit_tenant_entity ON audit.audit_records (tenant_id, entity_type, entity_id);
            CREATE INDEX IF NOT EXISTS ix_audit_tenant_action ON audit.audit_records (tenant_id, action_code);
            CREATE INDEX IF NOT EXISTS ix_audit_tenant_correlation ON audit.audit_records (tenant_id, correlation_id);

            -- Rule to prevent any UPDATE or DELETE on audit table (Append-Only Enforcement)
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_trigger WHERE tgname = 'trg_audit_immutable'
                ) THEN
                    CREATE OR REPLACE FUNCTION audit.fn_prevent_audit_mutation()
                    RETURNS TRIGGER AS $func$
                    BEGIN
                        RAISE EXCEPTION 'Audit records are immutable and append-only. Modification or deletion is strictly prohibited.';
                    END;
                    $func$ LANGUAGE plpgsql;

                    CREATE TRIGGER trg_audit_immutable
                    BEFORE UPDATE OR DELETE ON audit.audit_records
                    FOR EACH ROW EXECUTE FUNCTION audit.fn_prevent_audit_mutation();
                END IF;
            END $$;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
