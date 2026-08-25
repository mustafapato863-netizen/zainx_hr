using Npgsql;

namespace Workforce.Modules.Tenancy.Infrastructure;

public static class TenancyMigrations
{
    public static async Task ApplyAsync(string connectionString, bool seedDevelopmentContext, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        const string schemaSql = """
            CREATE SCHEMA IF NOT EXISTS platform;

            CREATE TABLE IF NOT EXISTS platform.tenants (
                id UUID PRIMARY KEY,
                code VARCHAR(100) NOT NULL,
                name_en VARCHAR(200) NOT NULL,
                name_ar VARCHAR(200) NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                CONSTRAINT uq_platform_tenant_code UNIQUE (code)
            );

            CREATE TABLE IF NOT EXISTS platform.legal_entities (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL REFERENCES platform.tenants(id),
                code VARCHAR(100) NOT NULL,
                name_en VARCHAR(200) NOT NULL,
                name_ar VARCHAR(200) NOT NULL,
                country_code VARCHAR(3) NOT NULL,
                currency_code VARCHAR(3) NOT NULL,
                timezone_id VARCHAR(100) NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                row_version BIGINT NOT NULL DEFAULT 1,
                CONSTRAINT uq_platform_legal_entity_code UNIQUE (tenant_id, code)
            );

            CREATE INDEX IF NOT EXISTS ix_platform_legal_entities_tenant
                ON platform.legal_entities (tenant_id, is_active, code);
            """;

        await using (var cmd = new NpgsqlCommand(schemaSql, conn))
        {
            await cmd.ExecuteNonQueryAsync(ct);
        }

        if (!seedDevelopmentContext)
        {
            return;
        }

        const string seedSql = """
            INSERT INTO platform.tenants (id, code, name_en, name_ar, is_active)
            VALUES ('22222222-2222-2222-2222-222222222222', 'ZAINX-DEV', 'Zain X Development', 'زين إكس للتطوير', TRUE)
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO platform.legal_entities (
                id, tenant_id, code, name_en, name_ar, country_code, currency_code, timezone_id, is_active
            ) VALUES (
                '33333333-3333-3333-3333-333333333333',
                '22222222-2222-2222-2222-222222222222',
                'ZAINX-DEV-SA',
                'Zain X Development Saudi Arabia',
                'زين إكس للتطوير - المملكة العربية السعودية',
                'SA', 'SAR', 'Asia/Riyadh', TRUE
            )
            ON CONFLICT (id) DO NOTHING;
            """;

        await using var seedCmd = new NpgsqlCommand(seedSql, conn);
        await seedCmd.ExecuteNonQueryAsync(ct);
    }
}
