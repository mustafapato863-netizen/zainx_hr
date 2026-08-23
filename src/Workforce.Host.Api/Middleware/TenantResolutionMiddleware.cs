using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Workforce.SharedKernel.Security;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Host.Api.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // For Phase 1A, we extract tenant from claims or headers.
            // In a real system this involves verifying the user actually belongs to this tenant.
            var tenantClaim = context.User.FindFirst("tenant_id")?.Value 
                              ?? context.Request.Headers["X-Tenant-ID"].ToString();
                              
            var legalEntityClaim = context.User.FindFirst("legal_entity_id")?.Value 
                                   ?? context.Request.Headers["X-Legal-Entity-ID"].ToString();
            
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (Guid.TryParse(tenantClaim, out var tenantIdGuid) && Guid.TryParse(userIdClaim, out var userIdGuid))
            {
                var tenantId = new TenantId(tenantIdGuid);
                var userId = new UserId(userIdGuid);
                LegalEntityId? legalEntityId = Guid.TryParse(legalEntityClaim, out var leGuid) 
                    ? new LegalEntityId(leGuid) 
                    : null;
                
                // TODO: Read actual permissions/entitlements from database or token
                var permissions = new[] { "platform.access" }; // Dummy baseline
                var entitlements = new[] { "core.platform" }; // Dummy baseline

                var userContext = new UserContext(
                    userId: userId,
                    tenantId: tenantId,
                    legalEntityId: legalEntityId,
                    culture: "en-US", // Default or extract from claim/header
                    timezone: "UTC", // Default
                    permissions: permissions,
                    entitlements: entitlements
                );

                // Assuming we use Scoped DI for IUserContext via a setter interface or provider
                var userContextProvider = context.RequestServices.GetService<IUserContextProvider>();
                if (userContextProvider != null)
                {
                    userContextProvider.SetContext(userContext);
                }
            }
        }

        await _next(context);
    }
}

public interface IUserContextProvider
{
    IUserContext? Current { get; }
    void SetContext(IUserContext context);
}

public class DefaultUserContextProvider : IUserContextProvider
{
    public IUserContext? Current { get; private set; }

    public void SetContext(IUserContext context)
    {
        Current = context;
    }
}
