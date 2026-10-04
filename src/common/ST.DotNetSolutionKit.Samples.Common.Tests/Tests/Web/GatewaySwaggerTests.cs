using Microsoft.Extensions.Configuration;
using ST.DotNetSolutionKit.Samples.Common.Web.Gateway;

namespace ST.DotNetSolutionKit.Samples.Common.Tests.Tests.Web;

/// <summary>
/// The services' Swagger documents the gateway lists come from its /swagger/&lt;cluster&gt;/ routes.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
internal class GatewaySwaggerTests
{
    private static IConfiguration ReverseProxy(params (string Route, string Path)[] routes) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(routes.Select(r => new KeyValuePair<string, string?>($"Routes:{r.Route}:Match:Path", r.Path)))
            .Build();

    [Test]
    public void Each_swagger_route_lists_its_service()
    {
        var documents = GatewaySwagger.FromRoutes(ReverseProxy(
            ("orders", "/api/{version}/orders/{**rest}"),
            ("orders-swagger", "/swagger/orders/{**rest}"),
            ("billing-swagger", "/swagger/billing/{**rest}")));

        documents.ShouldBe([
            new ServiceSwaggerDocument("orders", "/swagger/orders/all/swagger.json"),
            new ServiceSwaggerDocument("billing", "/swagger/billing/all/swagger.json")
        ], ignoreOrder: true);
    }

    [Test]
    public void A_gateway_without_swagger_routes_lists_nothing()
    {
        GatewaySwagger.FromRoutes(ReverseProxy(("orders", "/api/{version}/orders/{**rest}"))).ShouldBeEmpty();
    }
}
