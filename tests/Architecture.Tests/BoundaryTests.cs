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
}
