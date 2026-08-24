using System.Threading.Tasks;
using Npgsql;

namespace Workforce.Modules.Settlement.Infrastructure;

public static class SettlementMigrations
{
    public static async Task ApplyAsync(NpgsqlDataSource dataSource)
    {
        await using var cmd = dataSource.CreateCommand("""
            CREATE SCHEMA IF NOT EXISTS settlement;

            -- 1. Settlement Batches
            CREATE TABLE IF NOT EXISTS settlement.settlement_batches (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                legal_entity_id UUID NOT NULL,
                payroll_run_id UUID NOT NULL REFERENCES payroll.payroll_runs(id) ON DELETE RESTRICT,
                batch_number VARCHAR(100) NOT NULL,
                total_amount NUMERIC(14, 4) NOT NULL,
                currency VARCHAR(10) NOT NULL DEFAULT 'EGP',
                payment_date DATE NOT NULL,
                status INT NOT NULL DEFAULT 1,
                created_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                row_version BIGINT NOT NULL DEFAULT 1,
                CONSTRAINT uq_settlement_batch UNIQUE (tenant_id, legal_entity_id, batch_number)
            );

            CREATE INDEX IF NOT EXISTS ix_settlement_batches_query
                ON settlement.settlement_batches (tenant_id, legal_entity_id, status);

            -- 2. Payment Instructions
            CREATE TABLE IF NOT EXISTS settlement.payment_instructions (
                id UUID PRIMARY KEY,
                settlement_batch_id UUID NOT NULL REFERENCES settlement.settlement_batches(id) ON DELETE CASCADE,
                employment_id UUID NOT NULL,
                beneficiary_name VARCHAR(200) NOT NULL,
                bank_code VARCHAR(50) NOT NULL,
                encrypted_account_or_iban TEXT NOT NULL,
                amount NUMERIC(14, 4) NOT NULL,
                status INT NOT NULL DEFAULT 1
            );

            CREATE INDEX IF NOT EXISTS ix_payment_instructions_batch
                ON settlement.payment_instructions (settlement_batch_id);

            -- 3. Payment Exports
            CREATE TABLE IF NOT EXISTS settlement.payment_exports (
                id UUID PRIMARY KEY,
                settlement_batch_id UUID NOT NULL REFERENCES settlement.settlement_batches(id) ON DELETE CASCADE,
                format INT NOT NULL,
                storage_path TEXT NOT NULL,
                file_sha256 VARCHAR(128) NOT NULL,
                download_count INT NOT NULL DEFAULT 0,
                created_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            -- 4. Outbox Messages
            CREATE TABLE IF NOT EXISTS settlement.outbox_messages (
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
