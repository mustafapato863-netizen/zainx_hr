using Npgsql;

namespace Workforce.Modules.Organization.Infrastructure;

public static class OrganizationMigrations
{
    public static async Task ApplyMigrationsAsync(string connectionString, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            CREATE SCHEMA IF NOT EXISTS organization;

            CREATE TABLE IF NOT EXISTS organization.organization_units (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                legal_entity_id UUID NOT NULL,
                code VARCHAR(50) NOT NULL,
                name_en VARCHAR(200) NOT NULL,
                name_ar VARCHAR(200) NOT NULL,
                type INT NOT NULL,
                parent_unit_id UUID NULL REFERENCES organization.organization_units(id),
                manager_position_id UUID NULL,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                effective_from DATE NOT NULL,
                effective_to DATE NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                row_version INT NOT NULL DEFAULT 1,
                CONSTRAINT uq_org_unit_code UNIQUE (tenant_id, legal_entity_id, code)
            );

            CREATE TABLE IF NOT EXISTS organization.positions (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                legal_entity_id UUID NOT NULL,
                organization_unit_id UUID NOT NULL REFERENCES organization.organization_units(id),
                job_code VARCHAR(50) NOT NULL,
                title_en VARCHAR(200) NOT NULL,
                title_ar VARCHAR(200) NOT NULL,
                grade VARCHAR(50) NOT NULL DEFAULT 'N/A',
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT uq_position_code UNIQUE (tenant_id, legal_entity_id, job_code)
            );

            CREATE TABLE IF NOT EXISTS organization.locations (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                legal_entity_id UUID NOT NULL,
                code VARCHAR(50) NOT NULL,
                name_en VARCHAR(200) NOT NULL,
                name_ar VARCHAR(200) NOT NULL,
                country VARCHAR(10) NOT NULL DEFAULT 'SA',
                city VARCHAR(100) NOT NULL DEFAULT 'Riyadh',
                address TEXT NOT NULL DEFAULT '',
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT uq_location_code UNIQUE (tenant_id, legal_entity_id, code)
            );

            CREATE TABLE IF NOT EXISTS organization.cost_centers (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                legal_entity_id UUID NOT NULL,
                code VARCHAR(50) NOT NULL,
                name_en VARCHAR(200) NOT NULL,
                name_ar VARCHAR(200) NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                CONSTRAINT uq_cost_center_code UNIQUE (tenant_id, legal_entity_id, code)
            );

            CREATE INDEX IF NOT EXISTS ix_org_units_tenant_parent ON organization.organization_units(tenant_id, parent_unit_id);
            CREATE INDEX IF NOT EXISTS ix_positions_unit ON organization.positions(organization_unit_id);
            CREATE INDEX IF NOT EXISTS ix_cost_centers_tenant_entity ON organization.cost_centers(tenant_id, legal_entity_id, is_active);
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
