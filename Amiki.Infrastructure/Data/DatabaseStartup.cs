using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Amiki.Infrastructure.Data;

/// <summary>Brings the database up to date at startup, so a deploy is a single step.</summary>
public static class DatabaseStartup
{
    /// <summary>Waits between tries when the database is full (about two minutes in all).</summary>
    internal static TimeSpan[] RetryDelays = [.. Enumerable.Range(1, 8).Select(i => TimeSpan.FromSeconds(Math.Min(5 * i, 25)))];

    /// <summary>
    /// Migrates, waiting a little when the database has no room for another connection (during a
    /// deploy the old copy of the app still holds its connections until it stops or they go idle).
    /// </summary>
    public static async Task MigrateAsync(IServiceProvider services, CancellationToken ct = default)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await services.GetRequiredService<AmikiDb>().Database.MigrateAsync(ct);
                return;
            }
            catch (Exception ex) when (IsFull(ex) && attempt < RetryDelays.Length)
            {
                Console.WriteLine($"Database is full ({ex.Message.Split('\n')[0]}); trying again in {RetryDelays[attempt].TotalSeconds:0}s.");
                await Task.Delay(RetryDelays[attempt], ct);
            }
        }
    }

    // Supabase's pooler says EMAXCONNSESSION; Postgres itself says 53300 (too many connections).
    internal static bool IsFull(Exception ex) =>
        ex is PostgresException pg && (pg.SqlState == PostgresErrorCodes.TooManyConnections || pg.MessageText.Contains("EMAXCONNSESSION", StringComparison.Ordinal))
        || ex.InnerException is { } inner && IsFull(inner);
}
