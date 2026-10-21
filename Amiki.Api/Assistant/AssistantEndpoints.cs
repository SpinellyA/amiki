using Amiki.Api.Data;
using Amiki.Api.Endpoints;
using Amiki.Modules.Assistant;
using Microsoft.Extensions.Options;

namespace Amiki.Api.Assistant;

/// <summary>
///   GET  /api/assistant/status             whether the assistant is set up, and which model
///   POST /api/assistant/triage             suggestions for Inbox notes
///   POST /api/assistant/chat               one chat turn
///   GET  /api/assistant/preview?now=…      exactly what she's told before your next message
///   POST /api/assistant/test               a tiny call to check the key works
///   /api/assistant-settings                her settings (the usual get / save / delete)
/// </summary>
public static class AssistantEndpoints
{
    public static IServiceCollection AddAssistant(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<GeminiOptions>(config.GetSection("Gemini"));
        services.AddHttpClient<ILanguageModel, GeminiModel>(http => http.Timeout = TimeSpan.FromSeconds(60));
        services.AddScoped<InboxTriageService>();
        services.AddScoped<AssistantTools>();
        services.AddScoped<AssistantChatService>();
        return services;
    }

    public static RouteGroupBuilder MapAssistant(this RouteGroupBuilder api)
    {
        api.MapEntity("assistant-settings", db => db.AssistantSettings);

        var group = api.MapGroup("/assistant").WithTags("assistant");

        group.MapGet("/status", (IOptions<GeminiOptions> options, ILanguageModel model) =>
            Results.Ok(new { enabled = options.Value.Enabled, model = model.Description }));

        group.MapPost("/triage", (TriageRequest request, InboxTriageService triage, IOptions<GeminiOptions> options, CancellationToken ct) =>
            Answer(options, request.Validate(), () => triage.SuggestAsync(request, ct)));

        group.MapPost("/chat", (ChatRequest request, AssistantChatService chat, IOptions<GeminiOptions> options, CancellationToken ct) =>
            Answer(options, request.Validate(), () => chat.ChatAsync(request, ct)));

        group.MapGet("/preview", async (DateTime? now, AssistantChatService chat, CancellationToken ct) =>
            Results.Ok(await chat.PreviewAsync(now ?? DateTime.Now, ct)));

        group.MapPost("/test", (AssistantChatService chat, IOptions<GeminiOptions> options, CancellationToken ct) =>
            Answer(options, null, async () => new { reply = await chat.TestAsync(ct) }));

        return group;
    }

    // The same handling for every call that reaches the model: off without a key, readable errors.
    private static async Task<IResult> Answer<T>(IOptions<GeminiOptions> options, string? problem, Func<Task<T>> ask)
    {
        if (!options.Value.Enabled) return Results.Problem("The assistant isn't set up yet: add a Gemini API key.", statusCode: StatusCodes.Status503ServiceUnavailable);
        if (problem is not null) return Results.Problem(problem, statusCode: StatusCodes.Status400BadRequest);
        try
        {
            return Results.Ok(await ask());
        }
        catch (LanguageModelException ex)
        {
            return Results.Problem(ex.Message, statusCode: ex.RateLimited ? StatusCodes.Status429TooManyRequests : StatusCodes.Status502BadGateway);
        }
    }
}
