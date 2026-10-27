using Amiki.Application.Assistant;
using Amiki.Application.People;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Amiki.Domain.Assistant;
using Microsoft.Extensions.Options;

namespace Amiki.Infrastructure.Assistant;

/// <summary>
/// Google's Gemini API (generateContent). The key goes in a header, never in the URL, so it can't
/// end up in logs.
///
/// Each person uses their own key. The one in the config (Gemini__ApiKey) is the owner's, and is
/// never used for anyone else: a person without a key simply has the assistant off.
/// </summary>
public sealed class GeminiModel(HttpClient http, IOptions<GeminiOptions> options, ICurrentUser user) : ILanguageModel
{
    private const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/models/";

    /// <summary>Most lookups a single reply may make, so a confused model can't loop forever on the free tier.</summary>
    internal const int MaxToolRounds = 5;

    public string Description => $"Gemini · chat: {options.Value.Model} · background: {options.Value.LightModel}";

    private string? ApiKey => user.GeminiKey ?? (user.IsOwner && options.Value.Enabled ? options.Value.ApiKey : null);

    public bool IsEnabled => !string.IsNullOrWhiteSpace(ApiKey);

    public string? LastNote { get; private set; }

    public async Task<string> AskForJsonAsync(string instructions, string input, JsonObject schema, CancellationToken ct)
    {
        var body = Request(instructions, new JsonArray(UserText(input)));
        body["generationConfig"] = new JsonObject
        {
            ["temperature"] = 0.2,
            ["responseMimeType"] = "application/json",
            ["responseSchema"] = ToGeminiSchema(schema),
        };
        var content = await SendAsync(options.Value.LightModel, body, ct);
        return Text(content) ?? throw new LanguageModelException("Gemini sent back an empty answer.");
    }

    public async Task<string> WriteAsync(string instructions, string input, CancellationToken ct)
    {
        var body = Request(instructions, new JsonArray(UserText(input)));
        body["generationConfig"] = new JsonObject { ["temperature"] = 0.7 };
        var content = await SendAsync(options.Value.LightModel, body, ct);
        return Text(content) ?? throw new LanguageModelException("Gemini sent back an empty answer.");
    }

    public async Task<string> ChatAsync(
        string instructions, IReadOnlyList<ChatLine> history, string message, IReadOnlyList<ToolDefinition> tools,
        Func<ToolCall, CancellationToken, Task<JsonNode>> runTool, string? thinking, CancellationToken ct)
    {
        var contents = new JsonArray();
        foreach (var line in history)
            contents.Add(new JsonObject
            {
                ["role"] = line.FromAmiki ? "model" : "user",
                ["parts"] = new JsonArray(new JsonObject { ["text"] = line.ForModel() }),
            });
        contents.Add(UserText(message));

        var body = Request(instructions, contents);
        body["generationConfig"] = new JsonObject { ["temperature"] = 0.6 };
        if (!string.IsNullOrWhiteSpace(thinking))
            body["generationConfig"]!["thinkingConfig"] = new JsonObject { ["thinkingLevel"] = thinking };
        if (tools.Count > 0) body["tools"] = new JsonArray(new JsonObject { ["functionDeclarations"] = Declarations(tools) });

        var model = options.Value.Model;
        LastNote = null;
        for (var round = 0; ; round++)
        {
            JsonObject content;
            try
            {
                content = await SendAsync(model, body, ct);
            }
            catch (LanguageModelException ex) when (ex.RateLimited && model != options.Value.LightModel)
            {
                // Out of quota on the chat model: carry on with the light one, which has its own.
                // (Thinking settings are for the chat model, so they're left out.)
                model = options.Value.LightModel;
                (body["generationConfig"] as JsonObject)?.Remove("thinkingConfig");
                LastNote = $"answered by {model}: {ex.Message}";
                content = await SendAsync(model, body, ct);
            }
            var calls = content["parts"]?.AsArray()
                .Select(p => p?["functionCall"])
                .OfType<JsonObject>()
                .ToList() ?? [];
            if (calls.Count == 0) return Text(content) ?? throw new LanguageModelException("Gemini sent back an empty answer.");
            if (round >= MaxToolRounds) throw new LanguageModelException("Amiki got stuck looking things up. Try asking more simply.");

            // Her turn goes back exactly as she sent it (it can carry signatures Gemini checks), then the results.
            contents.Add(content.DeepClone());
            var results = new JsonArray();
            foreach (var call in calls)
            {
                var name = call["name"]?.GetValue<string>() ?? "";
                var args = call["args"] as JsonObject ?? [];
                JsonNode result;
                try { result = await runTool(new ToolCall(name, (JsonObject)args.DeepClone()), ct); }
                catch (ToolException ex) { result = new JsonObject { ["error"] = ex.Message }; }
                results.Add(new JsonObject
                {
                    ["functionResponse"] = new JsonObject { ["name"] = name, ["response"] = new JsonObject { ["result"] = result } },
                });
            }
            contents.Add(new JsonObject { ["role"] = "user", ["parts"] = results });
        }
    }

    private static JsonObject Request(string instructions, JsonArray contents) => new()
    {
        ["systemInstruction"] = new JsonObject { ["parts"] = new JsonArray(new JsonObject { ["text"] = instructions }) },
        ["contents"] = contents,
    };

    private static JsonObject UserText(string text) => new()
    {
        ["role"] = "user",
        ["parts"] = new JsonArray(new JsonObject { ["text"] = text }),
    };

