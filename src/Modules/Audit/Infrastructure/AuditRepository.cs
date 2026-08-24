using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Workforce.Modules.Audit.Domain;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Audit.Infrastructure;

public record AuditSearchFilter(
    Guid? ActorUserId = null,
    string? ActionCode = null,
    string? EntityType = null,
    string? EntityId = null,
    string? CorrelationId = null,
    Guid? LegalEntityId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 50
);

public record PagedAuditResult(
    IReadOnlyList<AuditRecord> Items,
    long TotalCount,
    int Page,
    int PageSize
);

public interface IAuditRepository
{
    Task RecordAsync(AuditRecord record, CancellationToken ct = default);
    Task RecordBatchAsync(IEnumerable<AuditRecord> records, CancellationToken ct = default);
    Task<PagedAuditResult> SearchAsync(TenantId tenantId, AuditSearchFilter filter, CancellationToken ct = default);
    Task<AuditRecord?> GetByIdAsync(TenantId tenantId, Guid id, CancellationToken ct = default);
}

public class AuditRepository : IAuditRepository
{
    private readonly string _connectionString;

    public AuditRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task RecordAsync(AuditRecord record, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            INSERT INTO audit.audit_records (
                id, tenant_id, legal_entity_id, actor_user_id, actor_type, action_code,
                entity_type, entity_id, occurred_at_utc, correlation_id, ip_address,
                user_agent, reason_code, changes_before_json, changes_after_json,
                safe_metadata_json, data_classification
            ) VALUES (
                @id, @tenantId, @legalEntityId, @actorUserId, @actorType, @actionCode,
                @entityType, @entityId, @occurredAtUtc, @correlationId, @ipAddress,
                @userAgent, @reasonCode, @changesBefore::jsonb, @changesAfter::jsonb,
                @safeMetadata::jsonb, @dataClassification
            );
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", record.Id);
        cmd.Parameters.AddWithValue("tenantId", record.TenantId.Value);
        cmd.Parameters.AddWithValue("legalEntityId", record.LegalEntityId.HasValue ? record.LegalEntityId.Value.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("actorUserId", record.ActorUserId);
        cmd.Parameters.AddWithValue("actorType", record.ActorType);
        cmd.Parameters.AddWithValue("actionCode", record.ActionCode);
        cmd.Parameters.AddWithValue("entityType", record.EntityType);
        cmd.Parameters.AddWithValue("entityId", record.EntityId);
        cmd.Parameters.AddWithValue("occurredAtUtc", record.OccurredAtUtc);
        cmd.Parameters.AddWithValue("correlationId", (object?)record.CorrelationId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("ipAddress", (object?)record.IpAddress ?? DBNull.Value);
        cmd.Parameters.AddWithValue("userAgent", (object?)record.UserAgent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("reasonCode", (object?)record.ReasonCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("changesBefore", (object?)record.ChangesBeforeJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("changesAfter", (object?)record.ChangesAfterJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("safeMetadata", (object?)record.SafeMetadataJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("dataClassification", record.DataClassification);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task RecordBatchAsync(IEnumerable<AuditRecord> records, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        const string sql = @"
            INSERT INTO audit.audit_records (
                id, tenant_id, legal_entity_id, actor_user_id, actor_type, action_code,
                entity_type, entity_id, occurred_at_utc, correlation_id, ip_address,
                user_agent, reason_code, changes_before_json, changes_after_json,
                safe_metadata_json, data_classification
            ) VALUES (
                @id, @tenantId, @legalEntityId, @actorUserId, @actorType, @actionCode,
                @entityType, @entityId, @occurredAtUtc, @correlationId, @ipAddress,
                @userAgent, @reasonCode, @changesBefore::jsonb, @changesAfter::jsonb,
                @safeMetadata::jsonb, @dataClassification
            );
        ";

        foreach (var record in records)
        {
            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("id", record.Id);
            cmd.Parameters.AddWithValue("tenantId", record.TenantId.Value);
            cmd.Parameters.AddWithValue("legalEntityId", record.LegalEntityId.HasValue ? record.LegalEntityId.Value.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("actorUserId", record.ActorUserId);
            cmd.Parameters.AddWithValue("actorType", record.ActorType);
            cmd.Parameters.AddWithValue("actionCode", record.ActionCode);
            cmd.Parameters.AddWithValue("entityType", record.EntityType);
            cmd.Parameters.AddWithValue("entityId", record.EntityId);
            cmd.Parameters.AddWithValue("occurredAtUtc", record.OccurredAtUtc);
            cmd.Parameters.AddWithValue("correlationId", (object?)record.CorrelationId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("ipAddress", (object?)record.IpAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("userAgent", (object?)record.UserAgent ?? DBNull.Value);
            cmd.Parameters.AddWithValue("reasonCode", (object?)record.ReasonCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("changesBefore", (object?)record.ChangesBeforeJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("changesAfter", (object?)record.ChangesAfterJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("safeMetadata", (object?)record.SafeMetadataJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("dataClassification", record.DataClassification);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task<PagedAuditResult> SearchAsync(TenantId tenantId, AuditSearchFilter filter, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var whereSb = new StringBuilder("WHERE tenant_id = @tenantId");
        var cmd = new NpgsqlCommand { Connection = conn };
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);

        if (filter.ActorUserId.HasValue)
        {
            whereSb.Append(" AND actor_user_id = @actorUserId");
            cmd.Parameters.AddWithValue("actorUserId", filter.ActorUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.ActionCode))
        {
            whereSb.Append(" AND action_code = @actionCode");
            cmd.Parameters.AddWithValue("actionCode", filter.ActionCode.Trim().ToLowerInvariant());
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
        {
            whereSb.Append(" AND entity_type = @entityType");
            cmd.Parameters.AddWithValue("entityType", filter.EntityType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityId))
        {
            whereSb.Append(" AND entity_id = @entityId");
            cmd.Parameters.AddWithValue("entityId", filter.EntityId.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filter.CorrelationId))
        {
            whereSb.Append(" AND correlation_id = @correlationId");
            cmd.Parameters.AddWithValue("correlationId", filter.CorrelationId.Trim());
        }

        if (filter.LegalEntityId.HasValue)
        {
            whereSb.Append(" AND legal_entity_id = @legalEntityId");
            cmd.Parameters.AddWithValue("legalEntityId", filter.LegalEntityId.Value);
        }

        if (filter.FromUtc.HasValue)
        {
            whereSb.Append(" AND occurred_at_utc >= @fromUtc");
            cmd.Parameters.AddWithValue("fromUtc", filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue)
        {
            whereSb.Append(" AND occurred_at_utc <= @toUtc");
            cmd.Parameters.AddWithValue("toUtc", filter.ToUtc.Value);
        }

        // Count Total
        var countSql = $"SELECT COUNT(*) FROM audit.audit_records {whereSb};";
        cmd.CommandText = countSql;
        var totalCount = (long)(await cmd.ExecuteScalarAsync(ct) ?? 0L);

        // Fetch Paged Items
        var pageSize = Math.Clamp(filter.PageSize, 1, 200);
        var page = Math.Max(1, filter.Page);
        var offset = (page - 1) * pageSize;

        var querySql = $@"
            SELECT id, tenant_id, legal_entity_id, actor_user_id, actor_type, action_code,
                   entity_type, entity_id, occurred_at_utc, correlation_id, ip_address,
                   user_agent, reason_code, changes_before_json, changes_after_json,
                   safe_metadata_json, data_classification
            FROM audit.audit_records
            {whereSb}
            ORDER BY occurred_at_utc DESC
            LIMIT {pageSize} OFFSET {offset};
        ";

        cmd.CommandText = querySql;
        var items = new List<AuditRecord>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            items.Add(MapReaderToAuditRecord(reader));
        }

        return new PagedAuditResult(items, totalCount, page, pageSize);
    }

    public async Task<AuditRecord?> GetByIdAsync(TenantId tenantId, Guid id, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id, tenant_id, legal_entity_id, actor_user_id, actor_type, action_code,
                   entity_type, entity_id, occurred_at_utc, correlation_id, ip_address,
                   user_agent, reason_code, changes_before_json, changes_after_json,
                   safe_metadata_json, data_classification
            FROM audit.audit_records
            WHERE id = @id AND tenant_id = @tenantId
            LIMIT 1;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return MapReaderToAuditRecord(reader);
        }

        return null;
    }

    private static AuditRecord MapReaderToAuditRecord(NpgsqlDataReader reader)
    {
        return new AuditRecord(
            reader.GetGuid(0),
            new TenantId(reader.GetGuid(1)),
            reader.IsDBNull(2) ? null : new LegalEntityId(reader.GetGuid(2)),
            reader.GetGuid(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetString(7),
            reader.GetDateTime(8),
            reader.IsDBNull(9) ? null : reader.GetString(9),
            reader.IsDBNull(10) ? null : reader.GetString(10),
            reader.IsDBNull(11) ? null : reader.GetString(11),
            reader.IsDBNull(12) ? null : reader.GetString(12),
            reader.IsDBNull(13) ? null : reader.GetString(13),
            reader.IsDBNull(14) ? null : reader.GetString(14),
            reader.IsDBNull(15) ? null : reader.GetString(15),
            reader.GetString(16)
        );
    }
}
