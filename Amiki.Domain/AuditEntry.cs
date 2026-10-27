namespace Amiki.Domain;

/// <summary>
/// One line of the money history. Written by the server in the same database transaction as
/// the change it describes, and never edited or deleted, so the history can't drift from the data.
/// </summary>
public sealed class AuditEntry
{
    public const string Created = "created";
    public const string Edited = "edited";
    public const string Deleted = "deleted";

    public long Id { get; init; }
    public DateTime At { get; init; }
    /// <summary>"transactions", "transfers", "balance-checks" or "categories".</summary>
    public string Entity { get; init; } = "";
    public Guid EntityId { get; init; }
    /// <summary><see cref="Created"/>, <see cref="Edited"/> or <see cref="Deleted"/>.</summary>
    public string Action { get; init; } = "";
    public string Summary { get; init; } = "";
    /// <summary>The item as JSON before the change (null when created).</summary>
    public string? Before { get; init; }
    /// <summary>The item as JSON after the change (null when deleted).</summary>
    public string? After { get; init; }
}
