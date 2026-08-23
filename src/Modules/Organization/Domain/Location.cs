using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Organization.Domain;

public class Location
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public LegalEntityId LegalEntityId { get; private set; }
    public string Code { get; private set; }
    public string NameEn { get; private set; }
    public string NameAr { get; private set; }
    public string Country { get; private set; }
    public string City { get; private set; }
    public string Address { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Location()
    {
        Code = string.Empty;
        NameEn = string.Empty;
        NameAr = string.Empty;
        Country = string.Empty;
        City = string.Empty;
        Address = string.Empty;
    }

    public Location(
        Guid id,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        string code,
        string nameEn,
        string nameAr,
        string country = "SA",
        string city = "Riyadh",
        string address = "")
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code cannot be empty.", nameof(code));
        if (string.IsNullOrWhiteSpace(nameEn)) throw new ArgumentException("English name cannot be empty.", nameof(nameEn));
        if (string.IsNullOrWhiteSpace(nameAr)) throw new ArgumentException("Arabic name cannot be empty.", nameof(nameAr));

        Id = id;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        Code = code.Trim().ToUpperInvariant();
        NameEn = nameEn.Trim();
        NameAr = nameAr.Trim();
        Country = country.Trim();
        City = city.Trim();
        Address = address.Trim();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }
}
