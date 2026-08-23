using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Workforce.Modules.Documents.Domain;
using Workforce.Modules.Documents.Infrastructure;
using Workforce.Modules.Organization.Domain;
using Workforce.Modules.People.Domain;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;
using Xunit;

namespace Architecture.Tests;

public class Phase2SecurityIntegrationTests
{
    private readonly TenantId _tenantA = TenantId.New();
    private readonly TenantId _tenantB = TenantId.New();
    private readonly LegalEntityId _legalEntityA = LegalEntityId.New();
    private readonly LegalEntityId _legalEntityB = LegalEntityId.New();
    private readonly UserId _userA = UserId.New();

    [Fact]
    public void MultiTenant_OrganizationUnit_ShouldEnforceTenantBoundaries()
    {
        var unitA = new OrganizationUnit(
            Guid.NewGuid(),
            _tenantA,
            _legalEntityA,
            "HR-01",
            "Human Resources",
            "الموارد البشرية",
            OrganizationUnitType.Department,
            null,
            new EffectivePeriod(new DateOnly(2024, 1, 1), null)
        );

        // Verification: Tenant A unit must not match Tenant B
        Assert.Equal(_tenantA, unitA.TenantId);
        Assert.NotEqual(_tenantB, unitA.TenantId);
    }

    [Fact]
    public void MultiTenant_Employment_ShouldEnforceTenantAndLegalEntityIsolation()
    {
        var personA = new Person(
            Guid.NewGuid(),
            _tenantA,
            "Tariq",
            "Al-Mansoor",
            "طارق",
            "المنصور",
            new DateOnly(1990, 5, 15),
            "Male",
            "SA",
            "1098765432",
            "tariq@zainx.com",
            "+966500000001"
        );

        var employmentA = new Employment(
            Guid.NewGuid(),
            _tenantA,
            personA.Id,
            _legalEntityA,
            "EMP-1001",
            new DateOnly(2024, 1, 1),
            null,
            EmploymentStatus.Active
        );

        Assert.Equal(_tenantA, employmentA.TenantId);
        Assert.Equal(_legalEntityA, employmentA.LegalEntityId);

        // Cross-tenant verification
        Assert.NotEqual(_tenantB, employmentA.TenantId);
        Assert.NotEqual(_legalEntityB, employmentA.LegalEntityId);
    }

    [Fact]
    public void SensitivePII_UserContextPermission_ShouldDenyUnauthorizedUser()
    {
        var unauthorizedContext = new UserContext(
            _userA,
            _tenantA,
            _legalEntityA,
            "en-US",
            "UTC",
            new[] { "people.employee.read" }, // Lacks 'people.employee.reveal_pii'
            new[] { "core.platform" }
        );

        var authorizedContext = new UserContext(
            _userA,
            _tenantA,
            _legalEntityA,
            "en-US",
            "UTC",
            new[] { "people.employee.read", "people.employee.reveal_pii" },
            new[] { "core.platform" }
        );

        Assert.False(unauthorizedContext.HasPermission("people.employee.reveal_pii"));
        Assert.True(authorizedContext.HasPermission("people.employee.reveal_pii"));
    }

    [Fact]
    public void EffectiveDate_Integrity_ShouldPreventEndBeforeStart()
    {
        var validPeriod = new EffectivePeriod(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));
        Assert.True(validPeriod.EffectiveTo >= validPeriod.EffectiveFrom);

        Assert.Throws<ArgumentException>(() =>
            new EffectivePeriod(new DateOnly(2024, 12, 31), new DateOnly(2024, 1, 1))
        );
    }

    [Fact]
    public void Assignment_Timeline_ClosingCurrent_ShouldTransitionCleanly()
    {
        var initial = new EmploymentAssignment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Junior Engineer",
            "مهندس مبتدئ",
            new DateOnly(2024, 1, 1),
            null,
            null,
            null,
            null,
            true
        );

        Assert.True(initial.IsCurrent);
        Assert.Null(initial.EffectiveTo);

        // Close when promoting
        var promotionDate = new DateOnly(2024, 7, 1);
        initial.CloseAssignment(promotionDate.AddDays(-1));

        Assert.False(initial.IsCurrent);
        Assert.Equal(new DateOnly(2024, 6, 30), initial.EffectiveTo);
    }

    [Fact]
    public void OptimisticConcurrency_Employment_ShouldThrowOnVersionMismatch()
    {
        var emp = new Employment(
            Guid.NewGuid(),
            _tenantA,
            Guid.NewGuid(),
            _legalEntityA,
            "EMP-1002",
            new DateOnly(2024, 1, 1)
        );

        Assert.Equal(1u, emp.RowVersion);

        // Simulating concurrent update: expecting version 1 increments to version 2
        emp.Activate(expectedRowVersion: 1);
        Assert.Equal(2u, emp.RowVersion);

        // Submitting with stale version 1 must throw concurrency conflict
        Assert.Throws<InvalidOperationException>(() =>
            emp.UpdateEmploymentDates(new DateOnly(2024, 2, 1), null, expectedRowVersion: 1)
        );
    }

    [Fact]
    public async Task DocumentSecurity_MagicBytes_ShouldRejectSpoofedFiles()
    {
        // Valid PDF magic bytes (%PDF-1.4)
        var validPdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4 sample pdf content");
        using var validPdfStream = new MemoryStream(validPdfBytes);
        await DocumentSecurityValidator.ValidateContentSignatureAsync(validPdfStream, "contract.pdf");

        // Spoofed executable renamed to .pdf
        var maliciousExeBytes = Encoding.ASCII.GetBytes("MZ\x90\x00\x03\x00fake_executable");
        using var maliciousStream = new MemoryStream(maliciousExeBytes);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            DocumentSecurityValidator.ValidateContentSignatureAsync(maliciousStream, "contract.pdf")
        );
    }

    [Fact]
    public void DocumentSecurity_PathTraversal_ShouldRejectMaliciousFileNames()
    {
        Assert.Throws<ArgumentException>(() =>
            DocumentSecurityValidator.ValidateFileName("../../etc/shadow")
        );

        Assert.Throws<ArgumentException>(() =>
            DocumentSecurityValidator.ValidateFileName("..\\..\\windows\\system32\\cmd.exe")
        );

        Assert.Throws<ArgumentException>(() =>
            DocumentSecurityValidator.ValidateFileName("malicious.exe")
        );

        // Valid file name passes
        DocumentSecurityValidator.ValidateFileName("employment_contract.pdf");
    }

    [Fact]
    public void DocumentVersion_Replacement_ShouldPreserveHistoryModel()
    {
        var docId = Guid.NewGuid();
        var v1 = new DocumentVersion(
            Guid.NewGuid(),
            docId,
            1,
            "tenant-1/v1_key.pdf",
            "national_id.pdf",
            102400,
            "application/pdf",
            "sha256-v1",
            Guid.NewGuid()
        );

        var v2 = new DocumentVersion(
            Guid.NewGuid(),
            docId,
            2,
            "tenant-1/v2_key.pdf",
            "national_id_renewed.pdf",
            105600,
            "application/pdf",
            "sha256-v2",
            Guid.NewGuid()
        );

        Assert.Equal(1, v1.VersionNumber);
        Assert.Equal(2, v2.VersionNumber);
        Assert.NotEqual(v1.Sha256Checksum, v2.Sha256Checksum);
        Assert.NotEqual(v1.StorageKey, v2.StorageKey);
    }
}
