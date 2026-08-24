using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Workforce.Modules.Audit.Domain;
using Workforce.Modules.Audit.Infrastructure;
using Workforce.Modules.Identity.Domain;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Identity.Infrastructure;

public interface IAdministrationRepository
{
    Task<IReadOnlyList<Role>> ListRolesAsync(TenantId tenantId, CancellationToken ct = default);
    Task<Role?> GetRoleByIdAsync(TenantId tenantId, Guid roleId, CancellationToken ct = default);
    Task<Role?> GetRoleByCodeAsync(TenantId tenantId, string roleCode, CancellationToken ct = default);
    Task CreateRoleAsync(Role role, Guid callerUserId, HashSet<string> callerPermissions, CancellationToken ct = default);
    Task UpdateRoleAsync(Role role, Guid callerUserId, HashSet<string> callerPermissions, CancellationToken ct = default);

    Task<IReadOnlyList<RoleAssignment>> GetUserRoleAssignmentsAsync(TenantId tenantId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<RoleAssignment>> ListAllRoleAssignmentsAsync(TenantId tenantId, CancellationToken ct = default);
    Task AssignRoleAsync(RoleAssignment assignment, Guid callerUserId, HashSet<string> callerPermissions, CancellationToken ct = default);
    Task RevokeRoleAssignmentAsync(TenantId tenantId, Guid assignmentId, Guid callerUserId, CancellationToken ct = default);

    Task<IReadOnlyList<PlatformSetting>> ListSettingsAsync(TenantId tenantId, CancellationToken ct = default);
    Task SaveSettingAsync(PlatformSetting setting, Guid callerUserId, CancellationToken ct = default);

    Task<IReadOnlyList<RetentionPolicy>> ListRetentionPoliciesAsync(TenantId tenantId, CancellationToken ct = default);
    Task SaveRetentionPolicyAsync(RetentionPolicy policy, Guid callerUserId, CancellationToken ct = default);
}

public class AdministrationRepository : IAdministrationRepository
{
    private readonly string _connectionString;
    private readonly IAuditRepository _auditRepository;

    public AdministrationRepository(string connectionString, IAuditRepository auditRepository)
    {
        _connectionString = connectionString;
        _auditRepository = auditRepository;
    }

    public async Task<IReadOnlyList<Role>> ListRolesAsync(TenantId tenantId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id, tenant_id, code, name_en, name_ar, description, permissions_json, is_system_role, row_version
            FROM admin.roles
            WHERE tenant_id = @tenantId
            ORDER BY is_system_role DESC, code;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);

