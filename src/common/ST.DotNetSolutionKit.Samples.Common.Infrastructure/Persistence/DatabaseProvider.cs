using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ST.DotNetSolutionKit.Samples.Common.Application.Persistence;
using ST.DotNetSolutionKit.Samples.Common.Domain.Specifications;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Diagnostics;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Persistence.EntityFramework.Specifications;
using Npgsql;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Persistence.Postgres;

namespace ST.DotNetSolutionKit.Samples.Common.Infrastructure.Persistence;

/// <summary>
/// The database of the solution, PostgreSQL or SQL Server: the one place that knows which. A service and
/// <c>Common</c> ask it instead of naming a provider, so the choice made at generation, or a provider
/// added later, touches this file.
/// </summary>
/// <remarks>
/// A generated solution keeps one provider. The template's own sources keep both, PostgreSQL, the
/// default, first.
/// </remarks>
public static class DatabaseProvider
{
    /// <summary>The provider's name, as a health check and a log call it.</summary>
    public static string Name
    {
        get
        {
            return "postgres";
        }
    }

    /// <summary>True when the solution's database is SQL Server.</summary>
    public static bool IsSqlServer
    {
        get
        {
            return false;
        }
    }

    /// <summary>
    /// A connection string that names no real server: enough for a design-time context, which adding a
    /// migration builds without connecting.
    /// </summary>
    public static string DesignTimeConnectionString
    {
        get
        {
            return "Host=localhost;Database=design-time-placeholder";
        }
    }

    /// <summary>
    /// Points <paramref name="options"/> at the solution's database. With <paramref name="schema"/>, EF's
    /// migrations history lives in that schema, next to the tables it describes.
    /// </summary>
    public static DbContextOptionsBuilder UseDatabase(
        this DbContextOptionsBuilder options, string connectionString, string? schema = null)
    {
        return options.UseNpgsql(connectionString, x =>
        {
            if (schema is not null)
                x.MigrationsHistoryTable("__EFMigrationsHistory", schema);
        });
    }

    /// <summary>The server, database and user a connection string names, without its credentials.</summary>
    public static string DescribeTarget(string connectionString)
    {
        var target = new NpgsqlConnectionStringBuilder(connectionString);
        return $"{target.Host}:{target.Port}/{target.Database} as {target.Username ?? "(no user)"}";
    }

    /// <summary>
    /// Ensures that <paramref name="schema"/> is either empty or owned by <paramref name="serviceName"/>,
    /// so two services never share one; throws otherwise.
    /// </summary>
    public static void EnsureExclusiveSchema(string connectionString, string schema, string serviceName)
    {
        PostgresSchemaGuard.EnsureExclusiveSchema(connectionString, schema, serviceName);
    }

    /// <summary>
    /// Registers what a service's queries need from its database: case-insensitive search, and readable
    /// numbers from a sequence of <typeparamref name="TContext"/>.
    /// </summary>
    public static IServiceCollection AddDatabaseQueries<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<ICaseInsensitiveSearch, PostgresCaseInsensitiveSearch>();
        services.AddScoped<IShortIdGenerator, PostgresShortIdGenerator<TContext>>();
        return services;
    }

    /// <summary>True when a failed save broke a unique index.</summary>
    public static bool IsUniqueViolation(DbUpdateException exception)
    {
        return PostgresErrors.IsUniqueViolation(exception);
    }

    /// <summary>The lock statements the MassTransit outbox runs against the solution's database.</summary>
    public static IEntityFrameworkOutboxConfigurator UseDatabaseLocks(this IEntityFrameworkOutboxConfigurator outbox)
    {
        outbox.UsePostgres();
        return outbox;
    }

    /// <summary>How the outbox statistics are read from the solution's database.</summary>
    internal static IOutboxStatsDialect CreateOutboxStatsDialect()
    {
        return new PostgresOutboxStatsDialect();
    }

    /// <summary>The lock migrations take turns under, one at a time across every instance.</summary>
    public static IMigrationLock CreateMigrationLock()
    {
        return new PostgresMigrationLock();
    }
}
