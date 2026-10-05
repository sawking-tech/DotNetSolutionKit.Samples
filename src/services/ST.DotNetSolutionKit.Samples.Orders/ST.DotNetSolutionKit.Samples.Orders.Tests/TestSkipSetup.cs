using System.Runtime.CompilerServices;
using ST.DotNetSolutionKit.Samples.Common.Tests;

namespace ST.DotNetSolutionKit.Samples.Orders.Tests;

/// <summary>Tells the shared test infrastructure that a skip here is xUnit's skip.</summary>
internal static class TestSkipSetup
{
    [ModuleInitializer]
    internal static void UseXunit() => TestSkip.Handler = reason => Assert.Skip(reason);
}
