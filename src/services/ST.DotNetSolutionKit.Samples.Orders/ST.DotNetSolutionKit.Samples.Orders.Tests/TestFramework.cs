global using NUnit.Framework;
global using Shouldly;
using System.Runtime.CompilerServices;
using ST.DotNetSolutionKit.Samples.Common.Tests;

// A fixture is built for each test, as xUnit does: a test sets itself up in the constructor and cleans up
// in Dispose, whatever runs it.
[assembly: FixtureLifeCycle(LifeCycle.InstancePerTestCase)]

namespace ST.DotNetSolutionKit.Samples.Orders.Tests;

// The one place that knows the test framework. The tests carry these attributes and nothing of the
// framework's own, so a test reads the same under NUnit and xUnit and a change of framework touches only
// this file:
//
//   [Test(Description = "...")]  a test
//   [TestOf(typeof(...))]        the class a fixture tests, so a search for the class finds its tests
//   [Integration]                needs a real database: CI runs it apart, `TestCategory=Integration`
//   [RunsInParallel]             the fixture runs in parallel with others; under NUnit its tests do too,
//                                under xUnit they run one after another
//   [RunsAlone]                  the fixture does not run in parallel with others

/// <summary>A test or fixture that needs a real database.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class IntegrationAttribute() : CategoryAttribute(TestCategories.Integration);

/// <summary>A fixture whose tests run in parallel with each other and with other fixtures.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RunsInParallelAttribute() : ParallelizableAttribute(ParallelScope.All);

/// <summary>A fixture that does not run in parallel with others.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RunsAloneAttribute() : ParallelizableAttribute(ParallelScope.None);

/// <summary>Tells the shared test infrastructure that a skip here is NUnit's ignore.</summary>
internal static class TestSkipSetup
{
    [ModuleInitializer]
    internal static void UseNUnit() => TestSkip.Handler = Assert.Ignore;
}
