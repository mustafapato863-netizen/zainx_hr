using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.People.Application;
using Workforce.Modules.People.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.People.Api;

/// <summary>
/// ESS/MSS projections over the authoritative People model.
/// This controller never creates a second employee or self-service data store.
/// </summary>
[ApiController]
[Route("api/v1/self-service")]
public sealed class SelfServiceController : ControllerBase
{
    private readonly PeopleRepository _repository;
    private readonly IUserContext _userContext;

    public SelfServiceController(PeopleRepository repository, IUserContext userContext)
    {
        _repository = repository;
        _userContext = userContext;
    }

    [HttpGet("profile")]
    [ProducesResponseType(typeof(EmployeeProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        if (!HasAnyPermission("self.profile.read"))
        {
            return Forbid();
        }

        if (!TryGetLegalEntity(out var legalEntity, out var legalEntityError))
        {
            return legalEntityError!;
        }

        var employmentId = await _repository.GetLinkedEmploymentIdAsync(
            _userContext.TenantId,
            legalEntity,
            _userContext.UserId.Value,
            ct);
        if (!employmentId.HasValue)
        {
            return IdentityLinkRequired();
        }

        var profile = await _repository.GetEmployeeProfileAsync(
            employmentId.Value,
            _userContext.TenantId,
            legalEntity,
            ct);
        return profile == null ? IdentityLinkRequired() : Ok(profile);
    }

    [HttpPut("profile")]
    [ProducesResponseType(typeof(EmployeeProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] SelfServiceProfileUpdateRequest request,
        CancellationToken ct)
    {
        if (!HasAnyPermission("self.profile.update"))
        {
            return Forbid();
        }

        if (request.PrimaryEmail == null && request.PhoneNumber == null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "No Profile Changes Supplied",
                Detail = "Provide primaryEmail and/or phoneNumber explicitly."
            });
        }

        if (!TryGetLegalEntity(out var legalEntity, out var legalEntityError))
        {
            return legalEntityError!;
        }

        var employmentId = await _repository.GetLinkedEmploymentIdAsync(
            _userContext.TenantId,
            legalEntity,
            _userContext.UserId.Value,
            ct);
        if (!employmentId.HasValue)
        {
            return IdentityLinkRequired();
        }

        var updated = await _repository.UpdateSelfServiceContactAsync(
            _userContext.TenantId,
            legalEntity,
            employmentId.Value,
            request.RowVersion,
            request.PrimaryEmail?.Trim(),
            request.PhoneNumber?.Trim(),
            _userContext.UserId.Value,
            ct);
        if (!updated)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Profile Changed",
                Detail = "The profile was changed by another operation. Refresh the profile and retry."
            });
        }

        var profile = await _repository.GetEmployeeProfileAsync(
            employmentId.Value,
            _userContext.TenantId,
            legalEntity,
            ct);
        return profile == null ? IdentityLinkRequired() : Ok(profile);
    }

    [HttpGet("team")]
    [ProducesResponseType(typeof(PagedResult<EmployeeSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeam(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!HasAnyPermission("self.team.read"))
        {
            return Forbid();
        }

        if (!TryGetLegalEntity(out var legalEntity, out var legalEntityError))
        {
            return legalEntityError!;
        }

        var employmentId = await _repository.GetLinkedEmploymentIdAsync(
            _userContext.TenantId,
            legalEntity,
            _userContext.UserId.Value,
            ct);
        if (!employmentId.HasValue)
        {
            return IdentityLinkRequired();
        }

        return Ok(await _repository.QueryManagerTeamAsync(
            _userContext.TenantId,
            legalEntity,
            employmentId.Value,
            page,
            pageSize,
            ct));
    }

    [HttpPost("~/api/v1/people/identity-links")]
    [ProducesResponseType(typeof(UserEmploymentLinkDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(UserEmploymentLinkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> LinkUserToEmployment(
        [FromBody] UserEmploymentLinkRequest request,
        CancellationToken ct)
    {
        if (!HasAnyPermission("people.identity.link"))
        {
            return Forbid();
        }

        if (request.UserId == Guid.Empty || request.EmploymentId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Identity Link Requires Explicit Identifiers",
                Detail = "userId and employmentId must be valid, non-empty GUIDs."
            });
        }

        if (!TryGetLegalEntity(out var legalEntity, out var legalEntityError))
        {
            return legalEntityError!;
        }

        var result = await _repository.LinkUserToEmploymentAsync(
            _userContext.TenantId,
            legalEntity,
            request.UserId,
            request.EmploymentId,
            _userContext.UserId.Value,
            ct);

        return result switch
        {
            UserEmploymentLinkResult.Created => StatusCode(StatusCodes.Status201Created, LinkResponse(request, legalEntity, "created")),
            UserEmploymentLinkResult.AlreadyLinked => Ok(LinkResponse(request, legalEntity, "already-linked")),
            UserEmploymentLinkResult.EmploymentNotFound => NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Employment Not Found",
                Detail = "The employment does not exist in the active tenant and legal-entity scope."
            }),
            UserEmploymentLinkResult.UserAlreadyLinked => Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "User Already Linked",
                Detail = "The user already has an active employment link in this legal-entity scope. Unlink it before assigning another employment."
            }),
            UserEmploymentLinkResult.EmploymentAlreadyLinked => Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Employment Already Linked",
                Detail = "The employment already has an active user link in this legal-entity scope."
            }),
            _ => Problem("The identity link could not be created.")
        };
    }

    [HttpDelete("~/api/v1/people/identity-links/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlinkUserFromEmployment(Guid userId, CancellationToken ct)
    {
        if (!HasAnyPermission("people.identity.link"))
        {
            return Forbid();
        }

        if (!TryGetLegalEntity(out var legalEntity, out var legalEntityError))
        {
            return legalEntityError!;
        }

        var unlinked = await _repository.UnlinkUserFromEmploymentAsync(
            _userContext.TenantId,
            legalEntity,
            userId,
            _userContext.UserId.Value,
            ct);
        return unlinked
            ? NoContent()
            : NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Active Identity Link Not Found",
                Detail = "No active user-to-employment link exists in the current tenant and legal-entity scope."
            });
    }

    private bool HasAnyPermission(params string[] permissions)
    {
        if (_userContext.HasPermission("admin")) return true;
        return permissions.Any(_userContext.HasPermission);
    }

    private bool TryGetLegalEntity(out Workforce.SharedKernel.Primitives.LegalEntityId legalEntity, out IActionResult? error)
    {
        if (_userContext.LegalEntityId.HasValue)
        {
            legalEntity = _userContext.LegalEntityId.Value;
            error = null;
            return true;
        }

        legalEntity = default;
        error = BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Legal Entity Context Required",
            Detail = "Select an authorized legal entity before using self-service."
        });
        return false;
    }

    private NotFoundObjectResult IdentityLinkRequired()
    {
        return NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Employee Identity Link Required",
            Detail = "The current authenticated user is not explicitly linked to an employment in this legal-entity scope. Ask an administrator to configure the link."
        });
    }

    private UserEmploymentLinkDto LinkResponse(UserEmploymentLinkRequest request, Workforce.SharedKernel.Primitives.LegalEntityId legalEntity, string status)
    {
        Response.Headers["X-Identity-Link-Status"] = status;
        return new UserEmploymentLinkDto
        {
            UserId = request.UserId,
            EmploymentId = request.EmploymentId,
            LegalEntityId = legalEntity.Value.ToString(),
            LinkedAtUtc = DateTime.UtcNow
        };
    }
}