        var list = new List<Role>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new Role(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                reader.GetString(6),
                reader.GetBoolean(7),
                (uint)reader.GetInt64(8)
            ));
        }

        return list;
    }

    public async Task<Role?> GetRoleByIdAsync(TenantId tenantId, Guid roleId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id, tenant_id, code, name_en, name_ar, description, permissions_json, is_system_role, row_version
            FROM admin.roles
            WHERE id = @id AND tenant_id = @tenantId
            LIMIT 1;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", roleId);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new Role(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                reader.GetString(6),
                reader.GetBoolean(7),
                (uint)reader.GetInt64(8)
            );
        }

        return null;
    }

    public async Task<Role?> GetRoleByCodeAsync(TenantId tenantId, string roleCode, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id, tenant_id, code, name_en, name_ar, description, permissions_json, is_system_role, row_version
            FROM admin.roles
            WHERE code = @code AND tenant_id = @tenantId
            LIMIT 1;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("code", roleCode.Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new Role(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                reader.GetString(6),
                reader.GetBoolean(7),
                (uint)reader.GetInt64(8)
            );
        }

        return null;
    }

    public async Task CreateRoleAsync(Role role, Guid callerUserId, HashSet<string> callerPermissions, CancellationToken ct = default)
    {
        // Privilege Escalation Guard
        ValidatePrivilegeEscalation(role.GetPermissions(), callerPermissions);

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            INSERT INTO admin.roles (
                id, tenant_id, code, name_en, name_ar, description, permissions_json, is_system_role, row_version
            ) VALUES (
                @id, @tenantId, @code, @nameEn, @nameAr, @desc, @perms::jsonb, @isSys, @ver
            );
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", role.Id);
        cmd.Parameters.AddWithValue("tenantId", role.TenantId.Value);
        cmd.Parameters.AddWithValue("code", role.Code);
        cmd.Parameters.AddWithValue("nameEn", role.NameEn);
        cmd.Parameters.AddWithValue("nameAr", role.NameAr);
        cmd.Parameters.AddWithValue("desc", role.Description);
        cmd.Parameters.AddWithValue("perms", role.PermissionsJson);
        cmd.Parameters.AddWithValue("isSys", role.IsSystemRole);
        cmd.Parameters.AddWithValue("ver", (long)role.RowVersion);

        await cmd.ExecuteNonQueryAsync(ct);

        // Audit Record
        await _auditRepository.RecordAsync(new AuditRecord(
            Guid.NewGuid(),
            role.TenantId,
            null,
            callerUserId,
            "User",
            "role.created",
            "Role",
            role.Code,
            DateTime.UtcNow,
            changesAfterJson: role.PermissionsJson,
            dataClassification: "Internal"
        ), ct);
    }

    public async Task UpdateRoleAsync(Role role, Guid callerUserId, HashSet<string> callerPermissions, CancellationToken ct = default)
    {
        // Privilege Escalation Guard
        ValidatePrivilegeEscalation(role.GetPermissions(), callerPermissions);

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            UPDATE admin.roles
            SET name_en = @nameEn,
                name_ar = @nameAr,
                description = @desc,
                permissions_json = @perms::jsonb,
                row_version = @ver
            WHERE id = @id AND tenant_id = @tenantId AND row_version = @expectedVer;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("nameEn", role.NameEn);
        cmd.Parameters.AddWithValue("nameAr", role.NameAr);
        cmd.Parameters.AddWithValue("desc", role.Description);
        cmd.Parameters.AddWithValue("perms", role.PermissionsJson);
        cmd.Parameters.AddWithValue("ver", (long)role.RowVersion);
        cmd.Parameters.AddWithValue("id", role.Id);
        cmd.Parameters.AddWithValue("tenantId", role.TenantId.Value);
        cmd.Parameters.AddWithValue("expectedVer", (long)role.RowVersion - 1);

        var affected = await cmd.ExecuteNonQueryAsync(ct);
        if (affected == 0)
        {
            throw new InvalidOperationException($"Concurrency conflict updating role '{role.Code}'. Expected version was modified.");
        }

        // Audit Record
        await _auditRepository.RecordAsync(new AuditRecord(
            Guid.NewGuid(),
            role.TenantId,
            null,
            callerUserId,
            "User",
            "role.updated",
            "Role",
            role.Code,
            DateTime.UtcNow,
            changesAfterJson: role.PermissionsJson,
            dataClassification: "Internal"
        ), ct);
    }

    public async Task<IReadOnlyList<RoleAssignment>> GetUserRoleAssignmentsAsync(TenantId tenantId, Guid userId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id, tenant_id, user_id, role_id, legal_entity_scope_id, organization_unit_scope_id, assigned_by_user_id, assigned_at_utc
            FROM admin.role_assignments
            WHERE tenant_id = @tenantId AND user_id = @userId;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        cmd.Parameters.AddWithValue("userId", userId);

        var list = new List<RoleAssignment>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new RoleAssignment(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.GetGuid(2),
                reader.GetGuid(3),
                reader.IsDBNull(4) ? null : new LegalEntityId(reader.GetGuid(4)),
                reader.IsDBNull(5) ? null : reader.GetGuid(5),
                reader.GetGuid(6),
                reader.GetDateTime(7)
            ));
        }

        return list;
    }

    public async Task<IReadOnlyList<RoleAssignment>> ListAllRoleAssignmentsAsync(TenantId tenantId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id, tenant_id, user_id, role_id, legal_entity_scope_id, organization_unit_scope_id, assigned_by_user_id, assigned_at_utc
            FROM admin.role_assignments
            WHERE tenant_id = @tenantId
            ORDER BY assigned_at_utc DESC;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);

        var list = new List<RoleAssignment>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new RoleAssignment(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.GetGuid(2),
                reader.GetGuid(3),
                reader.IsDBNull(4) ? null : new LegalEntityId(reader.GetGuid(4)),
                reader.IsDBNull(5) ? null : reader.GetGuid(5),
                reader.GetGuid(6),
                reader.GetDateTime(7)
            ));
        }

        return list;
    }

    public async Task AssignRoleAsync(RoleAssignment assignment, Guid callerUserId, HashSet<string> callerPermissions, CancellationToken ct = default)
    {
        var role = await GetRoleByIdAsync(assignment.TenantId, assignment.RoleId, ct);
        if (role == null) throw new InvalidOperationException($"Role ID '{assignment.RoleId}' does not exist.");

        // Validate caller possesses all permissions in the target role
        ValidatePrivilegeEscalation(role.GetPermissions(), callerPermissions);

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            INSERT INTO admin.role_assignments (
                id, tenant_id, user_id, role_id, legal_entity_scope_id, organization_unit_scope_id, assigned_by_user_id, assigned_at_utc
            ) VALUES (
                @id, @tenantId, @userId, @roleId, @leScope, @ouScope, @assignedBy, @assignedAt
            ) ON CONFLICT (tenant_id, user_id, role_id, COALESCE(legal_entity_scope_id, '00000000-0000-0000-0000-000000000000'::uuid)) DO NOTHING;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", assignment.Id);
        cmd.Parameters.AddWithValue("tenantId", assignment.TenantId.Value);
        cmd.Parameters.AddWithValue("userId", assignment.UserId);
        cmd.Parameters.AddWithValue("roleId", assignment.RoleId);
        cmd.Parameters.AddWithValue("leScope", assignment.LegalEntityScopeId.HasValue ? assignment.LegalEntityScopeId.Value.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("ouScope", (object?)assignment.OrganizationUnitScopeId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("assignedBy", assignment.AssignedByUserId);
        cmd.Parameters.AddWithValue("assignedAt", assignment.AssignedAtUtc);

        await cmd.ExecuteNonQueryAsync(ct);

        await _auditRepository.RecordAsync(new AuditRecord(
            Guid.NewGuid(),
            assignment.TenantId,
            assignment.LegalEntityScopeId,
            callerUserId,
            "User",
            "role.assigned",
            "RoleAssignment",
            assignment.UserId.ToString(),
            DateTime.UtcNow,
            safeMetadataJson: JsonSerializer.Serialize(new { RoleCode = role.Code, UserId = assignment.UserId }),
            dataClassification: "Internal"
        ), ct);
    }

    public async Task RevokeRoleAssignmentAsync(TenantId tenantId, Guid assignmentId, Guid callerUserId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            DELETE FROM admin.role_assignments
            WHERE id = @id AND tenant_id = @tenantId;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", assignmentId);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);

        await cmd.ExecuteNonQueryAsync(ct);

        await _auditRepository.RecordAsync(new AuditRecord(
            Guid.NewGuid(),
            tenantId,
            null,
            callerUserId,
            "User",
            "role.revoked",
            "RoleAssignment",
            assignmentId.ToString(),
            DateTime.UtcNow,
            dataClassification: "Internal"
        ), ct);
    }

    public async Task<IReadOnlyList<PlatformSetting>> ListSettingsAsync(TenantId tenantId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id, tenant_id, category, key, value_json, effective_start_date, effective_end_date, is_current, changed_by_user_id, changed_at_utc, row_version
            FROM admin.platform_settings
            WHERE tenant_id = @tenantId AND is_current = TRUE
            ORDER BY category, key;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);

        var list = new List<PlatformSetting>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new PlatformSetting(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetDateTime(5),
                reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                reader.GetBoolean(7),
                reader.GetGuid(8),
                reader.GetDateTime(9),
                (uint)reader.GetInt64(10)
            ));
        }

        return list;
    }

    public async Task SaveSettingAsync(PlatformSetting setting, Guid callerUserId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            INSERT INTO admin.platform_settings (
                id, tenant_id, category, key, value_json, effective_start_date, effective_end_date, is_current, changed_by_user_id, changed_at_utc, row_version
            ) VALUES (
                @id, @tenantId, @category, @key, @value::jsonb, @start, @end, @isCurrent, @user, @changed, @ver
            ) ON CONFLICT (tenant_id, category, key, effective_start_date) DO UPDATE
            SET value_json = EXCLUDED.value_json,
                effective_end_date = EXCLUDED.effective_end_date,
                is_current = EXCLUDED.is_current,
                changed_by_user_id = EXCLUDED.changed_by_user_id,
                changed_at_utc = EXCLUDED.changed_at_utc,
                row_version = EXCLUDED.row_version;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", setting.Id);
        cmd.Parameters.AddWithValue("tenantId", setting.TenantId.Value);
        cmd.Parameters.AddWithValue("category", setting.Category);
        cmd.Parameters.AddWithValue("key", setting.Key);
        cmd.Parameters.AddWithValue("value", setting.ValueJson);
        cmd.Parameters.AddWithValue("start", setting.EffectiveStartDate);
        cmd.Parameters.AddWithValue("end", (object?)setting.EffectiveEndDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("isCurrent", setting.IsCurrent);
        cmd.Parameters.AddWithValue("user", callerUserId);
        cmd.Parameters.AddWithValue("changed", setting.ChangedAtUtc);
        cmd.Parameters.AddWithValue("ver", (long)setting.RowVersion);

        await cmd.ExecuteNonQueryAsync(ct);

        await _auditRepository.RecordAsync(new AuditRecord(
            Guid.NewGuid(),
            setting.TenantId,
            null,
            callerUserId,
            "User",
            "setting.updated",
            "PlatformSetting",
            $"{setting.Category}.{setting.Key}",
            DateTime.UtcNow,
            changesAfterJson: setting.ValueJson,
            dataClassification: "Restricted"
        ), ct);
    }

    public async Task<IReadOnlyList<RetentionPolicy>> ListRetentionPoliciesAsync(TenantId tenantId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id, tenant_id, module, data_category, retention_days, action_on_expiry, is_active, effective_start_date, changed_by_user_id, row_version
            FROM admin.retention_policies
            WHERE tenant_id = @tenantId
            ORDER BY module, data_category;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);

        var list = new List<RetentionPolicy>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new RetentionPolicy(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4),
                (ExpiryAction)reader.GetInt32(5),
                reader.GetBoolean(6),
                reader.GetDateTime(7),
                reader.GetGuid(8),
                (uint)reader.GetInt64(9)
            ));
        }

        return list;
    }

    public async Task SaveRetentionPolicyAsync(RetentionPolicy policy, Guid callerUserId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            INSERT INTO admin.retention_policies (
                id, tenant_id, module, data_category, retention_days, action_on_expiry, is_active, effective_start_date, changed_by_user_id, row_version
            ) VALUES (
                @id, @tenantId, @module, @category, @days, @action, @isActive, @start, @user, @ver
            ) ON CONFLICT (tenant_id, module, data_category) DO UPDATE
            SET retention_days = EXCLUDED.retention_days,
                action_on_expiry = EXCLUDED.action_on_expiry,
                is_active = EXCLUDED.is_active,
                changed_by_user_id = EXCLUDED.changed_by_user_id,
                row_version = EXCLUDED.row_version;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", policy.Id);
        cmd.Parameters.AddWithValue("tenantId", policy.TenantId.Value);
        cmd.Parameters.AddWithValue("module", policy.Module);
        cmd.Parameters.AddWithValue("category", policy.DataCategory);
        cmd.Parameters.AddWithValue("days", policy.RetentionDays);
        cmd.Parameters.AddWithValue("action", (int)policy.ActionOnExpiry);
        cmd.Parameters.AddWithValue("isActive", policy.IsActive);
        cmd.Parameters.AddWithValue("start", policy.EffectiveStartDate);
        cmd.Parameters.AddWithValue("user", callerUserId);
        cmd.Parameters.AddWithValue("ver", (long)policy.RowVersion);

        await cmd.ExecuteNonQueryAsync(ct);

        await _auditRepository.RecordAsync(new AuditRecord(
            Guid.NewGuid(),
            policy.TenantId,
            null,
            callerUserId,
            "User",
            "retention_policy.updated",
            "RetentionPolicy",
            $"{policy.Module}:{policy.DataCategory}",
            DateTime.UtcNow,
            dataClassification: "Restricted"
        ), ct);
    }

    private static void ValidatePrivilegeEscalation(HashSet<string> targetPermissions, HashSet<string> callerPermissions)
    {
        // If caller has wild-card '*', they can grant anything
        if (callerPermissions.Contains("*")) return;

        foreach (var perm in targetPermissions)
        {
            if (!callerPermissions.Contains(perm))
            {
                throw new UnauthorizedAccessException($"Privilege Escalation Blocked: You cannot assign or create a role with permission '{perm}' because you do not hold it.");
            }
        }
    }
}
