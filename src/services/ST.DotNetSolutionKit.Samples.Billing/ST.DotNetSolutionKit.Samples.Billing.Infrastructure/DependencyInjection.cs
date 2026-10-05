using System.Diagnostics.CodeAnalysis;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ST.DotNetSolutionKit.Samples.Common.Application.Configuration;
using ST.DotNetSolutionKit.Samples.Common.Application.Persistence;
using ST.DotNetSolutionKit.Samples.Common.Domain.Persistence;
using ST.DotNetSolutionKit.Samples.Common.Domain.Specifications;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Configuration;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Messaging;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Persistence.EntityFramework;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Persistence.EntityFramework.Audit;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Persistence.EntityFramework.Events;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Persistence.SqlServer;
using ST.DotNetSolutionKit.Samples.Billing.Application;
using ST.DotNetSolutionKit.Samples.Billing.Infrastructure.EntityFramework;
using ST.DotNetSolutionKit.Samples.Billing.Infrastructure.EntityFramework.DataSeeding;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Persistence.EntityFramework.Specifications;

namespace ST.DotNetSolutionKit.Samples.Billing.Infrastructure;

/// <summary>
/// Extensions for registering infrastructure services in DI container.
/// </summary>
[SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local")]
public static class DependencyInjection
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
            SqlServerSchemaGuard.EnsureExclusiveSchema(connectionString, BillingDbContext.DefaultSchemaName, serviceName);
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
            options.UseSqlServer(connectionString,
                x => { x.MigrationsHistoryTable("__EFMigrationsHistory", BillingDbContext.DefaultSchemaName); });
            if (!switches.Database)
                options.UseSwitchedOffDatabase();
            options.ApplyDomainEventInterceptors(sp);
            options.ApplyAuditInterceptor(sp);
        });

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<BillingDbContext>());

        // The bus, with the outbox in this service's schema: a message and the change that caused it
        // commit together. Consumers are found in this assembly.
        services.AddMessaging<BillingDbContext>(
            configuration, "billing", typeof(InfrastructureMarker).Assembly);

        // The audit journal: a change to an entity marked [Auditable] publishes AuditRecordedV1 into the
        // outbox in the same save, so a rolled back change leaves no entry. IAuditRecorder and
        // ISetBasedAuditCapture record what bypasses the change tracker; see docs/features/audit.md.
        services.AddAuditPersistence();
        services.AddAuditRecorder<BillingDbContext>("billing");

        // Repositories
        
        // Specifications
        services.AddScoped<ICaseInsensitiveSearch, SqlServerCaseInsensitiveSearch>();

        // Readable numbers from a sequence, taken before the entity is created
        services.AddScoped<IShortIdGenerator, SqlServerShortIdGenerator<BillingDbContext>>();
        
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

    // "Hangfire" in ASCII: one key for every service of the database.
    private const long HangfireInstallLockKey = 0x48616E6766697265;

    /// <summary>
    /// Register Hangfire and background job services.
    /// </summary>
    private static IServiceCollection AddBackgroundJobs(this IServiceCollection services, string connectionString)
    {
        var serviceName = typeof(DomainMarker).Namespace!;
        var hangfireSchemaName = $"{BillingDbContext.DefaultSchemaName}_hangfire";

        // Guard for Hangfire Schema
        SqlServerSchemaGuard.EnsureExclusiveSchema(connectionString, hangfireSchemaName, serviceName);

        // Hangfire.SqlServer installs its tables in a transaction that deadlocks with another service
        // installing into the same database at the same moment, and gives up after three attempts, leaving
        // the service without a job server. The installs take turns under one lock for the whole database.
        var installLock = new SqlServerMigrationLock();
        using (var connection = installLock.Connect(connectionString))
        {
            installLock.Acquire(connection, HangfireInstallLockKey);
            SqlServerObjectsInstaller.Install(connection, hangfireSchemaName);
            installLock.Release(connection, HangfireInstallLockKey);
        }

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                SchemaName = hangfireSchemaName,
                // Installed above, under the lock.
                PrepareSchemaIfNecessary = false
            }));
    
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
