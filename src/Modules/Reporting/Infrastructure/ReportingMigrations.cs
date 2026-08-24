using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace Workforce.Modules.Reporting.Infrastructure;

public static class ReportingMigrations
{
    public static async Task ApplyMigrationsAsync(string connectionString, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            CREATE SCHEMA IF NOT EXISTS reporting;

            CREATE TABLE IF NOT EXISTS reporting.report_definitions (
                report_code VARCHAR(100) PRIMARY KEY,
                name_en VARCHAR(255) NOT NULL,
                name_ar VARCHAR(255) NOT NULL,
                domain VARCHAR(100) NOT NULL,
                description_en TEXT NULL,
                description_ar TEXT NULL,
                allowed_filters_json JSONB NOT NULL DEFAULT '[]'::jsonb,
                allowed_columns_json JSONB NOT NULL DEFAULT '[]'::jsonb,
                required_permissions_json JSONB NOT NULL DEFAULT '[]'::jsonb,
                data_classification VARCHAR(50) NOT NULL DEFAULT 'Internal',
                supported_formats_json JSONB NOT NULL DEFAULT '[""CSV"", ""JSON""]'::jsonb,
                execution_mode INT NOT NULL DEFAULT 3,
                version INT NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS reporting.saved_views (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                legal_entity_id UUID NULL,
                report_code VARCHAR(100) NOT NULL REFERENCES reporting.report_definitions(report_code),
                view_name VARCHAR(255) NOT NULL,
                is_tenant_shared BOOLEAN NOT NULL DEFAULT FALSE,
                owner_user_id UUID NOT NULL,
                filters_json JSONB NOT NULL DEFAULT '{}'::jsonb,
                columns_json JSONB NOT NULL DEFAULT '[]'::jsonb,
                sort_json JSONB NOT NULL DEFAULT '[]'::jsonb,
                grouping_json JSONB NOT NULL DEFAULT '[]'::jsonb,
                created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS reporting.report_jobs (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                legal_entity_id UUID NULL,
                report_code VARCHAR(100) NOT NULL REFERENCES reporting.report_definitions(report_code),
                status INT NOT NULL DEFAULT 1,
                requested_by_user_id UUID NOT NULL,
                requested_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                completed_at_utc TIMESTAMPTZ NULL,
                filters_json JSONB NOT NULL DEFAULT '{}'::jsonb,
                output_format VARCHAR(10) NOT NULL DEFAULT 'CSV',
                storage_key VARCHAR(500) NULL,
                file_size_bytes BIGINT NOT NULL DEFAULT 0,
                sha256_checksum VARCHAR(100) NULL,
                error_message TEXT NULL,
                row_count BIGINT NOT NULL DEFAULT 0,
                idempotency_key VARCHAR(200) NULL,
                CONSTRAINT uq_reporting_job_idempotency UNIQUE (tenant_id, idempotency_key)
            );

            CREATE INDEX IF NOT EXISTS ix_reporting_jobs_queue ON reporting.report_jobs (status, requested_at_utc ASC);
            CREATE INDEX IF NOT EXISTS ix_reporting_jobs_tenant ON reporting.report_jobs (tenant_id, requested_at_utc DESC);
            CREATE INDEX IF NOT EXISTS ix_reporting_saved_views_tenant ON reporting.saved_views (tenant_id, report_code);

            -- Seed Core Operational Reports
            INSERT INTO reporting.report_definitions (
                report_code, name_en, name_ar, domain, description_en, description_ar,
                allowed_filters_json, allowed_columns_json, required_permissions_json,
                data_classification, supported_formats_json, execution_mode, version
            ) VALUES
            (
                'HEADCOUNT_SUMMARY',
                'Headcount & Demographics Summary',
                'ملخص القوى العاملة والتركيبة السكانية',
                'People',
                'Enterprise overview of active headcounts, department distribution, and employment status.',
                'نظرة عامة على إجمالي الموظفين وتوزيع الأقسام وحالات التوظيف.',
                '[""department"", ""employmentType"", ""legalEntityId"", ""status""]'::jsonb,
                '[""employeeNumber"", ""fullNameEn"", ""fullNameAr"", ""nationalId"", ""jobTitle"", ""department"", ""hireDate"", ""status""]'::jsonb,
                '[""people.read""]'::jsonb,
                'Internal',
                '[""CSV"", ""JSON""]'::jsonb,
                3, 1
            ),
            (
                'ATTENDANCE_MONTHLY',
                'Monthly Attendance & Exception Summary',
                'تقرير الحضور والانصراف الشهري والاستثناءات',
                'Attendance',
                'Aggregated employee attendance, work hours, overtime, and punctuality exceptions.',
                'ملخص ساعات العمل والحضور الإجمالي وساعات العمل الإضافي وحالات التأخير.',
                '[""month"", ""year"", ""department"", ""employeeId""]'::jsonb,
                '[""employeeNumber"", ""employeeName"", ""scheduledHours"", ""workedHours"", ""overtimeHours"", ""lateMinutes"", ""exceptionCount""]'::jsonb,
                '[""attendance.read""]'::jsonb,
                'Internal',
                '[""CSV"", ""JSON""]'::jsonb,
                3, 1
            ),
            (
                'LEAVE_UTILIZATION',
                'Leave Balances & Utilization',
                'تقرير أرصدة واستخدام الإجازات',
                'Leave',
                'Tracks annual, sick, and statutory leave accruals, taken balances, and pending requests.',
                'تتبع أرصدة الإجازات السنوية والمرضية والمستهلك منها والطلبات المعلقة.',
                '[""leaveType"", ""year"", ""department""]'::jsonb,
                '[""employeeNumber"", ""employeeName"", ""leaveType"", ""entitlement"", ""taken"", ""remainingBalance"", ""pendingApproval""]'::jsonb,
                '[""leave.read""]'::jsonb,
                'Internal',
                '[""CSV"", ""JSON""]'::jsonb,
                3, 1
            ),
            (
                'PAYROLL_RECONCILIATION',
                'Finalized Payroll Reconciliation & Statutory',
                'مطابقة مسيرات الرواتب المعتمدة والاشتراكات النظامية',
                'Payroll',
                'Strictly derived from finalized payroll snapshots: Gross pay, statutory GOSI deductions, net pay, cost center allocations.',
                'تقرير مشتق حصراً من مسيرات الرواتب المعتمدة: إجمالي الرواتب، استقطاعات التأمينات، صافي الرواتب، وتوزيع مراكز التكلفة.',
                '[""periodId"", ""legalEntityId"", ""costCenter""]'::jsonb,
                '[""employeeNumber"", ""employeeName"", ""basicSalary"", ""housingAllowance"", ""transportAllowance"", ""otherEarnings"", ""gosiEmployee"", ""gosiEmployer"", ""totalDeductions"", ""netPay""]'::jsonb,
                '[""payroll.read"", ""payroll.result.read_sensitive""]'::jsonb,
                'Confidential',
                '[""CSV"", ""JSON""]'::jsonb,
                3, 1
            ),
            (
                'RECRUITMENT_FUNNEL',
                'Recruitment Pipeline & Conversion Metrics',
                'قمع التوظيف ومؤشرات التحويل والتعيين',
                'Recruitment',
                'Applicant funnel metrics, stage transition durations, conversion rates, and offer acceptance ratios.',
                'إحصائيات المتقدمين ومعدلات الانتقال بين المراحل وسرعة التعيين ونسب قبول العروض.',
                '[""requisitionId"", ""fromDate"", ""toDate""]'::jsonb,
                '[""requisitionCode"", ""requisitionTitle"", ""appliedCount"", ""screenedCount"", ""interviewedCount"", ""offeredCount"", ""hiredCount"", ""averageDaysToHire""]'::jsonb,
                '[""recruitment.read""]'::jsonb,
                'Internal',
                '[""CSV"", ""JSON""]'::jsonb,
                3, 1
            ),
            (
                'AUDIT_SECURITY_EVENTS',
                'Audit Trail & Security Access Log',
                'سجل التدقيق والعمليات الأمنية والحساسة',
                'Audit',
                'Chronological audit trail of administrative changes, sensitive data access, and privilege assignments.',
                'سجل تاريخي للعمليات الإدارية، الوصول للبيانات الحساسة، ومنح الصلاحيات.',
                '[""actorUserId"", ""actionCode"", ""entityType"", ""fromDate"", ""toDate""]'::jsonb,
                '[""occurredAtUtc"", ""actorUserId"", ""actorType"", ""actionCode"", ""entityType"", ""entityId"", ""correlationId"", ""ipAddress""]'::jsonb,
                '[""audit.read""]'::jsonb,
                'Restricted',
                '[""CSV"", ""JSON""]'::jsonb,
                3, 1
            )
            ON CONFLICT (report_code) DO NOTHING;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
