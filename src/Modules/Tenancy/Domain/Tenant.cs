using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Tenancy.Domain;

public sealed class Tenant
{
    public TenantId Id { get; private set; }
    public string Code { get; private set; }
    public string NameEn { get; private set; }
    public string NameAr { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Tenant()
    {
        Code = string.Empty;
        NameEn = string.Empty;
        NameAr = string.Empty;
    }

    public Tenant(TenantId id, string code, string nameEn, string nameAr)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Tenant code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(nameEn)) throw new ArgumentException("English tenant name is required.", nameof(nameEn));
        if (string.IsNullOrWhiteSpace(nameAr)) throw new ArgumentException("Arabic tenant name is required.", nameof(nameAr));

        Id = id;
        Code = code.Trim().ToUpperInvariant();
        NameEn = nameEn.Trim();
        NameAr = nameAr.Trim();
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static Tenant Rehydrate(TenantId id, string code, string nameEn, string nameAr, bool isActive, DateTime createdAtUtc)
    {
        return new Tenant
        {
            Id = id,
            Code = code,
            NameEn = nameEn,
            NameAr = nameAr,
            IsActive = isActive,
            CreatedAtUtc = createdAtUtc
        };
    }
}
