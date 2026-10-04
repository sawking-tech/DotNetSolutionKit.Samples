using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ST.DotNetSolutionKit.Samples.Common.Web.Gateway;

/// <summary>
/// The Swagger documents of the services behind the gateway, shown in the gateway's own Swagger page.
/// </summary>
/// <param name="Name">The cluster of the service, as the page lists it.</param>
/// <param name="Url">Where the gateway serves the service's document.</param>
public sealed record ServiceSwaggerDocument(string Name, string Url);

/// <summary>
/// Finds the services' documents in the gateway's routes.
/// </summary>
/// <remarks>
/// A service's document is reached through a route of its own in <c>ReverseProxy:Routes</c>:
/// <c>/swagger/&lt;cluster&gt;/{**rest}</c> to the cluster, with the path transformed to
/// <c>/swagger/{**rest}</c>. Each such route adds the service's <c>all</c> document to the page, so
/// "Try it out" sends the request to the gateway, which routes it like any other. The list is the
/// routes; there is no second setting to keep in step with them.
/// </remarks>
public static partial class GatewaySwagger
{
    [GeneratedRegex(@"^/swagger/(?<cluster>[^/{}]+)/\{\*\*rest\}$")]
    private static partial Regex SwaggerRoute();

    public static IReadOnlyList<ServiceSwaggerDocument> FromRoutes(IConfiguration reverseProxy) =>
        reverseProxy.GetSection("Routes").GetChildren()
            .Select(route => SwaggerRoute().Match(route["Match:Path"] ?? string.Empty))
            .Where(match => match.Success)
            .Select(match => match.Groups["cluster"].Value)
            .Distinct()
            .Select(cluster => new ServiceSwaggerDocument(cluster, $"/swagger/{cluster}/all/swagger.json"))
            .ToList();

    /// <summary>
    /// Lists the services' documents of <paramref name="reverseProxy"/> on the gateway's Swagger page.
    /// </summary>
    public static IServiceCollection AddGatewaySwagger(this IServiceCollection services, IConfiguration reverseProxy)
    {
        foreach (var document in FromRoutes(reverseProxy))
            services.AddSingleton(document);
        return services;
    }
}
