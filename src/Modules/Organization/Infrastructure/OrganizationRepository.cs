using Npgsql;
using Workforce.Modules.Organization.Application;
using Workforce.Modules.Organization.Domain;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Organization.Infrastructure;

public class OrganizationRepository
{
    private readonly string _connectionString;

    public OrganizationRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<OrganizationUnit?> GetUnitByIdAsync(Guid id, TenantId tenantId, LegalEntityId? legalEntityId = null, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var sql = @"
            SELECT id, tenant_id, legal_entity_id, code, name_en, name_ar, type, 
                   parent_unit_id, manager_position_id, is_active, effective_from, effective_to, 
                   created_at, updated_at, row_version
            FROM organization.organization_units
            WHERE id = @id AND tenant_id = @tenantId
        ";
        if (legalEntityId.HasValue) sql += " AND legal_entity_id = @legalEntityId";
        sql += ";";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        if (legalEntityId.HasValue) cmd.Parameters.AddWithValue("legalEntityId", legalEntityId.Value.Value);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        var effectiveFrom = DateOnly.FromDateTime(reader.GetDateTime(10));
        DateOnly? effectiveTo = reader.IsDBNull(11) ? null : DateOnly.FromDateTime(reader.GetDateTime(11));

        return OrganizationUnit.Rehydrate(
            reader.GetGuid(0),
            new TenantId(reader.GetGuid(1)),
            new LegalEntityId(reader.GetGuid(2)),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            (OrganizationUnitType)reader.GetInt32(6),
            reader.IsDBNull(7) ? null : reader.GetGuid(7),
            new EffectivePeriod(effectiveFrom, effectiveTo),
            reader.IsDBNull(8) ? null : reader.GetGuid(8),
            reader.GetBoolean(9),
            reader.GetDateTime(12),
            reader.GetDateTime(13),
            (uint)reader.GetInt32(14)
        );
    }

