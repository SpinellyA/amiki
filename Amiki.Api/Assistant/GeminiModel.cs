using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace Amiki.Api.Assistant;

/// <summary>
/// Google's Gemini API (generateContent) with a JSON-only answer. The key goes in a header,
/// never in the URL, so it can't end up in logs.
/// </summary>
public sealed class GeminiModel(HttpClient http, IOptions<GeminiOptions> options) : ILanguageModel
{
    private const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/models/";

    public async Task<string> AskForJsonAsync(string instructions, string input, JsonObject schema, CancellationToken ct)
    {
        var settings = options.Value;
        if (!settings.Enabled) throw new LanguageModelException("The assistant isn't set up: add a Gemini API key (Gemini__ApiKey).");

        var body = new JsonObject
        {
            ["systemInstruction"] = new JsonObject { ["parts"] = new JsonArray(new JsonObject { ["text"] = instructions }) },
            ["contents"] = new JsonArray(new JsonObject
            {
                ["role"] = "user",
                ["parts"] = new JsonArray(new JsonObject { ["text"] = input }),
            }),
            ["generationConfig"] = new JsonObject
            {
                ["temperature"] = 0.2,
                ["responseMimeType"] = "application/json",
                ["responseSchema"] = ToGeminiSchema(schema),
            },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{Endpoint}{Uri.EscapeDataString(settings.Model)}:generateContent")
        {
            Content = new StringContent(body.ToJsonString(), System.Text.Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("x-goog-api-key", settings.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, ct);
        }
        catch (HttpRequestException)
        {
            throw new LanguageModelException("Couldn't reach Gemini. Try again in a moment.");
        }

        using (response)
        {
            var json = await response.Content.ReadAsStringAsync(ct);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                throw new LanguageModelException("Gemini's free-tier limit was reached. Try again in a minute.", rateLimited: true);
            if (!response.IsSuccessStatusCode)
                throw new LanguageModelException($"Gemini said no ({(int)response.StatusCode}): {ErrorMessage(json)}");
            return AnswerText(json) ?? throw new LanguageModelException("Gemini sent back an empty answer.");
        }
    }

    // candidates[0].content.parts[*].text
    private static string? AnswerText(string json)
    {
        var root = JsonNode.Parse(json);
        var parts = root?["candidates"]?[0]?["content"]?["parts"]?.AsArray();
        var text = parts is null ? null : string.Concat(parts.Select(p => p?["text"]?.GetValue<string>()));
        return string.IsNullOrWhiteSpace(text) ? null : text;
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
