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
    public static RouteGroupBuilder MapEntity<T>(
        this RouteGroupBuilder api,
        string path,
        Func<AmikiDb, DbSet<T>> set,
        Func<T, T, string?>? checkReplace = null)
        where T : class, IEntity
    {
        var group = api.MapGroup($"/{path}").WithTags(path);

        group.MapGet("/", async (AmikiDb db, CancellationToken ct) =>
            Results.Ok(await set(db).AsNoTracking().ToListAsync(ct)));

        group.MapPut("/{id:guid}", async (Guid id, T body, AmikiDb db, CancellationToken ct) =>
        {
            if (body.Id != id) return Results.Problem("The id in the URL and the body don't match.", statusCode: 400);
            if (body.Validate() is { } problem) return Results.Problem(problem, statusCode: 400);

            var stored = await set(db).FindAsync([id], ct);
            if (stored is null)
            {
                set(db).Add(body);
            }
            else
            {
                if (checkReplace?.Invoke(stored, body) is { } conflict) return Results.Problem(conflict, statusCode: 409);
                db.Entry(stored).CurrentValues.SetValues(body);
            }

            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, AmikiDb db, CancellationToken ct) =>
        {
            if (await set(db).FindAsync([id], ct) is { } stored)
            {
                set(db).Remove(stored);
                await db.SaveChangesAsync(ct);
            }
            return Results.NoContent();
        });

        return group;
    }
}
