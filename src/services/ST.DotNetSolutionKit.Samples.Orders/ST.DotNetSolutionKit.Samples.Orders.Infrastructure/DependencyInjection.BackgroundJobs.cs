using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.DependencyInjection;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Configuration;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Persistence;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Persistence.EntityFramework.Events;
using ST.DotNetSolutionKit.Samples.Orders.Infrastructure.EntityFramework;

namespace ST.DotNetSolutionKit.Samples.Orders.Infrastructure;

// Hangfire, with its storage in the service's own schema of the solution's database. Hangfire keeps a
// storage package per database, so this part of the wiring is the one place of the service, beside
// DatabaseProvider, that knows which database it is.
public static partial class DependencyInjection
{
    /// <summary>
    /// Register Hangfire and background job services.
    /// </summary>
    private static IServiceCollection AddBackgroundJobs(this IServiceCollection services, string connectionString)
    {
        var serviceName = typeof(DomainMarker).Namespace!;
        var hangfireSchemaName = $"{OrdersDbContext.DefaultSchemaName}_hangfire";

        // Guard for Hangfire Schema
        DatabaseProvider.EnsureExclusiveSchema(connectionString, hangfireSchemaName, serviceName);

        services.AddHangfire(config =>
        {
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();
            config.UsePostgreSqlStorage(c =>
                c.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
            {
                SchemaName = hangfireSchemaName,
                PrepareSchemaIfNecessary = true
            });
        });

        services.AddHangfireServer((sp, options) =>
        {
            var settings = sp.GetRequiredService<IHangfireSettings>();
            options.WorkerCount = settings.WorkerCount;
        });

        // Jobs run in a scope the domain event interceptors can see, so events a job raises are not
        // dropped; the filter carries the person who enqueued a job into it, for attribution.
        services.AddDomainEventJobActivator();
        services.AddHostedService(sp => new HangfireFilterInstaller(sp));

        return services;
    }
}
