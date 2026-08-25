namespace Workforce.Modules.Organization.Application;

public class OrganizationUnitDto
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string LegalEntityId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Guid? ParentUnitId { get; set; }
    public string? ParentNameEn { get; set; }
    public Guid? ManagerPositionId { get; set; }
    public bool IsActive { get; set; }
    public string EffectiveFrom { get; set; } = string.Empty;
    public string? EffectiveTo { get; set; }
    public uint RowVersion { get; set; }
}

public class LocationDto
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string LegalEntityId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class PositionDto
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string LegalEntityId { get; set; } = string.Empty;
    public Guid OrganizationUnitId { get; set; }
    public string JobCode { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CostCenterDto
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string LegalEntityId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
