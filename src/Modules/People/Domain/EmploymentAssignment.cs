namespace Workforce.Modules.People.Domain;

public class EmploymentAssignment
{
    public Guid Id { get; private set; }
    public Guid EmploymentId { get; private set; }
    public Guid OrganizationUnitId { get; private set; }
    public Guid? PositionId { get; private set; }
    public Guid? LocationId { get; private set; }
    public Guid? ManagerEmploymentId { get; private set; }
    public string JobTitleEn { get; private set; }
    public string JobTitleAr { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsCurrent { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private EmploymentAssignment()
    {
        JobTitleEn = string.Empty;
        JobTitleAr = string.Empty;
    }

    public EmploymentAssignment(
        Guid id,
        Guid employmentId,
        Guid organizationUnitId,
        string jobTitleEn,
        string jobTitleAr,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null,
        Guid? positionId = null,
        Guid? locationId = null,
        Guid? managerEmploymentId = null,
        bool isCurrent = true)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (employmentId == Guid.Empty) throw new ArgumentException("EmploymentId cannot be empty.", nameof(employmentId));
        if (organizationUnitId == Guid.Empty) throw new ArgumentException("OrganizationUnitId cannot be empty.", nameof(organizationUnitId));
        if (string.IsNullOrWhiteSpace(jobTitleEn)) throw new ArgumentException("English job title is required.", nameof(jobTitleEn));
        if (string.IsNullOrWhiteSpace(jobTitleAr)) throw new ArgumentException("Arabic job title is required.", nameof(jobTitleAr));
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
        {
            throw new ArgumentException("EffectiveTo date cannot be earlier than EffectiveFrom date.");
        }

        Id = id;
        EmploymentId = employmentId;
        OrganizationUnitId = organizationUnitId;
        JobTitleEn = jobTitleEn.Trim();
        JobTitleAr = jobTitleAr.Trim();
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        PositionId = positionId;
        LocationId = locationId;
        ManagerEmploymentId = managerEmploymentId;
        IsCurrent = isCurrent;
        CreatedAt = DateTime.UtcNow;
    }

    public void CloseAssignment(DateOnly effectiveTo)
    {
        if (effectiveTo < EffectiveFrom)
        {
            throw new ArgumentException("EffectiveTo cannot be earlier than EffectiveFrom.");
        }

        EffectiveTo = effectiveTo;
        IsCurrent = false;
    }
}
