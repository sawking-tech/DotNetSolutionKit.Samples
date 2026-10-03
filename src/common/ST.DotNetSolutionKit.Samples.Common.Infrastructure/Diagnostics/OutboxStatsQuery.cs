using ST.DotNetSolutionKit.Samples.Common.Contracts.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace ST.DotNetSolutionKit.Samples.Common.Infrastructure.Diagnostics;

/// <summary>
/// Reads the MassTransit EF outbox table of a service: pending and sent counts and a sample of the
/// latest rows.
/// </summary>
/// <remarks>
/// A table name cannot be a SQL parameter, so it goes into the text of the query. It is taken from
/// the EF model of the context, where <c>AddTransactionalOutbox</c> placed it in the service's schema,
/// and quoted by the provider, so nothing a caller passes reaches the SQL text.
/// </remarks>
public static class OutboxStatsQuery
{
    public static async Task<OutboxStatsResponse> RunAsync(
        DbContext db,
        string? filter,
        int sampleSize,
        CancellationToken ct)
    {
        var outboxTable = ResolveOutboxTable(db);

        // Aggregate counters across the whole table — pending vs sent + the age of the
        // oldest pending row + the freshness of the last sent row tells the operator
        // at a glance whether the delivery worker is keeping up or stopped draining.
        // MassTransit's EF migration makes "SentTime" NOT NULL and writes the .NET
        // DateTime.MinValue sentinel (0001-01-01) for rows that haven't been delivered
        // yet, so a "pending" row is one with "SentTime" at or before 1900-01-01.
        var totalsSql = $@"
                SELECT
                    COUNT(*) FILTER (WHERE ""SentTime"" <= TIMESTAMPTZ '1900-01-01')::bigint AS ""Pending"",
                    COUNT(*) FILTER (WHERE ""SentTime"" >  TIMESTAMPTZ '1900-01-01')::bigint AS ""Sent"",
                    MIN(""EnqueueTime"") FILTER (WHERE ""SentTime"" <= TIMESTAMPTZ '1900-01-01') AS ""OldestPendingAt"",
                    MAX(""SentTime"")    FILTER (WHERE ""SentTime"" >  TIMESTAMPTZ '1900-01-01') AS ""LastSentAt""
                FROM {outboxTable}";

        var totals = await db.Database
            .SqlQueryRaw<OutboxTotals>(totalsSql)
            .SingleAsync(ct);

        // Sample of the most recent rows for hands-on inspection; respects the optional
        // body-substring filter so the caller can zoom in on one publish (e.g. an order id).
        var sql = $@"
            SELECT ""MessageId""::text AS ""MessageId"",
                   ""EnqueueTime""     AS ""EnqueueTime"",
                   CASE WHEN ""SentTime"" <= TIMESTAMPTZ '1900-01-01' THEN NULL ELSE ""SentTime"" END AS ""SentTime""
            FROM {outboxTable}
            WHERE (@filter IS NULL OR ""Body"" LIKE '%' || @filter || '%')
            ORDER BY ""EnqueueTime"" DESC
            LIMIT @sampleSize";

        // Explicit NpgsqlDbType — without it the provider can't infer the type of @filter
        // when the value is DBNull, raising `42P08: could not determine data type`.
        var filterParam = new NpgsqlParameter("filter", NpgsqlTypes.NpgsqlDbType.Text)
        {
            Value = (object?)filter ?? DBNull.Value
        };
        var sizeParam = new NpgsqlParameter("sampleSize", NpgsqlTypes.NpgsqlDbType.Integer)
        {
            Value = Math.Clamp(sampleSize, 1, 50)
        };

        var rows = await db.Database
            .SqlQueryRaw<OutboxRowSnapshot>(sql, filterParam, sizeParam)
            .ToListAsync(ct);

        return new OutboxStatsResponse
        {
            Pending = totals.Pending,
            Sent = totals.Sent,
            OldestPendingAt = totals.OldestPendingAt,
            LastSentAt = totals.LastSentAt,
            Sample = rows,
        };
    }

    /// <summary>The quoted, schema-qualified outbox table of <paramref name="db"/>.</summary>
    internal static string ResolveOutboxTable(DbContext db)
    {
        var entity = db.Model.FindEntityType(typeof(MassTransit.EntityFrameworkCoreIntegration.OutboxMessage))
                     ?? throw new InvalidOperationException(
                         $"{db.GetType().Name} has no outbox: call modelBuilder.AddTransactionalOutbox(schema) in OnModelCreating.");

        var table = entity.GetTableName()
                    ?? throw new InvalidOperationException("The outbox message entity is not mapped to a table.");

        return db.GetService<ISqlGenerationHelper>().DelimitIdentifier(table, entity.GetSchema());
    }

    private sealed record OutboxTotals(long Pending, long Sent, DateTime? OldestPendingAt, DateTime? LastSentAt);
}
