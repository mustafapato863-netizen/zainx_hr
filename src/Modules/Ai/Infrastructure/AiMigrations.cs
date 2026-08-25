using System;
using System.Threading.Tasks;
using Npgsql;

namespace Workforce.Modules.Ai.Infrastructure;

public static class AiMigrations
{
    public static async Task ApplyMigrationsAsync(string connectionString)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        string[] statements =
        [
            "CREATE SCHEMA IF NOT EXISTS ai;",

            """
            CREATE TABLE IF NOT EXISTS ai.conversations (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                legal_entity_id UUID,
                user_id UUID NOT NULL,
                title TEXT NOT NULL,
                context_entity_type TEXT,
                context_entity_id TEXT,
                created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
            """,
            "CREATE INDEX IF NOT EXISTS ix_ai_conversations_tenant_user ON ai.conversations (tenant_id, user_id, updated_at_utc DESC);",

            """
            CREATE TABLE IF NOT EXISTS ai.messages (
                id UUID PRIMARY KEY,
                conversation_id UUID NOT NULL REFERENCES ai.conversations(id) ON DELETE CASCADE,
                sender_role TEXT NOT NULL,
                content TEXT NOT NULL,
                source_category INT NOT NULL,
                tokens_used INT NOT NULL DEFAULT 0,
                created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
            """,
            "CREATE INDEX IF NOT EXISTS ix_ai_messages_conv_created ON ai.messages (conversation_id, created_at_utc ASC);",

            """
            CREATE TABLE IF NOT EXISTS ai.tool_executions (
                id UUID PRIMARY KEY,
                message_id UUID NOT NULL,
                tool_code TEXT NOT NULL,
                input_payload_json TEXT NOT NULL,
                output_payload_json TEXT NOT NULL,
                duration_ms BIGINT NOT NULL,
                status TEXT NOT NULL,
                created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
            """,
            "CREATE INDEX IF NOT EXISTS ix_ai_tool_exec_msg ON ai.tool_executions (message_id);",

            """
            CREATE TABLE IF NOT EXISTS ai.source_references (
                id UUID PRIMARY KEY,
                message_id UUID NOT NULL,
                source_category INT NOT NULL,
                title TEXT NOT NULL,
                entity_type TEXT,
                entity_id TEXT,
                policy_code TEXT,
                policy_version INT,
                payroll_run_id UUID,
                metadata_json TEXT,
                retrieved_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
            """,
            "CREATE INDEX IF NOT EXISTS ix_ai_source_refs_msg ON ai.source_references (message_id);",

            """
            CREATE TABLE IF NOT EXISTS ai.company_policies (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                policy_code TEXT NOT NULL,
                title_en TEXT NOT NULL,
                title_ar TEXT NOT NULL,
                version INT NOT NULL,
                effective_from_utc TIMESTAMPTZ NOT NULL,
                effective_to_utc TIMESTAMPTZ,
                content_en TEXT NOT NULL,
                content_ar TEXT NOT NULL,
                classification TEXT NOT NULL DEFAULT 'Internal',
                is_active BOOLEAN NOT NULL DEFAULT TRUE
            );
            """,
            "CREATE INDEX IF NOT EXISTS ix_ai_policies_tenant_code ON ai.company_policies (tenant_id, policy_code, version);",
            "CREATE INDEX IF NOT EXISTS ix_ai_policies_effective ON ai.company_policies (tenant_id, effective_from_utc, effective_to_utc);",

            """
            CREATE TABLE IF NOT EXISTS ai.product_knowledge (
                id UUID PRIMARY KEY,
                topic_code TEXT NOT NULL UNIQUE,
                title_en TEXT NOT NULL,
                title_ar TEXT NOT NULL,
                content_en TEXT NOT NULL,
                content_ar TEXT NOT NULL,
                category TEXT NOT NULL,
                tags_json TEXT NOT NULL DEFAULT '[]'
            );
            """,
            "CREATE INDEX IF NOT EXISTS ix_ai_product_knowledge_topic ON ai.product_knowledge (topic_code);",

            """
            INSERT INTO ai.product_knowledge (id, topic_code, title_en, title_ar, content_en, content_ar, category, tags_json)
            VALUES 
            (
                'e1111111-1111-1111-1111-111111111111',
                'PAYROLL_FINALIZATION',
                'Payroll Finalization & Historical Truth',
                'اعتماد مسير الرواتب والحقيقة التاريخية',
                'When a payroll run is finalized, all calculation traces, gross-to-net deductions, and statutory GOSI amounts are permanently locked. No subsequent employee profile changes can mutate finalized numbers.',
                'عند اعتماد مسير الرواتب، يتم قفل جميع مسارات الاحتساب والاستقطاعات النظامية والتأمينات بشكل دائم كحقيقة تاريخية لا تتأثر بأي تعديلات لاحقة على بيانات الموظف.',
                'Payroll',
                '["payroll", "finalization", "trace"]'
            )
            ON CONFLICT (topic_code) DO NOTHING;
            """,

            """
            INSERT INTO ai.product_knowledge (id, topic_code, title_en, title_ar, content_en, content_ar, category, tags_json)
            VALUES 
            (
                'e2222222-2222-2222-2222-222222222222',
                'RECRUITMENT_STAGES',
                'ATS Hiring Pipeline & Scorecards',
                'مراحل التوظيف وبطاقات التقييم',
                'Recruitment candidate pipelines progress through Screen, Interview, Offer, and Hire stages. Interviewer scorecards remain confidential to authorized evaluation committees.',
                'يمر المتقدم عبر مراحل الفرز والمقابلة والعرض والتعيين. تظل بطاقات تقييم المقابلات سرية ولا تتاح إلا للجان التقييم المخولة.',
                'Recruitment',
                '["recruitment", "ats", "scorecards"]'
            )
            ON CONFLICT (topic_code) DO NOTHING;
            """,

            """
            INSERT INTO ai.product_knowledge (id, topic_code, title_en, title_ar, content_en, content_ar, category, tags_json)
            VALUES 
            (
                'e3333333-3333-3333-3333-333333333333',
                'LEAVE_APPROVAL_WORKFLOW',
                'Leave Entitlement & Approval Matrix',
                'استحقاق الإجازات ومصفوفة الموافقات',
                'Annual leaves deduct strictly from available accrued balances. Multi-level workflow approvals route automatically through universal approvals.',
                'تخصم الإجازات السنوية من الرصيد الفعلي المستحق وتمر عبر مسار الموافقات الشاملة المعتمد.',
                'Leave',
                '["leave", "approvals", "balances"]'
            )
            ON CONFLICT (topic_code) DO NOTHING;
            """,

            """
            INSERT INTO ai.company_policies (
                id, tenant_id, policy_code, title_en, title_ar, version, 
                effective_from_utc, effective_to_utc, content_en, content_ar, classification, is_active
            )
            VALUES 
            (
                'f1111111-1111-1111-1111-111111111111',
                '22222222-2222-2222-2222-222222222222',
                'REMOTE_WORK_POLICY',
                'Remote & Flexible Work Policy (H1)',
                'لائحة العمل عن بعد والمرن (النصف الأول)',
                1,
                '2026-01-01 00:00:00+00',
                '2026-06-30 23:59:59+00',
                'Remote work permitted 1 day per week with team lead notification.',
                'يسمح بالعمل عن بعد يوماً واحداً في الأسبوع بعد إشعار قائد الفريق.',
                'Internal',
                true
            )
            ON CONFLICT (id) DO NOTHING;
            """,

            """
            INSERT INTO ai.company_policies (
                id, tenant_id, policy_code, title_en, title_ar, version, 
                effective_from_utc, effective_to_utc, content_en, content_ar, classification, is_active
            )
            VALUES 
            (
                'f2222222-2222-2222-2222-222222222222',
                '22222222-2222-2222-2222-222222222222',
                'REMOTE_WORK_POLICY',
                'Remote & Flexible Work Policy (H2)',
                'لائحة العمل عن بعد والمرن (النصف الثاني)',
                2,
                '2026-07-01 00:00:00+00',
                NULL,
                'Remote work permitted up to 2 days per week with line manager approval.',
                'يسمح بالعمل عن بعد يومين في الأسبوع بموافقة المدير المباشر.',
                'Internal',
                true
            )
            ON CONFLICT (id) DO NOTHING;
            """,

            """
            CREATE TABLE IF NOT EXISTS ai.action_proposals (
                id UUID PRIMARY KEY,
                conversation_id UUID NOT NULL REFERENCES ai.conversations(id) ON DELETE CASCADE,
                tenant_id UUID NOT NULL,
                legal_entity_id UUID,
                requested_by_user_id UUID NOT NULL,
                action_code TEXT NOT NULL,
                target_entity_type TEXT NOT NULL,
                target_entity_id TEXT NOT NULL,
                status TEXT NOT NULL,
                expected_row_version BIGINT NOT NULL,
                effective_date_utc TIMESTAMPTZ,
                before_snapshot_json TEXT NOT NULL,
                after_snapshot_json TEXT NOT NULL,
                impact_summary_json TEXT NOT NULL,
                required_permission TEXT NOT NULL,
                idempotency_key TEXT NOT NULL,
                proposal_hash TEXT NOT NULL,
                created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                expires_at_utc TIMESTAMPTZ NOT NULL,
                confirmed_at_utc TIMESTAMPTZ,
                completed_at_utc TIMESTAMPTZ,
                error_message TEXT
            );
            """,
            "CREATE INDEX IF NOT EXISTS ix_ai_action_proposals_tenant_user ON ai.action_proposals (tenant_id, requested_by_user_id, created_at_utc DESC);",
            "CREATE INDEX IF NOT EXISTS ix_ai_action_proposals_conv ON ai.action_proposals (conversation_id);",
            "CREATE INDEX IF NOT EXISTS ix_ai_action_proposals_idempotency ON ai.action_proposals (tenant_id, idempotency_key);",

            """
            CREATE TABLE IF NOT EXISTS ai.action_executions (
                id UUID PRIMARY KEY,
                proposal_id UUID NOT NULL REFERENCES ai.action_proposals(id) ON DELETE CASCADE,
                tenant_id UUID NOT NULL,
                actor_user_id UUID NOT NULL,
                action_code TEXT NOT NULL,
                idempotency_key TEXT NOT NULL UNIQUE,
                status TEXT NOT NULL,
                result_payload_json TEXT NOT NULL,
                duration_ms BIGINT NOT NULL,
                executed_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
            """,
            "CREATE INDEX IF NOT EXISTS ix_ai_action_executions_tenant_proposal ON ai.action_executions (tenant_id, proposal_id);",
            "CREATE INDEX IF NOT EXISTS ix_ai_action_executions_idempotency ON ai.action_executions (idempotency_key);"
        ];

        foreach (var sql in statements)
        {
            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
