using System;
using System.Collections.Generic;
using System.Text.Json;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Identity.Domain;

public enum ExpiryAction
{
    Anonymize = 1,
    Archive = 2,
    Purge = 3
}

public class Role
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public string Code { get; private set; }
    public string NameEn { get; private set; }
    public string NameAr { get; private set; }
    public string Description { get; private set; }
    public string PermissionsJson { get; private set; }
    public bool IsSystemRole { get; private set; }
    public uint RowVersion { get; private set; }

    private Role()
    {
        Code = string.Empty;
        NameEn = string.Empty;
        NameAr = string.Empty;
        Description = string.Empty;
        PermissionsJson = "[]";
    }

    public Role(
        Guid id,
        TenantId tenantId,
        string code,
        string nameEn,
        string nameAr,
        string description,
        string permissionsJson,
        bool isSystemRole = false,
        uint rowVersion = 1)
    {
        if (id == Guid.Empty) throw new ArgumentException("Role ID cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Role code cannot be empty.", nameof(code));
        if (string.IsNullOrWhiteSpace(nameEn)) throw new ArgumentException("Role name (EN) cannot be empty.", nameof(nameEn));

        Id = id;
        TenantId = tenantId;
        Code = code.Trim().ToUpperInvariant();
        NameEn = nameEn.Trim();
        NameAr = string.IsNullOrWhiteSpace(nameAr) ? nameEn.Trim() : nameAr.Trim();
        Description = description?.Trim() ?? string.Empty;
        PermissionsJson = string.IsNullOrWhiteSpace(permissionsJson) ? "[]" : permissionsJson.Trim();
        IsSystemRole = isSystemRole;
        RowVersion = rowVersion;
    }

    public void Update(string nameEn, string nameAr, string description, string permissionsJson, uint expectedVersion)
    {
        if (RowVersion != expectedVersion)
        {
            throw new InvalidOperationException($"Concurrency conflict on Role '{Code}'. Expected version {expectedVersion} but found {RowVersion}.");
        }

        NameEn = nameEn.Trim();
        NameAr = string.IsNullOrWhiteSpace(nameAr) ? nameEn.Trim() : nameAr.Trim();
        Description = description?.Trim() ?? string.Empty;
        PermissionsJson = string.IsNullOrWhiteSpace(permissionsJson) ? "[]" : permissionsJson.Trim();
        RowVersion++;
    }

    public HashSet<string> GetPermissions()
    {
        try
        {
            return JsonSerializer.Deserialize<HashSet<string>>(PermissionsJson) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}

public class RoleAssignment
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public LegalEntityId? LegalEntityScopeId { get; private set; }
    public Guid? OrganizationUnitScopeId { get; private set; }
    public Guid AssignedByUserId { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }

    public RoleAssignment(
        Guid id,
        TenantId tenantId,
        Guid userId,
        Guid roleId,
        LegalEntityId? legalEntityScopeId,
        Guid? organizationUnitScopeId,
        Guid assignedByUserId,
        DateTime assignedAtUtc)
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        RoleId = roleId;
        LegalEntityScopeId = legalEntityScopeId;
        OrganizationUnitScopeId = organizationUnitScopeId;
        AssignedByUserId = assignedByUserId;
        AssignedAtUtc = assignedAtUtc == default ? DateTime.UtcNow : assignedAtUtc;
    }
}

public class PlatformSetting
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public string Category { get; private set; }
    public string Key { get; private set; }
    public string ValueJson { get; private set; }
    public DateTime EffectiveStartDate { get; private set; }
    public DateTime? EffectiveEndDate { get; private set; }
    public bool IsCurrent { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }
    public uint RowVersion { get; private set; }

    public PlatformSetting(
        Guid id,
        TenantId tenantId,
        string category,
        string key,
        string valueJson,
        DateTime effectiveStartDate,
        DateTime? effectiveEndDate,
        bool isCurrent,
        Guid changedByUserId,
        DateTime changedAtUtc,
        uint rowVersion = 1)
    {
        Id = id;
        TenantId = tenantId;
        Category = category.Trim();
        Key = key.Trim();
        ValueJson = valueJson;
        EffectiveStartDate = effectiveStartDate;
        EffectiveEndDate = effectiveEndDate;
        IsCurrent = isCurrent;
        ChangedByUserId = changedByUserId;
        ChangedAtUtc = changedAtUtc == default ? DateTime.UtcNow : changedAtUtc;
        RowVersion = rowVersion;
    }
}

public class RetentionPolicy
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public string Module { get; private set; }
    public string DataCategory { get; private set; }
    public int RetentionDays { get; private set; }
    public ExpiryAction ActionOnExpiry { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime EffectiveStartDate { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public uint RowVersion { get; private set; }

    public RetentionPolicy(
        Guid id,
        TenantId tenantId,
        string module,
        string dataCategory,
        int retentionDays,
        ExpiryAction actionOnExpiry,
        bool isActive,
        DateTime effectiveStartDate,
        Guid changedByUserId,
        uint rowVersion = 1)
    {
        Id = id;
        TenantId = tenantId;
        Module = module.Trim();
        DataCategory = dataCategory.Trim();
        RetentionDays = retentionDays;
        ActionOnExpiry = actionOnExpiry;
        IsActive = isActive;
        EffectiveStartDate = effectiveStartDate;
        ChangedByUserId = changedByUserId;
        RowVersion = rowVersion;
    }

    public void Update(int retentionDays, ExpiryAction actionOnExpiry, bool isActive, Guid changedByUserId, uint expectedVersion)
    {
        if (RowVersion != expectedVersion)
        {
            throw new InvalidOperationException($"Concurrency conflict on Retention Policy for '{Module}:{DataCategory}'. Expected version {expectedVersion} but found {RowVersion}.");
        }

        RetentionDays = retentionDays;
        ActionOnExpiry = actionOnExpiry;
        IsActive = isActive;
        ChangedByUserId = changedByUserId;
        RowVersion++;
    }
}
