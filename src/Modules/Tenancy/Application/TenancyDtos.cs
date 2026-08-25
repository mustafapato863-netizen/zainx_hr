namespace Workforce.Modules.Tenancy.Application;

public sealed class TenantDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class LegalEntityDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public string TimezoneId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public uint RowVersion { get; set; }
}

public sealed record TenantContextDto(
    TenantDto Tenant,
    IReadOnlyList<LegalEntityDto> LegalEntities,
    Guid? ActiveLegalEntityId);
