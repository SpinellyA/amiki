using Amiki.Application.Data;
using Amiki.Domain;

namespace Amiki.Application.Auditing;

/// <summary>
/// Adds a line to the money history. It's saved together with the change it describes (same
/// SaveChanges), so the history can't drift from the data.
/// </summary>
public sealed class AuditTrail(IAmikiDb db, TimeProvider time)
{
    public void Record(string entity, Guid id, string action, string summary, AuditSnapshot? before, AuditSnapshot? after)
    {
        var changes = before is not null && after is not null ? after.ChangesSince(before) : [];
        db.AuditLog.Add(new AuditEntry
        {
            At = time.GetLocalNow().DateTime,
            Entity = entity,
            EntityId = id,
            Action = action,
            Summary = action == AuditEntry.Edited && changes.Count > 0 ? $"{summary} · changed {string.Join(", ", changes)}" : summary,
            Before = before?.Json,
            After = after?.Json,
        });
    }
}
