using System;
using System.Threading;
using System.Threading.Tasks;

namespace Workforce.Modules.People.Application.Contracts;

public class HirePersonCommand
{
    public Guid IdempotencyKey { get; set; }
    
    // Person Details
    public string FirstNameEn { get; set; } = string.Empty;
    public string LastNameEn { get; set; } = string.Empty;
    public string FirstNameAr { get; set; } = string.Empty;
    public string LastNameAr { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public string EncryptedNationalId { get; set; } = string.Empty;
    public string NationalIdHash { get; set; } = string.Empty;
    public string MaskedNationalId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    // Employment Details
    public Guid LegalEntityId { get; set; }
    public string? EmployeeNumber { get; set; }
    public DateOnly HireDate { get; set; }
    
    // Assignment Details
    public Guid OrganizationUnitId { get; set; }
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public Guid? PositionId { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? HiringManagerId { get; set; }
}

public class HirePersonResult
{
    public Guid PersonId { get; set; }
    public Guid EmploymentId { get; set; }
    public Guid AssignmentId { get; set; }
    public bool WasIdempotentHit { get; set; }
}

public interface IPeopleHiringContract
{
    Task<HirePersonResult> HireAsync(string tenantId, HirePersonCommand command, CancellationToken ct = default);
}
