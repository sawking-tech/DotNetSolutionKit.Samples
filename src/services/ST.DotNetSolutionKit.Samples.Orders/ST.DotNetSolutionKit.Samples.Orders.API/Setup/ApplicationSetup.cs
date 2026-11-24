using ST.DotNetSolutionKit.Samples.Common.Application.Configuration;
using ST.DotNetSolutionKit.Samples.Orders.Application;
using ST.DotNetSolutionKit.Samples.Orders.Infrastructure;

namespace ST.DotNetSolutionKit.Samples.Orders.API.Setup;

internal static class ApplicationSetup
{
    public static WebApplicationBuilder SetupAppServices(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var services = builder.Services;
        
        // services.AddJwtConfiguration(configuration);
        services.AddInternalApiConfiguration(configuration);
        services.AddAuthValidationConfiguration(configuration);

        // Register Application Layer services. They register the domain services too:
        // the API reaches the domain only through the application layer.
        services.AddApplicationServices();
        
        // Register Infrastructure Layer services
        services.AddInfrastructureServices(builder.Configuration);

        return builder;
    }
}