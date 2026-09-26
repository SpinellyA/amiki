using Amiki.Api.Data;
using Amiki.Core;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Api.Endpoints;

/// <summary>
/// The same three endpoints for every module's items:
///   GET    /api/{path}        everything (one person's data is small; the client filters)
///   PUT    /api/{path}/{id}   create or replace. Safe to retry: same id, same result.
///   DELETE /api/{path}/{id}   remove. Deleting something already gone still succeeds.
/// </summary>
public static class EntityEndpoints
{
    /// <param name="checkReplace">Rules comparing the stored item with the incoming one (409 when broken).</param>
    /// <param name="checkAsync">Rules that need the database, e.g. "that account exists" (400 when broken).</param>
    /// <param name="describe">When set, every real change is written to the audit log in the same save.</param>
    public static RouteGroupBuilder MapEntity<T>(
        this RouteGroupBuilder api,
        string path,
        Func<AmikiDb, DbSet<T>> set,
        Func<T, T, string?>? checkReplace = null,
        Func<AmikiDb, T, CancellationToken, Task<string?>>? checkAsync = null,
        Func<T, string>? describe = null)
        where T : class, IEntity
    {
        var group = api.MapGroup($"/{path}").WithTags(path);

        group.MapGet("/", async (AmikiDb db, CancellationToken ct) =>
            Results.Ok(await set(db).AsNoTracking().ToListAsync(ct)));

        group.MapPut("/{id:guid}", async (Guid id, T body, AmikiDb db, CancellationToken ct) =>
        {
            if (body.Id != id) return Results.Problem("The id in the URL and the body don't match.", statusCode: 400);
            if (body.Validate() is { } problem) return Results.Problem(problem, statusCode: 400);
            if (checkAsync is not null && await checkAsync(db, body, ct) is { } dbProblem) return Results.Problem(dbProblem, statusCode: 400);

            var stored = await set(db).FindAsync([id], ct);
            var before = stored is null || describe is null ? null : Audit.Serialize(stored);
            if (stored is null)
            {
                set(db).Add(body);
            }
            else
            {
                if (checkReplace?.Invoke(stored, body) is { } conflict) return Results.Problem(conflict, statusCode: 409);
                db.Entry(stored).CurrentValues.SetValues(body);
            }

            if (describe is not null)
            {
                var after = Audit.Serialize(body);
                // A retried save of the same content isn't a change, so it isn't logged twice.
                if (Audit.HasChanges(before, after))
                    db.AuditLog.Add(Audit.Entry(path, id, stored is null ? "created" : "edited", describe(body), before, after));
            }

            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, AmikiDb db, CancellationToken ct) =>
        {
            if (await set(db).FindAsync([id], ct) is { } stored)
            {
                if (describe is not null)
                    db.AuditLog.Add(Audit.Entry(path, id, "deleted", describe(stored), Audit.Serialize(stored), null));
                set(db).Remove(stored);
                await db.SaveChangesAsync(ct);
            }
            return Results.NoContent();
        });

        return group;
    }
}
