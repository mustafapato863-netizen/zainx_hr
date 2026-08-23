using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Organization.Domain;

public class Position
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public LegalEntityId LegalEntityId { get; private set; }
    public Guid OrganizationUnitId { get; private set; }
    public string JobCode { get; private set; }
    public string TitleEn { get; private set; }
    public string TitleAr { get; private set; }
    public string Grade { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Position()
    {
        JobCode = string.Empty;
        TitleEn = string.Empty;
        TitleAr = string.Empty;
        Grade = string.Empty;
    }

    public Position(
        Guid id,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid organizationUnitId,
        string jobCode,
        string titleEn,
        string titleAr,
        string grade = "N/A")
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (organizationUnitId == Guid.Empty) throw new ArgumentException("OrganizationUnitId cannot be empty.", nameof(organizationUnitId));
        if (string.IsNullOrWhiteSpace(jobCode)) throw new ArgumentException("JobCode cannot be empty.", nameof(jobCode));
        if (string.IsNullOrWhiteSpace(titleEn)) throw new ArgumentException("English title cannot be empty.", nameof(titleEn));
        if (string.IsNullOrWhiteSpace(titleAr)) throw new ArgumentException("Arabic title cannot be empty.", nameof(titleAr));

        Id = id;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        OrganizationUnitId = organizationUnitId;
        JobCode = jobCode.Trim().ToUpperInvariant();
        TitleEn = titleEn.Trim();
        TitleAr = titleAr.Trim();
        Grade = string.IsNullOrWhiteSpace(grade) ? "N/A" : grade.Trim();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }
}
