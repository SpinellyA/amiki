using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Amiki.Modules.Assistant;
using Microsoft.Extensions.Options;

namespace Amiki.Api.Assistant;

/// <summary>
/// Google's Gemini API (generateContent). The key goes in a header, never in the URL, so it can't
/// end up in logs.
/// </summary>
public sealed class GeminiModel(HttpClient http, IOptions<GeminiOptions> options) : ILanguageModel
{
    private const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/models/";

    /// <summary>Most lookups a single reply may make, so a confused model can't loop forever on the free tier.</summary>
    internal const int MaxToolRounds = 5;

    public string Description => $"Gemini · chat: {options.Value.Model} · background: {options.Value.LightModel}";

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

        for (var round = 0; ; round++)
        {
            var content = await SendAsync(options.Value.Model, body, ct);
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
        var settings = options.Value;
        if (!settings.Enabled) throw new LanguageModelException("The assistant isn't set up: add a Gemini API key (Gemini__ApiKey).");

        var json = body.ToJsonString();
        HttpResponseMessage response;
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{Endpoint}{Uri.EscapeDataString(model)}:generateContent")
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            };
            request.Headers.Add("x-goog-api-key", settings.ApiKey);
            try
            {
                response = await http.SendAsync(request, ct);
            }
            catch (HttpRequestException)
            {
                throw new LanguageModelException("Couldn't reach Gemini. Try again in a moment.");
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
                throw new LanguageModelException("Gemini's free-tier limit was reached. Try again in a minute.", rateLimited: true);
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
