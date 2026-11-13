using MassTransit;

namespace ST.DotNetSolutionKit.Samples.Capabilities.Messaging.RabbitMQ;

/// <summary>The lock statements the outbox runs against the solution's database.</summary>
internal static class OutboxDatabaseLocks
{
    public static IEntityFrameworkOutboxConfigurator UseDatabaseLocks(this IEntityFrameworkOutboxConfigurator outbox)
    {
        outbox.UsePostgres();
        return outbox;
    }
}
