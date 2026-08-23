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

    public PeopleController(PeopleRepository repository, IUserContext userContext)
    {
        _repository = repository;
        _userContext = userContext;
    }

    [HttpGet("employees")]
    [ProducesResponseType(typeof(PagedResult<EmployeeSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] string? search,
        [FromQuery] Guid? departmentId,
        [FromQuery] string? status,
        [FromQuery] Guid? legalEntityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userContext = _userContext;
        var leId = legalEntityId.HasValue ? new LegalEntityId(legalEntityId.Value) : userContext.LegalEntityId;

        var result = await _repository.QueryDirectoryAsync(
            userContext.TenantId,
            leId,
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
    public async Task<IActionResult> GetEmployeeById(Guid id, CancellationToken ct)
    {
        var userContext = _userContext;
        var profile = await _repository.GetEmployeeProfileAsync(id, userContext.TenantId, ct);
        if (profile == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Employee Not Found",
                Detail = $"No employee record with ID '{id}' exists in the current tenant context.",
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(profile);
    }

    [HttpPost("employees")]
    [ProducesResponseType(typeof(EmployeeProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest request, CancellationToken ct)
    {
        var userContext = _userContext;
        var legalEntity = userContext.LegalEntityId ?? (request.LegalEntityId.HasValue ? new LegalEntityId(request.LegalEntityId.Value) : LegalEntityId.New());

        var personId = Guid.NewGuid();
        var employmentId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();

        var dob = DateOnly.TryParse(request.DateOfBirth, out var parsedDob) ? parsedDob : new DateOnly(1990, 1, 1);
        var hireDate = DateOnly.TryParse(request.HireDate, out var parsedHire) ? parsedHire : DateOnly.FromDateTime(DateTime.UtcNow);

        var person = new Person(
            personId,
            userContext.TenantId,
            request.FirstNameEn,
            request.LastNameEn,
            request.FirstNameAr,
            request.LastNameAr,
            dob,
            request.Gender ?? "Unspecified",
            request.Nationality ?? "SA",
            request.NationalIdentifier,
            request.PrimaryEmail ?? string.Empty,
            request.PhoneNumber ?? string.Empty
        );

        var empNumber = string.IsNullOrWhiteSpace(request.EmployeeNumber)
            ? $"EMP-{Random.Shared.Next(100000, 999999)}"
            : request.EmployeeNumber;

        var employment = new Employment(
            employmentId,
            userContext.TenantId,
            personId,
            legalEntity,
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

        var createdProfile = await _repository.GetEmployeeProfileAsync(employmentId, userContext.TenantId, ct);
        return CreatedAtAction(nameof(GetEmployeeById), new { id = employmentId }, createdProfile);
    }

    [HttpPost("employees/{id:guid}/assignment")]
    [ProducesResponseType(typeof(EmployeeProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeAssignment(Guid id, [FromBody] ChangeAssignmentRequest request, CancellationToken ct)
    {
        var userContext = _userContext;
        var profile = await _repository.GetEmployeeProfileAsync(id, userContext.TenantId, ct);
        if (profile == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Employee Not Found",
                Detail = $"No employee record with ID '{id}' was found.",
                Instance = HttpContext.Request.Path
            });
        }

        var effectiveFrom = DateOnly.TryParse(request.EffectiveFrom, out var parsedEff) ? parsedEff : DateOnly.FromDateTime(DateTime.UtcNow);

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

        var success = await _repository.ChangeAssignmentAsync(id, newAssignment, request.RowVersion, ct);
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

        var updatedProfile = await _repository.GetEmployeeProfileAsync(id, userContext.TenantId, ct);
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
        
        // Capability permission check
        if (!userContext.HasPermission("people.employee.reveal_pii") && !userContext.HasPermission("admin"))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Access Denied",
                Detail = "The current user does not have permission 'people.employee.reveal_pii' to reveal sensitive PII.",
                Instance = HttpContext.Request.Path
            });
        }

        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

        var plaintext = await _repository.RevealSensitiveFieldAsync(
            id,
            userContext.TenantId,
            userContext.UserId.Value,
            request.FieldName,
            request.Purpose,
            correlationId,
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
    public string FieldName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
}
