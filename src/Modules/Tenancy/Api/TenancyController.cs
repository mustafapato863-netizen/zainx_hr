using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Workforce.Modules.Tenancy.Application;
using Workforce.Modules.Tenancy.Domain;
using Workforce.Modules.Tenancy.Infrastructure;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Tenancy.Api;

[ApiController]
[Route("api/v1/tenancy")]
public sealed class TenancyController : ControllerBase
{
    private readonly TenancyRepository _repository;
    private readonly IUserContext _userContext;

    public TenancyController(TenancyRepository repository, IUserContext userContext)
    {
        _repository = repository;
        _userContext = userContext;
    }

    [HttpGet("context")]
    [ProducesResponseType(typeof(TenantContextDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContext(CancellationToken ct)
    {
        if (!HasAnyPermission("platform.tenant.read", "platform.legal_entity.read"))
        {
            return AccessDenied("platform.tenant.read");
        }

        var tenant = await _repository.GetTenantAsync(_userContext.TenantId, ct);
        if (tenant == null || !tenant.IsActive)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Tenant Context Not Found",
                Detail = "The current tenant is not provisioned in the platform context."
            });
        }

        var entities = await _repository.ListLegalEntitiesAsync(
            _userContext.TenantId,
            _userContext.AllowedLegalEntities,
            ct);

        return Ok(new TenantContextDto(
            MapTenant(tenant),
            entities.Select(MapLegalEntity).ToArray(),
            _userContext.LegalEntityId?.Value));
    }

    [HttpGet("legal-entities")]
    [ProducesResponseType(typeof(IReadOnlyList<LegalEntityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListLegalEntities(CancellationToken ct)
    {
        if (!HasAnyPermission("platform.tenant.read", "platform.legal_entity.read"))
        {
            return AccessDenied("platform.legal_entity.read");
        }

        var entities = await _repository.ListLegalEntitiesAsync(
            _userContext.TenantId,
            _userContext.AllowedLegalEntities,
            ct);
        return Ok(entities.Select(MapLegalEntity).ToArray());
    }

    [HttpPost("legal-entities")]
    [ProducesResponseType(typeof(LegalEntityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateLegalEntity([FromBody] CreateLegalEntityRequest request, CancellationToken ct)
    {
        if (!HasAnyPermission("platform.legal_entity.manage"))
        {
            return AccessDenied("platform.legal_entity.manage");
        }

        if (string.IsNullOrWhiteSpace(request.Code) ||
            string.IsNullOrWhiteSpace(request.NameEn) ||
            string.IsNullOrWhiteSpace(request.NameAr) ||
            string.IsNullOrWhiteSpace(request.CountryCode) ||
            string.IsNullOrWhiteSpace(request.CurrencyCode) ||
            string.IsNullOrWhiteSpace(request.TimezoneId))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Incomplete Legal Entity",
                Detail = "code, names, countryCode, currencyCode, and timezoneId must be supplied explicitly."
            });
        }

        var tenant = await _repository.GetTenantAsync(_userContext.TenantId, ct);
        if (tenant == null || !tenant.IsActive)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Tenant Context Not Found",
                Detail = "The current tenant must be provisioned before a legal entity can be created."
            });
        }

        var entity = new LegalEntity(
            LegalEntityId.New(),
            _userContext.TenantId,
            request.Code,
            request.NameEn,
            request.NameAr,
            request.CountryCode,
            request.CurrencyCode,
            request.TimezoneId);

        try
        {
            await _repository.InsertLegalEntityAsync(entity, ct);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Duplicate Legal Entity Code",
                Detail = "The legal entity code is already registered in this tenant."
            });
        }

        return Created($"/api/v1/tenancy/legal-entities/{entity.Id.Value}", MapLegalEntity(entity));
    }

    [HttpPut("legal-entities/{id:guid}")]
    [ProducesResponseType(typeof(LegalEntityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateLegalEntity(Guid id, [FromBody] UpdateLegalEntityRequest request, CancellationToken ct)
    {
        if (!HasAnyPermission("platform.legal_entity.manage"))
        {
            return AccessDenied("platform.legal_entity.manage");
        }

        LegalEntityId legalEntityId;
        try
        {
            legalEntityId = new LegalEntityId(id);
        }
        catch (ArgumentException)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Invalid Legal Entity Identifier" });
        }

        var entity = await _repository.GetLegalEntityAsync(
            _userContext.TenantId,
            legalEntityId,
            _userContext.AllowedLegalEntities,
            ct);
        if (entity == null) return NotFound();

        try
        {
            entity.UpdateDetails(
                request.NameEn,
                request.NameAr,
                request.CountryCode,
                request.CurrencyCode,
                request.TimezoneId,
                request.RowVersion);

            if (!await _repository.UpdateLegalEntityAsync(entity, ct))
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Optimistic Concurrency Conflict",
                    Detail = "The legal entity has changed. Refresh before saving again."
                });
            }

            return Ok(MapLegalEntity(entity));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Invalid Legal Entity Details", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Optimistic Concurrency Conflict", Detail = ex.Message });
        }
    }

    private bool HasAnyPermission(params string[] permissions)
    {
        if (_userContext.HasPermission("admin")) return true;
        return permissions.Any(_userContext.HasPermission);
    }

    private IActionResult AccessDenied(string permission)
    {
        return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Access Denied",
            Detail = $"The current user does not have permission '{permission}'."
        });
    }

    private static TenantDto MapTenant(Tenant tenant) => new()
    {
        Id = tenant.Id.Value,
        Code = tenant.Code,
        NameEn = tenant.NameEn,
        NameAr = tenant.NameAr,
        IsActive = tenant.IsActive
    };

    private static LegalEntityDto MapLegalEntity(LegalEntity entity) => new()
    {
        Id = entity.Id.Value,
        TenantId = entity.TenantId.Value,
        Code = entity.Code,
        NameEn = entity.NameEn,
        NameAr = entity.NameAr,
        CountryCode = entity.CountryCode,
        CurrencyCode = entity.CurrencyCode,
        TimezoneId = entity.TimezoneId,
        IsActive = entity.IsActive,
        RowVersion = entity.RowVersion
    };
}

public sealed class CreateLegalEntityRequest
{
    public string Code { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public string TimezoneId { get; set; } = string.Empty;
}

public sealed class UpdateLegalEntityRequest
{
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public string TimezoneId { get; set; } = string.Empty;
    public uint RowVersion { get; set; }
}
