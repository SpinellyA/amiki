namespace Amiki.Core;

/// <summary>
/// One line of the money history. Written by the server in the same database transaction as
/// the change it describes, and never edited or deleted, so the history can't drift from the data.
/// </summary>
public sealed class AuditEntry
{
    public long Id { get; init; }
    public DateTime At { get; init; }
    /// <summary>"transactions", "transfers" or "balance-checks".</summary>
    public string Entity { get; init; } = "";
    public Guid EntityId { get; init; }
    /// <summary>"created", "edited" or "deleted".</summary>
    public string Action { get; init; } = "";
    public string Summary { get; init; } = "";
    /// <summary>The item as JSON before the change (null when created).</summary>
    public string? Before { get; init; }
    /// <summary>The item as JSON after the change (null when deleted).</summary>
    public string? After { get; init; }
}
