using ST.DotNetSolutionKit.Samples.Common.Web.Authentication;
using ST.DotNetSolutionKit.Samples.Billing.Infrastructure.Security.Handlers;

namespace ST.DotNetSolutionKit.Samples.Billing.API.Setup;

/// <summary>
/// Authentication and authorization for this service: the API key forwarded by the gateway, with JWT as
/// an optional fallback when a "Jwt" section is configured. The handler lives in this service's
/// infrastructure; the shared setup is <see cref="ServiceAuthenticationSetup"/>. The middleware is added
/// by the platform pipeline.
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
