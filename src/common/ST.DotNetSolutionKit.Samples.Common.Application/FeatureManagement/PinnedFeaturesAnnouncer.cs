using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ST.DotNetSolutionKit.Samples.Common.FeatureManagement;

namespace ST.DotNetSolutionKit.Samples.Common.Application.FeatureManagement;

/// <summary>
/// Names the pinned flags once, at startup.
/// </summary>
/// <remarks>
/// A pin is meant to be temporary: set during an outage of the store, it keeps overriding the store after
/// the outage is over, and a value changed there then does nothing. A warning in every start's log is what
/// keeps a forgotten pin from going unnoticed.
/// </remarks>
public sealed class PinnedFeaturesAnnouncer(IFeatureCatalog catalog, ILogger<PinnedFeaturesAnnouncer> logger)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var pinned = catalog.GetAll()
            .Where(state => state.Source == FeatureValueSource.Pinned)
            .Select(state => $"{state.Key}={state.Enabled}")
            .ToList();

        if (pinned.Count > 0)
            logger.LogWarning(
                "Pinned by features.json, over every other layer: {PinnedFeatures}. Unpin them once the store is to decide again.",
                string.Join(", ", pinned));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
