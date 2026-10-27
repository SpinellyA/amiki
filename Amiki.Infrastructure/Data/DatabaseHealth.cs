using Amiki.Application.Data;
using Npgsql;

namespace Amiki.Infrastructure.Data;

/// <summary>
/// Pings Postgres on its own unpooled connection with hard limits, so a database that accepts the
/// connection but never answers still fails fast instead of hanging the monitor. 8 seconds leaves
/// room for Neon waking a suspended database.
/// </summary>
public sealed class DatabaseHealth(string connectionString) : IDatabaseHealth
{
    private readonly string _connection = new NpgsqlConnectionStringBuilder(connectionString)
    {
        Timeout = 8,
        CommandTimeout = 8,
        CancellationTimeout = 1000,
        Pooling = false,
    }.ConnectionString;

    public async Task<bool> AnswersAsync(CancellationToken ct)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connection);
            await connection.OpenAsync(ct);
            await using var ping = new NpgsqlCommand("SELECT 1", connection);
            await ping.ExecuteScalarAsync(ct);
            return true;
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException or InvalidOperationException or OperationCanceledException)
        {
            return false;
        }
    }
}
