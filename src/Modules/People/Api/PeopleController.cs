using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.People.Application;
using Workforce.Modules.People.Domain;
using Workforce.Modules.People.Infrastructure;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.People.Api;

[ApiController]
[Route("api/v1/people")]
public class PeopleController : ControllerBase
{
    private readonly PeopleRepository _repository;
    private readonly IUserContext _userContext;
    private readonly IPiiEncryptionService _piiEncryptionService;

    public PeopleController(
        PeopleRepository repository, 
        IUserContext userContext,
        IPiiEncryptionService? piiEncryptionService = null)
    {
        _repository = repository;
        _userContext = userContext;
        _piiEncryptionService = piiEncryptionService ?? new AesPiiEncryptionService();
    }

    [HttpGet("employees")]
    [ProducesResponseType(typeof(PagedResult<EmployeeSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] string? search,
        [FromQuery] Guid? departmentId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userContext = _userContext;
        if (!HasAnyPermission("people.employee.read"))
        {
            return AccessDenied("people.employee.read");
        }

        var result = await _repository.QueryDirectoryAsync(
            userContext.TenantId,
            userContext.LegalEntityId,
            search,
            departmentId,
            status,
            page,
            pageSize,
            ct);

        return Ok(result);
    }

    [HttpGet("employees/{id:guid}")]
    [ProducesResponseType(typeof(EmployeeProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEmployeeById(Guid id, CancellationToken ct)
    {
        var userContext = _userContext;
        if (!HasAnyPermission("people.employee.read"))
        {
            return AccessDenied("people.employee.read");
        }

        var profile = await _repository.GetEmployeeProfileAsync(id, userContext.TenantId, userContext.LegalEntityId, ct);
        if (profile == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Employee Not Found",
                Detail = $"No employee record with ID '{id}' exists in the current tenant and legal entity context.",
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(profile);
    }

    [HttpPost("employees")]
    [ProducesResponseType(typeof(EmployeeProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest request, CancellationToken ct)
    {
        var userContext = _userContext;
        if (!HasAnyPermission("people.employee.create"))
        {
            return AccessDenied("people.employee.create");
        }

        var missingFields = new List<string>();
        if (string.IsNullOrWhiteSpace(request.EmployeeNumber)) missingFields.Add("employeeNumber");
        if (string.IsNullOrWhiteSpace(request.FirstNameEn)) missingFields.Add("firstNameEn");
        if (string.IsNullOrWhiteSpace(request.LastNameEn)) missingFields.Add("lastNameEn");
        if (string.IsNullOrWhiteSpace(request.FirstNameAr)) missingFields.Add("firstNameAr");
        if (string.IsNullOrWhiteSpace(request.LastNameAr)) missingFields.Add("lastNameAr");
        if (string.IsNullOrWhiteSpace(request.DateOfBirth)) missingFields.Add("dateOfBirth");
        if (string.IsNullOrWhiteSpace(request.NationalIdentifier)) missingFields.Add("nationalIdentifier");
        if (string.IsNullOrWhiteSpace(request.HireDate)) missingFields.Add("hireDate");
        if (request.OrganizationUnitId == Guid.Empty) missingFields.Add("organizationUnitId");
        if (string.IsNullOrWhiteSpace(request.JobTitleEn)) missingFields.Add("jobTitleEn");
        if (string.IsNullOrWhiteSpace(request.JobTitleAr)) missingFields.Add("jobTitleAr");

        if (missingFields.Count > 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Incomplete Employee Master Data",
                Detail = $"The following fields are required and must be supplied explicitly: {string.Join(", ", missingFields)}.",
                Instance = HttpContext.Request.Path
            });
        }

        var legalEntity = request.LegalEntityId.HasValue
            ? new LegalEntityId(request.LegalEntityId.Value)
            : userContext.LegalEntityId;

        if (!legalEntity.HasValue)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Legal Entity Context Required",
                Detail = "An authorized legal entity is required to create an employee.",
                Instance = HttpContext.Request.Path
            });
        }

        if (!userContext.IsAuthorizedForLegalEntity(legalEntity.Value))
        {
            return AccessDenied("the requested legal entity");
        }

        var personId = Guid.NewGuid();
        var employmentId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();

        if (!DateOnly.TryParse(request.DateOfBirth, out var dob) || !DateOnly.TryParse(request.HireDate, out var hireDate))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Employee Dates",
                Detail = "dateOfBirth and hireDate must be valid ISO dates in yyyy-MM-dd format.",
                Instance = HttpContext.Request.Path
            });
        }

        // Encrypt National ID and generate blind index hash
        var plainNatId = request.NationalIdentifier.Trim();
        var encryptedNatId = _piiEncryptionService.Encrypt(plainNatId);
        var natIdHash = _piiEncryptionService.ComputeSearchHash(plainNatId);
        var maskedNatId = _piiEncryptionService.MaskNationalId(plainNatId);

        var person = new Person(
            personId,
            userContext.TenantId,
            request.FirstNameEn,
            request.LastNameEn,
            request.FirstNameAr,
            request.LastNameAr,
            dob,
            request.Gender ?? "Unspecified",
            // Nationality is optional master data. Preserve "not provided" as an
            // empty value instead of inventing a value or exceeding the persisted
            // ISO-code-compatible VARCHAR(10) column with "Unspecified".
            request.Nationality?.Trim() ?? string.Empty,
            encryptedNatId,
            natIdHash,
            maskedNatId,
            request.PrimaryEmail ?? string.Empty,
            request.PhoneNumber ?? string.Empty
        );

