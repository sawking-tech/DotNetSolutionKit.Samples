using Microsoft.Extensions.DependencyInjection;
using ST.DotNetSolutionKit.Samples.Common.Tests;
using ST.DotNetSolutionKit.Samples.Common.Tests.Integration;
using ST.DotNetSolutionKit.Samples.Orders.Infrastructure.EntityFramework;

namespace ST.DotNetSolutionKit.Samples.Orders.Tests.Integration;

/// <summary>
/// A test on this service's real SQL Server schema: a database of its own, restored from one migrated
/// once per test run, and dropped after the test. For what the in-memory provider cannot show:
/// transactions, constraints, raw SQL, the migrations themselves. Needs <c>TEST_SQLSERVER</c>; the test is
/// skipped without it.
/// </summary>
/// <example>
/// <code>
/// [Fact, Trait(TestCategories.TraitName, TestCategories.Integration)]
/// public async Task Should_RejectADuplicateNumber()
/// {
///     await using var ctx = await SqlServerDbTestExecutionContext&lt;OrderService&gt;.CreateAsync();
///     ...
/// }
/// </code>
/// </example>
internal sealed class SqlServerDbTestExecutionContext<TService>
    : SqlServerIntegrationTestBase<TService, OrdersDbContext>
    where TService : class
{
    private SqlServerDbTestExecutionContext(string adminConnStr, string testDbName, string testConnStr,
        Action<IServiceCollection>? configure)
        : base(adminConnStr, testDbName, testConnStr, configure) { }

    public static Task<SqlServerDbTestExecutionContext<TService>> CreateAsync(
        Action<IServiceCollection>? configure = null)
        => CreateCoreAsync<SqlServerDbTestExecutionContext<TService>>(
            SqlServer.ConnectionString(),
            "orders_test",
            (admin, dbName, connStr, cfg) => new SqlServerDbTestExecutionContext<TService>(admin, dbName, connStr, cfg),
            configure);
}
