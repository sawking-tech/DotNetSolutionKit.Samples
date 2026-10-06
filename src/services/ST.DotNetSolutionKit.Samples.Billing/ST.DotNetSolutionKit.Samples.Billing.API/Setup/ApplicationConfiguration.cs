using ST.DotNetSolutionKit.Samples.Common.Application.Configuration;
using ST.DotNetSolutionKit.Samples.Common.Application.FeatureManagement;
namespace ST.DotNetSolutionKit.Samples.Billing.API.Setup;

internal static class ApplicationConfiguration
{
    public static IConfigurationBuilder SetupAppConfiguration(
        this IConfigurationBuilder builder, IHostEnvironment env, string[] args)
    {
        // Platform feature flags, shipped from Common so every service reads the same file and a
        // feature means one thing across the platform. First, so everything below can override it.
        builder.AddPlatformFeatures();

        // Always base config
        builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        // Environment-specific configuration
        if (!string.IsNullOrWhiteSpace(env.EnvironmentName))
        {
            var envConfigFile = $"appsettings.{env.EnvironmentName}.json";
            builder.AddJsonFile(envConfigFile, optional: true, reloadOnChange: true);
        }

        // Secret files only for Local
        if (env.IsEnvironment("Local"))
        {
            builder.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);
        }

        // Always connect environment variables (secrets, dynamic parameters)
        builder.AddEnvironmentVariables();

        // With the database switched off, what is stored in it goes off too: the job server, and the bus
        // when it delivers through the outbox.
        builder.AddDependencyOverrides(busNeedsDatabase: true);

        return builder;
    }
}