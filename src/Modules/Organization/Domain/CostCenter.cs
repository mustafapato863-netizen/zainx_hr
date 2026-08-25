using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Organization.Domain;

public sealed class CostCenter
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public LegalEntityId LegalEntityId { get; private set; }
    public string Code { get; private set; }
    public string NameEn { get; private set; }
    public string NameAr { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private CostCenter()
    {
        Code = string.Empty;
        NameEn = string.Empty;
        NameAr = string.Empty;
    }

    public CostCenter(
        Guid id,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        string code,
        string nameEn,
        string nameAr)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Cost center code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(nameEn)) throw new ArgumentException("English cost center name is required.", nameof(nameEn));
        if (string.IsNullOrWhiteSpace(nameAr)) throw new ArgumentException("Arabic cost center name is required.", nameof(nameAr));

        Id = id;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        Code = code.Trim().ToUpperInvariant();
        NameEn = nameEn.Trim();
        NameAr = nameAr.Trim();
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
