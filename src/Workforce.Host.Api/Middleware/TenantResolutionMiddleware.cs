using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

namespace Workforce.Host.Api.Middleware;

public sealed class TenantResolutionMiddleware
{
    private static readonly Guid DevelopmentUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DevelopmentTenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid DevelopmentLegalEntityId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsPublicHealthEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var environment = context.RequestServices.GetRequiredService<IHostEnvironment>();
        var allowDevelopmentFallback = environment.IsDevelopment() || environment.IsEnvironment("Test");
        var isAuthenticated = context.User.Identity?.IsAuthenticated == true;

        UserContext userContext;
        if (!isAuthenticated)
        {
            if (!allowDevelopmentFallback)
            {
                await WriteProblemResponseAsync(
                    context,
                    StatusCodes.Status401Unauthorized,
                    "Authentication Required",
                    "The API requires an authenticated principal. Configure the approved identity provider before serving non-development traffic.");
                return;
            }

            // This fixed sandbox context is available only in Development/Test.
            // Request headers never grant permissions, tenants, legal entities, or admin access.
            userContext = CreateDevelopmentContext();
        }
        else if (!TryBuildAuthenticatedContext(context.User, out userContext, out var failure))
        {
            await WriteProblemResponseAsync(context, failure.StatusCode, failure.Title, failure.Detail);
            return;
        }

        if (!TryResolveTenant(context, userContext, out var effectiveTenantId, out var tenantFailure))
        {
            await WriteProblemResponseAsync(context, tenantFailure.StatusCode, tenantFailure.Title, tenantFailure.Detail);
            return;
        }

        if (!TryResolveLegalEntity(context, userContext, out var effectiveLegalEntityId, out var legalEntityFailure))
        {
            await WriteProblemResponseAsync(context, legalEntityFailure.StatusCode, legalEntityFailure.Title, legalEntityFailure.Detail);
            return;
        }

        userContext = new UserContext(
            userContext.UserId,
            effectiveTenantId,
            effectiveLegalEntityId,
            userContext.Culture,
            userContext.Timezone,
            userContext.Permissions,
            userContext.Entitlements,
            userContext.AllowedTenants,
            userContext.AllowedLegalEntities);

