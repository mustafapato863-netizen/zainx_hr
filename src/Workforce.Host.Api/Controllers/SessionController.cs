using Microsoft.AspNetCore.Mvc;
using Workforce.Host.Api.Middleware;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

namespace Workforce.Host.Api.Controllers;

[ApiController]
[Route("api/session")]
public class SessionController : ControllerBase
{
    private readonly IUserContextProvider _userContextProvider;

    public SessionController(IUserContextProvider userContextProvider)
    {
        _userContextProvider = userContextProvider;
    }

    [HttpGet("current")]
    public IActionResult GetCurrentSession()
    {
        var context = _userContextProvider.Current;
        if (context == null)
        {
            return Unauthorized(new { message = "No valid session or context found." });
        }

        return Ok(new
        {
            user = new
            {
                id = context.UserId.Value,
                culture = context.Culture,
                timezone = context.Timezone
            },
            context = new
            {
                tenantId = context.TenantId.Value,
                legalEntityId = context.LegalEntityId?.Value
            },
            permissions = context.Permissions,
            entitlements = context.Entitlements
        });
    }

    public record ChangeContextRequest(string TenantId, string? LegalEntityId);

    [HttpPost("context")]
    public IActionResult ChangeContext([FromBody] ChangeContextRequest request)
    {
        var current = _userContextProvider.Current;
        if (current == null)
        {
            return Unauthorized(new { message = "No valid session or context found." });
        }

        if (!Guid.TryParse(request.TenantId, out var tenantGuid))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Tenant Identifier",
                Detail = "tenantId must be a valid GUID."
            });
        }

        var requestedTenant = new TenantId(tenantGuid);
        if (!current.IsAuthorizedForTenant(requestedTenant))
        {
            return Forbid();
        }

        LegalEntityId? requestedLegalEntity = null;
        if (!string.IsNullOrWhiteSpace(request.LegalEntityId))
        {
            if (!Guid.TryParse(request.LegalEntityId, out var legalEntityGuid))
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid Legal Entity Identifier",
                    Detail = "legalEntityId must be a valid GUID when supplied."
                });
            }

            requestedLegalEntity = new LegalEntityId(legalEntityGuid);
            if (!current.IsAuthorizedForLegalEntity(requestedLegalEntity.Value))
            {
                return Forbid();
            }
        }

        return StatusCode(StatusCodes.Status501NotImplemented, new ProblemDetails
        {
            Status = StatusCodes.Status501NotImplemented,
            Title = "Secure Context Switch Is Not Configured",
            Detail = "The target context is authorized, but the configured identity provider has not supplied a secure token or session-refresh mechanism. No context was changed."
        });
    }
}
