using System.Threading.Tasks;
using Npgsql;

namespace Workforce.Modules.Approvals.Infrastructure;

public static class ApprovalsMigrations
{
    public static async Task ApplyAsync(NpgsqlDataSource dataSource)
    {
        await using var cmd = dataSource.CreateCommand();
        cmd.CommandText = """
            CREATE SCHEMA IF NOT EXISTS approvals;

            -- 1. Approval Definitions
            CREATE TABLE IF NOT EXISTS approvals.approval_definitions (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                module_name VARCHAR(50) NOT NULL,
                workflow_type VARCHAR(50) NOT NULL,
                name_en VARCHAR(200) NOT NULL,
                name_ar VARCHAR(200) NOT NULL,
                steps_count INT NOT NULL DEFAULT 1,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                CONSTRAINT uq_approval_definitions_tenant_wf UNIQUE (tenant_id, module_name, workflow_type)
            );

            -- 2. Approval Requests (Aggregate Root)
            CREATE TABLE IF NOT EXISTS approvals.approval_requests (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                legal_entity_id UUID NOT NULL,
                source_module VARCHAR(50) NOT NULL,
                source_entity_id UUID NOT NULL,
                workflow_type VARCHAR(50) NOT NULL,
                title VARCHAR(300) NOT NULL,
                current_step_order INT NOT NULL DEFAULT 1,
                total_steps INT NOT NULL DEFAULT 1,
                status INT NOT NULL,
                requester_user_id UUID NOT NULL,
                requester_employment_id UUID NOT NULL,
                payload_snapshot_json JSONB,
                created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                row_version BIGINT NOT NULL DEFAULT 1
            );

            CREATE INDEX IF NOT EXISTS ix_approval_requests_tenant_status
                ON approvals.approval_requests (tenant_id, status);

            CREATE INDEX IF NOT EXISTS ix_approval_requests_source
                ON approvals.approval_requests (tenant_id, source_module, source_entity_id);

            -- 3. Approval Steps
            CREATE TABLE IF NOT EXISTS approvals.approval_steps (
                id UUID PRIMARY KEY,
                approval_request_id UUID NOT NULL REFERENCES approvals.approval_requests(id) ON DELETE CASCADE,
                step_order INT NOT NULL,
                assigned_approver_user_id UUID,
                assigned_role VARCHAR(100),
                status INT NOT NULL,
                decided_at_utc TIMESTAMPTZ,
                decided_by_user_id UUID,
                decision_reason TEXT,
                row_version BIGINT NOT NULL DEFAULT 1
            );

            CREATE INDEX IF NOT EXISTS ix_approval_steps_approver
                ON approvals.approval_steps (assigned_approver_user_id, status);

            -- 4. Decision Histories
            CREATE TABLE IF NOT EXISTS approvals.decision_histories (
                id UUID PRIMARY KEY,
                approval_request_id UUID NOT NULL REFERENCES approvals.approval_requests(id) ON DELETE CASCADE,
                step_order INT NOT NULL,
                actor_user_id UUID NOT NULL,
                action VARCHAR(50) NOT NULL,
                reason TEXT,
                timestamp_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            -- 5. Outbox Messages
            CREATE TABLE IF NOT EXISTS approvals.outbox_messages (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                event_type VARCHAR(200) NOT NULL,
                payload_json JSONB NOT NULL,
                occurred_at_utc TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                processed_at_utc TIMESTAMPTZ
            );
        """;

        await cmd.ExecuteNonQueryAsync();
    }
}
