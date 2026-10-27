using Amiki.Application.People;
using Amiki.Contracts.People;

namespace Amiki.Api.Endpoints;

/// <summary>
/// The owner's People page, owner only:
///   GET    /api/people                       everyone who can sign in
///   POST   /api/people                       let a Google account in (with starter accounts and categories)
///   PUT    /api/people/{id}/enabled          turn their sign-in on or off
///   PUT    /api/people/{id}/gemini-key       set or remove their key
///   GET/PUT /api/people/{id}/assistant       what Amiki knows about them, and her personality
///   DELETE /api/people/{id}                  remove them and everything of theirs
///
/// And for everyone, about themselves:
///   GET/PUT /api/me/gemini-key               their own key (replaces one the owner set)
///   POST   /api/me/notice-seen               they've read the "what the owner can see" notice
/// </summary>
public static class PeopleEndpoints
{
    public static void MapPeople(this RouteGroupBuilder api)
    {
        var people = api.MapGroup("/people").WithTags("people").AddEndpointFilter(OwnerOnly);

        people.MapGet("/", async (PeopleService service, CancellationToken ct) => Results.Ok(await service.ListAsync(ct)));
        people.MapPost("/", async (AddPersonRequest request, PeopleService service, CancellationToken ct) =>
            (await service.AddAsync(request, ct)).ToHttp());
        people.MapPut("/{id:guid}/enabled", async (Guid id, EnabledRequest request, PeopleService service, CancellationToken ct) =>
            (await service.SetEnabledAsync(id, request.Enabled, ct)).ToHttp());
        people.MapPut("/{id:guid}/gemini-key", async (Guid id, GeminiKeyRequest request, PeopleService service, CancellationToken ct) =>
            (await service.SetKeyAsync(id, request, ct)).ToHttp());
        people.MapGet("/{id:guid}/assistant", async (Guid id, PeopleService service, CancellationToken ct) =>
            (await service.WordsAsync(id, ct)).ToHttp());
        people.MapPut("/{id:guid}/assistant", async (Guid id, AssistantWords words, PeopleService service, CancellationToken ct) =>
            (await service.SetWordsAsync(id, words, ct)).ToHttp());
        people.MapDelete("/{id:guid}", async (Guid id, PeopleService service, CancellationToken ct) =>
            (await service.RemoveAsync(id, ct)).ToHttp());

        var me = api.MapGroup("/me").WithTags("me");

        me.MapGet("/gemini-key", async (MyAccountService account, CancellationToken ct) => Results.Ok(await account.KeyAsync(ct)));
        me.MapPut("/gemini-key", async (GeminiKeyRequest request, MyAccountService account, CancellationToken ct) =>
            (await account.SetKeyAsync(request, ct)).ToHttp());
        me.MapPost("/notice-seen", async (MyAccountService account, CancellationToken ct) =>
        {
            await account.NoticeSeenAsync(ct);
            return Results.NoContent();
        });
    }

    private static async ValueTask<object?> OwnerOnly(EndpointFilterInvocationContext context, EndpointFilterDelegate next) =>
        context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>().IsOwner
            ? await next(context)
            : Results.Problem("Only the owner can manage people.", statusCode: StatusCodes.Status403Forbidden);
}
