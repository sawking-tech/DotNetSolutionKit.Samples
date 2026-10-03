namespace ST.DotNetSolutionKit.Samples.Common.Tests;

/// <summary>
/// Test categories, for running a kind of test alone or leaving it out:
/// <c>dotnet test --filter "TestCategory!=Integration"</c>.
/// </summary>
public static class TestCategories
{
    /// <summary>Needs a real PostgreSQL; see <see cref="Integration.Postgres"/>.</summary>
    public const string Integration = "Integration";
}
