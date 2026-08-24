using System;
using System.Threading.Tasks;
using Npgsql;

namespace Workforce.Modules.Recruitment.Infrastructure;

public static class RecruitmentMigrations
{
    public static async Task ApplyAsync(string connectionString)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var sql = """
        CREATE SCHEMA IF NOT EXISTS recruitment;

        -- 1. Pipelines
        CREATE TABLE IF NOT EXISTS recruitment.pipelines (
            id UUID PRIMARY KEY,
            tenant_id UUID NOT NULL,
            code TEXT NOT NULL,
            name_en TEXT NOT NULL,
            name_ar TEXT NOT NULL,
            is_active BOOLEAN NOT NULL DEFAULT TRUE,
            created_at_utc TIMESTAMPTZ NOT NULL,
            row_version BIGINT NOT NULL DEFAULT 1,
            CONSTRAINT uq_recruitment_pipelines_tenant_code UNIQUE (tenant_id, code)
        );

        -- 2. Pipeline Versions
        CREATE TABLE IF NOT EXISTS recruitment.pipeline_versions (
            id UUID PRIMARY KEY,
            pipeline_id UUID NOT NULL REFERENCES recruitment.pipelines(id) ON DELETE CASCADE,
            version_number INT NOT NULL,
            is_immutable BOOLEAN NOT NULL DEFAULT FALSE,
            created_at_utc TIMESTAMPTZ NOT NULL,
            CONSTRAINT uq_recruitment_pipeline_versions UNIQUE (pipeline_id, version_number)
        );

        -- 3. Pipeline Stages
        CREATE TABLE IF NOT EXISTS recruitment.pipeline_stages (
            id UUID PRIMARY KEY,
            pipeline_version_id UUID NOT NULL REFERENCES recruitment.pipeline_versions(id) ON DELETE CASCADE,
            stage_order INT NOT NULL,
            code TEXT NOT NULL,
            name_en TEXT NOT NULL,
            name_ar TEXT NOT NULL,
            stage_kind INT NOT NULL,
            CONSTRAINT uq_recruitment_pipeline_stages_order UNIQUE (pipeline_version_id, stage_order),
            CONSTRAINT uq_recruitment_pipeline_stages_code UNIQUE (pipeline_version_id, code)
        );

        -- 4. Job Requisitions
        CREATE TABLE IF NOT EXISTS recruitment.job_requisitions (
            id UUID PRIMARY KEY,
            tenant_id UUID NOT NULL,
            legal_entity_id UUID NOT NULL,
            organization_unit_id UUID NOT NULL,
            position_id UUID,
            location_id UUID,
            hiring_manager_id UUID NOT NULL,
            recruiter_id UUID NOT NULL,
            requisition_number TEXT NOT NULL,
            title_en TEXT NOT NULL,
            title_ar TEXT NOT NULL,
            openings_count INT NOT NULL,
            employment_type TEXT NOT NULL,
            pipeline_id UUID NOT NULL REFERENCES recruitment.pipelines(id),
            pipeline_version INT NOT NULL,
            status INT NOT NULL,
            approval_request_id UUID,
            requisition_reason TEXT,
            target_start_date DATE,
            opened_at_utc TIMESTAMPTZ,
            closed_at_utc TIMESTAMPTZ,
            created_at_utc TIMESTAMPTZ NOT NULL,
            row_version BIGINT NOT NULL DEFAULT 1
        );

        CREATE INDEX IF NOT EXISTS ix_recruitment_job_requisitions_tenant_status 
            ON recruitment.job_requisitions(tenant_id, status);

        -- 5. Candidates
        CREATE TABLE IF NOT EXISTS recruitment.candidates (
            id UUID PRIMARY KEY,
            tenant_id UUID NOT NULL,
            first_name_en TEXT NOT NULL,
            last_name_en TEXT NOT NULL,
            first_name_ar TEXT NOT NULL,
            last_name_ar TEXT NOT NULL,
            email TEXT NOT NULL,
            phone_number TEXT NOT NULL,
            location TEXT,
            headline TEXT,
            source TEXT,
            resume_document_id UUID,
            skills_json JSONB NOT NULL DEFAULT '[]'::jsonb,
            normalized_email_hash TEXT NOT NULL,
            normalized_phone_hash TEXT NOT NULL,
            created_at_utc TIMESTAMPTZ NOT NULL,
            row_version BIGINT NOT NULL DEFAULT 1
        );

        CREATE INDEX IF NOT EXISTS ix_recruitment_candidates_tenant_email_hash 
            ON recruitment.candidates(tenant_id, normalized_email_hash);
        CREATE INDEX IF NOT EXISTS ix_recruitment_candidates_tenant_phone_hash 
            ON recruitment.candidates(tenant_id, normalized_phone_hash);

        -- 6. Applications
        CREATE TABLE IF NOT EXISTS recruitment.applications (
            id UUID PRIMARY KEY,
            tenant_id UUID NOT NULL,
            legal_entity_id UUID NOT NULL,
            requisition_id UUID NOT NULL REFERENCES recruitment.job_requisitions(id),
            candidate_id UUID NOT NULL REFERENCES recruitment.candidates(id),
            pipeline_version_id UUID NOT NULL REFERENCES recruitment.pipeline_versions(id),
            current_stage_id UUID NOT NULL REFERENCES recruitment.pipeline_stages(id),
            status INT NOT NULL,
            source TEXT,
            applied_at_utc TIMESTAMPTZ NOT NULL,
            disposed_at_utc TIMESTAMPTZ,
            disposition_reason TEXT,
            disposition_note TEXT,
            hired_person_id UUID,
            hired_employment_id UUID,
            hired_at_utc TIMESTAMPTZ,
            row_version BIGINT NOT NULL DEFAULT 1
        );

        -- Partial unique index: Only one ACTIVE application per (tenant, requisition, candidate)
        CREATE UNIQUE INDEX IF NOT EXISTS uq_recruitment_applications_active 
            ON recruitment.applications (tenant_id, requisition_id, candidate_id) 
            WHERE (status = 1);

        CREATE INDEX IF NOT EXISTS ix_recruitment_applications_req_stage 
            ON recruitment.applications(tenant_id, requisition_id, current_stage_id);

        -- 7. Application Stage History
        CREATE TABLE IF NOT EXISTS recruitment.application_stage_history (
            id UUID PRIMARY KEY,
            application_id UUID NOT NULL REFERENCES recruitment.applications(id) ON DELETE CASCADE,
            from_stage_id UUID REFERENCES recruitment.pipeline_stages(id),
            to_stage_id UUID NOT NULL REFERENCES recruitment.pipeline_stages(id),
            changed_by_user_id UUID NOT NULL,
            changed_at_utc TIMESTAMPTZ NOT NULL,
            reason TEXT,
            idempotency_key TEXT
        );

        CREATE INDEX IF NOT EXISTS ix_recruitment_stage_history_app 
            ON recruitment.application_stage_history(application_id, changed_at_utc);

        -- 8. Interviews
        CREATE TABLE IF NOT EXISTS recruitment.interviews (
            id UUID PRIMARY KEY,
            tenant_id UUID NOT NULL,
            application_id UUID NOT NULL REFERENCES recruitment.applications(id),
            stage_id UUID NOT NULL REFERENCES recruitment.pipeline_stages(id),
            title TEXT NOT NULL,
            interview_type INT NOT NULL,
            scheduled_start_utc TIMESTAMPTZ NOT NULL,
            scheduled_end_utc TIMESTAMPTZ NOT NULL,
            timezone TEXT NOT NULL,
            location_or_meeting_url TEXT,
            status INT NOT NULL,
            interview_kit_json JSONB NOT NULL DEFAULT '{}'::jsonb,
            created_at_utc TIMESTAMPTZ NOT NULL,
            row_version BIGINT NOT NULL DEFAULT 1
        );

        CREATE INDEX IF NOT EXISTS ix_recruitment_interviews_app 
            ON recruitment.interviews(tenant_id, application_id);

        -- 9. Interview Participants
        CREATE TABLE IF NOT EXISTS recruitment.interview_participants (
            id UUID PRIMARY KEY,
            interview_id UUID NOT NULL REFERENCES recruitment.interviews(id) ON DELETE CASCADE,
            interviewer_user_id UUID NOT NULL,
            role INT NOT NULL,
            is_required BOOLEAN NOT NULL DEFAULT TRUE,
            CONSTRAINT uq_recruitment_interview_participants UNIQUE (interview_id, interviewer_user_id)
        );

        -- 10. Scorecard Submissions
        CREATE TABLE IF NOT EXISTS recruitment.scorecard_submissions (
            id UUID PRIMARY KEY,
            interview_id UUID NOT NULL REFERENCES recruitment.interviews(id) ON DELETE CASCADE,
            application_id UUID NOT NULL REFERENCES recruitment.applications(id),
            interviewer_user_id UUID NOT NULL,
            ratings_json JSONB NOT NULL DEFAULT '{}'::jsonb,
            strengths TEXT,
            concerns TEXT,
            recommendation INT NOT NULL,
            is_finalized BOOLEAN NOT NULL DEFAULT TRUE,
            submitted_at_utc TIMESTAMPTZ NOT NULL,
            row_version BIGINT NOT NULL DEFAULT 1,
            CONSTRAINT uq_recruitment_scorecard_interviewer UNIQUE (interview_id, interviewer_user_id)
        );

        -- 11. Offers
        CREATE TABLE IF NOT EXISTS recruitment.offers (
            id UUID PRIMARY KEY,
            tenant_id UUID NOT NULL,
            legal_entity_id UUID NOT NULL,
            application_id UUID NOT NULL REFERENCES recruitment.applications(id),
            candidate_id UUID NOT NULL REFERENCES recruitment.candidates(id),
            offer_version_number INT NOT NULL,
            title_en TEXT NOT NULL,
            title_ar TEXT NOT NULL,
            proposed_start_date DATE NOT NULL,
            base_salary_monthly NUMERIC(18,4) NOT NULL,
            currency TEXT NOT NULL,
            allowances_json JSONB NOT NULL DEFAULT '[]'::jsonb,
            conditions_note TEXT,
            status INT NOT NULL,
            approval_request_id UUID,
            issued_at_utc TIMESTAMPTZ,
            accepted_at_utc TIMESTAMPTZ,
            expiry_date DATE,
            offer_document_id UUID,
            created_at_utc TIMESTAMPTZ NOT NULL,
            row_version BIGINT NOT NULL DEFAULT 1,
            CONSTRAINT uq_recruitment_offers_version UNIQUE (application_id, offer_version_number)
        );

        -- 12. Outbox Messages
        CREATE TABLE IF NOT EXISTS recruitment.outbox_messages (
            id UUID PRIMARY KEY,
            tenant_id UUID NOT NULL,
            event_type TEXT NOT NULL,
            payload_json JSONB NOT NULL,
            occurred_at_utc TIMESTAMPTZ NOT NULL,
            processed_at_utc TIMESTAMPTZ
        );

        -- 13. Seed Standard Default Pipeline if none exists
        DO $$
        DECLARE
            v_pipeline_id UUID := 'a0000000-0000-0000-0000-000000000001'::UUID;
            v_version_id UUID := 'b0000000-0000-0000-0000-000000000001'::UUID;
            v_tenant_id UUID := '11111111-1111-1111-1111-111111111111'::UUID;
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM recruitment.pipelines WHERE id = v_pipeline_id) THEN
                INSERT INTO recruitment.pipelines (id, tenant_id, code, name_en, name_ar, is_active, created_at_utc, row_version)
                VALUES (v_pipeline_id, v_tenant_id, 'STANDARD', 'Standard Hiring Pipeline', 'مسار التوظيف القياسي', TRUE, NOW(), 1);

                INSERT INTO recruitment.pipeline_versions (id, pipeline_id, version_number, is_immutable, created_at_utc)
                VALUES (v_version_id, v_pipeline_id, 1, FALSE, NOW());

                INSERT INTO recruitment.pipeline_stages (id, pipeline_version_id, stage_order, code, name_en, name_ar, stage_kind)
                VALUES 
                    ('c0000000-0000-0000-0000-000000000001'::UUID, v_version_id, 1, 'APPLIED', 'Applied', 'تم التقديم', 1),
                    ('c0000000-0000-0000-0000-000000000002'::UUID, v_version_id, 2, 'SCREENING', 'Recruiter Screening', 'الفرز الأولي', 2),
                    ('c0000000-0000-0000-0000-000000000003'::UUID, v_version_id, 3, 'TECHNICAL_INTERVIEW', 'Technical Interview', 'المقابلة الفنية', 4),
                    ('c0000000-0000-0000-0000-000000000004'::UUID, v_version_id, 4, 'MANAGEMENT_INTERVIEW', 'Management Interview', 'مقابلة الإدارة', 4),
                    ('c0000000-0000-0000-0000-000000000005'::UUID, v_version_id, 5, 'OFFER', 'Offer & Decision', 'العرض والقرار', 5),
                    ('c0000000-0000-0000-0000-000000000006'::UUID, v_version_id, 6, 'HIRED', 'Hired', 'تم التعيين', 6),
                    ('c0000000-0000-0000-0000-000000000007'::UUID, v_version_id, 7, 'REJECTED', 'Rejected', 'مستبعد', 7);
            END IF;
        END $$;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }
}