        context.RequestServices.GetRequiredService<IUserContextProvider>().SetContext(userContext);
        await _next(context);
    }

    private static bool IsPublicHealthEndpoint(PathString path)
    {
        return path.Equals("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/health/", StringComparison.OrdinalIgnoreCase);
    }

    private static UserContext CreateDevelopmentContext()
    {
        return new UserContext(
            new UserId(DevelopmentUserId),
            new TenantId(DevelopmentTenantId),
            new LegalEntityId(DevelopmentLegalEntityId),
            "en-US",
            "UTC",
            new[]
            {
                "admin",
                "people.employee.read", "people.employee.create", "people.employee.update", "people.employee.reveal_pii",
                "people.identity.link", "self.profile.read", "self.profile.update", "self.team.read",
                "self.leave.read", "self.leave.request", "self.leave.cancel", "self.attendance.read", "self.attendance.clock", "self.documents.read",
                "organization.unit.read", "organization.unit.create", "organization.unit.update",
                "organization.unit.deactivate", "organization.location.read", "organization.location.create",
                "organization.position.read", "organization.position.create",
                "organization.cost_center.read", "organization.cost_center.create",
                "platform.tenant.read", "platform.legal_entity.read", "platform.legal_entity.manage",
                "documents.read", "documents.types.read", "documents.upload", "documents.download", "documents.replace",
                "attendance.clock.create", "attendance.adjustment.submit", "attendance.day.read", "attendance.read", "attendance.schedule.read", "attendance.day.approve", "attendance.exception.resolve",
                "leave.type.read", "leave.balance.read", "leave.request.read", "leave.request.create", "leave.request.approve", "leave.request.reject",
                "approvals.inbox.read", "approvals.action.execute", "approvals.delegation.manage", "approvals.decision.approve", "approvals.decision.reject", "approvals.decision.cancel",
                "payroll.run.read", "payroll.run.create", "payroll.run.calculate", "payroll.run.finalize", "payroll.exceptions.resolve",
                "settlement.batch.read", "settlement.batch.generate", "settlement.batch.approve", "settlement.batch.export",
                "compliance.rules.read",
                "recruitment.requisition.read", "recruitment.requisition.create", "recruitment.requisition.approve",
                "recruitment.candidate.read", "recruitment.candidate.manage",
                "recruitment.application.read", "recruitment.application.move", "recruitment.application.reject",
                "recruitment.interview.manage", "recruitment.scorecard.submit", "recruitment.scorecard.read_all",
                "recruitment.offer.read", "recruitment.offer.read_sensitive", "recruitment.offer.create", "recruitment.offer.approve", "recruitment.offer.issue",
                "recruitment.hire",
                "reports.read", "reports.export",
                "admin.roles.manage", "admin.settings.manage", "admin.retention.manage",
                "integrations.manage",
                "audit.read"
            },
            new[] { "core.platform", "people", "organization", "documents", "attendance", "leave", "approvals", "payroll", "compliance", "settlement", "recruitment", "reports", "admin", "integrations", "notifications", "audit" },
            new[] { new TenantId(DevelopmentTenantId) },
            new[] { new LegalEntityId(DevelopmentLegalEntityId) });
    }

    private static bool TryBuildAuthenticatedContext(
        ClaimsPrincipal principal,
        out UserContext userContext,
        out SecurityFailure failure)
    {
        userContext = null!;

        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(userIdValue, out var userGuid))
        {
            failure = new SecurityFailure(
                StatusCodes.Status401Unauthorized,
                "Invalid Authenticated Principal",
                "The authenticated principal has no valid subject identifier.");
            return false;
        }

        var allowedTenants = ParseGuidClaims(principal, "allowed_tenants")
            .Select(value => new TenantId(value))
            .ToHashSet();
        if (allowedTenants.Count == 0)
        {
            failure = new SecurityFailure(
                StatusCodes.Status403Forbidden,
                "Tenant Membership Required",
                "The authenticated principal has no server-issued tenant membership claims.");
            return false;
        }

        var allowedLegalEntities = ParseGuidClaims(principal, "allowed_legal_entities")
            .Select(value => new LegalEntityId(value))
            .ToHashSet();

        var permissions = ParseStringClaims(principal, "permission", "permissions");
        var entitlements = ParseStringClaims(principal, "entitlement", "entitlements");

        userContext = new UserContext(
            new UserId(userGuid),
            allowedTenants.First(),
            allowedLegalEntities.FirstOrDefault(),
            principal.FindFirstValue("culture") ?? "en-US",
            principal.FindFirstValue("timezone") ?? "UTC",
            permissions,
            entitlements,
            allowedTenants,
            allowedLegalEntities);

        failure = default;
        return true;
    }

    private static bool TryResolveTenant(
        HttpContext context,
        UserContext userContext,
        out TenantId effectiveTenantId,
        out SecurityFailure failure)
    {
        var requestedTenantHeader = context.Request.Headers["X-Tenant-ID"].ToString();
        if (string.IsNullOrWhiteSpace(requestedTenantHeader))
        {
            effectiveTenantId = userContext.TenantId;
            failure = default;
            return true;
        }

        if (!Guid.TryParse(requestedTenantHeader, out var requestedTenantGuid))
        {
            effectiveTenantId = default;
            failure = new SecurityFailure(StatusCodes.Status400BadRequest, "Invalid Tenant Identifier", "The requested tenant identifier is not a valid GUID.");
            return false;
        }

        effectiveTenantId = new TenantId(requestedTenantGuid);
        if (!userContext.IsAuthorizedForTenant(effectiveTenantId))
        {
            failure = new SecurityFailure(StatusCodes.Status403Forbidden, "Forbidden Tenant Context", "The authenticated user is not authorized for the requested tenant context.");
            return false;
        }

        failure = default;
        return true;
    }

    private static bool TryResolveLegalEntity(
        HttpContext context,
        UserContext userContext,
        out LegalEntityId? effectiveLegalEntityId,
        out SecurityFailure failure)
    {
        var requestedEntityHeader = context.Request.Headers["X-Legal-Entity-ID"].ToString();
        if (string.IsNullOrWhiteSpace(requestedEntityHeader))
        {
            effectiveLegalEntityId = userContext.LegalEntityId;
            failure = default;
            return true;
        }

        if (!Guid.TryParse(requestedEntityHeader, out var requestedLegalEntityGuid))
        {
            effectiveLegalEntityId = null;
            failure = new SecurityFailure(StatusCodes.Status400BadRequest, "Invalid Legal Entity Identifier", "The requested legal entity identifier is not a valid GUID.");
            return false;
        }

        effectiveLegalEntityId = new LegalEntityId(requestedLegalEntityGuid);
        if (!userContext.IsAuthorizedForLegalEntity(effectiveLegalEntityId.Value))
        {
            failure = new SecurityFailure(StatusCodes.Status403Forbidden, "Forbidden Legal Entity Context", "The authenticated user is not authorized for the requested legal entity context.");
            return false;
        }

        failure = default;
        return true;
    }

    private static IEnumerable<Guid> ParseGuidClaims(ClaimsPrincipal principal, string claimType)
    {
        return principal.FindAll(claimType)
            .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(value => Guid.TryParse(value, out _))
            .Select(Guid.Parse);
    }

    private static HashSet<string> ParseStringClaims(ClaimsPrincipal principal, params string[] claimTypes)
    {
        return claimTypes
            .SelectMany(claimType => principal.FindAll(claimType))
            .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
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

    private readonly record struct SecurityFailure(int StatusCode, string Title, string Detail);
}

public interface IUserContextProvider
{
    IUserContext? Current { get; }
    void SetContext(IUserContext context);
}

public sealed class RequestUserContextProvider : IUserContextProvider
{
    public IUserContext? Current { get; private set; }

    public void SetContext(IUserContext context)
    {
        Current = context;
    }
}
