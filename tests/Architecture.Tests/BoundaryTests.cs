using System.Reflection;
using Xunit;

namespace Architecture.Tests;

public class BoundaryTests
{
    [Fact]
    public void SharedKernel_ShouldNotReference_ModulesOrHost()
    {
        var sharedKernelAssembly = typeof(Workforce.SharedKernel.Class1).Assembly;
        var referencedAssemblies = sharedKernelAssembly.GetReferencedAssemblies();

        foreach (var assemblyName in referencedAssemblies)
        {
            Assert.DoesNotContain("Workforce.Modules.", assemblyName.Name);
            Assert.DoesNotContain("Workforce.Host.", assemblyName.Name);
        }
    }

    [Fact]
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
