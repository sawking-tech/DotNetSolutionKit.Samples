using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using ST.DotNetSolutionKit.Samples.Common.Web.Setup;

namespace ST.DotNetSolutionKit.Samples.Common.Tests.Tests.Web;

/// <summary>
/// The scheme and the client address behind a proxy: taken from the forwarded headers, and only from
/// the listed proxies once a list is configured.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
internal class PlatformForwardedHeadersTests
{
    private static async Task<string> Send(string connectedFrom, params (string Key, string Value)[] settings)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(settings.ToDictionary(s => s.Key, s => (string?)s.Value));
        builder.Services.AddPlatformForwardedHeaders(builder.Configuration);
        await using var app = builder.Build();

        // The address the request arrives from, as a proxy's would be.
        app.Use((context, next) =>
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse(connectedFrom);
            return next(context);
        });
        app.UsePlatformForwardedHeaders();
        app.Run(context => context.Response.WriteAsync($"{context.Request.Scheme} {context.Connection.RemoteIpAddress}"));
        await app.StartAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Forwarded-Proto", "https");
        request.Headers.Add("X-Forwarded-For", "203.0.113.7");
        return await (await app.GetTestClient().SendAsync(request)).Content.ReadAsStringAsync();
    }

    [Test(Description = "By default the scheme and the client address come from the proxy's headers")]
    public async Task Should_UseTheForwardedValues_ByDefault() =>
        (await Send("172.18.0.5")).ShouldBe("https 203.0.113.7");

    [Test(Description = "A listed proxy is trusted")]
    public async Task Should_UseTheForwardedValues_When_TheProxyIsListed() =>
        (await Send("10.0.0.2", ("ForwardedHeaders:KnownProxies:0", "10.0.0.2"))).ShouldBe("https 203.0.113.7");

    [Test(Description = "A proxy inside a listed network is trusted")]
    public async Task Should_UseTheForwardedValues_When_TheProxyIsInAListedNetwork() =>
        (await Send("10.1.2.3", ("ForwardedHeaders:KnownNetworks:0", "10.1.0.0/16"))).ShouldBe("https 203.0.113.7");

    [Test(Description = "Once proxies are listed, headers from any other address are ignored")]
    public async Task Should_IgnoreTheHeaders_When_TheSenderIsNotAListedProxy() =>
        (await Send("198.51.100.9", ("ForwardedHeaders:KnownProxies:0", "10.0.0.2"))).ShouldBe("http 198.51.100.9");
}
