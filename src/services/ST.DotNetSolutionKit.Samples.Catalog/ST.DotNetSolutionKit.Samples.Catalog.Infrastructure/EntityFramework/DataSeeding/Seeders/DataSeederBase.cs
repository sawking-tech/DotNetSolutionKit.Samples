using Microsoft.Extensions.Logging;
using ST.DotNetSolutionKit.Samples.Common.Domain.Context;

namespace ST.DotNetSolutionKit.Samples.Catalog.Infrastructure.EntityFramework.DataSeeding.Seeders;

public abstract class DataSeederBase
{
    protected readonly ILogger Logger;
    protected readonly CatalogDbContext DbContext;
    protected readonly IDomainExecutionContext SeedContext;

    protected DataSeederBase(
        CatalogDbContext dbContext,
        ILogger logger)
    {
        Logger = logger;
        DbContext = dbContext;
        SeedContext = new SystemSeedContext();
    }

    public abstract Task SeedAsync(CancellationToken cancellationToken = default);
}