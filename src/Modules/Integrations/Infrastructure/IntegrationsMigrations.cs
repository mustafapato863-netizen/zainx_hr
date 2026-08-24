using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace Workforce.Modules.Integrations.Infrastructure;

public static class IntegrationsMigrations
{
    public static async Task ApplyMigrationsAsync(string connectionString, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            CREATE SCHEMA IF NOT EXISTS integrations;

            CREATE TABLE IF NOT EXISTS integrations.connectors (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                code VARCHAR(100) NOT NULL,
                name_en VARCHAR(255) NOT NULL,
                name_ar VARCHAR(255) NOT NULL,
                connector_type INT NOT NULL DEFAULT 1,
                direction INT NOT NULL DEFAULT 1,
                endpoint_url VARCHAR(1000) NOT NULL,
                auth_type INT NOT NULL DEFAULT 1,
                encrypted_credentials TEXT NULL,
                credentials_key_version INT NOT NULL DEFAULT 1,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                event_subscriptions_json JSONB NOT NULL DEFAULT '[]'::jsonb,
                config_json JSONB NOT NULL DEFAULT '{}'::jsonb,
                row_version BIGINT NOT NULL DEFAULT 1,
                CONSTRAINT uq_integrations_connector_code UNIQUE (tenant_id, code)
            );

            CREATE TABLE IF NOT EXISTS integrations.deliveries (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                connector_id UUID NOT NULL REFERENCES integrations.connectors(id),
                event_id UUID NOT NULL,
                event_type VARCHAR(150) NOT NULL,
                status INT NOT NULL DEFAULT 1,
                attempt_count INT NOT NULL DEFAULT 0,
                max_attempts INT NOT NULL DEFAULT 5,
                next_attempt_at_utc TIMESTAMPTZ NULL,
                last_attempt_at_utc TIMESTAMPTZ NULL,
                last_http_status INT NULL,
                last_error_message TEXT NULL,
                payload_json JSONB NOT NULL,
                idempotency_key VARCHAR(200) NOT NULL,
                created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT uq_integrations_delivery_idempotency UNIQUE (tenant_id, idempotency_key)
            );

            CREATE TABLE IF NOT EXISTS integrations.inbox (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                provider_code VARCHAR(100) NOT NULL,
                external_message_id VARCHAR(200) NOT NULL,
                payload_json JSONB NOT NULL,
                received_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                processed_at_utc TIMESTAMPTZ NULL,
                status VARCHAR(50) NOT NULL DEFAULT 'Received',
                CONSTRAINT uq_integrations_inbox_dedup UNIQUE (tenant_id, provider_code, external_message_id)
            );

            CREATE INDEX IF NOT EXISTS ix_integrations_deliveries_queue ON integrations.deliveries (tenant_id, status, next_attempt_at_utc);
            CREATE INDEX IF NOT EXISTS ix_integrations_deliveries_created ON integrations.deliveries (tenant_id, created_at_utc DESC);

            -- Seed a default Webhook Connector
            INSERT INTO integrations.connectors (
                id, tenant_id, code, name_en, name_ar, connector_type, direction, endpoint_url,
                auth_type, is_active, event_subscriptions_json, config_json, row_version
            ) VALUES (
                'e1111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111',
                'GENERIC_WEBHOOK', 'Enterprise Webhook Dispatcher', 'مرسل الويب هوك للمؤسسة',
                1, 1, 'https://api.enterprise.com/webhooks/zainx', 3, TRUE,
                '[""EmployeeCreatedEvent"", ""CandidateHiredEvent"", ""PayrollFinalizedEvent""]'::jsonb,
                '{}'::jsonb, 1
            ) ON CONFLICT (tenant_id, code) DO NOTHING;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
