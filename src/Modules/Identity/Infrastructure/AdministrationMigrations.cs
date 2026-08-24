using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace Workforce.Modules.Identity.Infrastructure;

public static class AdministrationMigrations
{
    public static async Task ApplyMigrationsAsync(string connectionString, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            CREATE SCHEMA IF NOT EXISTS admin;

            CREATE TABLE IF NOT EXISTS admin.roles (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                code VARCHAR(100) NOT NULL,
                name_en VARCHAR(255) NOT NULL,
                name_ar VARCHAR(255) NOT NULL,
                description TEXT NULL,
                permissions_json JSONB NOT NULL DEFAULT '[]'::jsonb,
                is_system_role BOOLEAN NOT NULL DEFAULT FALSE,
                row_version BIGINT NOT NULL DEFAULT 1,
                CONSTRAINT uq_admin_roles_code UNIQUE (tenant_id, code)
            );

            CREATE TABLE IF NOT EXISTS admin.role_assignments (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                user_id UUID NOT NULL,
                role_id UUID NOT NULL REFERENCES admin.roles(id),
                legal_entity_scope_id UUID NULL,
                organization_unit_scope_id UUID NULL,
                assigned_by_user_id UUID NOT NULL,
                assigned_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE UNIQUE INDEX IF NOT EXISTS uq_admin_role_assignment ON admin.role_assignments (
                tenant_id, user_id, role_id, COALESCE(legal_entity_scope_id, '00000000-0000-0000-0000-000000000000'::uuid)
            );

            CREATE TABLE IF NOT EXISTS admin.platform_settings (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                category VARCHAR(100) NOT NULL,
                key VARCHAR(100) NOT NULL,
                value_json JSONB NOT NULL,
                effective_start_date DATE NOT NULL,
                effective_end_date DATE NULL,
                is_current BOOLEAN NOT NULL DEFAULT TRUE,
                changed_by_user_id UUID NOT NULL,
                changed_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                row_version BIGINT NOT NULL DEFAULT 1,
                CONSTRAINT uq_admin_platform_setting_key_date UNIQUE (tenant_id, category, key, effective_start_date)
            );

            CREATE TABLE IF NOT EXISTS admin.retention_policies (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                module VARCHAR(100) NOT NULL,
                data_category VARCHAR(100) NOT NULL,
                retention_days INT NOT NULL,
                action_on_expiry INT NOT NULL DEFAULT 1,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                effective_start_date DATE NOT NULL,
                changed_by_user_id UUID NOT NULL,
                row_version BIGINT NOT NULL DEFAULT 1,
                CONSTRAINT uq_admin_retention_policy UNIQUE (tenant_id, module, data_category)
            );

            CREATE INDEX IF NOT EXISTS ix_admin_roles_tenant ON admin.roles (tenant_id);
            CREATE INDEX IF NOT EXISTS ix_admin_role_assignments_user ON admin.role_assignments (tenant_id, user_id);

            -- Seed Standard System Roles
            INSERT INTO admin.roles (id, tenant_id, code, name_en, name_ar, description, permissions_json, is_system_role, row_version)
            VALUES 
                ('f1111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'SUPER_ADMIN', 'Super Administrator', 'المشرف العام', 'Full access to all platform domains and governance.', '[""*""]'::jsonb, TRUE, 1),
                ('f2222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', 'HR_MANAGER', 'HR Operations Manager', 'مدير الموارد البشرية', 'Operational management of People, Attendance, Leave, Recruitment.', '[""people.read"", ""people.write"", ""attendance.read"", ""attendance.write"", ""leave.read"", ""leave.write"", ""recruitment.read"", ""recruitment.write"", ""reports.read""]'::jsonb, TRUE, 1),
                ('f3333333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111111', 'PAYROLL_ADMIN', 'Payroll Specialist', 'مسؤول الرواتب والتسويات', 'Manage payroll calculation, finalization, and settlement files.', '[""payroll.read"", ""payroll.run"", ""payroll.finalize"", ""settlement.export"", ""payroll.result.read_sensitive"", ""reports.read""]'::jsonb, TRUE, 1),
                ('f4444444-4444-4444-4444-444444444444', '11111111-1111-1111-1111-111111111111', 'AUDITOR', 'Compliance & Security Auditor', 'مدقق الامتثال والأمان', 'Read-only access to audit trail, security events, and compliance reports.', '[""audit.read"", ""compliance.read"", ""reports.read""]'::jsonb, TRUE, 1),
                ('f5555555-5555-5555-5555-555555555555', '11111111-1111-1111-1111-111111111111', 'EMPLOYEE', 'Standard Employee', 'موظف عادي', 'Self-service for leave, attendance, and profile view.', '[""self.leave.request"", ""self.attendance.checkin"", ""self.profile.read""]'::jsonb, TRUE, 1)
            ON CONFLICT (tenant_id, code) DO NOTHING;

            -- Seed Default Retention Policies
            INSERT INTO admin.retention_policies (id, tenant_id, module, data_category, retention_days, action_on_expiry, is_active, effective_start_date, changed_by_user_id, row_version)
            VALUES
                ('a1111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'Audit', 'SecurityLogs', 2555, 2, TRUE, '2026-01-01', '11111111-1111-1111-1111-111111111111', 1),
                ('a2222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', 'Recruitment', 'RejectedCandidateCV', 365, 1, TRUE, '2026-01-01', '11111111-1111-1111-1111-111111111111', 1),
                ('a3333333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111111', 'Payroll', 'MonthlySnapshots', 3650, 2, TRUE, '2026-01-01', '11111111-1111-1111-1111-111111111111', 1)
            ON CONFLICT (tenant_id, module, data_category) DO NOTHING;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
