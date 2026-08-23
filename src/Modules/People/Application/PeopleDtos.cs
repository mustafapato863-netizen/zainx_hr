namespace Workforce.Modules.People.Application;

public class EmployeeSummaryDto
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string LegalEntityId { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstNameEn { get; set; } = string.Empty;
    public string LastNameEn { get; set; } = string.Empty;
    public string FirstNameAr { get; set; } = string.Empty;
    public string LastNameAr { get; set; } = string.Empty;
    public string FullNameEn { get; set; } = string.Empty;
    public string FullNameAr { get; set; } = string.Empty;
    public string PrimaryEmail { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string DepartmentNameEn { get; set; } = string.Empty;
    public string DepartmentNameAr { get; set; } = string.Empty;
    public string JobTitleEn { get; set; } = string.Empty;
    public string JobTitleAr { get; set; } = string.Empty;
    public string LocationNameEn { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public string HireDate { get; set; } = string.Empty;
    public string MaskedNationalId { get; set; } = string.Empty;
    public uint RowVersion { get; set; }
}

public class EmployeeProfileDto
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string LegalEntityId { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstNameEn { get; set; } = string.Empty;
    public string LastNameEn { get; set; } = string.Empty;
    public string FirstNameAr { get; set; } = string.Empty;
    public string LastNameAr { get; set; } = string.Empty;
    public string FullNameEn { get; set; } = string.Empty;
    public string FullNameAr { get; set; } = string.Empty;
    public string Gender { get; set; } = "Unspecified";
    public string Nationality { get; set; } = "SA";
    public string MaskedDateOfBirth { get; set; } = string.Empty;
    public string MaskedNationalId { get; set; } = string.Empty;
    public string PrimaryEmail { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public string HireDate { get; set; } = string.Empty;
    public string? ProbationEndDate { get; set; }
    public string? TerminationDate { get; set; }
    public string? TerminationReason { get; set; }
    public uint RowVersion { get; set; }
    public EmployeeAssignmentDto? CurrentAssignment { get; set; }
    public List<EmployeeAssignmentDto> AssignmentHistory { get; set; } = new();
}

public class EmployeeAssignmentDto
{
    public Guid Id { get; set; }
    public Guid EmploymentId { get; set; }
    public Guid OrganizationUnitId { get; set; }
    public string DepartmentNameEn { get; set; } = string.Empty;
    public string DepartmentNameAr { get; set; } = string.Empty;
    public Guid? PositionId { get; set; }
    public Guid? LocationId { get; set; }
    public string LocationNameEn { get; set; } = string.Empty;
    public Guid? ManagerEmploymentId { get; set; }
    public string? ManagerNameEn { get; set; }
    public string JobTitleEn { get; set; } = string.Empty;
    public string JobTitleAr { get; set; } = string.Empty;
    public string EffectiveFrom { get; set; } = string.Empty;
    public string? EffectiveTo { get; set; }
    public bool IsCurrent { get; set; }
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize));
}

public class SensitiveRevealResponse
{
    public string FieldName { get; set; } = string.Empty;
    public string PlaintextValue { get; set; } = string.Empty;
    public string RevealedAt { get; set; } = string.Empty;
    public int ExpirySeconds { get; set; } = 60;
}
