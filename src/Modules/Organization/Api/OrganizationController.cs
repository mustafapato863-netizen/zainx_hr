using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Organization.Application;
using Workforce.Modules.Organization.Domain;
using Workforce.Modules.Organization.Infrastructure;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Organization.Api;

[ApiController]
[Route("api/v1/organization")]
public class OrganizationController : ControllerBase
{
    private readonly OrganizationRepository _repository;
    private readonly IUserContext _userContext;

    public OrganizationController(OrganizationRepository repository, IUserContext userContext)
    {
        _repository = repository;
        _userContext = userContext;
    }

    [HttpGet("units")]
    [ProducesResponseType(typeof(IReadOnlyList<OrganizationUnitDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUnits([FromQuery] Guid? legalEntityId, CancellationToken ct)
    {
        var userContext = _userContext;
        var legalEntity = legalEntityId.HasValue ? new LegalEntityId(legalEntityId.Value) : userContext.LegalEntityId;
        
        var units = await _repository.ListUnitsAsync(userContext.TenantId, legalEntity, ct);
        return Ok(units);
    }

    [HttpGet("units/{id:guid}")]
    [ProducesResponseType(typeof(OrganizationUnitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUnit(Guid id, CancellationToken ct)
    {
        var userContext = _userContext;
        var unit = await _repository.GetUnitByIdAsync(id, userContext.TenantId, ct);
        if (unit == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Organization Unit Not Found",
                Detail = $"No organization unit with ID '{id}' was found in the current tenant.",
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(new OrganizationUnitDto
        {
            Id = unit.Id,
            TenantId = unit.TenantId.Value.ToString(),
            LegalEntityId = unit.LegalEntityId.Value.ToString(),
            Code = unit.Code,
            NameEn = unit.NameEn,
            NameAr = unit.NameAr,
            Type = unit.Type.ToString(),
            ParentUnitId = unit.ParentUnitId,
            ManagerPositionId = unit.ManagerPositionId,
            IsActive = unit.IsActive,
            EffectiveFrom = unit.EffectivePeriod.EffectiveFrom.ToString("yyyy-MM-dd"),
            EffectiveTo = unit.EffectivePeriod.EffectiveTo?.ToString("yyyy-MM-dd"),
            RowVersion = unit.RowVersion
        });
    }

    [HttpPost("units")]
    [ProducesResponseType(typeof(OrganizationUnitDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUnit([FromBody] CreateOrganizationUnitRequest request, CancellationToken ct)
    {
        var userContext = _userContext;
        var legalEntity = userContext.LegalEntityId ?? (request.LegalEntityId.HasValue ? new LegalEntityId(request.LegalEntityId.Value) : LegalEntityId.New());

        if (!Enum.TryParse<OrganizationUnitType>(request.Type, true, out var unitType))
        {
            unitType = OrganizationUnitType.Department;
        }

        var effectiveFrom = DateOnly.TryParse(request.EffectiveFrom, out var ef) ? ef : DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly? effectiveTo = DateOnly.TryParse(request.EffectiveTo, out var et) ? et : null;

        var unit = new OrganizationUnit(
            Guid.NewGuid(),
            userContext.TenantId,
            legalEntity,
            request.Code,
            request.NameEn,
            request.NameAr,
            unitType,
            request.ParentUnitId,
            new EffectivePeriod(effectiveFrom, effectiveTo),
            request.ManagerPositionId
        );

        await _repository.InsertUnitAsync(unit, ct);

        return CreatedAtAction(nameof(GetUnit), new { id = unit.Id }, new OrganizationUnitDto
        {
            Id = unit.Id,
            TenantId = unit.TenantId.Value.ToString(),
            LegalEntityId = unit.LegalEntityId.Value.ToString(),
            Code = unit.Code,
            NameEn = unit.NameEn,
            NameAr = unit.NameAr,
            Type = unit.Type.ToString(),
            ParentUnitId = unit.ParentUnitId,
            ManagerPositionId = unit.ManagerPositionId,
            IsActive = unit.IsActive,
            EffectiveFrom = unit.EffectivePeriod.EffectiveFrom.ToString("yyyy-MM-dd"),
            EffectiveTo = unit.EffectivePeriod.EffectiveTo?.ToString("yyyy-MM-dd"),
            RowVersion = unit.RowVersion
        });
    }

    [HttpPut("units/{id:guid}")]
    [ProducesResponseType(typeof(OrganizationUnitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateUnit(Guid id, [FromBody] UpdateOrganizationUnitRequest request, CancellationToken ct)
    {
        var userContext = _userContext;
        var unit = await _repository.GetUnitByIdAsync(id, userContext.TenantId, ct);
        if (unit == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Organization Unit Not Found",
                Detail = $"No organization unit with ID '{id}' was found.",
                Instance = HttpContext.Request.Path
            });
        }

        if (!Enum.TryParse<OrganizationUnitType>(request.Type, true, out var unitType))
        {
            unitType = unit.Type;
        }

        var effectiveFrom = DateOnly.TryParse(request.EffectiveFrom, out var ef) ? ef : unit.EffectivePeriod.EffectiveFrom;
        DateOnly? effectiveTo = DateOnly.TryParse(request.EffectiveTo, out var et) ? et : null;

        try
        {
            unit.UpdateDetails(
                request.NameEn,
                request.NameAr,
                unitType,
                request.ParentUnitId,
                new EffectivePeriod(effectiveFrom, effectiveTo),
                request.ManagerPositionId,
                request.RowVersion
            );

            var success = await _repository.UpdateUnitAsync(unit, ct);
            if (!success)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Optimistic Concurrency Conflict",
                    Detail = "The organization unit has been modified or updated by another user. Please refresh and try again.",
                    Instance = HttpContext.Request.Path
                });
            }

            return Ok(new OrganizationUnitDto
            {
                Id = unit.Id,
                TenantId = unit.TenantId.Value.ToString(),
                LegalEntityId = unit.LegalEntityId.Value.ToString(),
                Code = unit.Code,
                NameEn = unit.NameEn,
                NameAr = unit.NameAr,
                Type = unit.Type.ToString(),
                ParentUnitId = unit.ParentUnitId,
                ManagerPositionId = unit.ManagerPositionId,
                IsActive = unit.IsActive,
                EffectiveFrom = unit.EffectivePeriod.EffectiveFrom.ToString("yyyy-MM-dd"),
                EffectiveTo = unit.EffectivePeriod.EffectiveTo?.ToString("yyyy-MM-dd"),
                RowVersion = unit.RowVersion
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Optimistic Concurrency Conflict",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            });
        }
    }

    [HttpGet("locations")]
    [ProducesResponseType(typeof(IReadOnlyList<LocationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListLocations(CancellationToken ct)
    {
        var userContext = _userContext;
        var locations = await _repository.ListLocationsAsync(userContext.TenantId, ct);
        return Ok(locations);
    }

    [HttpPost("locations")]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationRequest request, CancellationToken ct)
    {
        var userContext = _userContext;
        var legalEntity = userContext.LegalEntityId ?? LegalEntityId.New();

        var location = new Location(
            Guid.NewGuid(),
            userContext.TenantId,
            legalEntity,
            request.Code,
            request.NameEn,
            request.NameAr,
            request.Country ?? "SA",
            request.City ?? "Riyadh",
            request.Address ?? ""
        );

        await _repository.InsertLocationAsync(location, ct);

        return CreatedAtAction(nameof(ListLocations), new LocationDto
        {
            Id = location.Id,
            TenantId = location.TenantId.Value.ToString(),
            LegalEntityId = location.LegalEntityId.Value.ToString(),
            Code = location.Code,
            NameEn = location.NameEn,
            NameAr = location.NameAr,
            Country = location.Country,
            City = location.City,
            Address = location.Address,
            IsActive = location.IsActive
        });
    }
}

public class CreateOrganizationUnitRequest
{
    public Guid? LegalEntityId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Type { get; set; } = "Department";
    public Guid? ParentUnitId { get; set; }
    public Guid? ManagerPositionId { get; set; }
    public string EffectiveFrom { get; set; } = string.Empty;
    public string? EffectiveTo { get; set; }
}

public class UpdateOrganizationUnitRequest
{
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Type { get; set; } = "Department";
    public Guid? ParentUnitId { get; set; }
    public Guid? ManagerPositionId { get; set; }
    public string EffectiveFrom { get; set; } = string.Empty;
    public string? EffectiveTo { get; set; }
    public uint RowVersion { get; set; }
}

public class CreateLocationRequest
{
    public string Code { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
}
