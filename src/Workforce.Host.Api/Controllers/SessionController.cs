using Microsoft.AspNetCore.Mvc;
using Workforce.Host.Api.Middleware;
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
        // In a real application, this would validate that the user has access 
        // to the requested tenant/legal entity, and then issue a new token or 
        // update a secure cookie.
        // For Phase 1A, we just return a success payload.
        
        return Ok(new
        {
            message = "Context change requested successfully. Secure token refresh required.",
            requestedTenantId = request.TenantId,
            requestedLegalEntityId = request.LegalEntityId
        });
    }
}
