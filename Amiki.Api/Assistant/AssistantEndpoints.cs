using Amiki.Modules.Assistant;
using Microsoft.Extensions.Options;

namespace Amiki.Api.Assistant;

/// <summary>
///   GET  /api/assistant/status   whether the assistant is set up (a Gemini key is present)
///   POST /api/assistant/triage   suggestions for Inbox notes
/// </summary>
public static class AssistantEndpoints
{
    public static IServiceCollection AddAssistant(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<GeminiOptions>(config.GetSection("Gemini"));
        services.AddHttpClient<ILanguageModel, GeminiModel>(http => http.Timeout = TimeSpan.FromSeconds(30));
        services.AddScoped<InboxTriageService>();
        return services;
    }

    public static RouteGroupBuilder MapAssistant(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/assistant").WithTags("assistant");

        group.MapGet("/status", (IOptions<GeminiOptions> options) => Results.Ok(new { enabled = options.Value.Enabled }));

        group.MapPost("/triage", async (TriageRequest request, InboxTriageService triage, IOptions<GeminiOptions> options, CancellationToken ct) =>
        {
            if (!options.Value.Enabled) return Results.Problem("The assistant isn't set up yet.", statusCode: StatusCodes.Status503ServiceUnavailable);
            if (request.Validate() is { } problem) return Results.Problem(problem, statusCode: StatusCodes.Status400BadRequest);
            try
            {
                return Results.Ok(await triage.SuggestAsync(request, ct));
            }
            catch (LanguageModelException ex)
            {
                return Results.Problem(ex.Message, statusCode: ex.RateLimited ? StatusCodes.Status429TooManyRequests : StatusCodes.Status502BadGateway);
            }
        });

        return group;
    }
}
