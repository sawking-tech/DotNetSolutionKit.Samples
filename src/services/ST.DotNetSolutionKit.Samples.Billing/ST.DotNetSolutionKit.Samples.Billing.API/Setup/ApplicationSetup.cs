using ST.DotNetSolutionKit.Samples.Common.Application.FeatureManagement;
using ST.DotNetSolutionKit.Samples.Common.Application.Configuration;
using ST.DotNetSolutionKit.Samples.Billing.Application;
using ST.DotNetSolutionKit.Samples.Billing.Infrastructure;

namespace ST.DotNetSolutionKit.Samples.Billing.API.Setup;

internal static class ApplicationSetup
{
    public static WebApplicationBuilder SetupAppServices(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var services = builder.Services;
        
        // Platform feature flags: the catalogue, the store, and the library that evaluates them.
        // Flags come from the shared features.json, so nothing is declared here: a service gains a
        // new flag without a line of code, and adds a constant to FeatureKeys only for the ones its
        // own code reads.
        services.AddPlatformFeatureManagement();

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