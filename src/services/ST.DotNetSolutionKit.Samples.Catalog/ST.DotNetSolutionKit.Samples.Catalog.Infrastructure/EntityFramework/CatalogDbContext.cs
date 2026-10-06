using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ST.DotNetSolutionKit.Samples.Common.Domain.Persistence;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Persistence.EntityFramework;

namespace ST.DotNetSolutionKit.Samples.Catalog.Infrastructure.EntityFramework;

[SuppressMessage("ReSharper", "RedundantExtendsListEntry")]
public class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : DbContextBase(options), IUnitOfWork
{
    public static readonly string DefaultSchemaName = "catalog";
    
    // Add DbSet here

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(DefaultSchemaName);

        // Automatic registration of configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}