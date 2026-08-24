using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using Workforce.Modules.Approvals.Domain;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Approvals.Infrastructure;

public interface IApprovalsRepository
{
    Task<(IReadOnlyList<ApprovalInboxItemDto> Items, int TotalCount)> GetApprovalInboxAsync(TenantId tenantId, Guid? assignedUserId, int? status, int page = 1, int pageSize = 50);
    Task<ApprovalRequestDetailDto?> GetApprovalRequestByIdAsync(TenantId tenantId, Guid id);
    Task<ApprovalRequest?> GetApprovalRequestEntityByIdAsync(TenantId tenantId, Guid id);
    Task CreateApprovalWorkflowAsync(ApprovalRequest request, IEnumerable<ApprovalStep> steps);
    Task SaveApprovalRequestAsync(ApprovalRequest request);
}

public record ApprovalInboxItemDto(
    Guid Id,
    Guid TenantId,
    Guid LegalEntityId,
    string SourceModule,
    Guid SourceEntityId,
    string WorkflowType,
    string Title,
    int CurrentStepOrder,
    int TotalSteps,
    string Status,
    Guid RequesterUserId,
    Guid RequesterEmploymentId,
    DateTime CreatedAt,
    uint RowVersion
);

public record ApprovalRequestDetailDto(
    Guid Id,
    Guid TenantId,
    Guid LegalEntityId,
    string SourceModule,
    Guid SourceEntityId,
    string WorkflowType,
    string Title,
    int CurrentStepOrder,
    int TotalSteps,
    string Status,
    Guid RequesterUserId,
    Guid RequesterEmploymentId,
    string? PayloadSnapshotJson,
    IReadOnlyList<ApprovalStepDto> Steps,
    IReadOnlyList<ApprovalDecisionHistoryDto> History,
    DateTime CreatedAt,
    uint RowVersion
);

public record ApprovalStepDto(
    Guid Id,
    Guid ApprovalRequestId,
    int StepOrder,
    Guid? AssignedApproverUserId,
    string? AssignedRole,
    string Status,
    DateTime? DecidedAtUtc,
    Guid? DecidedByUserId,
    string? DecisionReason,
    uint RowVersion
);

public record ApprovalDecisionHistoryDto(
    Guid Id,
    int StepOrder,
    Guid ActorUserId,
    string Action,
    string? Reason,
    DateTime TimestampUtc
);

