using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Workforce.Modules.Integrations.Domain;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Integrations.Infrastructure;

public record PagedDeliveriesResult(
    IReadOnlyList<IntegrationDeliveryJob> Items,
    long TotalCount,
    int Page,
    int PageSize
);

public interface IIntegrationsRepository
{
    Task<IReadOnlyList<IntegrationConnector>> ListConnectorsAsync(TenantId tenantId, CancellationToken ct = default);
    Task<IntegrationConnector?> GetConnectorByIdAsync(TenantId tenantId, Guid id, CancellationToken ct = default);
    Task<IntegrationConnector?> GetConnectorByCodeAsync(TenantId tenantId, string code, CancellationToken ct = default);
    Task CreateConnectorAsync(IntegrationConnector connector, CancellationToken ct = default);
    Task UpdateConnectorAsync(IntegrationConnector connector, CancellationToken ct = default);

    Task<bool> QueueDeliveryAsync(IntegrationDeliveryJob job, CancellationToken ct = default);
    Task<IReadOnlyList<IntegrationDeliveryJob>> GetPendingDeliveriesAsync(int batchSize = 50, CancellationToken ct = default);
    Task UpdateDeliveryStatusAsync(IntegrationDeliveryJob job, CancellationToken ct = default);
    Task<PagedDeliveriesResult> ListDeliveriesAsync(TenantId tenantId, Guid? connectorId, DeliveryStatus? status, int page, int pageSize, CancellationToken ct = default);
    Task<bool> RetryDeliveryAsync(TenantId tenantId, Guid deliveryId, CancellationToken ct = default);

    Task<bool> RecordInboxMessageAsync(IntegrationInboxMessage message, CancellationToken ct = default);
}

public class IntegrationsRepository : IIntegrationsRepository
{
    private readonly string _connectionString;

    public IntegrationsRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<IntegrationConnector>> ListConnectorsAsync(TenantId tenantId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id, tenant_id, code, name_en, name_ar, connector_type, direction,
                   endpoint_url, auth_type, encrypted_credentials, credentials_key_version,
                   is_active, event_subscriptions_json, config_json, row_version
            FROM integrations.connectors
            WHERE tenant_id = @tenantId
            ORDER BY code;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);

