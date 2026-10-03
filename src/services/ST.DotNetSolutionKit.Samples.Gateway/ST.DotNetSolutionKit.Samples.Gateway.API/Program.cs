using System.Reflection;
using ST.DotNetSolutionKit.Samples.Common.Application.Configuration;
using ST.DotNetSolutionKit.Samples.Common.Web.Authentication;
using ST.DotNetSolutionKit.Samples.Common.Web.Gateway;
using ST.DotNetSolutionKit.Samples.Common.Web.Setup;
using Serilog;
using Yarp.ReverseProxy.Transforms;

// The gateway: the one public entry point. It validates the caller's token, routes the request to a
// service (ReverseProxy in appsettings.json) and tells the service who the caller is - headers the
// service trusts because the internal API key comes with them.
try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddPlatformLogging();
    builder.Host.UseDefaultServiceProvider((_, options) =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });

    // Errors, JSON, CORS, Swagger and health, the same as every service (Common.Web).
    builder.AddPlatformWebApi(typeof(Program).Assembly);

    builder.Services.AddInternalApiConfiguration(builder.Configuration);
    builder.SetupGatewayAuthentication();
    builder.Services.AddAuthorization();

    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
        .AddTransforms(context => context.AddRequestTransform(transform =>
        {
            var internalApiKey = transform.HttpContext.RequestServices
                .GetRequiredService<IInternalApiConfiguration>().ApiKey;
            GatewayForwarding.ForwardUser(transform.ProxyRequest.Headers, transform.HttpContext.User, internalApiKey);
            return ValueTask.CompletedTask;
        }));

    var app = builder.Build();

    // A token that failed validation stops here; no token goes on, and the service decides whether
    // its endpoint is public.
    app.UsePlatformPipeline(typeof(Program).Assembly, beforeEndpoints: pipeline => pipeline.UseRejectInvalidCredentials());
    app.MapReverseProxy();

    app.Run();
}
catch (HostAbortedException ex)
{
    Log.Warning(ex, "Host was aborted. This may be expected in some environments.");
}
catch (Exception ex) when (ex.GetType().Name == "StopTheHostException")
{
    throw;
}
catch (Exception ex)
{
    Log.Fatal(ex, "The {EntryAssemblyName} application startup failed", Assembly.GetEntryAssembly()?.GetName().Name);
    Console.Error.WriteLine(ex);
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}