        var empNumber = request.EmployeeNumber!.Trim();

        var employment = new Employment(
            employmentId,
            userContext.TenantId,
            personId,
            legalEntity.Value,
            empNumber,
            hireDate,
            null,
            EmploymentStatus.Active
        );

        var assignment = new EmploymentAssignment(
            assignmentId,
            employmentId,
            request.OrganizationUnitId,
            request.JobTitleEn,
            request.JobTitleAr,
            hireDate,
            null,
            request.PositionId,
            request.LocationId,
            request.ManagerEmploymentId,
            true
        );

        await _repository.CreateEmployeeAsync(person, employment, assignment, ct);

        var createdProfile = await _repository.GetEmployeeProfileAsync(employmentId, userContext.TenantId, legalEntity.Value, ct);
        return CreatedAtAction(nameof(GetEmployeeById), new { id = employmentId }, createdProfile);
    }

    [HttpPost("employees/{id:guid}/assignment")]
    [ProducesResponseType(typeof(EmployeeProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ChangeAssignment(
        Guid id,
        [FromBody] ChangeAssignmentRequest request,
        CancellationToken ct)
    {
        var userContext = _userContext;
        if (!HasAnyPermission("people.employee.update", "people.employment.manage"))
        {
            return AccessDenied("people.employee.update");
        }

        var effectiveFrom = DateOnly.TryParse(request.EffectiveFrom, out var parsedEff)
            ? parsedEff
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var newAssignment = new EmploymentAssignment(
            Guid.NewGuid(),
            id,
            request.OrganizationUnitId,
            request.JobTitleEn,
            request.JobTitleAr,
            effectiveFrom,
            null,
            request.PositionId,
            request.LocationId,
            request.ManagerEmploymentId,
            true
        );

        var success = await _repository.ChangeAssignmentAsync(id, newAssignment, request.RowVersion, userContext.LegalEntityId, ct);
        if (!success)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Optimistic Concurrency Conflict",
                Detail = "The employee record was modified by another operation. Please refresh and retry.",
                Instance = HttpContext.Request.Path
            });
        }

        var updatedProfile = await _repository.GetEmployeeProfileAsync(id, userContext.TenantId, userContext.LegalEntityId, ct);
        if (updatedProfile == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Employee Not Found",
                Detail = $"Employee '{id}' not found.",
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(updatedProfile);
    }

    [HttpPost("employees/{id:guid}/reveal-sensitive")]
    [ProducesResponseType(typeof(SensitiveRevealResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevealSensitiveField(
        Guid id,
        [FromBody] RevealSensitiveFieldRequest request,
        CancellationToken ct)
    {
        var userContext = _userContext;
        
        // 1. Permission check
        if (!HasAnyPermission("people.employee.reveal_pii"))
        {
            return AccessDenied("people.employee.reveal_pii");
        }

        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

        // 2. Fetch value and write unforgeable audit log
        var plaintext = await _repository.RevealSensitiveFieldAsync(
            id,
            userContext.TenantId,
            userContext.UserId.Value,
            request.FieldName,
            request.Purpose,
            correlationId,
            userContext.LegalEntityId,
            ct);

        if (plaintext == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Employee or Field Not Found",
                Detail = $"Could not retrieve sensitive field '{request.FieldName}' for employee '{id}'.",
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(new SensitiveRevealResponse
        {
            FieldName = request.FieldName,
            PlaintextValue = plaintext,
            RevealedAt = DateTime.UtcNow.ToString("o"),
            ExpirySeconds = 60
        });
    }

    private bool HasAnyPermission(params string[] permissions)
    {
        if (_userContext.HasPermission("admin")) return true;
        foreach (var permission in permissions)
        {
            if (_userContext.HasPermission(permission)) return true;
        }

        return false;
    }

    private IActionResult AccessDenied(string permission)
    {
        return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Access Denied",
            Detail = $"The current user does not have permission '{permission}'.",
            Instance = HttpContext.Request.Path
        });
    }
}

public class CreateEmployeeRequest
{
    public Guid? LegalEntityId { get; set; }
    public string? EmployeeNumber { get; set; }
    public string FirstNameEn { get; set; } = string.Empty;
    public string LastNameEn { get; set; } = string.Empty;
    public string FirstNameAr { get; set; } = string.Empty;
    public string LastNameAr { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string NationalIdentifier { get; set; } = string.Empty;
    public string? PrimaryEmail { get; set; }
    public string? PhoneNumber { get; set; }
    public string HireDate { get; set; } = string.Empty;
    public Guid OrganizationUnitId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? ManagerEmploymentId { get; set; }
    public string JobTitleEn { get; set; } = string.Empty;
    public string JobTitleAr { get; set; } = string.Empty;
}

public class ChangeAssignmentRequest
{
    public Guid OrganizationUnitId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? ManagerEmploymentId { get; set; }
    public string JobTitleEn { get; set; } = string.Empty;
    public string JobTitleAr { get; set; } = string.Empty;
    public string EffectiveFrom { get; set; } = string.Empty;
    public uint RowVersion { get; set; }
}

public class RevealSensitiveFieldRequest
{
    public string FieldName { get; set; } = "nationalIdentifier";
    public string Purpose { get; set; } = "Operational Workforce Verification";
}

public class SensitiveRevealResponse
{
    public string FieldName { get; set; } = string.Empty;
    public string PlaintextValue { get; set; } = string.Empty;
    public string RevealedAt { get; set; } = string.Empty;
    public int ExpirySeconds { get; set; } = 60;
}
