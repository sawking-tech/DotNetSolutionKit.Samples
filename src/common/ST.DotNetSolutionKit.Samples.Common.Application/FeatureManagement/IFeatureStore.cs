using JetBrains.Annotations;

namespace ST.DotNetSolutionKit.Samples.Common.Application.FeatureManagement;

/// <summary>
/// Where a changed feature value is written.
/// </summary>
/// <remarks>
/// <para>
/// Reading flags is configuration's job and needs no abstraction — the layers do it. Writing does:
/// a platform with an external store should write there, one without should write to the shared
/// file, and the code asking for the change must not know which of the two it is talking to.
/// </para>
/// <para>
/// Whatever the implementation, the change reaches services the same way it always would — they
/// re-read configuration. Nothing here bypasses that, so no second source of truth appears, and a
/// value written while a store is unreachable fails loudly instead of diverging quietly.
/// </para>
/// </remarks>
[PublicAPI]
public interface IFeatureStore
{
    /// <summary>
    /// Sets a feature's value, either as the default or for one environment.
    /// </summary>
    /// <param name="key">Feature to change. Must already be declared.</param>
    /// <param name="enabled">The value to store.</param>
    /// <param name="environment">
    /// Environment the value applies to, or <c>null</c> to change the default that applies where no
    /// environment says otherwise.
    /// </param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <exception cref="InvalidOperationException">No feature is declared under that key.</exception>
    Task SetAsync(string key, bool enabled, string? environment = null, CancellationToken cancellationToken = default);
}
