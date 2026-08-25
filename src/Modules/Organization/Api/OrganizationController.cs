using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListUnits([FromQuery] Guid? legalEntityId, CancellationToken ct)
    {
        var userContext = _userContext;
        if (!HasAnyPermission("organization.unit.read"))
        {
            return AccessDenied("organization.unit.read");
        }

        var legalEntity = legalEntityId.HasValue ? new LegalEntityId(legalEntityId.Value) : userContext.LegalEntityId;
        if (legalEntity.HasValue && !userContext.IsAuthorizedForLegalEntity(legalEntity.Value))
        {
            return AccessDenied("the requested legal entity");
        }
        
        var units = await _repository.ListUnitsAsync(userContext.TenantId, legalEntity, ct);
        return Ok(units);
    }

    [HttpGet("units/{id:guid}")]
    [ProducesResponseType(typeof(OrganizationUnitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUnit(Guid id, CancellationToken ct)
    {
        var userContext = _userContext;
        if (!HasAnyPermission("organization.unit.read"))
        {
            return AccessDenied("organization.unit.read");
        }

        var unit = await _repository.GetUnitByIdAsync(id, userContext.TenantId, userContext.LegalEntityId, ct);
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateUnit([FromBody] CreateOrganizationUnitRequest request, CancellationToken ct)
    {
        var userContext = _userContext;
        if (!HasAnyPermission("organization.unit.create"))
        {
            return AccessDenied("organization.unit.create");
        }

        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.NameEn) || string.IsNullOrWhiteSpace(request.NameAr))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Incomplete Organization Unit",
                Detail = "code, nameEn, and nameAr must be supplied explicitly.",
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
                Detail = "An authorized legal entity is required to create an organization unit.",
                Instance = HttpContext.Request.Path
            });
        }
        if (!userContext.IsAuthorizedForLegalEntity(legalEntity.Value))
        {
            return AccessDenied("the requested legal entity");
        }

        if (request.ParentUnitId.HasValue)
        {
            var parent = await _repository.GetUnitByIdAsync(request.ParentUnitId.Value, userContext.TenantId, legalEntity, ct);
            if (parent == null)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid Parent Organization Unit",
                    Detail = "The selected parent unit does not exist in the current tenant and legal entity.",
                    Instance = HttpContext.Request.Path
                });
            }
        }

        if (request.ManagerPositionId.HasValue &&
            !await _repository.PositionExistsAsync(request.ManagerPositionId.Value, userContext.TenantId, legalEntity.Value, ct))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Manager Position",
                Detail = "The selected manager position does not exist in the current tenant and legal entity.",
                Instance = HttpContext.Request.Path
            });
        }

        if (!Enum.TryParse<OrganizationUnitType>(request.Type, true, out var unitType))
        {
            unitType = OrganizationUnitType.Department;
        }

        var effectiveFrom = DateOnly.TryParse(request.EffectiveFrom, out var ef) ? ef : DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly? effectiveTo = DateOnly.TryParse(request.EffectiveTo, out var et) ? et : null;

        var unit = new OrganizationUnit(
            Guid.NewGuid(),
            userContext.TenantId,
            legalEntity.Value,
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUnit(Guid id, [FromBody] UpdateOrganizationUnitRequest request, CancellationToken ct)
    {
        var userContext = _userContext;
        if (!HasAnyPermission("organization.unit.update"))
        {
            return AccessDenied("organization.unit.update");
        }

        var unit = await _repository.GetUnitByIdAsync(id, userContext.TenantId, userContext.LegalEntityId, ct);
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

        if (request.ParentUnitId.HasValue)
        {
            var parent = await _repository.GetUnitByIdAsync(request.ParentUnitId.Value, userContext.TenantId, unit.LegalEntityId, ct);
            if (parent == null)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid Parent Organization Unit",
                    Detail = "The selected parent unit does not exist in the current tenant and legal entity.",
                    Instance = HttpContext.Request.Path
                });
            }

            if (await _repository.WouldCreateCycleAsync(id, request.ParentUnitId.Value, userContext.TenantId, unit.LegalEntityId, ct))
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Organization Hierarchy Cycle",
                    Detail = "An organization unit cannot be moved beneath one of its own descendants.",
                    Instance = HttpContext.Request.Path
                });
            }
        }

        if (request.ManagerPositionId.HasValue &&
            !await _repository.PositionExistsAsync(request.ManagerPositionId.Value, userContext.TenantId, unit.LegalEntityId, ct))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Manager Position",
                Detail = "The selected manager position does not exist in the current tenant and legal entity.",
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

    [HttpPost("units/{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeactivateUnit(Guid id, [FromBody] DeactivateOrganizationUnitRequest request, CancellationToken ct)
    {
        var userContext = _userContext;
        if (!HasAnyPermission("organization.unit.deactivate", "organization.unit.update"))
        {
            return AccessDenied("organization.unit.deactivate");
        }

        var unit = await _repository.GetUnitByIdAsync(id, userContext.TenantId, userContext.LegalEntityId, ct);
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

        try
        {
            unit.Deactivate(request.RowVersion);
            if (!await _repository.UpdateUnitAsync(unit, ct))
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Optimistic Concurrency Conflict",
                    Detail = "The organization unit has changed. Refresh before deactivating it.",
                    Instance = HttpContext.Request.Path
                });
            }

            return NoContent();
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListLocations(CancellationToken ct)
    {
        var userContext = _userContext;
        if (!HasAnyPermission("organization.location.read"))
        {
            return AccessDenied("organization.location.read");
        }

        var locations = await _repository.ListLocationsAsync(userContext.TenantId, userContext.LegalEntityId, ct);
        return Ok(locations);
    }

    [HttpPost("locations")]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationRequest request, CancellationToken ct)
    {
        var userContext = _userContext;
        if (!HasAnyPermission("organization.location.create"))
        {
            return AccessDenied("organization.location.create");
        }

        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.NameEn) ||
            string.IsNullOrWhiteSpace(request.NameAr) || string.IsNullOrWhiteSpace(request.Country) ||
            string.IsNullOrWhiteSpace(request.City))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Incomplete Location",
                Detail = "code, nameEn, nameAr, country, and city must be supplied explicitly.",
                Instance = HttpContext.Request.Path
            });
        }

        if (!userContext.LegalEntityId.HasValue || !userContext.IsAuthorizedForLegalEntity(userContext.LegalEntityId.Value))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Legal Entity Context Required",
                Detail = "An authorized legal entity is required to create a location.",
                Instance = HttpContext.Request.Path
            });
        }

        var legalEntity = userContext.LegalEntityId.Value;

        var location = new Location(
            Guid.NewGuid(),
            userContext.TenantId,
            legalEntity,
            request.Code,
            request.NameEn,
            request.NameAr,
            request.Country.Trim(),
            request.City.Trim(),
            request.Address?.Trim() ?? ""
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

    [HttpGet("positions")]
    [ProducesResponseType(typeof(IReadOnlyList<PositionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListPositions(
        [FromQuery] Guid? legalEntityId,
        [FromQuery] Guid? organizationUnitId,
        CancellationToken ct)
    {
        var userContext = _userContext;
        if (!HasAnyPermission("organization.position.read"))
        {
            return AccessDenied("organization.position.read");
        }

        var legalEntity = legalEntityId.HasValue ? new LegalEntityId(legalEntityId.Value) : userContext.LegalEntityId;
        if (legalEntity.HasValue && !userContext.IsAuthorizedForLegalEntity(legalEntity.Value))
        {
            return AccessDenied("the requested legal entity");
        }

        var positions = await _repository.ListPositionsAsync(userContext.TenantId, legalEntity, organizationUnitId, ct);
        return Ok(positions);
    }

    [HttpPost("positions")]
    [ProducesResponseType(typeof(PositionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreatePosition([FromBody] CreatePositionRequest request, CancellationToken ct)
    {
        var userContext = _userContext;
        if (!HasAnyPermission("organization.position.create"))
        {
            return AccessDenied("organization.position.create");
        }

        if (string.IsNullOrWhiteSpace(request.JobCode) ||
            string.IsNullOrWhiteSpace(request.TitleEn) ||
            string.IsNullOrWhiteSpace(request.TitleAr) ||
            request.OrganizationUnitId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Incomplete Position Master Data",
                Detail = "organizationUnitId, jobCode, titleEn, and titleAr must be supplied explicitly.",
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
                Detail = "An authorized legal entity is required to create a position.",
                Instance = HttpContext.Request.Path
            });
        }
        if (!userContext.IsAuthorizedForLegalEntity(legalEntity.Value))
        {
            return AccessDenied("the requested legal entity");
        }

        var unit = await _repository.GetUnitByIdAsync(request.OrganizationUnitId, userContext.TenantId, legalEntity, ct);
        if (unit == null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Organization Unit",
                Detail = "The selected organization unit does not exist in the current tenant and legal entity.",
                Instance = HttpContext.Request.Path
            });
        }

        var position = new Position(
            Guid.NewGuid(),
            userContext.TenantId,
            legalEntity.Value,
            request.OrganizationUnitId,
            request.JobCode,
            request.TitleEn,
            request.TitleAr,
            request.Grade ?? "N/A");

        await _repository.InsertPositionAsync(position, ct);

        return Created($"/api/v1/organization/positions/{position.Id}", new PositionDto
        {
            Id = position.Id,
            TenantId = position.TenantId.Value.ToString(),
            LegalEntityId = position.LegalEntityId.Value.ToString(),
            OrganizationUnitId = position.OrganizationUnitId,
            JobCode = position.JobCode,
            TitleEn = position.TitleEn,
            TitleAr = position.TitleAr,
            Grade = position.Grade,
            IsActive = position.IsActive
        });
    }

    [HttpGet("cost-centers")]
    [ProducesResponseType(typeof(IReadOnlyList<CostCenterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListCostCenters(
        [FromQuery] Guid? legalEntityId,
        CancellationToken ct)
    {
        if (!HasAnyPermission("organization.cost_center.read", "organization.unit.read"))
        {
            return AccessDenied("organization.cost_center.read");
        }

        var legalEntity = legalEntityId.HasValue ? new LegalEntityId(legalEntityId.Value) : _userContext.LegalEntityId;
        if (legalEntity.HasValue && !_userContext.IsAuthorizedForLegalEntity(legalEntity.Value))
        {
            return AccessDenied("the requested legal entity");
        }

        var costCenters = await _repository.ListCostCentersAsync(_userContext.TenantId, legalEntity, ct);
        return Ok(costCenters);
    }

    [HttpPost("cost-centers")]
    [ProducesResponseType(typeof(CostCenterDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCostCenter([FromBody] CreateCostCenterRequest request, CancellationToken ct)
    {
        if (!HasAnyPermission("organization.cost_center.create"))
        {
            return AccessDenied("organization.cost_center.create");
        }

        if (string.IsNullOrWhiteSpace(request.Code) ||
            string.IsNullOrWhiteSpace(request.NameEn) ||
            string.IsNullOrWhiteSpace(request.NameAr))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Incomplete Cost Center Master Data",
                Detail = "code, nameEn, and nameAr must be supplied explicitly."
            });
        }

        if (!_userContext.LegalEntityId.HasValue || !_userContext.IsAuthorizedForLegalEntity(_userContext.LegalEntityId.Value))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Legal Entity Context Required",
                Detail = "An authorized legal entity is required to create a cost center."
            });
        }

        var costCenter = new CostCenter(
            Guid.NewGuid(),
            _userContext.TenantId,
            _userContext.LegalEntityId.Value,
            request.Code,
            request.NameEn,
            request.NameAr);

        try
        {
            await _repository.InsertCostCenterAsync(costCenter, ct);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Duplicate Cost Center Code",
                Detail = "The cost center code is already registered in this legal entity."
            });
        }

        return Created("/api/v1/organization/cost-centers", new CostCenterDto
        {
            Id = costCenter.Id,
            TenantId = costCenter.TenantId.Value.ToString(),
            LegalEntityId = costCenter.LegalEntityId.Value.ToString(),
            Code = costCenter.Code,
            NameEn = costCenter.NameEn,
            NameAr = costCenter.NameAr,
            IsActive = costCenter.IsActive
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

public class DeactivateOrganizationUnitRequest
{
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

public class CreatePositionRequest
{
    public Guid? LegalEntityId { get; set; }
    public Guid OrganizationUnitId { get; set; }
    public string JobCode { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string? Grade { get; set; }
}

public class CreateCostCenterRequest
{
    public string Code { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
}
