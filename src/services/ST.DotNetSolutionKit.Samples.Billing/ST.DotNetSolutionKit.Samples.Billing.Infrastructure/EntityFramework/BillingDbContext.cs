using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ST.DotNetSolutionKit.Samples.Common.Domain.Persistence;
using ST.DotNetSolutionKit.Samples.Common.Infrastructure.Persistence.EntityFramework;

namespace ST.DotNetSolutionKit.Samples.Billing.Infrastructure.EntityFramework;

[SuppressMessage("ReSharper", "RedundantExtendsListEntry")]
public class BillingDbContext(DbContextOptions<BillingDbContext> options)
    : DbContextBase(options), IUnitOfWork
{
    public static readonly string DefaultSchemaName = "billing";
    
    // Add DbSet here

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(DefaultSchemaName);

        // Automatic registration of configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}