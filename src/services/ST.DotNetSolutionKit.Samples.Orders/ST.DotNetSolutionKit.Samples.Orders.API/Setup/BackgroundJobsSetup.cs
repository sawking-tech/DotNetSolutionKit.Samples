using Hangfire;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Configuration;
using ST.DotNetSolutionKit.Samples.Orders.Infrastructure.Security.Filters;

namespace ST.DotNetSolutionKit.Samples.Orders.API.Setup;

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
            DashboardTitle = "ST Orders Jobs",
            AppPath = "/swagger"
        });

        return app;
    }
}