        var list = new List<IntegrationConnector>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new IntegrationConnector(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                (ConnectorType)reader.GetInt32(5),
                (IntegrationDirection)reader.GetInt32(6),
                reader.GetString(7),
                (IntegrationAuthType)reader.GetInt32(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.GetInt32(10),
                reader.GetBoolean(11),
                reader.GetString(12),
                reader.GetString(13),
                (uint)reader.GetInt64(14)
            ));
        }

        return list;
    }

    public async Task<IntegrationConnector?> GetConnectorByIdAsync(TenantId tenantId, Guid id, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id, tenant_id, code, name_en, name_ar, connector_type, direction,
                   endpoint_url, auth_type, encrypted_credentials, credentials_key_version,
                   is_active, event_subscriptions_json, config_json, row_version
            FROM integrations.connectors
            WHERE id = @id AND tenant_id = @tenantId
            LIMIT 1;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new IntegrationConnector(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                (ConnectorType)reader.GetInt32(5),
                (IntegrationDirection)reader.GetInt32(6),
                reader.GetString(7),
                (IntegrationAuthType)reader.GetInt32(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.GetInt32(10),
                reader.GetBoolean(11),
                reader.GetString(12),
                reader.GetString(13),
                (uint)reader.GetInt64(14)
            );
        }

        return null;
    }

    public async Task<IntegrationConnector?> GetConnectorByCodeAsync(TenantId tenantId, string code, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id, tenant_id, code, name_en, name_ar, connector_type, direction,
                   endpoint_url, auth_type, encrypted_credentials, credentials_key_version,
                   is_active, event_subscriptions_json, config_json, row_version
            FROM integrations.connectors
            WHERE code = @code AND tenant_id = @tenantId
            LIMIT 1;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("code", code.Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new IntegrationConnector(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                (ConnectorType)reader.GetInt32(5),
                (IntegrationDirection)reader.GetInt32(6),
                reader.GetString(7),
                (IntegrationAuthType)reader.GetInt32(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.GetInt32(10),
                reader.GetBoolean(11),
                reader.GetString(12),
                reader.GetString(13),
                (uint)reader.GetInt64(14)
            );
        }

        return null;
    }

    public async Task CreateConnectorAsync(IntegrationConnector connector, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            INSERT INTO integrations.connectors (
                id, tenant_id, code, name_en, name_ar, connector_type, direction,
                endpoint_url, auth_type, encrypted_credentials, credentials_key_version,
                is_active, event_subscriptions_json, config_json, row_version
            ) VALUES (
                @id, @tenantId, @code, @nameEn, @nameAr, @cType, @dir,
                @endpoint, @auth, @creds, @keyVer, @isActive, @subs::jsonb, @cfg::jsonb, @ver
            );
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", connector.Id);
        cmd.Parameters.AddWithValue("tenantId", connector.TenantId.Value);
        cmd.Parameters.AddWithValue("code", connector.Code);
        cmd.Parameters.AddWithValue("nameEn", connector.NameEn);
        cmd.Parameters.AddWithValue("nameAr", connector.NameAr);
        cmd.Parameters.AddWithValue("cType", (int)connector.ConnectorType);
        cmd.Parameters.AddWithValue("dir", (int)connector.Direction);
        cmd.Parameters.AddWithValue("endpoint", connector.EndpointUrl);
        cmd.Parameters.AddWithValue("auth", (int)connector.AuthType);
        cmd.Parameters.AddWithValue("creds", (object?)connector.EncryptedCredentials ?? DBNull.Value);
        cmd.Parameters.AddWithValue("keyVer", connector.CredentialsKeyVersion);
        cmd.Parameters.AddWithValue("isActive", connector.IsActive);
        cmd.Parameters.AddWithValue("subs", connector.EventSubscriptionsJson);
        cmd.Parameters.AddWithValue("cfg", connector.ConfigJson);
        cmd.Parameters.AddWithValue("ver", (long)connector.RowVersion);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateConnectorAsync(IntegrationConnector connector, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            UPDATE integrations.connectors
            SET name_en = @nameEn,
                name_ar = @nameAr,
                endpoint_url = @endpoint,
                auth_type = @auth,
                encrypted_credentials = @creds,
                credentials_key_version = @keyVer,
                is_active = @isActive,
                event_subscriptions_json = @subs::jsonb,
                config_json = @cfg::jsonb,
                row_version = @ver
            WHERE id = @id AND tenant_id = @tenantId AND row_version = @expectedVer;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("nameEn", connector.NameEn);
        cmd.Parameters.AddWithValue("nameAr", connector.NameAr);
        cmd.Parameters.AddWithValue("endpoint", connector.EndpointUrl);
        cmd.Parameters.AddWithValue("auth", (int)connector.AuthType);
        cmd.Parameters.AddWithValue("creds", (object?)connector.EncryptedCredentials ?? DBNull.Value);
        cmd.Parameters.AddWithValue("keyVer", connector.CredentialsKeyVersion);
        cmd.Parameters.AddWithValue("isActive", connector.IsActive);
        cmd.Parameters.AddWithValue("subs", connector.EventSubscriptionsJson);
        cmd.Parameters.AddWithValue("cfg", connector.ConfigJson);
        cmd.Parameters.AddWithValue("ver", (long)connector.RowVersion);
        cmd.Parameters.AddWithValue("id", connector.Id);
        cmd.Parameters.AddWithValue("tenantId", connector.TenantId.Value);
        cmd.Parameters.AddWithValue("expectedVer", (long)connector.RowVersion - 1);

        var affected = await cmd.ExecuteNonQueryAsync(ct);
        if (affected == 0)
        {
            throw new InvalidOperationException("Concurrency conflict updating integration connector.");
        }
    }

    public async Task<bool> QueueDeliveryAsync(IntegrationDeliveryJob job, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            INSERT INTO integrations.deliveries (
                id, tenant_id, connector_id, event_id, event_type, status,
                attempt_count, max_attempts, next_attempt_at_utc, last_attempt_at_utc,
                last_http_status, last_error_message, payload_json, idempotency_key, created_at_utc
            ) VALUES (
                @id, @tenantId, @connId, @eventId, @eventType, @status,
                @attempts, @maxAttempts, @nextAttempt, @lastAttempt,
                @httpStatus, @error, @payload::jsonb, @idempKey, @createdAt
            ) ON CONFLICT (tenant_id, idempotency_key) DO NOTHING;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", job.Id);
        cmd.Parameters.AddWithValue("tenantId", job.TenantId.Value);
        cmd.Parameters.AddWithValue("connId", job.ConnectorId);
        cmd.Parameters.AddWithValue("eventId", job.EventId);
        cmd.Parameters.AddWithValue("eventType", job.EventType);
        cmd.Parameters.AddWithValue("status", (int)job.Status);
        cmd.Parameters.AddWithValue("attempts", job.AttemptCount);
        cmd.Parameters.AddWithValue("maxAttempts", job.MaxAttempts);
        cmd.Parameters.AddWithValue("nextAttempt", (object?)job.NextAttemptAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("lastAttempt", (object?)job.LastAttemptAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("httpStatus", (object?)job.LastHttpStatus ?? DBNull.Value);
        cmd.Parameters.AddWithValue("error", (object?)job.LastErrorMessage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("payload", job.PayloadJson);
        cmd.Parameters.AddWithValue("idempKey", job.IdempotencyKey);
        cmd.Parameters.AddWithValue("createdAt", job.CreatedAtUtc);

        var affected = await cmd.ExecuteNonQueryAsync(ct);
        return affected > 0;
    }

    public async Task<IReadOnlyList<IntegrationDeliveryJob>> GetPendingDeliveriesAsync(int batchSize = 50, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id, tenant_id, connector_id, event_id, event_type, status,
                   attempt_count, max_attempts, next_attempt_at_utc, last_attempt_at_utc,
                   last_http_status, last_error_message, payload_json, idempotency_key, created_at_utc
            FROM integrations.deliveries
            WHERE status IN (1, 4) AND next_attempt_at_utc <= NOW()
            ORDER BY next_attempt_at_utc ASC
            LIMIT @limit;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("limit", batchSize);

        var list = new List<IntegrationDeliveryJob>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var job = new IntegrationDeliveryJob(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.GetGuid(2),
                reader.GetGuid(3),
                reader.GetString(4),
                reader.GetString(12),
                reader.GetString(13),
                reader.GetInt32(7)
            );
            list.Add(job);
        }

        return list;
    }

    public async Task UpdateDeliveryStatusAsync(IntegrationDeliveryJob job, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            UPDATE integrations.deliveries
            SET status = @status,
                attempt_count = @attempts,
                next_attempt_at_utc = @nextAttempt,
                last_attempt_at_utc = @lastAttempt,
                last_http_status = @httpStatus,
                last_error_message = @error
            WHERE id = @id AND tenant_id = @tenantId;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("status", (int)job.Status);
        cmd.Parameters.AddWithValue("attempts", job.AttemptCount);
        cmd.Parameters.AddWithValue("nextAttempt", (object?)job.NextAttemptAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("lastAttempt", (object?)job.LastAttemptAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("httpStatus", (object?)job.LastHttpStatus ?? DBNull.Value);
        cmd.Parameters.AddWithValue("error", (object?)job.LastErrorMessage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("id", job.Id);
        cmd.Parameters.AddWithValue("tenantId", job.TenantId.Value);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<PagedDeliveriesResult> ListDeliveriesAsync(TenantId tenantId, Guid? connectorId, DeliveryStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var whereClause = "WHERE tenant_id = @tenantId";
        var cmd = new NpgsqlCommand { Connection = conn };
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);

        if (connectorId.HasValue)
        {
            whereClause += " AND connector_id = @connId";
            cmd.Parameters.AddWithValue("connId", connectorId.Value);
        }

        if (status.HasValue)
        {
            whereClause += " AND status = @status";
            cmd.Parameters.AddWithValue("status", (int)status.Value);
        }

        var countCmd = new NpgsqlCommand($"SELECT COUNT(*) FROM integrations.deliveries {whereClause};", conn);
        foreach (NpgsqlParameter p in cmd.Parameters) countCmd.Parameters.AddWithValue(p.ParameterName, p.Value!);
        var totalCount = (long)(await countCmd.ExecuteScalarAsync(ct) ?? 0L);

        var limit = Math.Clamp(pageSize, 1, 100);
        var pNum = Math.Max(1, page);
        var offset = (pNum - 1) * limit;

        var listSql = $@"
            SELECT id, tenant_id, connector_id, event_id, event_type, status,
                   attempt_count, max_attempts, next_attempt_at_utc, last_attempt_at_utc,
                   last_http_status, last_error_message, payload_json, idempotency_key, created_at_utc
            FROM integrations.deliveries
            {whereClause}
            ORDER BY created_at_utc DESC
            LIMIT {limit} OFFSET {offset};
        ";

        cmd.CommandText = listSql;
        var items = new List<IntegrationDeliveryJob>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            items.Add(new IntegrationDeliveryJob(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.GetGuid(2),
                reader.GetGuid(3),
                reader.GetString(4),
                reader.GetString(12),
                reader.GetString(13),
                reader.GetInt32(7)
            ));
        }

        return new PagedDeliveriesResult(items, totalCount, pNum, limit);
    }

    public async Task<bool> RetryDeliveryAsync(TenantId tenantId, Guid deliveryId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            UPDATE integrations.deliveries
            SET status = 1, next_attempt_at_utc = NOW()
            WHERE id = @id AND tenant_id = @tenantId;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", deliveryId);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);

        var affected = await cmd.ExecuteNonQueryAsync(ct);
        return affected > 0;
    }

    public async Task<bool> RecordInboxMessageAsync(IntegrationInboxMessage message, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            INSERT INTO integrations.inbox (
                id, tenant_id, provider_code, external_message_id, payload_json, received_at_utc, status
            ) VALUES (
                @id, @tenantId, @provider, @extId, @payload::jsonb, @received, @status
            ) ON CONFLICT (tenant_id, provider_code, external_message_id) DO NOTHING;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", message.Id);
        cmd.Parameters.AddWithValue("tenantId", message.TenantId.Value);
        cmd.Parameters.AddWithValue("provider", message.ProviderCode);
        cmd.Parameters.AddWithValue("extId", message.ExternalMessageId);
        cmd.Parameters.AddWithValue("payload", message.PayloadJson);
        cmd.Parameters.AddWithValue("received", message.ReceivedAtUtc);
        cmd.Parameters.AddWithValue("status", message.Status);

        var affected = await cmd.ExecuteNonQueryAsync(ct);
        return affected > 0;
    }
}
