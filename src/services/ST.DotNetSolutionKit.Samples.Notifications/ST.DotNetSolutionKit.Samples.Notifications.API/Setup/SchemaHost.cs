using ST.DotNetSolutionKit.Samples.Common.Web.Setup;

namespace ST.DotNetSolutionKit.Samples.Notifications.API.Setup;

/// <summary>
/// Builds the application exactly as <c>Program</c> does, and stops there.
/// </summary>
/// <remarks>
/// The OpenAPI document lives in the built application's services, so anything that wants to read the
/// contract has to get that far - and no further. Going through a started service instead means a
/// port, a readiness wait and a process to kill, each of which can fail on its own; asking the
/// container directly cannot.
/// </remarks>
public static class SchemaHost
{
    /// <param name="args">Host arguments.</param>
    /// <param name="contentRootPath">Where the settings files live. Callers that do not run from the
    /// service's own directory - a test project, for one - have to say so.</param>
    public static WebApplication Build(string[] args, string? contentRootPath = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = contentRootPath,
            // Stated rather than inferred: MVC discovers controllers through the application name, and
            // a caller outside this service - a test runner - would otherwise hand it its own.
            ApplicationName = typeof(SchemaHost).Assembly.GetName().Name,
        });

        // --- Logging configuration ---
        builder.AddPlatformLogging();

        // --- DI validation configuration ---
        // In every environment: a missing registration or a scoped service resolved from the root then
        // fails the start, instead of the first request or background job that happens to need it.
        builder.Host.UseDefaultServiceProvider((_, options) =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });

        // --- Application configuration ---
        builder.Configuration.SetupAppConfiguration(builder.Environment, args);

        // --- Web layer shared by every service (Common.Web), with this service's own MVC additions ---
        builder.AddPlatformWebApi(typeof(SchemaHost).Assembly, mvc =>
        {
        });

        // --- This service's own registrations ---
        builder.SetupAppServices()
            .SetupHealthChecks();

        builder.SetupAppAuthentication();

        return builder.Build();
    }
}
