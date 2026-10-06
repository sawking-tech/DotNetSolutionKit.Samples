using Hangfire;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Configuration;
using ST.DotNetSolutionKit.Samples.Catalog.Infrastructure.Security.Filters;

namespace ST.DotNetSolutionKit.Samples.Catalog.API.Setup;

/// <summary>
/// Extensions for configuring Hangfire background jobs and dashboard.
/// </summary>
internal static class BackgroundJobsSetup
{
    /// <summary>
    /// Configures Hangfire dashboard with administrative authorization.
    /// </summary>
    /// <param name="app">Application builder.</param>
    /// <returns>Updated application builder.</returns>
    internal static WebApplication UseAppHangfire(this WebApplication app)
    {
        var settings = app.Services.GetRequiredService<IHangfireSettings>();

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new HangfireAdminAuthorizationFilter(settings)],
            DashboardTitle = "ST Catalog Jobs",
            AppPath = "/swagger"
        });

        return app;
    }
}