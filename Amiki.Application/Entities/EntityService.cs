using Amiki.Application.Auditing;
using Amiki.Application.Common;
using Amiki.Application.Data;
using Amiki.Application.People;
using Amiki.Domain;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.Entities;

/// <summary>
/// List, save (create or replace) and delete for one kind of item, with its <see cref="EntityRules{T}"/>.
/// Saving is safe to retry: same id, same result. Deleting something already gone succeeds.
/// </summary>
public sealed class EntityService<T>(IAmikiDb db, EntityRules<T> rules, ICurrentUser user, AuditTrail audit)
    where T : class, IEntity
{
    /// <summary>Everything of this kind (one person's data is small; the app filters).</summary>
    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct) =>
        rules.PrivateFromOwner && user.IsViewingSomeoneElse
            ? []
            : await db.Set<T>().AsNoTracking().ToListAsync(ct);

    public async Task<Outcome> SaveAsync(Guid id, T incoming, CancellationToken ct)
    {
        if (incoming.Id != id) return Outcome.Invalid("The id in the URL and the body don't match.");
        if (incoming.Validate() is { } problem) return Outcome.Invalid(problem);
        if (await rules.CheckAsync(incoming, ct) is { } dbProblem) return Outcome.Invalid(dbProblem);

        var stored = await db.Set<T>().FirstOrDefaultAsync(e => e.Id == id, ct);
        if (!user.IsOwner) rules.FromMember(stored, incoming);
        var before = stored is null ? null : AuditSnapshot.Of(stored);

        if (stored is null)
        {
            db.Set<T>().Add(incoming);
        }
        else
        {
            if (rules.CheckReplace(stored, incoming) is { } conflict) return Outcome.Conflict(conflict);
            db.Entry(stored).CurrentValues.SetValues(incoming);
        }

        // A retried save of the same content isn't a change, so it isn't logged twice.
        if (rules.AuditAs is { } entity && AuditSnapshot.Of(incoming) is var after && (before is null || after.DiffersFrom(before)))
            audit.Record(entity, id, before is null ? AuditEntry.Created : AuditEntry.Edited, rules.Describe(incoming), before, after);

        await db.SaveChangesAsync(ct);
        return Outcome.Ok;
    }

    public async Task<Outcome> DeleteAsync(Guid id, CancellationToken ct)
    {
        if (await db.Set<T>().FirstOrDefaultAsync(e => e.Id == id, ct) is not { } stored) return Outcome.Ok;
        if (await rules.CheckDeleteAsync(stored, ct) is { } conflict) return Outcome.Conflict(conflict);
        if (rules.AuditAs is { } entity)
            audit.Record(entity, id, AuditEntry.Deleted, rules.Describe(stored), AuditSnapshot.Of(stored), null);
        db.Set<T>().Remove(stored);
        await db.SaveChangesAsync(ct);
        return Outcome.Ok;
    }
}
