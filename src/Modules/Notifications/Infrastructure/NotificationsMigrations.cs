using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace Workforce.Modules.Notifications.Infrastructure;

public static class NotificationsMigrations
{
    public static async Task ApplyMigrationsAsync(string connectionString, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            CREATE SCHEMA IF NOT EXISTS notifications;

            CREATE TABLE IF NOT EXISTS notifications.templates (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                template_code VARCHAR(100) NOT NULL,
                locale VARCHAR(10) NOT NULL DEFAULT 'en',
                subject VARCHAR(255) NOT NULL,
                body_template TEXT NOT NULL,
                allowed_variables_json JSONB NOT NULL DEFAULT '[]'::jsonb,
                channel INT NOT NULL DEFAULT 1,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                version INT NOT NULL DEFAULT 1,
                CONSTRAINT uq_notifications_template_code_locale UNIQUE (tenant_id, template_code, locale, channel)
            );

            CREATE TABLE IF NOT EXISTS notifications.notifications (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                recipient_user_id UUID NOT NULL,
                category VARCHAR(50) NOT NULL DEFAULT 'General',
                title_en VARCHAR(255) NOT NULL,
                title_ar VARCHAR(255) NOT NULL,
                body_en TEXT NOT NULL,
                body_ar TEXT NOT NULL,
                deep_link_url VARCHAR(500) NULL,
                channel INT NOT NULL DEFAULT 1,
                status INT NOT NULL DEFAULT 3,
                is_read BOOLEAN NOT NULL DEFAULT FALSE,
                read_at_utc TIMESTAMPTZ NULL,
                is_archived BOOLEAN NOT NULL DEFAULT FALSE,
                created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                source_event_id UUID NULL,
                idempotency_key VARCHAR(150) NULL,
                CONSTRAINT uq_notifications_idempotency UNIQUE (tenant_id, idempotency_key)
            );

            CREATE TABLE IF NOT EXISTS notifications.preferences (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                user_id UUID NOT NULL,
                category VARCHAR(50) NOT NULL,
                allow_email BOOLEAN NOT NULL DEFAULT TRUE,
                allow_in_app BOOLEAN NOT NULL DEFAULT TRUE,
                allow_push BOOLEAN NOT NULL DEFAULT FALSE,
                CONSTRAINT uq_notifications_user_pref UNIQUE (tenant_id, user_id, category)
            );

            CREATE INDEX IF NOT EXISTS ix_notifications_recipient_unread ON notifications.notifications (tenant_id, recipient_user_id, is_read, created_at_utc DESC);
            CREATE INDEX IF NOT EXISTS ix_notifications_recipient_all ON notifications.notifications (tenant_id, recipient_user_id, created_at_utc DESC);

            -- Seed standard default templates
            INSERT INTO notifications.templates (id, tenant_id, template_code, locale, subject, body_template, allowed_variables_json, channel, is_active, version)
            VALUES 
                ('d1111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'LEAVE_APPROVED', 'en', 'Leave Request Approved', 'Your leave request for {{startDate}} to {{endDate}} has been approved.', '[""startDate"", ""endDate""]'::jsonb, 1, TRUE, 1),
                ('d1111111-1111-1111-1111-111111111112', '11111111-1111-1111-1111-111111111111', 'LEAVE_APPROVED', 'ar', 'تمت الموافقة على طلب الإجازة', 'تمت الموافقة على طلب الإجازة من تاريخ {{startDate}} إلى {{endDate}}.', '[""startDate"", ""endDate""]'::jsonb, 1, TRUE, 1),
                ('d2222222-2222-2222-2222-222222222221', '11111111-1111-1111-1111-111111111111', 'PAYROLL_FINALIZED', 'en', 'Payroll Finalized', 'The payroll period {{periodName}} has been finalized and processed.', '[""periodName""]'::jsonb, 1, TRUE, 1),
                ('d2222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', 'PAYROLL_FINALIZED', 'ar', 'تم اعتماد مسيرات الرواتب', 'تم اعتماد وإغلاق مسير الرواتب لفترة {{periodName}} بنجاح.', '[""periodName""]'::jsonb, 1, TRUE, 1),
                ('d3333333-3333-3333-3333-333333333331', '11111111-1111-1111-1111-111111111111', 'OFFER_ISSUED', 'en', 'Job Offer Issued', 'A job offer for {{candidateName}} on requisition {{reqTitle}} has been issued.', '[""candidateName"", ""reqTitle""]'::jsonb, 1, TRUE, 1),
                ('d3333333-3333-3333-3333-333333333332', '11111111-1111-1111-1111-111111111111', 'OFFER_ISSUED', 'ar', 'تم إصدار عرض عمل', 'تم إصدار عرض عمل للمرشح {{candidateName}} على الوظيفة {{reqTitle}}.', '[""candidateName"", ""reqTitle""]'::jsonb, 1, TRUE, 1)
            ON CONFLICT (tenant_id, template_code, locale, channel) DO NOTHING;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
