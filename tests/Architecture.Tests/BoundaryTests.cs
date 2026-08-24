using System;
using System.Reflection;
using Xunit;

namespace Architecture.Tests;

public class BoundaryTests
{
    [Fact]
    public void SharedKernel_ShouldNotReference_ModulesOrHost()
    {
        var sharedKernelAssembly = typeof(Workforce.SharedKernel.Primitives.TenantId).Assembly;
        var referencedAssemblies = sharedKernelAssembly.GetReferencedAssemblies();

        foreach (var assemblyName in referencedAssemblies)
        {
            Assert.DoesNotContain("Workforce.Modules.", assemblyName.Name ?? "");
            Assert.DoesNotContain("Workforce.Host.", assemblyName.Name ?? "");
        }
    }

    [Fact]
    public void BuildingBlocks_ShouldNotReference_ModulesOrHost()
    {
        var buildingBlocksAssembly = typeof(Workforce.BuildingBlocks.Database.MigrationRunner).Assembly;
        var referencedAssemblies = buildingBlocksAssembly.GetReferencedAssemblies();

        foreach (var assemblyName in referencedAssemblies)
        {
            Assert.DoesNotContain("Workforce.Modules.", assemblyName.Name ?? "");
            Assert.DoesNotContain("Workforce.Host.", assemblyName.Name ?? "");
        }
    }

    [Fact]
    public void Recruitment_ShouldNotReference_PeopleInfrastructure()
    {
        var recruitmentAssembly = typeof(Workforce.Modules.Recruitment.Api.RecruitmentApplicationsController).Assembly;
        
        foreach (var type in recruitmentAssembly.GetTypes())
        {
            // Check fields
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var field in fields)
            {
                Assert.DoesNotContain("Workforce.Modules.People.Infrastructure", field.FieldType.FullName ?? "");
                Assert.DoesNotContain("Workforce.Modules.People.Domain", field.FieldType.FullName ?? "");
            }

            // Check constructors
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var ctor in constructors)
            {
                foreach (var param in ctor.GetParameters())
                {
                    Assert.DoesNotContain("Workforce.Modules.People.Infrastructure", param.ParameterType.FullName ?? "");
                    Assert.DoesNotContain("Workforce.Modules.People.Domain", param.ParameterType.FullName ?? "");
                }
            }
        }
    }
}
