using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.DependencyInjection;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Persistence;

namespace ST.DotNetSolutionKit.Samples.Capabilities.Background.Hangfire;

// Hangfire, with its storage in the service's own schema of the solution's database. Hangfire keeps a
// storage package per database, so this wiring and the database provider switch in Common are the two
// places that know which database the solution uses.
public static class DependencyInjection
{
    /// <summary>Register Hangfire, its storage in the service's schema, and the job filters.</summary>
    /// <param name="serviceName">The service that owns the schema, named in the guard's message.</param>
    /// <param name="schemaName">The schema of the job storage, the service's own name and a suffix.</param>
    public static IServiceCollection AddHangfireJobs(
        this IServiceCollection services,
        string connectionString,
        string serviceName,
        string schemaName)
    {
        // Guard for Hangfire Schema
        DatabaseProvider.EnsureExclusiveSchema(connectionString, schemaName, serviceName);

        services.AddHangfire(config =>
        {
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();
            config.UsePostgreSqlStorage(c =>
                c.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
            {
                SchemaName = schemaName,
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
        services.AddSingleton<JobActivator>(sp =>
            new DomainEventJobActivator(sp.GetRequiredService<IServiceScopeFactory>()));
        services.AddSingleton<JobActorPropagationFilter>();
        services.AddHostedService(sp => new HangfireFilterInstaller(sp));

        return services;
    }
}
