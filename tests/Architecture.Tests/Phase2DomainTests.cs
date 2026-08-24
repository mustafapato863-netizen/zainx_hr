using System;
using Workforce.SharedKernel.Primitives;
using Workforce.Modules.Organization.Domain;
using Workforce.Modules.People.Domain;
using Workforce.Modules.Documents.Domain;
using Xunit;

namespace Architecture.Tests;

public class Phase2DomainTests
{
    [Fact]
    public void OrganizationUnit_EffectivePeriod_ShouldDetectActiveStatus()
    {
        var period = new EffectivePeriod(new DateOnly(2024, 1, 1), new DateOnly(2025, 1, 1));
        
        Assert.True(period.IsActiveAt(new DateOnly(2024, 6, 1)));
        Assert.False(period.IsActiveAt(new DateOnly(2023, 12, 31)));
        Assert.False(period.IsActiveAt(new DateOnly(2025, 1, 2)));
    }

    [Fact]
    public void OrganizationUnit_Creation_ShouldSetInitialState()
    {
        var unit = new OrganizationUnit(
            Guid.NewGuid(),
            TenantId.New(),
            LegalEntityId.New(),
            "HR-01",
            "Human Resources",
            "الموارد البشرية",
            OrganizationUnitType.Department,
            null,
            new EffectivePeriod(new DateOnly(2024, 1, 1), null)
        );

        Assert.Equal("HR-01", unit.Code);
        Assert.Equal("Human Resources", unit.NameEn);
        Assert.Equal("الموارد البشرية", unit.NameAr);
        Assert.Equal(1u, unit.RowVersion);
        Assert.True(unit.IsActive);
    }

    [Fact]
    public void Employment_StateMachine_ShouldTransitionStatus()
    {
        var employment = new Employment(
            Guid.NewGuid(),
            TenantId.New(),
            Guid.NewGuid(),
            LegalEntityId.New(),
            "EMP-1001",
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 4, 1),
            EmploymentStatus.Draft
        );

        Assert.Equal(EmploymentStatus.Draft, employment.Status);

        employment.Activate(expectedRowVersion: 1);
        Assert.Equal(EmploymentStatus.Active, employment.Status);
        Assert.Equal(2u, employment.RowVersion);

        employment.Terminate(new DateOnly(2025, 1, 1), "Resignation", expectedRowVersion: 2);
        Assert.Equal(EmploymentStatus.Terminated, employment.Status);
        Assert.Equal(3u, employment.RowVersion);
    }

    [Fact]
    public void EmploymentAssignment_TemporalDating_ShouldValidateCurrent()
    {
        var assign = new EmploymentAssignment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Senior Engineer",
            "مهندس أول",
            new DateOnly(2024, 1, 1),
            null,
            null,
            null,
            null,
            true
        );

        Assert.True(assign.IsCurrent);
        Assert.Null(assign.EffectiveTo);

        assign.CloseAssignment(new DateOnly(2024, 12, 31));
        Assert.False(assign.IsCurrent);
        Assert.Equal(new DateOnly(2024, 12, 31), assign.EffectiveTo);
    }

    [Fact]
    public void Document_StateManagement_ShouldSupportLifecycle()
    {
        var doc = new Document(
            Guid.NewGuid(),
            TenantId.New(),
            LegalEntityId.New(),
            "Employee",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "National ID Copy",
            new DateOnly(2030, 1, 1),
            Guid.NewGuid()
        );

        Assert.Equal(DocumentStatus.Active, doc.Status);

        doc.Archive();
        Assert.Equal(DocumentStatus.Archived, doc.Status);

        doc.MarkExpired();
        Assert.Equal(DocumentStatus.Expired, doc.Status);
    }

    [Fact]
    public void Modules_ShouldNotReferenceDownstreamPayrollOrCompliance()
    {
        var peopleAssembly = typeof(Employment).Assembly;
        var orgAssembly = typeof(OrganizationUnit).Assembly;
        var docsAssembly = typeof(Document).Assembly;

        var assemblies = new[] { peopleAssembly, orgAssembly, docsAssembly };

        foreach (var asm in assemblies)
        {
            foreach (var refName in asm.GetReferencedAssemblies())
            {
                Assert.DoesNotContain("Payroll", refName.Name ?? "", StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Attendance", refName.Name ?? "", StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Leave", refName.Name ?? "", StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Compliance", refName.Name ?? "", StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Settlement", refName.Name ?? "", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
