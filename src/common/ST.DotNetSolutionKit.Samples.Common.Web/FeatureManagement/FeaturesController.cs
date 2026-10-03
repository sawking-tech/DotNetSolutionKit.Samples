using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ST.DotNetSolutionKit.Samples.Common.Application.FeatureManagement;
using ST.DotNetSolutionKit.Samples.Common.FeatureManagement;

namespace ST.DotNetSolutionKit.Samples.Common.Web.FeatureManagement;

/// <summary>
/// The platform's feature flags: what exists, what is on, and where each value came from.
/// </summary>
/// <remarks>
/// <para>
/// Shipped with the platform rather than written per service, so every service answers the same way.
/// A service opts in with <c>AddApplicationPart</c> on this assembly.
/// </para>
/// <para>
/// Reading is anonymous. Nothing here is secret — a key, a value and a sentence about what it turns
/// on — and a client needs the list before anyone has signed in, to decide what to render on the way
/// to the login screen. Writing is not: it changes platform behaviour and sits behind authorisation.
/// </para>
/// <para>
/// There is deliberately no per-key read. A client resolves flags synchronously while rendering, so
/// it holds the list it was given; a per-key endpoint would turn one page into a request per
/// conditional block. Inside a service the same question is a method on <c>IFeatureCatalog</c>.
/// </para>
/// <para>
/// What this endpoint is <em>not</em>: a means of enforcement. A client list decides what is drawn;
/// whether an operation is allowed is decided by the service performing it, which checks the flag
/// itself. Otherwise turning a feature off merely hides a button from whoever reloaded the page, and
/// leaves a stale mobile client untouched.
/// </para>
/// </remarks>
[ApiController]
[Route("api/v1/features")]
[Tags("Platform — Features")]
public sealed class FeaturesController(IFeatureCatalog catalog, IFeatureStore store) : ControllerBase
{
    /// <summary>Every declared feature with its current value, source and expiry.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FeaturesResponse), StatusCodes.Status200OK)]
    public ActionResult<FeaturesResponse> Get() =>
        Ok(new FeaturesResponse { Features = catalog.GetAll() });

    /// <summary>
    /// Sets a feature's value, either as the default or for one environment.
    /// </summary>
    /// <remarks>
    /// Writes to whichever store is configured. The change reaches services the way every other
    /// configuration change does — they re-read it — so nothing here is a second source of truth.
    /// </remarks>
    [HttpPut("{key}")]
    [Authorize]
    [ProducesResponseType(typeof(FeatureState), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeatureState>> Set(
        [FromRoute] string key,
        [FromBody] SetFeatureRequest request,
        CancellationToken cancellationToken)
    {
        if (catalog.Find(key) is null)
            return NotFound();

        await store.SetAsync(key, request.Enabled, request.Environment, cancellationToken);

        // Read back rather than echo: the caller sees what the platform now answers, including a
        // value still overridden by a later configuration layer.
        return catalog.Find(key) is { } state ? Ok(state) : NotFound();
    }
}

/// <summary>Body of <c>GET /api/v1/features</c>.</summary>
public sealed record FeaturesResponse
{
    /// <summary>
    /// Declared features, ordered by key. An empty list means this platform declares no optional
    /// features — not that the call failed.
    /// </summary>
    public required IReadOnlyList<FeatureState> Features { get; init; }
}

/// <summary>Body of <c>PUT /api/v1/features/{key}</c>.</summary>
public sealed record SetFeatureRequest
{
    /// <summary>The value to store.</summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Environment the value applies to. Omit to change the default that applies wherever no
    /// environment says otherwise.
    /// </summary>
    public string? Environment { get; init; }
}
