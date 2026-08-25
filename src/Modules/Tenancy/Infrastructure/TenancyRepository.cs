using Npgsql;
using Workforce.Modules.Tenancy.Application;
using Workforce.Modules.Tenancy.Domain;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Tenancy.Infrastructure;

public sealed class TenancyRepository
{
    private readonly string _connectionString;

    public TenancyRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Tenant?> GetTenantAsync(TenantId tenantId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        const string sql = """
            SELECT id, code, name_en, name_ar, is_active, created_at_utc
            FROM platform.tenants
            WHERE id = @tenantId;
            """;
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        return Tenant.Rehydrate(
            new TenantId(reader.GetGuid(0)),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetBoolean(4),
            reader.GetDateTime(5));
    }

    public async Task<IReadOnlyList<LegalEntity>> ListLegalEntitiesAsync(
        TenantId tenantId,
        IReadOnlySet<LegalEntityId> allowedLegalEntities,
        CancellationToken ct = default)
    {
        if (allowedLegalEntities.Count == 0) return Array.Empty<LegalEntity>();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        const string sql = """
            SELECT id, tenant_id, code, name_en, name_ar, country_code, currency_code,
                   timezone_id, is_active, created_at_utc, updated_at_utc, row_version
            FROM platform.legal_entities
            WHERE tenant_id = @tenantId
              AND id = ANY(@legalEntityIds)
            ORDER BY code ASC;
            """;
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        cmd.Parameters.AddWithValue("legalEntityIds", allowedLegalEntities.Select(x => x.Value).ToArray());

        var entities = new List<LegalEntity>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            entities.Add(ReadLegalEntity(reader));
        }
        return entities;
    }

    public async Task<LegalEntity?> GetLegalEntityAsync(
        TenantId tenantId,
        LegalEntityId legalEntityId,
        IReadOnlySet<LegalEntityId> allowedLegalEntities,
        CancellationToken ct = default)
    {
        if (!allowedLegalEntities.Contains(legalEntityId)) return null;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        const string sql = """
            SELECT id, tenant_id, code, name_en, name_ar, country_code, currency_code,
                   timezone_id, is_active, created_at_utc, updated_at_utc, row_version
            FROM platform.legal_entities
            WHERE tenant_id = @tenantId AND id = @legalEntityId;
            """;
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        cmd.Parameters.AddWithValue("legalEntityId", legalEntityId.Value);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadLegalEntity(reader) : null;
    }

    public async Task InsertLegalEntityAsync(LegalEntity entity, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        const string sql = """
            INSERT INTO platform.legal_entities (
                id, tenant_id, code, name_en, name_ar, country_code, currency_code,
                timezone_id, is_active, created_at_utc, updated_at_utc, row_version
            ) VALUES (
                @id, @tenantId, @code, @nameEn, @nameAr, @countryCode, @currencyCode,
                @timezoneId, @isActive, @createdAt, @updatedAt, @rowVersion
            );
            """;
        await using var cmd = new NpgsqlCommand(sql, conn);
        AddLegalEntityParameters(cmd, entity);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> UpdateLegalEntityAsync(LegalEntity entity, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        const string sql = """
            UPDATE platform.legal_entities
            SET name_en = @nameEn,
                name_ar = @nameAr,
                country_code = @countryCode,
                currency_code = @currencyCode,
                timezone_id = @timezoneId,
                updated_at_utc = @updatedAt,
                row_version = @rowVersion
            WHERE id = @id AND tenant_id = @tenantId AND row_version = @oldRowVersion;
            """;
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", entity.Id.Value);
        cmd.Parameters.AddWithValue("tenantId", entity.TenantId.Value);
        cmd.Parameters.AddWithValue("nameEn", entity.NameEn);
        cmd.Parameters.AddWithValue("nameAr", entity.NameAr);
        cmd.Parameters.AddWithValue("countryCode", entity.CountryCode);
        cmd.Parameters.AddWithValue("currencyCode", entity.CurrencyCode);
        cmd.Parameters.AddWithValue("timezoneId", entity.TimezoneId);
        cmd.Parameters.AddWithValue("updatedAt", entity.UpdatedAtUtc);
        cmd.Parameters.AddWithValue("rowVersion", (long)entity.RowVersion);
        cmd.Parameters.AddWithValue("oldRowVersion", (long)entity.RowVersion - 1);
        return await cmd.ExecuteNonQueryAsync(ct) == 1;
    }

    private static LegalEntity ReadLegalEntity(NpgsqlDataReader reader)
    {
        return LegalEntity.Rehydrate(
            new LegalEntityId(reader.GetGuid(0)),
            new TenantId(reader.GetGuid(1)),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetString(7),
            reader.GetBoolean(8),
            reader.GetDateTime(9),
            reader.GetDateTime(10),
            (uint)reader.GetInt64(11));
    }

    private static void AddLegalEntityParameters(NpgsqlCommand cmd, LegalEntity entity)
    {
        cmd.Parameters.AddWithValue("id", entity.Id.Value);
        cmd.Parameters.AddWithValue("tenantId", entity.TenantId.Value);
        cmd.Parameters.AddWithValue("code", entity.Code);
        cmd.Parameters.AddWithValue("nameEn", entity.NameEn);
        cmd.Parameters.AddWithValue("nameAr", entity.NameAr);
        cmd.Parameters.AddWithValue("countryCode", entity.CountryCode);
        cmd.Parameters.AddWithValue("currencyCode", entity.CurrencyCode);
        cmd.Parameters.AddWithValue("timezoneId", entity.TimezoneId);
        cmd.Parameters.AddWithValue("isActive", entity.IsActive);
        cmd.Parameters.AddWithValue("createdAt", entity.CreatedAtUtc);
        cmd.Parameters.AddWithValue("updatedAt", entity.UpdatedAtUtc);
        cmd.Parameters.AddWithValue("rowVersion", (long)entity.RowVersion);
    }
}
