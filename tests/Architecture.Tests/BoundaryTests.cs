using System;
using System.Reflection;

namespace Architecture.Tests;

public class BoundaryTests
{
    public void SharedKernel_ShouldNotReference_ModulesOrHost()
    {
        var sharedKernelAssembly = typeof(Workforce.SharedKernel.Primitives.TenantId).Assembly;
        var referencedAssemblies = sharedKernelAssembly.GetReferencedAssemblies();

        foreach (var assemblyName in referencedAssemblies)
        {
            Assert.DoesNotContain("Workforce.Modules.", assemblyName.Name);
            Assert.DoesNotContain("Workforce.Host.", assemblyName.Name);
        }
    }

    public void BuildingBlocks_ShouldNotReference_ModulesOrHost()
    {
        var buildingBlocksAssembly = typeof(Workforce.BuildingBlocks.Class1).Assembly;
        var referencedAssemblies = buildingBlocksAssembly.GetReferencedAssemblies();

        foreach (var assemblyName in referencedAssemblies)
        {
            Assert.DoesNotContain("Workforce.Modules.", assemblyName.Name);
            Assert.DoesNotContain("Workforce.Host.", assemblyName.Name);
        }
    }
}
