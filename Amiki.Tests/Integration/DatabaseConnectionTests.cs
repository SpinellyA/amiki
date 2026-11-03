using Amiki.Infrastructure.Data;
using Npgsql;

namespace Amiki.Tests.Integration;

public class DatabaseConnectionTests
{
    [Fact]
    public void A_small_pool_that_lets_idle_connections_go_by_default()
    {
        var tuned = new NpgsqlConnectionStringBuilder(DatabaseConnection.Tuned("Host=db.example.com;Database=postgres;Username=u;Password=p"));

        Assert.Equal(DatabaseConnection.DefaultMaxPoolSize, tuned.MaxPoolSize);
        Assert.Equal(DatabaseConnection.DefaultIdleSeconds, tuned.ConnectionIdleLifetime);
        Assert.Equal(GssEncryptionMode.Disable, tuned.GssEncryptionMode);
        Assert.Equal(("db.example.com", "p"), (tuned.Host, tuned.Password)); // everything else as given
    }

    [Fact]
    public void Settings_in_the_configured_string_win()
    {
        var tuned = new NpgsqlConnectionStringBuilder(DatabaseConnection.Tuned("Host=h;Maximum Pool Size=12;Connection Idle Lifetime=120"));

        Assert.Equal((12, 120), (tuned.MaxPoolSize, tuned.ConnectionIdleLifetime));
    }

    [Fact]
    public void A_full_pooler_is_waited_out_but_other_errors_arent()
    {
        var full = new PostgresException("(EMAXCONNSESSION) max clients reached in session mode", "FATAL", "FATAL", "XX000");
        var tooMany = new PostgresException("too many connections", "FATAL", "FATAL", PostgresErrorCodes.TooManyConnections);
        var badPassword = new PostgresException("password authentication failed", "FATAL", "FATAL", PostgresErrorCodes.InvalidPassword);

        Assert.True(DatabaseStartup.IsFull(full));
        Assert.True(DatabaseStartup.IsFull(new InvalidOperationException("wrapped", tooMany)));
        Assert.False(DatabaseStartup.IsFull(badPassword));
    }
}
