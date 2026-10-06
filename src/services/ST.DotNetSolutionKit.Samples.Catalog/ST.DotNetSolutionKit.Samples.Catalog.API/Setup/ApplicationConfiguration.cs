using ST.DotNetSolutionKit.Samples.Common.Application.Configuration;
namespace ST.DotNetSolutionKit.Samples.Catalog.API.Setup;

internal static class ApplicationConfiguration
{
    public static IConfigurationBuilder SetupAppConfiguration(
        this IConfigurationBuilder builder, IHostEnvironment env, string[] args)
    {
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
        builder.AddDependencyOverrides(busNeedsDatabase: false);

        return builder;
    }
}