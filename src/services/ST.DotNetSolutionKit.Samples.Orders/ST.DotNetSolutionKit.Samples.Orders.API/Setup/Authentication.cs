using ST.DotNetSolutionKit.Samples.Common.Web.Authentication;
using ST.DotNetSolutionKit.Samples.Orders.Infrastructure.Security.Handlers;

namespace ST.DotNetSolutionKit.Samples.Orders.API.Setup;

/// <summary>
/// Authentication and authorization for this service, by the shared composite scheme of
/// <see cref="ServiceAuthenticationSetup"/>, which says which request goes where. The API key handler lives
/// in this service's infrastructure. The middleware is added by the platform pipeline.
/// </summary>
internal static class Authentication
{
    public static WebApplicationBuilder SetupAppAuthentication(this WebApplicationBuilder builder)
    {
        builder.SetupServiceAuthentication<ApiKeyAuthenticationHandler>();
        builder.Services.AddAuthorization();
        return builder;
    }
}
