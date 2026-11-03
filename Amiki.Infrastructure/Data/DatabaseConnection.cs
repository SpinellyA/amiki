using Npgsql;

namespace Amiki.Infrastructure.Data;

/// <summary>
/// The connection string Amiki actually uses, tuned for a small hosted Postgres behind a pooler
/// (Supabase's session pooler allows only 15 clients at a time, shared by every running copy of
/// the app, including the old one still running during a deploy). Anything set explicitly in the
/// configured connection string wins over these defaults.
/// </summary>
public static class DatabaseConnection
{
    /// <summary>Most connections one copy of the app keeps open (a page load asks for about a dozen things at once; they queue).</summary>
    public const int DefaultMaxPoolSize = 5;

    /// <summary>Seconds an unused connection stays open before it's closed and its pooler slot freed.</summary>
    public const int DefaultIdleSeconds = 30;

    public static string Tuned(string configured)
    {
        var given = new NpgsqlConnectionStringBuilder(configured);
        var said = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = configured };
        bool Has(params string[] keys) => keys.Any(said.ContainsKey);

        if (!Has("Maximum Pool Size", "Max Pool Size", "MaxPoolSize")) given.MaxPoolSize = DefaultMaxPoolSize;
        if (!Has("Connection Idle Lifetime", "ConnectionIdleLifetime")) given.ConnectionIdleLifetime = DefaultIdleSeconds;
        // The server image has no Kerberos library; without this the driver looks for it on every
        // connection and logs "Cannot load library libgssapi_krb5.so.2".
        if (!Has("GSS Encryption Mode", "GssEncryptionMode")) given.GssEncryptionMode = GssEncryptionMode.Disable;
        return given.ConnectionString;
    }
}
