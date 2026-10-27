using Amiki.Application.Entities;
using Amiki.Domain;

namespace Amiki.Api.Endpoints;

/// <summary>
/// The same three endpoints for every kind of item. Its rules live in Application
/// (<see cref="EntityRules{T}"/>), so this stays plain HTTP:
///   GET    /api/{path}        everything (one person's data is small; the app filters)
///   PUT    /api/{path}/{id}   create or replace. Safe to retry: same id, same result.
///   DELETE /api/{path}/{id}   remove. Deleting something already gone still succeeds.
/// </summary>
public static class EntityEndpoints
{
    public static RouteGroupBuilder MapEntity<T>(this RouteGroupBuilder api, string path) where T : class, IEntity
    {
        var group = api.MapGroup($"/{path}").WithTags(path);

        group.MapGet("/", async (EntityService<T> items, CancellationToken ct) => Results.Ok(await items.ListAsync(ct)));
        group.MapPut("/{id:guid}", async (Guid id, T body, EntityService<T> items, CancellationToken ct) =>
            (await items.SaveAsync(id, body, ct)).ToHttp());
        group.MapDelete("/{id:guid}", async (Guid id, EntityService<T> items, CancellationToken ct) =>
            (await items.DeleteAsync(id, ct)).ToHttp());

        return group;
    }
}
