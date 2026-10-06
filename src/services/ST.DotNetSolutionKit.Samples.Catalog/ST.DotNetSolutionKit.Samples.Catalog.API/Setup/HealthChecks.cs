using ST.DotNetSolutionKit.Samples.Common.Application.Configuration;
using ST.DotNetSolutionKit.Samples.Common.Contracts.Health;
using ST.DotNetSolutionKit.Samples.Common.Web.Health;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Persistence;
using ST.DotNetSolutionKit.Samples.Catalog.Infrastructure.EntityFramework;

namespace ST.DotNetSolutionKit.Samples.Catalog.API.Setup;

internal static class HealthChecks
{
    /// <summary>
    /// Registers the dependencies <c>/ready</c> checks. Another dependency joins readiness by being
    /// registered here with <see cref="HealthConstants.ReadyTag"/>. A switched-off dependency is not
    /// checked: the service is ready without it.
    /// </summary>
    public static WebApplicationBuilder SetupHealthChecks(this WebApplicationBuilder builder)
    {
        var switches = DependencySwitches.Read(builder.Configuration);
        var checks = builder.Services.AddHealthChecks();

        if (switches.Jobs)
            checks.AddHangfire(options => options.MinimumAvailableServers = 1, name: "hangfire", tags: [HealthConstants.ReadyTag]);

        if (switches.Database)
            checks.AddDbContextCheck<CatalogDbContext>(name: DatabaseProvider.Name, tags: [HealthConstants.ReadyTag]);

        return builder;
    }

    public static WebApplication MapHealthEndpoints(this WebApplication app) => app.MapPlatformHealth();
}
