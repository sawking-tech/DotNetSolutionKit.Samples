using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ST.DotNetSolutionKit.Samples.Common.Web.Setup;

/// <summary>
/// The scheme and the client address as the proxy in front saw them, from <c>X-Forwarded-Proto</c> and
/// <c>X-Forwarded-For</c>.
/// </summary>
/// <remarks>
/// Behind a TLS proxy or a gateway a service sees plain HTTP from the proxy's address. Without this the
/// access token cookie would be written without <c>Secure</c>, redirects would point at http, and every
/// log line and rate limit would carry the proxy's address instead of the client's.
///
/// By default the headers are accepted from any address: in containers the proxy's address changes, and
/// the deployment files publish a service on the host's loopback only, so nothing but the proxy reaches
/// it. A service reachable from elsewhere lists its proxies in <c>ForwardedHeaders:KnownProxies</c> or
/// <c>ForwardedHeaders:KnownNetworks</c> (CIDR), and the headers from any other address are ignored.
/// <c>ForwardedHeaders:ForwardLimit</c> is the number of proxies in the chain, 1 by default.
/// </remarks>
public static class PlatformForwardedHeaders
{
    public const string SectionName = "ForwardedHeaders";

    public static IServiceCollection AddPlatformForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var proxies = section.GetSection("KnownProxies").Get<string[]>() ?? [];
        var networks = section.GetSection("KnownNetworks").Get<string[]>() ?? [];

        return services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = section.GetValue("ForwardLimit", 1);

            // The defaults trust only loopback, which in a container is never the proxy.
            options.KnownProxies.Clear();
            options.KnownNetworks.Clear();
            foreach (var proxy in proxies)
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            foreach (var network in networks)
            {
                var parts = network.Split('/');
                options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse(parts[0]), int.Parse(parts[1])));
            }
        });
    }

    public static WebApplication UsePlatformForwardedHeaders(this WebApplication app)
    {
        app.UseForwardedHeaders();
        return app;
    }
}
