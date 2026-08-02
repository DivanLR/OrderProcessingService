using ReflectionAssembly = System.Reflection.Assembly;
using ArchUnitNET.Domain;
using ArchUnitNET.Loader;

namespace ArchitectureTests;

public abstract class BaseTest
{
    protected static readonly ReflectionAssembly DomainAssembly = ReflectionAssembly.Load("Order.Processing.Domain");
    protected static readonly ReflectionAssembly ApplicationAssembly = ReflectionAssembly.Load("Order.Processing.Application");
    protected static readonly ReflectionAssembly PresentationAssembly = ReflectionAssembly.Load("Order.Processing.Api");

    protected static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            DomainAssembly,
            ApplicationAssembly,
            PresentationAssembly)
        .Build();
}
