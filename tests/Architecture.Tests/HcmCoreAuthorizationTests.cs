using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Documents.Api;
using Workforce.Modules.Documents.Infrastructure;
using Workforce.Modules.Organization.Api;
using Workforce.Modules.Organization.Infrastructure;
using Workforce.Modules.People.Api;
using Workforce.Modules.People.Infrastructure;
using Workforce.Modules.Tenancy.Api;
using Workforce.Modules.Tenancy.Domain;
using Workforce.Modules.Tenancy.Infrastructure;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;
using Xunit;

namespace Architecture.Tests;

public class HcmCoreAuthorizationTests
{
    [Fact]
    public async Task CoreReadEndpoints_WithoutRequiredPermissions_Return403BeforeRepositoryAccess()
    {
        var context = CreateContext();
        var people = Configure(new PeopleController(new PeopleRepository("unused"), context));
        var organization = Configure(new OrganizationController(new OrganizationRepository("unused"), context));
        var documents = Configure(new DocumentsController(new DocumentsRepository("unused"), new NullStorageProvider(), context));
        var tenancy = Configure(new TenancyController(new TenancyRepository("unused"), context));
        var selfService = Configure(new SelfServiceController(new PeopleRepository("unused"), context));

        var peopleResult = await people.GetEmployees(null, null, null, 1, 20, CancellationToken.None);
        var organizationResult = await organization.ListUnits(null, CancellationToken.None);
        var positionsResult = await organization.ListPositions(null, null, CancellationToken.None);
        var documentsResult = await documents.ListDocuments("Employee", Guid.NewGuid(), CancellationToken.None);
        var tenancyResult = await tenancy.GetContext(CancellationToken.None);
        var selfServiceResult = await selfService.GetProfile(CancellationToken.None);

        global::Xunit.Assert.Equal(StatusCodes.Status403Forbidden, global::Xunit.Assert.IsType<ObjectResult>(peopleResult).StatusCode);
        global::Xunit.Assert.Equal(StatusCodes.Status403Forbidden, global::Xunit.Assert.IsType<ObjectResult>(organizationResult).StatusCode);
        global::Xunit.Assert.Equal(StatusCodes.Status403Forbidden, global::Xunit.Assert.IsType<ObjectResult>(positionsResult).StatusCode);
        global::Xunit.Assert.Equal(StatusCodes.Status403Forbidden, global::Xunit.Assert.IsType<ObjectResult>(documentsResult).StatusCode);
        global::Xunit.Assert.Equal(StatusCodes.Status403Forbidden, global::Xunit.Assert.IsType<ObjectResult>(tenancyResult).StatusCode);
        global::Xunit.Assert.IsType<ForbidResult>(selfServiceResult);
    }

    [Fact]
    public async Task OrganizationRead_WhenLegalEntityIsOutsideContext_Returns403BeforeRepositoryAccess()
    {
        var context = CreateContext("organization.unit.read");
        var controller = Configure(new OrganizationController(new OrganizationRepository("unused"), context));

        var result = await controller.ListUnits(Guid.NewGuid(), CancellationToken.None);

        global::Xunit.Assert.Equal(StatusCodes.Status403Forbidden, global::Xunit.Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task CreateEmployee_WhenRequiredMasterDataIsMissing_Returns400WithoutInventingDefaults()
    {
        var context = CreateContext("people.employee.create");
        var controller = Configure(new PeopleController(new PeopleRepository("unused"), context));

        var result = await controller.CreateEmployee(new CreateEmployeeRequest(), CancellationToken.None);

        var objectResult = global::Xunit.Assert.IsType<BadRequestObjectResult>(result);
        var problem = global::Xunit.Assert.IsType<ProblemDetails>(objectResult.Value);
        global::Xunit.Assert.Equal("Incomplete Employee Master Data", problem.Title);
        global::Xunit.Assert.Contains("employeeNumber", problem.Detail!);
        global::Xunit.Assert.Contains("nationalIdentifier", problem.Detail!);
    }

    [Fact]
    public void LegalEntity_UpdateRequiresExpectedRowVersion()
    {
        var tenantId = TenantId.New();
        var entity = new LegalEntity(
            LegalEntityId.New(),
            tenantId,
            "ZAINX-SA",
            "Zain X Saudi",
            "زين إكس السعودية",
            "SA",
            "SAR",
            "Asia/Riyadh");

        var originalVersion = entity.RowVersion;
        entity.UpdateDetails("Zain X Saudi Updated", "زين إكس السعودية محدث", "SA", "SAR", "Asia/Riyadh", originalVersion);

        global::Xunit.Assert.Equal(originalVersion + 1, entity.RowVersion);
        global::Xunit.Assert.Throws<InvalidOperationException>(() => entity.UpdateDetails(
            "Stale", "قديم", "SA", "SAR", "Asia/Riyadh", originalVersion));
    }

    private static T Configure<T>(T controller) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    private static UserContext CreateContext(params string[] permissions)
    {
        var tenant = TenantId.New();
        var legalEntity = LegalEntityId.New();
        return new UserContext(
            UserId.New(),
            tenant,
            legalEntity,
            "en-US",
            "UTC",
            permissions,
            new[] { "core.platform" },
            new[] { tenant },
            new[] { legalEntity });
    }

    private sealed class NullStorageProvider : IStorageProvider
    {
        public Task<string> SaveAsync(Stream content, string tenantId, string fileName, CancellationToken ct = default) =>
            throw new InvalidOperationException("Storage must not be reached by authorization tests.");

        public Task<Stream?> ReadAsync(string storageKey, CancellationToken ct = default) =>
            throw new InvalidOperationException("Storage must not be reached by authorization tests.");

        public Task<bool> DeleteAsync(string storageKey, CancellationToken ct = default) =>
            throw new InvalidOperationException("Storage must not be reached by authorization tests.");
    }
}
