using Amiki.Api.Data;
using Amiki.Modules.Assistant;
using Amiki.Modules.People;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Api.Users;

/// <summary>
/// The owner's People page, owner only:
///   GET    /api/people                       everyone who can sign in
///   POST   /api/people                       let a Google account in (with starter accounts and categories)
///   PUT    /api/people/{id}/enabled          turn their sign-in on or off
///   PUT    /api/people/{id}/gemini-key       set or remove their key
///   GET/PUT /api/people/{id}/assistant       what Amiki knows about them, and her personality
///   DELETE /api/people/{id}                  remove them and everything of theirs
/// Viewing someone's Amiki isn't here: it's the X-Amiki-View-As header (UserContextMiddleware).
///
/// And for everyone, about themselves:
///   GET/PUT /api/me/gemini-key               their own key (replaces one the owner set)
///   POST   /api/me/notice-seen               they've read the "what the owner can see" notice
/// </summary>
public static class PeopleEndpoints
{
    public static void MapPeople(this RouteGroupBuilder api)
    {
        var people = api.MapGroup("/people").WithTags("people").AddEndpointFilter(async (context, next) =>
            context.HttpContext.RequestServices.GetRequiredService<UserContext>().IsOwner
                ? await next(context)
                : Results.Problem("Only the owner can manage people.", statusCode: StatusCodes.Status403Forbidden));

        people.MapGet("/", async (AmikiDb db, CancellationToken ct) =>
            Results.Ok((await db.Users.AsNoTracking().ToListAsync(ct))
                .OrderByDescending(u => u.IsOwner).ThenBy(u => u.AddedAt)
                .Select(ToPerson)));

        people.MapPost("/", async (AddPersonRequest request, AmikiDb db, ApiKeyProtector keys, CancellationToken ct) =>
        {
            if (request.Validate() is { } problem) return Results.Problem(problem, statusCode: 400);
            var email = request.Email.Trim().ToLowerInvariant();
            if (await db.Users.AnyAsync(u => u.Email == email, ct)) return Results.Problem($"{email} can already sign in.", statusCode: 409);

            var person = new AppUser { Email = email };
            SetKey(person, request.GeminiKey, "owner", keys);
            db.Users.Add(person);
            await db.SaveChangesAsync(ct);
            await StarterData.ProvideAsync(db, person.Id, ct);
            return Results.Ok(ToPerson(person));
        });

        people.MapPut("/{id:guid}/enabled", async (Guid id, EnabledRequest request, AmikiDb db, CancellationToken ct) =>
        {
            if (id == AppUser.OwnerId) return Results.Problem("You can't turn yourself off.", statusCode: 400);
            if (await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct) is not { } person) return Results.NotFound();
            person.Enabled = request.Enabled;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToPerson(person));
        });

        people.MapPut("/{id:guid}/gemini-key", async (Guid id, GeminiKeyRequest request, AmikiDb db, ApiKeyProtector keys, CancellationToken ct) =>
        {
            if (request.Validate() is { } problem) return Results.Problem(problem, statusCode: 400);
            if (await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct) is not { } person) return Results.NotFound();
            SetKey(person, request.Key, "owner", keys);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToPerson(person));
        });

        people.MapGet("/{id:guid}/assistant", async (Guid id, AmikiDb db, CancellationToken ct) =>
        {
            if (!await db.Users.AnyAsync(u => u.Id == id, ct)) return Results.NotFound();
            var settings = await StarterData.Owned(db.AssistantSettings, id).AsNoTracking().FirstOrDefaultAsync(ct) ?? new AssistantSettings();
            return Results.Ok(new AssistantWords(settings.AboutMe, settings.Personality));
        });

        people.MapPut("/{id:guid}/assistant", async (Guid id, AssistantWords words, AmikiDb db, CancellationToken ct) =>
        {
            if (words.Validate() is { } problem) return Results.Problem(problem, statusCode: 400);
            if (!await db.Users.AnyAsync(u => u.Id == id, ct)) return Results.NotFound();
            var settings = await StarterData.Owned(db.AssistantSettings, id).FirstOrDefaultAsync(ct);
            if (settings is null)
            {
                settings = new AssistantSettings();
                StarterData.AddFor(db, settings, id);
            }
            settings.AboutMe = words.AboutMe.Trim();
            settings.Personality = words.Personality.Trim();
            settings.UpdatedAt = DateTime.Now;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        people.MapDelete("/{id:guid}", async (Guid id, AmikiDb db, CancellationToken ct) =>
        {
            if (id == AppUser.OwnerId) return Results.Problem("You can't remove yourself.", statusCode: 400);
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            // Every table with a UserId column, so nothing of theirs is left behind (new tables included).
            // Table names come from Amiki's own model, never from the request; the id is a parameter.
            foreach (var table in db.Model.GetEntityTypes().Where(t => t.FindProperty(AmikiDb.UserIdColumn) is not null).Select(t => t.GetTableName()).Distinct())
            {
                var sql = $"DELETE FROM \"{table}\" WHERE \"{AmikiDb.UserIdColumn}\" = {{0}}";
                await db.Database.ExecuteSqlRawAsync(sql, [id], ct);
            }
            await db.Users.Where(u => u.Id == id).ExecuteDeleteAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.NoContent();
        });

        var me = api.MapGroup("/me").WithTags("me");

        me.MapGet("/gemini-key", async (AmikiDb db, UserContext user, CancellationToken ct) =>
            Results.Ok(new MyKeyInfo((await db.Users.AsNoTracking().FirstAsync(u => u.Id == user.SignedInId, ct)).GeminiKeySetBy)));

        me.MapPut("/gemini-key", async (GeminiKeyRequest request, AmikiDb db, UserContext user, ApiKeyProtector keys, CancellationToken ct) =>
        {
            if (request.Validate() is { } problem) return Results.Problem(problem, statusCode: 400);
            var person = await db.Users.FirstAsync(u => u.Id == user.SignedInId, ct);
            SetKey(person, request.Key, "self", keys);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new MyKeyInfo(person.GeminiKeySetBy));
        });

        me.MapPost("/notice-seen", async (AmikiDb db, UserContext user, CancellationToken ct) =>
        {
            await db.Users.Where(u => u.Id == user.SignedInId && u.NoticeSeenAt == null)
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.NoticeSeenAt, DateTime.Now), ct);
            return Results.NoContent();
        });
    }

    // Empty removes the key. It's stored encrypted and never sent back.
    private static void SetKey(AppUser person, string? key, string setBy, ApiKeyProtector keys)
    {
        var hasKey = !string.IsNullOrWhiteSpace(key);
        person.GeminiKeyProtected = hasKey ? keys.Protect(key!) : null;
        person.GeminiKeySetBy = hasKey ? setBy : null;
    }

    private static Person ToPerson(AppUser u) =>
        new(u.Id, u.Email, u.Name, u.IsOwner, u.Enabled, u.GeminiKeySetBy, u.AddedAt, u.LastSignInAt);
}
