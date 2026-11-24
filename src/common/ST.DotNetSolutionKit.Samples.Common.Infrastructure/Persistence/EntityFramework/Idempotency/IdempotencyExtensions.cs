using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ST.DotNetSolutionKit.Samples.Common.Application.Idempotency;
using ST.DotNetSolutionKit.Samples.Common.Domain.Idempotency;

namespace ST.DotNetSolutionKit.Samples.Common.Infrastructure.Persistence.EntityFramework.Idempotency;

public static class IdempotencyExtensions
{
    /// <summary>
    /// Adds the idempotency log's table to the service's model, in its schema. Call it in
    /// <c>OnModelCreating</c>; the next migration creates the table.
    /// </summary>
    public static ModelBuilder AddIdempotencyLog(this ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfiguration(new IdempotencyRecordConfiguration());

    /// <summary>
    /// Registers <see cref="IIdempotentExecutor"/> over a log kept by <typeparamref name="TContext"/>, whose
    /// model has <see cref="AddIdempotencyLog"/>.
    /// </summary>
    public static IServiceCollection AddIdempotency<TContext>(this IServiceCollection services)
        where TContext : DbContextBase
    {
        services.TryAddScoped<IIdempotencyLog, EntityFrameworkIdempotencyLog<TContext>>();
        services.TryAddScoped<IIdempotentExecutor, IdempotentExecutor>();
        return services;
    }
}
