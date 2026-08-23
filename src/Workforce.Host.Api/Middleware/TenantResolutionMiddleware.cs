using System;
using System.Collections.Generic;
using System.Linq;
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
        // 1. Check Claims or Headers for Tenant & User Context
        var tenantClaim = context.User.FindFirst("tenant_id")?.Value 
                          ?? context.Request.Headers["X-Tenant-ID"].ToString();
                          
        var legalEntityClaim = context.User.FindFirst("legal_entity_id")?.Value 
                               ?? context.Request.Headers["X-Legal-Entity-ID"].ToString();
        
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? context.Request.Headers["X-User-ID"].ToString();

        var permissionsHeader = context.Request.Headers["X-Permissions"].ToString();

        // Parse TenantId
        var tenantId = Guid.TryParse(tenantClaim, out var tenantIdGuid)
            ? new TenantId(tenantIdGuid)
            : new TenantId(Guid.Parse("22222222-2222-2222-2222-222222222222"));

        // Parse UserId
        var userId = Guid.TryParse(userIdClaim, out var userIdGuid)
            ? new UserId(userIdGuid)
            : new UserId(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        // Parse LegalEntityId
        LegalEntityId? legalEntityId = Guid.TryParse(legalEntityClaim, out var leGuid) 
            ? new LegalEntityId(leGuid) 
            : new LegalEntityId(Guid.Parse("33333333-3333-3333-3333-333333333333"));

        // Permissions
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(permissionsHeader))
        {
            foreach (var perm in permissionsHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                permissions.Add(perm);
            }
        }
        else
        {
            // Default developer permissions
            permissions.UnionWith(new[]
            {
                "people.employee.read",
                "people.employee.create",
                "people.employee.update",
                "people.employee.reveal_pii",
                "organization.unit.read",
                "organization.unit.create",
                "organization.unit.update",
                "documents.read",
                "documents.upload",
                "documents.download",
                "admin"
            });
        }

        var entitlements = new HashSet<string> { "core.platform", "people", "organization", "documents" };

        var userContext = new UserContext(
            userId: userId,
            tenantId: tenantId,
            legalEntityId: legalEntityId,
            culture: context.Request.Headers["Accept-Language"].FirstOrDefault() ?? "en-US",
            timezone: "UTC",
            permissions: permissions,
            entitlements: entitlements
        );

        var userContextProvider = context.RequestServices.GetService<IUserContextProvider>();
        if (userContextProvider != null)
        {
            userContextProvider.SetContext(userContext);
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
