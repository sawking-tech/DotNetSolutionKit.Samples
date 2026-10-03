using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using ST.DotNetSolutionKit.Samples.Common.Application.FeatureManagement;
using NUnit.Framework;
using Shouldly;

namespace ST.DotNetSolutionKit.Samples.Common.Tests.FeatureManagement;

/// <summary>
/// Writing is the half a management UI depends on. What is pinned here is that a write changes the
/// value and nothing else — the description, owner and expiry someone wrote down are not this class's
/// to touch — and that an undeclared key is refused rather than invented.
/// </summary>
[TestFixture]
public class FileFeatureStoreTests
{
    private string _path = null!;
    private FileFeatureStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _path = Path.Combine(Path.GetTempPath(), $"features-{Guid.NewGuid():N}.json");
        File.WriteAllText(_path, """
            {
              "Features": {
                "sample.feature": {
                  "enabled": false,
                  "description": "kept as written",
                  "owner": "someone"
                }
              }
            }
            """);

        _store = new FileFeatureStore(
            new FeatureStoreOptions { Path = _path },
            NullLogger<FileFeatureStore>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }

    private JsonElement Feature() =>
        JsonDocument.Parse(File.ReadAllText(_path)).RootElement
            .GetProperty("Features").GetProperty("sample.feature");

    [Test(Description = "Sets the default value")]
    public async Task Should_SetDefault()
    {
        await _store.SetAsync("sample.feature", true);

        Feature().GetProperty("enabled").GetBoolean().ShouldBeTrue();
    }

    [Test(Description = "Sets a value for one environment, leaving the default alone")]
    public async Task Should_SetEnvironmentValue()
    {
        await _store.SetAsync("sample.feature", true, "Staging");

        var feature = Feature();
        feature.GetProperty("enabled").GetBoolean().ShouldBeFalse();
        feature.GetProperty("environments").GetProperty("Staging").GetBoolean().ShouldBeTrue();
    }

    /// <summary>
    /// Everything except the value is a decision someone recorded. A write that quietly dropped the
    /// owner or the description would turn the file into state instead of a record.
    /// </summary>
    [Test(Description = "Leaves the rest of the declaration untouched")]
    public async Task Should_PreserveMetadata()
    {
        await _store.SetAsync("sample.feature", true);

        var feature = Feature();
        feature.GetProperty("description").GetString().ShouldBe("kept as written");
        feature.GetProperty("owner").GetString().ShouldBe("someone");
    }

    [Test(Description = "Refuses a key nothing declares, naming why")]
    public async Task Should_Refuse_When_FeatureIsNotDeclared()
    {
        var act = async () => await _store.SetAsync("no.such.feature", true);

        (await act.ShouldThrowAsync<InvalidOperationException>())
            .Message.ShouldContain("no.such.feature");
    }

    [Test(Description = "Leaves no temporary file behind")]
    public async Task Should_NotLeaveTemporaryFiles()
    {
        await _store.SetAsync("sample.feature", true);

        File.Exists(_path + ".tmp").ShouldBeFalse();
    }
}