    public async Task<IReadOnlyList<OrganizationUnitDto>> ListUnitsAsync(TenantId tenantId, LegalEntityId? legalEntityId = null, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var sql = @"
            SELECT u.id, u.tenant_id, u.legal_entity_id, u.code, u.name_en, u.name_ar, u.type, 
                   u.parent_unit_id, p.name_en as parent_name_en, u.manager_position_id, 
                   u.is_active, u.effective_from, u.effective_to, u.row_version
            FROM organization.organization_units u
            LEFT JOIN organization.organization_units p ON u.parent_unit_id = p.id
            WHERE u.tenant_id = @tenantId
        ";

        if (legalEntityId != null)
        {
            sql += " AND u.legal_entity_id = @legalEntityId";
        }

        sql += " ORDER BY u.type ASC, u.name_en ASC;";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        if (legalEntityId != null)
        {
            cmd.Parameters.AddWithValue("legalEntityId", legalEntityId.Value.Value);
        }

        var results = new List<OrganizationUnitDto>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new OrganizationUnitDto
            {
                Id = reader.GetGuid(0),
                TenantId = reader.GetGuid(1).ToString(),
                LegalEntityId = reader.GetGuid(2).ToString(),
                Code = reader.GetString(3),
                NameEn = reader.GetString(4),
                NameAr = reader.GetString(5),
                Type = ((OrganizationUnitType)reader.GetInt32(6)).ToString(),
                ParentUnitId = reader.IsDBNull(7) ? null : reader.GetGuid(7),
                ParentNameEn = reader.IsDBNull(8) ? null : reader.GetString(8),
                ManagerPositionId = reader.IsDBNull(9) ? null : reader.GetGuid(9),
                IsActive = reader.GetBoolean(10),
                EffectiveFrom = DateOnly.FromDateTime(reader.GetDateTime(11)).ToString("yyyy-MM-dd"),
                EffectiveTo = reader.IsDBNull(12) ? null : DateOnly.FromDateTime(reader.GetDateTime(12)).ToString("yyyy-MM-dd"),
                RowVersion = (uint)reader.GetInt32(13)
            });
        }

        return results;
    }

    public async Task InsertUnitAsync(OrganizationUnit unit, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            INSERT INTO organization.organization_units (
                id, tenant_id, legal_entity_id, code, name_en, name_ar, type, 
                parent_unit_id, manager_position_id, is_active, effective_from, effective_to, 
                created_at, updated_at, row_version
            ) VALUES (
                @id, @tenantId, @legalEntityId, @code, @nameEn, @nameAr, @type, 
                @parentUnitId, @managerPositionId, @isActive, @effectiveFrom, @effectiveTo, 
                @createdAt, @updatedAt, @rowVersion
            );
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", unit.Id);
        cmd.Parameters.AddWithValue("tenantId", unit.TenantId.Value);
        cmd.Parameters.AddWithValue("legalEntityId", unit.LegalEntityId.Value);
        cmd.Parameters.AddWithValue("code", unit.Code);
        cmd.Parameters.AddWithValue("nameEn", unit.NameEn);
        cmd.Parameters.AddWithValue("nameAr", unit.NameAr);
        cmd.Parameters.AddWithValue("type", (int)unit.Type);
        cmd.Parameters.AddWithValue("parentUnitId", (object?)unit.ParentUnitId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("managerPositionId", (object?)unit.ManagerPositionId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("isActive", unit.IsActive);
        cmd.Parameters.AddWithValue("effectiveFrom", unit.EffectivePeriod.EffectiveFrom.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue("effectiveTo", unit.EffectivePeriod.EffectiveTo.HasValue ? unit.EffectivePeriod.EffectiveTo.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
        cmd.Parameters.AddWithValue("createdAt", unit.CreatedAt);
        cmd.Parameters.AddWithValue("updatedAt", unit.UpdatedAt);
        cmd.Parameters.AddWithValue("rowVersion", (int)unit.RowVersion);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> UpdateUnitAsync(OrganizationUnit unit, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            UPDATE organization.organization_units
            SET name_en = @nameEn,
                name_ar = @nameAr,
                type = @type,
                parent_unit_id = @parentUnitId,
                manager_position_id = @managerPositionId,
                is_active = @isActive,
                effective_from = @effectiveFrom,
                effective_to = @effectiveTo,
                updated_at = @updatedAt,
                row_version = @rowVersion
            WHERE id = @id AND tenant_id = @tenantId AND legal_entity_id = @legalEntityId AND row_version = @oldRowVersion;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", unit.Id);
        cmd.Parameters.AddWithValue("tenantId", unit.TenantId.Value);
        cmd.Parameters.AddWithValue("legalEntityId", unit.LegalEntityId.Value);
        cmd.Parameters.AddWithValue("nameEn", unit.NameEn);
        cmd.Parameters.AddWithValue("nameAr", unit.NameAr);
        cmd.Parameters.AddWithValue("type", (int)unit.Type);
        cmd.Parameters.AddWithValue("parentUnitId", (object?)unit.ParentUnitId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("managerPositionId", (object?)unit.ManagerPositionId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("isActive", unit.IsActive);
        cmd.Parameters.AddWithValue("effectiveFrom", unit.EffectivePeriod.EffectiveFrom.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue("effectiveTo", unit.EffectivePeriod.EffectiveTo.HasValue ? unit.EffectivePeriod.EffectiveTo.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
        cmd.Parameters.AddWithValue("updatedAt", unit.UpdatedAt);
        cmd.Parameters.AddWithValue("rowVersion", (int)unit.RowVersion);
        cmd.Parameters.AddWithValue("oldRowVersion", (int)unit.RowVersion - 1);

        var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);
        return rowsAffected > 0;
    }

    public async Task<bool> WouldCreateCycleAsync(
        Guid unitId,
        Guid parentUnitId,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = """
            WITH RECURSIVE ancestors AS (
                SELECT id, parent_unit_id
                FROM organization.organization_units
                WHERE id = @parentUnitId
                  AND tenant_id = @tenantId
                  AND legal_entity_id = @legalEntityId
                UNION ALL
                SELECT child.id, child.parent_unit_id
                FROM organization.organization_units child
                INNER JOIN ancestors parent ON child.id = parent.parent_unit_id
                WHERE child.tenant_id = @tenantId
                  AND child.legal_entity_id = @legalEntityId
            )
            SELECT EXISTS (SELECT 1 FROM ancestors WHERE id = @unitId);
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("unitId", unitId);
        cmd.Parameters.AddWithValue("parentUnitId", parentUnitId);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        cmd.Parameters.AddWithValue("legalEntityId", legalEntityId.Value);
        return (bool)(await cmd.ExecuteScalarAsync(ct) ?? false);
    }

    public async Task<bool> PositionExistsAsync(
        Guid positionId,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM organization.positions
                WHERE id = @id AND tenant_id = @tenantId AND legal_entity_id = @legalEntityId
            );
            """;
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", positionId);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        cmd.Parameters.AddWithValue("legalEntityId", legalEntityId.Value);
        return (bool)(await cmd.ExecuteScalarAsync(ct) ?? false);
    }

    public async Task<IReadOnlyList<PositionDto>> ListPositionsAsync(
        TenantId tenantId,
        LegalEntityId? legalEntityId = null,
        Guid? organizationUnitId = null,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var sql = """
            SELECT p.id, p.tenant_id, p.legal_entity_id, p.organization_unit_id,
                   p.job_code, p.title_en, p.title_ar, p.grade, p.is_active
            FROM organization.positions p
            WHERE p.tenant_id = @tenantId
            """;
        if (legalEntityId.HasValue) sql += " AND p.legal_entity_id = @legalEntityId";
        if (organizationUnitId.HasValue) sql += " AND p.organization_unit_id = @organizationUnitId";
        sql += " ORDER BY p.title_en ASC;";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        if (legalEntityId.HasValue) cmd.Parameters.AddWithValue("legalEntityId", legalEntityId.Value.Value);
        if (organizationUnitId.HasValue) cmd.Parameters.AddWithValue("organizationUnitId", organizationUnitId.Value);

        var list = new List<PositionDto>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new PositionDto
            {
                Id = reader.GetGuid(0),
                TenantId = reader.GetGuid(1).ToString(),
                LegalEntityId = reader.GetGuid(2).ToString(),
                OrganizationUnitId = reader.GetGuid(3),
                JobCode = reader.GetString(4),
                TitleEn = reader.GetString(5),
                TitleAr = reader.GetString(6),
                Grade = reader.GetString(7),
                IsActive = reader.GetBoolean(8)
            });
        }

        return list;
    }

    public async Task InsertPositionAsync(Position position, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = """
            INSERT INTO organization.positions (
                id, tenant_id, legal_entity_id, organization_unit_id,
                job_code, title_en, title_ar, grade, is_active, created_at
            ) VALUES (
                @id, @tenantId, @legalEntityId, @organizationUnitId,
                @jobCode, @titleEn, @titleAr, @grade, @isActive, @createdAt
            );
            """;
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", position.Id);
        cmd.Parameters.AddWithValue("tenantId", position.TenantId.Value);
        cmd.Parameters.AddWithValue("legalEntityId", position.LegalEntityId.Value);
        cmd.Parameters.AddWithValue("organizationUnitId", position.OrganizationUnitId);
        cmd.Parameters.AddWithValue("jobCode", position.JobCode);
        cmd.Parameters.AddWithValue("titleEn", position.TitleEn);
        cmd.Parameters.AddWithValue("titleAr", position.TitleAr);
        cmd.Parameters.AddWithValue("grade", position.Grade);
        cmd.Parameters.AddWithValue("isActive", position.IsActive);
        cmd.Parameters.AddWithValue("createdAt", position.CreatedAt);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<CostCenterDto>> ListCostCentersAsync(
        TenantId tenantId,
        LegalEntityId? legalEntityId = null,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var sql = """
            SELECT id, tenant_id, legal_entity_id, code, name_en, name_ar, is_active
            FROM organization.cost_centers
            WHERE tenant_id = @tenantId
            """;
        if (legalEntityId.HasValue) sql += " AND legal_entity_id = @legalEntityId";
        sql += " ORDER BY code ASC;";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        if (legalEntityId.HasValue) cmd.Parameters.AddWithValue("legalEntityId", legalEntityId.Value.Value);

        var list = new List<CostCenterDto>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new CostCenterDto
            {
                Id = reader.GetGuid(0),
                TenantId = reader.GetGuid(1).ToString(),
                LegalEntityId = reader.GetGuid(2).ToString(),
                Code = reader.GetString(3),
                NameEn = reader.GetString(4),
                NameAr = reader.GetString(5),
                IsActive = reader.GetBoolean(6)
            });
        }
        return list;
    }

    public async Task InsertCostCenterAsync(CostCenter costCenter, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = """
            INSERT INTO organization.cost_centers (
                id, tenant_id, legal_entity_id, code, name_en, name_ar, is_active, created_at
            ) VALUES (
                @id, @tenantId, @legalEntityId, @code, @nameEn, @nameAr, @isActive, @createdAt
            );
            """;
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", costCenter.Id);
        cmd.Parameters.AddWithValue("tenantId", costCenter.TenantId.Value);
        cmd.Parameters.AddWithValue("legalEntityId", costCenter.LegalEntityId.Value);
        cmd.Parameters.AddWithValue("code", costCenter.Code);
        cmd.Parameters.AddWithValue("nameEn", costCenter.NameEn);
        cmd.Parameters.AddWithValue("nameAr", costCenter.NameAr);
        cmd.Parameters.AddWithValue("isActive", costCenter.IsActive);
        cmd.Parameters.AddWithValue("createdAt", costCenter.CreatedAtUtc);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<LocationDto>> ListLocationsAsync(TenantId tenantId, LegalEntityId? legalEntityId = null, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var sql = @"
            SELECT id, tenant_id, legal_entity_id, code, name_en, name_ar, country, city, address, is_active
            FROM organization.locations
            WHERE tenant_id = @tenantId
        ";
        if (legalEntityId.HasValue) sql += " AND legal_entity_id = @legalEntityId";
        sql += " ORDER BY name_en ASC;";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        if (legalEntityId.HasValue) cmd.Parameters.AddWithValue("legalEntityId", legalEntityId.Value.Value);

        var list = new List<LocationDto>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new LocationDto
            {
                Id = reader.GetGuid(0),
                TenantId = reader.GetGuid(1).ToString(),
                LegalEntityId = reader.GetGuid(2).ToString(),
                Code = reader.GetString(3),
                NameEn = reader.GetString(4),
                NameAr = reader.GetString(5),
                Country = reader.GetString(6),
                City = reader.GetString(7),
                Address = reader.GetString(8),
                IsActive = reader.GetBoolean(9)
            });
        }
        return list;
    }

    public async Task InsertLocationAsync(Location location, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            INSERT INTO organization.locations (
                id, tenant_id, legal_entity_id, code, name_en, name_ar, country, city, address, is_active, created_at
            ) VALUES (
                @id, @tenantId, @legalEntityId, @code, @nameEn, @nameAr, @country, @city, @address, @isActive, @createdAt
            );
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", location.Id);
        cmd.Parameters.AddWithValue("tenantId", location.TenantId.Value);
        cmd.Parameters.AddWithValue("legalEntityId", location.LegalEntityId.Value);
        cmd.Parameters.AddWithValue("code", location.Code);
        cmd.Parameters.AddWithValue("nameEn", location.NameEn);
        cmd.Parameters.AddWithValue("nameAr", location.NameAr);
        cmd.Parameters.AddWithValue("country", location.Country);
        cmd.Parameters.AddWithValue("city", location.City);
        cmd.Parameters.AddWithValue("address", location.Address);
        cmd.Parameters.AddWithValue("isActive", location.IsActive);
        cmd.Parameters.AddWithValue("createdAt", location.CreatedAt);

        await cmd.ExecuteNonQueryAsync(ct);
    }
}