    private static JsonArray Declarations(IEnumerable<ToolDefinition> tools) => new(tools.Select(t =>
    {
        var declaration = new JsonObject { ["name"] = t.Name, ["description"] = t.Description };
        if (t.Parameters is not null) declaration["parameters"] = ToGeminiSchema(t.Parameters);
        return (JsonNode)declaration;
    }).ToArray());

    /// <summary>Waits before each retry when Gemini is busy (the free tier often says "high demand" for a moment).</summary>
    internal static TimeSpan[] RetryDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)];

    /// <summary>Sends a request and returns the first candidate's content (role + parts).</summary>
    private async Task<JsonObject> SendAsync(string model, JsonObject body, CancellationToken ct)
    {
        var key = ApiKey ?? throw new LanguageModelException("The assistant isn't set up: add a Gemini API key in Amiki's settings.");

        var json = body.ToJsonString();
        HttpResponseMessage response;
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{Endpoint}{Uri.EscapeDataString(model)}:generateContent")
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            };
            request.Headers.Add("x-goog-api-key", key);
            try
            {
                response = await http.SendAsync(request, ct);
            }
            catch (HttpRequestException)
            {
                throw new LanguageModelException("Couldn't reach Gemini. Try again in a moment.");
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new LanguageModelException("Gemini took too long to answer. Try again, or ask something smaller.");
            }

            // Busy or briefly down: wait and try again, a couple of times.
            var busy = response.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.InternalServerError or HttpStatusCode.GatewayTimeout;
            if (!busy || attempt >= RetryDelays.Length) break;
            response.Dispose();
            await Task.Delay(RetryDelays[attempt], ct);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                throw new LanguageModelException("Gemini is very busy right now. Try again in a minute.");
            var answer = await response.Content.ReadAsStringAsync(ct);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                throw new LanguageModelException(LimitMessage(answer, model), rateLimited: true);
            if (!response.IsSuccessStatusCode)
                throw new LanguageModelException($"Gemini said no ({(int)response.StatusCode}): {ErrorMessage(answer)}");

            var candidate = JsonNode.Parse(answer)?["candidates"]?[0];
            if (candidate?["content"] is JsonObject content) return content;
            var reason = candidate?["finishReason"]?.GetValue<string>();
            throw new LanguageModelException(reason is "SAFETY" or "PROHIBITED_CONTENT"
                ? "Gemini declined to answer that one."
                : "Gemini sent back an empty answer.");
        }
    }

    // All text parts of a reply, joined.
    private static string? Text(JsonObject content)
    {
        var text = string.Concat(content["parts"]?.AsArray().Select(p => p?["text"]?.GetValue<string>()) ?? []);
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    /// <summary>
    /// Which limit was hit and when it lifts, from Gemini's error details: the free tier has a
    /// per-minute and a per-day limit for each model (e.g. 20 a day for gemini-3.8-flash).
    /// </summary>
    internal static string LimitMessage(string json, string model)
    {
        try
        {
            var details = JsonNode.Parse(json)?["error"]?["details"]?.AsArray() ?? [];
            var violation = details.Select(d => d?["violations"]?[0]).OfType<JsonObject>().FirstOrDefault();
            var daily = violation?["quotaId"]?.GetValue<string>().Contains("PerDay", StringComparison.OrdinalIgnoreCase) == true;
            var limit = violation?["quotaValue"]?.ToString();
            var retry = details.Select(d => d?["retryDelay"]?.GetValue<string>()).OfType<string>().FirstOrDefault();
            var wait = retry is not null && double.TryParse(retry.TrimEnd('s'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var s)
                ? TimeSpan.FromSeconds(s) : (TimeSpan?)null;
            var when = wait switch
            {
                null => daily ? "tomorrow" : "in a minute",
                { TotalMinutes: < 2 } w => $"in about {Math.Max(1, (int)Math.Ceiling(w.TotalSeconds))} seconds",
                { TotalHours: < 2 } w => $"in about {(int)Math.Ceiling(w.TotalMinutes)} minutes",
                var w => $"in about {(int)Math.Round(w.Value.TotalHours)} hours",
            };
            return daily
                ? $"The free daily limit for {model}{(limit is null ? "" : $" ({limit} requests)")} is used up; it resets {when}."
                : $"Gemini's free per-minute limit for {model} was reached; try again {when}.";
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return "Gemini's free-tier limit was reached. Try again later.";
        }
    }

    private static string ErrorMessage(string json)
    {
        try { return JsonNode.Parse(json)?["error"]?["message"]?.GetValue<string>() ?? "unknown error"; }
        catch (JsonException) { return "unknown error"; }
    }

    /// <summary>Gemini spells schema types in capitals ("OBJECT", "STRING"); everything else carries over.</summary>
    internal static JsonNode ToGeminiSchema(JsonNode node) => node switch
    {
        JsonObject obj => new JsonObject(obj.Select(p => KeyValuePair.Create(p.Key,
            p.Key == "type" && p.Value is JsonValue v && v.TryGetValue<string>(out var type)
                ? JsonValue.Create(type.ToUpperInvariant())
                : p.Value is null ? null : ToGeminiSchema(p.Value)))),
        JsonArray array => new JsonArray(array.Select(item => item is null ? null : ToGeminiSchema(item)).ToArray()),
        _ => node.DeepClone(),
    };
}
