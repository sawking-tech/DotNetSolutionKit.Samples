using System.Runtime.CompilerServices;
using ST.DotNetSolutionKit.Samples.Common.Tests;

namespace ST.DotNetSolutionKit.Samples.Billing.Tests;

/// <summary>Tells the shared test infrastructure that a skip here is NUnit's ignore.</summary>
internal static class TestSkipSetup
{
    [ModuleInitializer]
    internal static void UseNUnit() => TestSkip.Handler = Assert.Ignore;
}
