using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.People.Domain;

public class Employment
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public Guid PersonId { get; private set; }
    public LegalEntityId LegalEntityId { get; private set; }
    public string EmployeeNumber { get; private set; }
    public DateOnly HireDate { get; private set; }
    public DateOnly? ProbationEndDate { get; private set; }
    public DateOnly? TerminationDate { get; private set; }
    public string? TerminationReason { get; private set; }
    public EmploymentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public uint RowVersion { get; private set; }

    private Employment()
    {
        EmployeeNumber = string.Empty;
    }

    public Employment(
        Guid id,
        TenantId tenantId,
        Guid personId,
        LegalEntityId legalEntityId,
        string employeeNumber,
        DateOnly hireDate,
        DateOnly? probationEndDate = null,
        EmploymentStatus status = EmploymentStatus.Active)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (personId == Guid.Empty) throw new ArgumentException("PersonId cannot be empty.", nameof(personId));
        if (string.IsNullOrWhiteSpace(employeeNumber)) throw new ArgumentException("EmployeeNumber is required.", nameof(employeeNumber));

        Id = id;
        TenantId = tenantId;
        PersonId = personId;
        LegalEntityId = legalEntityId;
        EmployeeNumber = employeeNumber.Trim().ToUpperInvariant();
        HireDate = hireDate;
        ProbationEndDate = probationEndDate;
        Status = status;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RowVersion = 1;
    }

    public void Activate(uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        if (Status == EmploymentStatus.Terminated)
        {
            throw new InvalidOperationException("Cannot activate a terminated employment. Rehire requires a new employment record.");
        }

        Status = EmploymentStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        RowVersion++;
    }

    public void Deactivate(uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        if (Status == EmploymentStatus.Terminated)
        {
            throw new InvalidOperationException("Cannot deactivate an already terminated employment.");
        }

        Status = EmploymentStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
        RowVersion++;
    }

    public void Terminate(DateOnly terminationDate, string reason, uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        if (terminationDate < HireDate)
        {
            throw new ArgumentException("Termination date cannot be earlier than hire date.");
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A valid termination reason is required.");
        }

        TerminationDate = terminationDate;
        TerminationReason = reason.Trim();
        Status = EmploymentStatus.Terminated;
        UpdatedAt = DateTime.UtcNow;
        RowVersion++;
    }

    public void UpdateEmploymentDates(DateOnly hireDate, DateOnly? probationEndDate, uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        HireDate = hireDate;
        ProbationEndDate = probationEndDate;
        UpdatedAt = DateTime.UtcNow;
        RowVersion++;
    }

    private void VerifyRowVersion(uint expected)
    {
        if (expected != RowVersion)
        {
            throw new InvalidOperationException("Concurrency conflict: The employment record was modified by another user.");
        }
    }
}
