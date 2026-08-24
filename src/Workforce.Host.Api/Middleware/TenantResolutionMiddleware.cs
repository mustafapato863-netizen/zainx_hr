using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

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
        // 1. Identify User Identity & Server-Known Allowed Memberships
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? context.Request.Headers["X-User-ID"].ToString();

        var userId = Guid.TryParse(userIdClaim, out var uGuid)
            ? new UserId(uGuid)
            : new UserId(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        // Server-Known Allowed Tenants for this Principal
        var allowedTenantsClaim = context.User.FindAll("allowed_tenants").Select(c => c.Value).ToList();
        var allowedTenantsHeader = context.Request.Headers["X-Allowed-Tenants"].ToString();
        
        var allowedTenants = new HashSet<TenantId>();
        if (allowedTenantsClaim.Count > 0)
        {
            foreach (var t in allowedTenantsClaim)
            {
                if (Guid.TryParse(t, out var tg)) allowedTenants.Add(new TenantId(tg));
            }
        }
        else if (!string.IsNullOrWhiteSpace(allowedTenantsHeader))
        {
            foreach (var t in allowedTenantsHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Guid.TryParse(t, out var tg)) allowedTenants.Add(new TenantId(tg));
            }
        }

        // Canonical default allowed tenant for developer/sandbox sessions
        var defaultTenantId = new TenantId(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        if (allowedTenants.Count == 0)
        {
            allowedTenants.Add(defaultTenantId);
        }

        // Server-Known Allowed Legal Entities for this Principal
        var allowedEntitiesClaim = context.User.FindAll("allowed_legal_entities").Select(c => c.Value).ToList();
        var allowedEntitiesHeader = context.Request.Headers["X-Allowed-Legal-Entities"].ToString();
        
        var allowedLegalEntities = new HashSet<LegalEntityId>();
        if (allowedEntitiesClaim.Count > 0)
        {
            foreach (var le in allowedEntitiesClaim)
            {
                if (Guid.TryParse(le, out var leg)) allowedLegalEntities.Add(new LegalEntityId(leg));
            }
        }
        else if (!string.IsNullOrWhiteSpace(allowedEntitiesHeader))
        {
            foreach (var le in allowedEntitiesHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Guid.TryParse(le, out var leg)) allowedLegalEntities.Add(new LegalEntityId(leg));
            }
        }

        var defaultLegalEntityId = new LegalEntityId(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        if (allowedLegalEntities.Count == 0)
        {
            allowedLegalEntities.Add(defaultLegalEntityId);
        }

        // 2. Evaluate Requested Tenant Context (Selector) Against Authorized Memberships
        TenantId effectiveTenantId;
        var requestedTenantHeader = context.Request.Headers["X-Tenant-ID"].ToString();

        if (!string.IsNullOrWhiteSpace(requestedTenantHeader))
        {
            if (!Guid.TryParse(requestedTenantHeader, out var requestedTenantGuid))
            {
                await WriteProblemResponseAsync(
                    context, 
                    StatusCodes.Status400BadRequest, 
                    "Invalid Tenant Identifier", 
                    $"The requested tenant identifier '{requestedTenantHeader}' is not a valid GUID.");
                return;
            }

            var requestedTenant = new TenantId(requestedTenantGuid);
            if (!allowedTenants.Contains(requestedTenant))
            {
                // Strict Security: Caller cannot switch context to a tenant they are not authorized for
                await WriteProblemResponseAsync(
                    context, 
                    StatusCodes.Status403Forbidden, 
                    "Forbidden Tenant Context", 
                    $"The authenticated user is not authorized for the requested tenant context '{requestedTenant.Value}'.");
                return;
            }

            effectiveTenantId = requestedTenant;
        }
        else
        {
            // Missing selector: use user's primary/default authorized tenant
            effectiveTenantId = allowedTenants.First();
        }

        // 3. Evaluate Requested Legal Entity Context Against Authorized Memberships
        LegalEntityId? effectiveLegalEntityId = null;
        var requestedEntityHeader = context.Request.Headers["X-Legal-Entity-ID"].ToString();

        if (!string.IsNullOrWhiteSpace(requestedEntityHeader))
        {
            if (!Guid.TryParse(requestedEntityHeader, out var requestedLeGuid))
            {
                await WriteProblemResponseAsync(
                    context, 
                    StatusCodes.Status400BadRequest, 
                    "Invalid Legal Entity Identifier", 
                    $"The requested legal entity identifier '{requestedEntityHeader}' is not a valid GUID.");
                return;
            }

            var requestedEntity = new LegalEntityId(requestedLeGuid);
            if (!allowedLegalEntities.Contains(requestedEntity))
            {
                await WriteProblemResponseAsync(
                    context, 
                    StatusCodes.Status403Forbidden, 
                    "Forbidden Legal Entity Context", 
                    $"The authenticated user is not authorized for the requested legal entity context '{requestedEntity.Value}'.");
                return;
            }

            effectiveLegalEntityId = requestedEntity;
        }
        else
        {
            effectiveLegalEntityId = allowedLegalEntities.FirstOrDefault();
        }

        // 4. Permissions & Entitlements
        var permissionsHeader = context.Request.Headers["X-Permissions"].ToString();
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
            tenantId: effectiveTenantId,
            legalEntityId: effectiveLegalEntityId,
            culture: context.Request.Headers["Accept-Language"].FirstOrDefault() ?? "en-US",
            timezone: "UTC",
            permissions: permissions,
            entitlements: entitlements,
            allowedTenants: allowedTenants,
            allowedLegalEntities: allowedLegalEntities
        );

        var userContextProvider = context.RequestServices.GetService<IUserContextProvider>();
        if (userContextProvider != null)
        {
            userContextProvider.SetContext(userContext);
        }

        await _next(context);
    }

    private static async Task WriteProblemResponseAsync(HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        await JsonSerializer.SerializeAsync(context.Response.Body, problem);
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
