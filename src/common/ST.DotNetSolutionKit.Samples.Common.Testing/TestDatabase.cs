using Microsoft.EntityFrameworkCore;

namespace ST.DotNetSolutionKit.Samples.Common.Tests;

/// <summary>
/// The provider of the solution's database, for a context a unit test builds without connecting to it:
/// the model and the SQL it would send are the provider's own.
/// </summary>
public static class TestDatabase
{
    public static DbContextOptionsBuilder<TContext> UseSolutionDatabase<TContext>(
        this DbContextOptionsBuilder<TContext> builder, string connectionString)
        where TContext : DbContext
    {
        // One provider per generated solution; the template's own sources keep both, and PostgreSQL, the
        // default, comes first.
        return builder.UseNpgsql(connectionString);
    }
}
