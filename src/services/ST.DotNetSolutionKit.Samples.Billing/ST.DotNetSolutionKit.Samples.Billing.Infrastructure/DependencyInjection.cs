using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ST.DotNetSolutionKit.Samples.Common.Application.Configuration;
using ST.DotNetSolutionKit.Samples.Common.Domain.Persistence;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Configuration;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Messaging;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Persistence.EntityFramework;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Persistence.EntityFramework.Events;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Persistence;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.ClickHouse;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Mongo;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Storage;
using ST.DotNetSolutionKit.Samples.Billing.Application;
using ST.DotNetSolutionKit.Samples.Billing.Infrastructure.EntityFramework;
using ST.DotNetSolutionKit.Samples.Billing.Infrastructure.EntityFramework.DataSeeding;

namespace ST.DotNetSolutionKit.Samples.Billing.Infrastructure;

/// <summary>
/// Extensions for registering infrastructure services in DI container.
/// </summary>
[SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local")]
public static partial class DependencyInjection
{
    /// <summary>
    /// Register infrastructure services.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        // What this run uses: Database, HangfireSettings and RabbitMq each have an Enabled switch.
        var switches = DependencySwitches.Read(configuration);

        // Database
        string connectionString;
        if (switches.Database)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' not found. Set ConnectionStrings__DefaultConnection, " +
                    $"or run without a database: {DependencySwitches.DatabaseKey}=false.");

            // Unique name for this microservice (used for schema ownership)
            var serviceName = typeof(DomainMarker).Namespace!;

            // 1. Guard for Main Database Schema
            DatabaseProvider.EnsureExclusiveSchema(connectionString, BillingDbContext.DefaultSchemaName, serviceName);
        }
        else
        {
            // The context stays registered so everything built on it still resolves; opening a
            // connection answers 503 instead.
            connectionString = SwitchedOffDatabase.ConnectionString;
        }

        // Domain events: handlers from the application layer, run in three phases around SaveChanges and
        // the transaction by the interceptors attached to the context below.
        services.AddDomainEvents(typeof(ApplicationMarker).Assembly);

        // AddDbContext, not the pool: interceptors resolved per scope (the domain events', the outbox's)
        // are not re-attached to a context handed back by the pool, and they would quietly do nothing.
        services.AddDbContext<BillingDbContext>((sp, options) =>
        {
            options.UseDatabase(connectionString, BillingDbContext.DefaultSchemaName);
            if (!switches.Database)
                options.UseSwitchedOffDatabase();
            options.ApplyDomainEventInterceptors(sp);
        });

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<BillingDbContext>());

        // The bus, with the outbox in this service's schema: a message and the change that caused it
        // commit together. Consumers are found in this assembly.
        services.AddMessaging<BillingDbContext>(
            configuration, "billing", typeof(InfrastructureMarker).Assembly);

        // Repositories
        
        // Specifications: case-insensitive search; readable numbers from a sequence, taken before the
        // entity is created
        services.AddDatabaseQueries<BillingDbContext>();
        
        // ClickHouse, the ClickHouse section: connections, the schema check, readiness
        services.AddClickHouse(configuration);

        // MongoDB, the MongoDB section: the client, the service's database, readiness
        services.AddMongoDB(configuration);

        // Object storage, the S3 section; S3:Enabled=false keeps nothing
        services.AddS3ObjectStorage(configuration);

        // Configurations
        services.AddInfrastructureConfiguration();
        
        // Background jobs, with their settings validated only when the job server runs. Off together
        // with the database: the database is their storage.
        if (switches.Jobs)
        {
            services.AddValidatedOptions<IHangfireSettings, HangfireSettings>(HangfireSettings.SectionName);
            services.AddBackgroundJobs(connectionString);
        }

        // Data Seeding
        services.AddScoped<DataSeeder>();

        return services;
    }

    /// <summary>
    /// Register and validate infrastructure configuration settings.
    /// </summary>
    /// <param name="services">Service collection.</param>
    private static void AddInfrastructureConfiguration(this IServiceCollection services)
    {
        services.AddValidatedOptions<ICorsSettings, CorsSettings>(CorsSettings.SectionName);
    }
    
    /// <summary>
    /// Helper method to register validated options with interface.
    /// </summary>
    private static IServiceCollection AddValidatedOptions<TInterface, TSettings>(
        this IServiceCollection services, 
        string sectionName)
        where TInterface : class
        where TSettings : class, TInterface, new()
    {
        services.AddOptions<TSettings>()
            .BindConfiguration(sectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    
        services.AddSingleton<TInterface>(sp => 
            sp.GetRequiredService<IOptions<TSettings>>().Value);

        return services;
    }
}
