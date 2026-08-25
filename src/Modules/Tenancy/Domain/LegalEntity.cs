using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Tenancy.Domain;

public sealed class LegalEntity
{
    public LegalEntityId Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public string Code { get; private set; }
    public string NameEn { get; private set; }
    public string NameAr { get; private set; }
    public string CountryCode { get; private set; }
    public string CurrencyCode { get; private set; }
    public string TimezoneId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public uint RowVersion { get; private set; }

    private LegalEntity()
    {
        Code = string.Empty;
        NameEn = string.Empty;
        NameAr = string.Empty;
        CountryCode = string.Empty;
        CurrencyCode = string.Empty;
        TimezoneId = string.Empty;
    }

    public LegalEntity(
        LegalEntityId id,
        TenantId tenantId,
        string code,
        string nameEn,
        string nameAr,
        string countryCode,
        string currencyCode,
        string timezoneId)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Legal entity code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(nameEn)) throw new ArgumentException("English legal entity name is required.", nameof(nameEn));
        if (string.IsNullOrWhiteSpace(nameAr)) throw new ArgumentException("Arabic legal entity name is required.", nameof(nameAr));
        if (string.IsNullOrWhiteSpace(countryCode)) throw new ArgumentException("Country code is required.", nameof(countryCode));
        if (string.IsNullOrWhiteSpace(currencyCode)) throw new ArgumentException("Currency code is required.", nameof(currencyCode));
        if (string.IsNullOrWhiteSpace(timezoneId)) throw new ArgumentException("Timezone is required.", nameof(timezoneId));

        Id = id;
        TenantId = tenantId;
        Code = code.Trim().ToUpperInvariant();
        NameEn = nameEn.Trim();
        NameAr = nameAr.Trim();
        CountryCode = countryCode.Trim().ToUpperInvariant();
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        TimezoneId = timezoneId.Trim();
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
        RowVersion = 1;
    }

    public static LegalEntity Rehydrate(
        LegalEntityId id,
        TenantId tenantId,
        string code,
        string nameEn,
        string nameAr,
        string countryCode,
        string currencyCode,
        string timezoneId,
        bool isActive,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        uint rowVersion)
    {
        return new LegalEntity
        {
            Id = id,
            TenantId = tenantId,
            Code = code,
            NameEn = nameEn,
            NameAr = nameAr,
            CountryCode = countryCode,
            CurrencyCode = currencyCode,
            TimezoneId = timezoneId,
            IsActive = isActive,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc,
            RowVersion = rowVersion
        };
    }

    public void UpdateDetails(
        string nameEn,
        string nameAr,
        string countryCode,
        string currencyCode,
        string timezoneId,
        uint expectedRowVersion)
    {
        if (expectedRowVersion != RowVersion)
        {
            throw new InvalidOperationException("Optimistic concurrency conflict on legal entity.");
        }

        if (string.IsNullOrWhiteSpace(nameEn) || string.IsNullOrWhiteSpace(nameAr) ||
            string.IsNullOrWhiteSpace(countryCode) || string.IsNullOrWhiteSpace(currencyCode) ||
            string.IsNullOrWhiteSpace(timezoneId))
        {
            throw new ArgumentException("All legal entity details are required.");
        }

        NameEn = nameEn.Trim();
        NameAr = nameAr.Trim();
        CountryCode = countryCode.Trim().ToUpperInvariant();
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        TimezoneId = timezoneId.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion++;
    }
}
