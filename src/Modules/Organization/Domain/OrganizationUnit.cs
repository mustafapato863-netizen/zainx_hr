using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Organization.Domain;

public class OrganizationUnit
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public LegalEntityId LegalEntityId { get; private set; }
    public string Code { get; private set; }
    public string NameEn { get; private set; }
    public string NameAr { get; private set; }
    public OrganizationUnitType Type { get; private set; }
    public Guid? ParentUnitId { get; private set; }
    public Guid? ManagerPositionId { get; private set; }
    public bool IsActive { get; private set; }
    public EffectivePeriod EffectivePeriod { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public uint RowVersion { get; private set; }

    // Parameterless constructor for persistence
    private OrganizationUnit() 
    {
        Code = string.Empty;
        NameEn = string.Empty;
        NameAr = string.Empty;
        EffectivePeriod = new EffectivePeriod(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    public OrganizationUnit(
        Guid id,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        string code,
        string nameEn,
        string nameAr,
        OrganizationUnitType type,
        Guid? parentUnitId,
        EffectivePeriod effectivePeriod,
        Guid? managerPositionId = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code cannot be empty.", nameof(code));
        if (string.IsNullOrWhiteSpace(nameEn)) throw new ArgumentException("English name cannot be empty.", nameof(nameEn));
        if (string.IsNullOrWhiteSpace(nameAr)) throw new ArgumentException("Arabic name cannot be empty.", nameof(nameAr));
        if (parentUnitId.HasValue && parentUnitId.Value == id) throw new ArgumentException("A unit cannot be its own parent.");

        Id = id;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        Code = code.Trim().ToUpperInvariant();
        NameEn = nameEn.Trim();
        NameAr = nameAr.Trim();
        Type = type;
        ParentUnitId = parentUnitId;
        ManagerPositionId = managerPositionId;
        IsActive = true;
        EffectivePeriod = effectivePeriod ?? throw new ArgumentNullException(nameof(effectivePeriod));
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RowVersion = 1;
    }

    public static OrganizationUnit Rehydrate(
        Guid id,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        string code,
        string nameEn,
        string nameAr,
        OrganizationUnitType type,
        Guid? parentUnitId,
        EffectivePeriod effectivePeriod,
        Guid? managerPositionId,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt,
        uint rowVersion)
    {
        var unit = new OrganizationUnit(
            id,
            tenantId,
            legalEntityId,
            code,
            nameEn,
            nameAr,
            type,
            parentUnitId,
            effectivePeriod,
            managerPositionId);

        unit.IsActive = isActive;
        unit.CreatedAt = createdAt;
        unit.UpdatedAt = updatedAt;
        unit.RowVersion = rowVersion;
        return unit;
    }

    public void UpdateDetails(
        string nameEn,
        string nameAr,
        OrganizationUnitType type,
        Guid? parentUnitId,
        EffectivePeriod effectivePeriod,
        Guid? managerPositionId,
        uint expectedRowVersion)
    {
        if (expectedRowVersion != RowVersion)
        {
            throw new InvalidOperationException("Concurrency conflict: The organization unit was modified by another operation.");
        }

        if (string.IsNullOrWhiteSpace(nameEn)) throw new ArgumentException("English name cannot be empty.", nameof(nameEn));
        if (string.IsNullOrWhiteSpace(nameAr)) throw new ArgumentException("Arabic name cannot be empty.", nameof(nameAr));
        if (parentUnitId.HasValue && parentUnitId.Value == Id) throw new ArgumentException("A unit cannot be its own parent.");

        NameEn = nameEn.Trim();
        NameAr = nameAr.Trim();
        Type = type;
        ParentUnitId = parentUnitId;
        ManagerPositionId = managerPositionId;
        EffectivePeriod = effectivePeriod ?? throw new ArgumentNullException(nameof(effectivePeriod));
        UpdatedAt = DateTime.UtcNow;
        RowVersion++;
    }

    public void Deactivate(uint expectedRowVersion)
    {
        if (expectedRowVersion != RowVersion)
        {
            throw new InvalidOperationException("Concurrency conflict: The organization unit was modified by another operation.");
        }

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        RowVersion++;
    }
}
