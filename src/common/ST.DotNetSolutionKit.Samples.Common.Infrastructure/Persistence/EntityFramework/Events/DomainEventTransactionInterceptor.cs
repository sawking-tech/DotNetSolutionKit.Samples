using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ST.DotNetSolutionKit.Samples.Common.Infrastructure.Persistence.EntityFramework.Events;

/// <summary>
/// Finalizes the domain event lifecycle by executing Post-Commit or Rollback phases.
/// </summary>
/// <remarks>
/// <b>Note:</b> Must be registered as a <b>Singleton</b> to support <b>DbContextPool</b>.
/// Infrastructure is resolved via <see cref="DomainEventInfrastructureResolver"/>.
/// </remarks>
public sealed class DomainEventTransactionInterceptor : DbTransactionInterceptor
{
    public override async Task TransactionCommittedAsync(
        DbTransaction transaction, 
        TransactionEndEventData eventData, 
        CancellationToken ct = default)
    {
        var context = eventData.Context;
        
        // Resolve infrastructure. Skip if in Root Provider or outside of a valid Scope.
        if (context is null || !DomainEventInfrastructureResolver.TryResolve(context, out var storage, out var dispatcher))
        {
            await base.TransactionCommittedAsync(transaction, eventData, ct);
            return;
        }

        // Snapshot + IsDispatching guard (the recursion protection the pipeline Readme promises):
        // a PostCommit handler that commits its own transaction on another DbContext in the SAME
        // scope re-enters this interceptor and resolves the SAME scoped storage. Without the guard
        // the nested commit re-dispatches the outer events and Clear()s the live list the outer
        // dispatch is still iterating (GetEvents returns a view, not a copy).
        var events = storage.GetEvents().ToList();
        if (events.Count > 0 && !storage.IsDispatching)
        {
            storage.IsDispatching = true;
            try
            {
                // PHASE 2: Post-Commit (side effects after successful DB commit).
                await dispatcher.DispatchPostCommitAsync(events, ct);
            }
            finally
            {
                // Ensure storage is cleared even if dispatch fails to prevent event duplication.
                storage.IsDispatching = false;
                storage.Clear();
            }
        }

        await base.TransactionCommittedAsync(transaction, eventData, ct);
    }

    public override async Task TransactionRolledBackAsync(
        DbTransaction transaction, 
        TransactionEndEventData eventData, 
        CancellationToken ct = default)
    {
        var context = eventData.Context;

        if (context is null || !DomainEventInfrastructureResolver.TryResolve(context, out var storage, out var dispatcher))
        {
            await base.TransactionRolledBackAsync(transaction, eventData, ct);
            return;
        }

        // Same snapshot + reentrancy guard as the commit side - a rollback of a nested
        // same-scope transaction must not re-dispatch or clear the outer events mid-flight.
        var events = storage.GetEvents().ToList();
        if (events.Count > 0 && !storage.IsDispatching)
        {
            storage.IsDispatching = true;
            try
            {
                // PHASE 3: Rollback (notifying subscribers about failure with the captured exception).
                await dispatcher.DispatchRollbackAsync(events, storage.LastException, ct);
            }
            finally
            {
                storage.IsDispatching = false;
                storage.Clear();
            }
        }

        await base.TransactionRolledBackAsync(transaction, eventData, ct);
    }
}
