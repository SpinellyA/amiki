using System.Net;
using System.Text.Json.Nodes;
using Amiki.Api.Assistant;
using Amiki.Modules.Assistant;
using Amiki.Modules.Tasks;
using Microsoft.Extensions.Options;

namespace Amiki.Tests.Assistant;

public class GeminiTests
{
    private sealed class FakeGemini(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Sent { get; private set; }
        public string? SentBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Sent = request;
            SentBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(status) { Content = new StringContent(body) };
        }
    }

    private static GeminiModel Model(FakeGemini fake, string? key = "test-key") =>
        new(new HttpClient(fake), Options.Create(new GeminiOptions { ApiKey = key, Model = "gemini-test" }));

    private static readonly JsonObject Schema = new() { ["type"] = "object" };

    [Fact]
    public async Task Sends_the_key_in_a_header_and_reads_the_answer_text()
    {
        var fake = new FakeGemini(HttpStatusCode.OK, """{"candidates":[{"content":{"parts":[{"text":"[{\"id\":\"1\"}]"}]}}]}""");

        var answer = await Model(fake).AskForJsonAsync("rules", "notes", Schema, CancellationToken.None);

        Assert.Equal("""[{"id":"1"}]""", answer);
        Assert.Equal("test-key", fake.Sent!.Headers.GetValues("x-goog-api-key").Single());
        Assert.DoesNotContain("test-key", fake.Sent.RequestUri!.ToString());
        Assert.EndsWith("/models/gemini-test:generateContent", fake.Sent.RequestUri.AbsolutePath);
        Assert.Contains("\"responseMimeType\":\"application/json\"", fake.SentBody);
    }

    [Fact]
    public async Task The_free_tier_limit_is_reported_as_rate_limited()
    {
        var fake = new FakeGemini(HttpStatusCode.TooManyRequests, """{"error":{"message":"quota"}}""");

        var ex = await Assert.ThrowsAsync<LanguageModelException>(() => Model(fake).AskForJsonAsync("r", "n", Schema, CancellationToken.None));

        Assert.True(ex.RateLimited);
    }

    [Fact]
    public async Task Other_errors_pass_on_Geminis_message()
    {
        var fake = new FakeGemini(HttpStatusCode.BadRequest, """{"error":{"message":"API key not valid"}}""");

        var ex = await Assert.ThrowsAsync<LanguageModelException>(() => Model(fake).AskForJsonAsync("r", "n", Schema, CancellationToken.None));

        Assert.Contains("API key not valid", ex.Message);
        Assert.False(ex.RateLimited);
    }

    [Fact]
    public async Task Without_a_key_it_doesnt_call_out()
    {
        var fake = new FakeGemini(HttpStatusCode.OK, "{}");

        await Assert.ThrowsAsync<LanguageModelException>(() => Model(fake, key: null).AskForJsonAsync("r", "n", Schema, CancellationToken.None));

        Assert.Null(fake.Sent);
    }

    [Fact]
    public void Schema_types_are_capitalised_for_Gemini()
    {
        var schema = JsonNode.Parse("""{"type":"array","items":{"type":"object","properties":{"kind":{"type":"string","enum":["task"]}}}}""")!;

        var gemini = GeminiModel.ToGeminiSchema(schema).ToJsonString();

        Assert.Equal("""{"type":"ARRAY","items":{"type":"OBJECT","properties":{"kind":{"type":"STRING","enum":["task"]}}}}""", gemini);
    }

    [Fact]
    public void Reading_an_answer_keeps_known_notes_once_and_checks_them()
    {
        var gas = new TriageNote(Guid.NewGuid(), "bought gas 500");
        var quiz = new TriageNote(Guid.NewGuid(), "quiz thu #cmsc125");
        var context = new TriageContext(new DateTime(2026, 10, 21), ["Cash"], ["Transport", "Other"], ["Other income"], ["cmsc125"], []);
        var json = $$"""
            [
              {"id":"{{gas.Id}}","kind":"expense","title":"Gas","amount":500,"category":"Transport","account":"Cash","reason":"spent money"},
              {"id":"{{gas.Id}}","kind":"idea","title":"duplicate"},
              {"id":"{{quiz.Id}}","kind":"task","title":"Quiz","due":"2026-10-22","project":"cmsc125","priority":"high"},
              {"id":"{{Guid.NewGuid()}}","kind":"task","title":"not one of ours"}
            ]
            """;

        var suggestions = InboxTriageService.Read(json, [gas, quiz], context);

        Assert.Equal(2, suggestions.Count);
        Assert.Equal((TriageKind.Expense, 500m, "Transport"), (suggestions[0].Kind, suggestions[0].Amount!.Value, suggestions[0].Category));
        Assert.Equal((TriageKind.Task, new DateTime(2026, 10, 22), Priority.High), (suggestions[1].Kind, suggestions[1].Due!.Value, suggestions[1].Priority));
    }

    [Fact]
    public void A_garbled_answer_is_a_clear_error()
    {
        Assert.Throws<LanguageModelException>(() => InboxTriageService.Read("not json", [], new(DateTime.Today, [], [], [], [], [])));
    }
}
