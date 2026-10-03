using System.Reflection;
using ST.DotNetSolutionKit.Samples.Common.Application.Configuration;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Persistence.EntityFramework;
using ST.DotNetSolutionKit.Samples.Common.Web.Setup;
using ST.DotNetSolutionKit.Samples.Billing.API.Setup;
using ST.DotNetSolutionKit.Samples.Billing.Infrastructure.EntityFramework;
using ST.DotNetSolutionKit.Samples.Billing.Infrastructure.EntityFramework.DataSeeding;
using Serilog;

try
{
    // Services and configuration live in SchemaHost, which the schema generator builds without
    // starting: one description of the application, whether it is about to serve traffic or only to
    // say what its API looks like.
    var app = SchemaHost.Build(args);

    // Each dependency can be switched off in configuration; what is off is not registered, not
    // validated and not checked for readiness, and the log says so once, here.
    var switches = DependencySwitches.Read(app.Configuration);
    if (switches.SwitchedOff.Count > 0)
        app.Logger.LogWarning("Running without: {SwitchedOff}", string.Join(", ", switches.SwitchedOff.Select(key => $"{key}=false")));

    // --- Database Initialization ---
    if (switches.Database)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<BillingDbContext>();
        var logger = services.GetRequiredService<ILogger<MigrationRunner>>();

        // Run Migrations
        MigrationRunner.RunMigrations(dbContext, logger);

        // Data Seeding
        try
        {
            var dataSeeder = services.GetRequiredService<DataSeeder>();
            await dataSeeder.SeedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    // --- Middleware and endpoints, in the order the platform relies on (Common.Web) ---
    app.UsePlatformPipeline(typeof(Program).Assembly, beforeEndpoints: pipeline =>
    {
        if (switches.Jobs)
            pipeline.UseAppHangfire();
    });

    // --- Application startup ---
    app.Run();
}
catch (HostAbortedException ex)
{
    Log.Warning(ex, "Host was aborted. This may be expected in some environments.");
}
// Build-time tools such as dotnet-getdocument start this entry point and stop it with an internal
// exception once the host is built. Swallowing it below would make the run look like a clean exit,
// and the tool would report that no host was built.
catch (Exception ex) when (ex.GetType().Name == "StopTheHostException")
{
    throw;
}
catch (Exception ex)
{
    Log.Fatal(ex, "The {EntryAssemblyName} application startup failed", Assembly.GetEntryAssembly()?.GetName().Name);
    // A failure before logging is configured leaves Serilog silent, so the error also goes to stderr.
    // A non-zero exit code tells the orchestrator the service did not start.
    Console.Error.WriteLine(ex);
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}