public class ApprovalsRepository : IApprovalsRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public ApprovalsRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<(IReadOnlyList<ApprovalInboxItemDto> Items, int TotalCount)> GetApprovalInboxAsync(
        TenantId tenantId, Guid? assignedUserId, int? status, int page = 1, int pageSize = 50)
    {
        var list = new List<ApprovalInboxItemDto>();
        var offset = (Math.Max(1, page) - 1) * pageSize;

        await using var countCmd = _dataSource.CreateCommand();
        countCmd.CommandText = """
            SELECT COUNT(DISTINCT r.id)
            FROM approvals.approval_requests r
            LEFT JOIN approvals.approval_steps s ON r.id = s.approval_request_id AND r.current_step_order = s.step_order
            WHERE r.tenant_id = $1
              AND ($2::int IS NULL OR r.status = $2)
              AND ($3::uuid IS NULL OR s.assigned_approver_user_id = $3 OR r.requester_user_id = $3);
        """;
        countCmd.Parameters.AddWithValue(tenantId.Value);
        countCmd.Parameters.AddWithValue((object?)status ?? DBNull.Value);
        countCmd.Parameters.AddWithValue((object?)assignedUserId ?? DBNull.Value);

        var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            SELECT DISTINCT r.id, r.tenant_id, r.legal_entity_id, r.source_module, r.source_entity_id,
                   r.workflow_type, r.title, r.current_step_order, r.total_steps, r.status,
                   r.requester_user_id, r.requester_employment_id, r.created_at, r.row_version
            FROM approvals.approval_requests r
            LEFT JOIN approvals.approval_steps s ON r.id = s.approval_request_id AND r.current_step_order = s.step_order
            WHERE r.tenant_id = $1
              AND ($2::int IS NULL OR r.status = $2)
              AND ($3::uuid IS NULL OR s.assigned_approver_user_id = $3 OR r.requester_user_id = $3)
            ORDER BY r.created_at DESC
            LIMIT $4 OFFSET $5;
        """;
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue((object?)status ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)assignedUserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue(pageSize);
        cmd.Parameters.AddWithValue(offset);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new ApprovalInboxItemDto(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetString(3),
                reader.GetGuid(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetInt32(7),
                reader.GetInt32(8),
                ((ApprovalStatus)reader.GetInt32(9)).ToString(),
                reader.GetGuid(10),
                reader.GetGuid(11),
                reader.GetDateTime(12),
                (uint)reader.GetInt64(13)
            ));
        }

        return (list, total);
    }

    public async Task<ApprovalRequestDetailDto?> GetApprovalRequestByIdAsync(TenantId tenantId, Guid id)
    {
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            SELECT id, tenant_id, legal_entity_id, source_module, source_entity_id,
                   workflow_type, title, current_step_order, total_steps, status,
                   requester_user_id, requester_employment_id, payload_snapshot_json::text,
                   created_at, row_version
            FROM approvals.approval_requests
            WHERE tenant_id = $1 AND id = $2;
        """;
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(id);

        ApprovalRequestDetailDto? result = null;
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                result = new ApprovalRequestDetailDto(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetGuid(2),
                    reader.GetString(3),
                    reader.GetGuid(4),
                    reader.GetString(5),
                    reader.GetString(6),
                    reader.GetInt32(7),
                    reader.GetInt32(8),
                    ((ApprovalStatus)reader.GetInt32(9)).ToString(),
                    reader.GetGuid(10),
                    reader.GetGuid(11),
                    reader.IsDBNull(12) ? null : reader.GetString(12),
                    new List<ApprovalStepDto>(),
                    new List<ApprovalDecisionHistoryDto>(),
                    reader.GetDateTime(13),
                    (uint)reader.GetInt64(14)
                );
            }
        }

        if (result == null) return null;

        var steps = new List<ApprovalStepDto>();
        await using var stepCmd = _dataSource.CreateCommand();
        stepCmd.CommandText = """
            SELECT id, approval_request_id, step_order, assigned_approver_user_id,
                   assigned_role, status, decided_at_utc, decided_by_user_id, decision_reason, row_version
            FROM approvals.approval_steps
            WHERE approval_request_id = $1
            ORDER BY step_order ASC;
        """;
        stepCmd.Parameters.AddWithValue(id);

        await using (var stepReader = await stepCmd.ExecuteReaderAsync())
        {
            while (await stepReader.ReadAsync())
            {
                steps.Add(new ApprovalStepDto(
                    stepReader.GetGuid(0),
                    stepReader.GetGuid(1),
                    stepReader.GetInt32(2),
                    stepReader.IsDBNull(3) ? null : stepReader.GetGuid(3),
                    stepReader.IsDBNull(4) ? null : stepReader.GetString(4),
                    ((ApprovalStepStatus)stepReader.GetInt32(5)).ToString(),
                    stepReader.IsDBNull(6) ? null : stepReader.GetDateTime(6),
                    stepReader.IsDBNull(7) ? null : stepReader.GetGuid(7),
                    stepReader.IsDBNull(8) ? null : stepReader.GetString(8),
                    (uint)stepReader.GetInt64(9)
                ));
            }
        }

        var history = new List<ApprovalDecisionHistoryDto>();
        await using var histCmd = _dataSource.CreateCommand();
        histCmd.CommandText = """
            SELECT id, step_order, actor_user_id, action, reason, timestamp_utc
            FROM approvals.decision_histories
            WHERE approval_request_id = $1
            ORDER BY timestamp_utc ASC;
        """;
        histCmd.Parameters.AddWithValue(id);

        await using (var histReader = await histCmd.ExecuteReaderAsync())
        {
            while (await histReader.ReadAsync())
            {
                history.Add(new ApprovalDecisionHistoryDto(
                    histReader.GetGuid(0),
                    histReader.GetInt32(1),
                    histReader.GetGuid(2),
                    histReader.GetString(3),
                    histReader.IsDBNull(4) ? null : histReader.GetString(4),
                    histReader.GetDateTime(5)
                ));
            }
        }

        return result with { Steps = steps, History = history };
    }

    public async Task<ApprovalRequest?> GetApprovalRequestEntityByIdAsync(TenantId tenantId, Guid id)
    {
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            SELECT id, tenant_id, legal_entity_id, source_module, source_entity_id,
                   workflow_type, title, requester_user_id, requester_employment_id,
                   payload_snapshot_json::text, total_steps
            FROM approvals.approval_requests
            WHERE tenant_id = $1 AND id = $2;
        """;
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ApprovalRequest(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                new LegalEntityId(reader.GetGuid(2)),
                reader.GetString(3),
                reader.GetGuid(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetGuid(7),
                reader.GetGuid(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.GetInt32(10)
            );
        }

        return null;
    }

    public async Task CreateApprovalWorkflowAsync(ApprovalRequest request, IEnumerable<ApprovalStep> steps)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();

        await using var reqCmd = conn.CreateCommand();
        reqCmd.Transaction = tx;
        reqCmd.CommandText = """
            INSERT INTO approvals.approval_requests (
                id, tenant_id, legal_entity_id, source_module, source_entity_id,
                workflow_type, title, current_step_order, total_steps, status,
                requester_user_id, requester_employment_id, payload_snapshot_json,
                created_at, updated_at, row_version
            ) VALUES (
                $1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13::jsonb,
                CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, $14
            );
        """;
        reqCmd.Parameters.AddWithValue(request.Id);
        reqCmd.Parameters.AddWithValue(request.TenantId.Value);
        reqCmd.Parameters.AddWithValue(request.LegalEntityId.Value);
        reqCmd.Parameters.AddWithValue(request.SourceModule);
        reqCmd.Parameters.AddWithValue(request.SourceEntityId);
        reqCmd.Parameters.AddWithValue(request.WorkflowType);
        reqCmd.Parameters.AddWithValue(request.Title);
        reqCmd.Parameters.AddWithValue(request.CurrentStepOrder);
        reqCmd.Parameters.AddWithValue(request.TotalSteps);
        reqCmd.Parameters.AddWithValue((int)request.Status);
        reqCmd.Parameters.AddWithValue(request.RequesterUserId);
        reqCmd.Parameters.AddWithValue(request.RequesterEmploymentId);
        reqCmd.Parameters.AddWithValue((object?)request.PayloadSnapshotJson ?? "{}");
        reqCmd.Parameters.AddWithValue((long)request.RowVersion);

        await reqCmd.ExecuteNonQueryAsync();

        foreach (var step in steps)
        {
            await using var stepCmd = conn.CreateCommand();
            stepCmd.Transaction = tx;
            stepCmd.CommandText = """
                INSERT INTO approvals.approval_steps (
                    id, approval_request_id, step_order, assigned_approver_user_id,
                    assigned_role, status, row_version
                ) VALUES ($1, $2, $3, $4, $5, $6, $7);
            """;
            stepCmd.Parameters.AddWithValue(step.Id);
            stepCmd.Parameters.AddWithValue(step.ApprovalRequestId);
            stepCmd.Parameters.AddWithValue(step.StepOrder);
            stepCmd.Parameters.AddWithValue((object?)step.AssignedApproverUserId ?? DBNull.Value);
            stepCmd.Parameters.AddWithValue((object?)step.AssignedRole ?? DBNull.Value);
            stepCmd.Parameters.AddWithValue((int)step.Status);
            stepCmd.Parameters.AddWithValue((long)step.RowVersion);
            await stepCmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
    }

    public async Task SaveApprovalRequestAsync(ApprovalRequest request)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();

        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            UPDATE approvals.approval_requests SET
                current_step_order = $1,
                status = $2,
                updated_at = CURRENT_TIMESTAMP,
                row_version = approvals.approval_requests.row_version + 1
            WHERE tenant_id = $3 AND id = $4;
        """;
        cmd.Parameters.AddWithValue(request.CurrentStepOrder);
        cmd.Parameters.AddWithValue((int)request.Status);
        cmd.Parameters.AddWithValue(request.TenantId.Value);
        cmd.Parameters.AddWithValue(request.Id);

        await cmd.ExecuteNonQueryAsync();

        foreach (var hist in request.History)
        {
            await using var histCmd = conn.CreateCommand();
            histCmd.Transaction = tx;
            histCmd.CommandText = """
                INSERT INTO approvals.decision_histories (
                    id, approval_request_id, step_order, actor_user_id, action, reason, timestamp_utc
                ) VALUES ($1, $2, $3, $4, $5, $6, $7)
                ON CONFLICT (id) DO NOTHING;
            """;
            histCmd.Parameters.AddWithValue(hist.Id);
            histCmd.Parameters.AddWithValue(hist.ApprovalRequestId);
            histCmd.Parameters.AddWithValue(hist.StepOrder);
            histCmd.Parameters.AddWithValue(hist.ActorUserId);
            histCmd.Parameters.AddWithValue(hist.Action);
            histCmd.Parameters.AddWithValue((object?)hist.Reason ?? DBNull.Value);
            histCmd.Parameters.AddWithValue(hist.TimestampUtc);
            await histCmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
    }
}
