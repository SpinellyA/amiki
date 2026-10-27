using System.Net;
using System.Text.Json.Nodes;
using Amiki.Application.Assistant;
using Amiki.Infrastructure.Assistant;
using Amiki.Api.Users;
using Amiki.Application.People;
using Amiki.Modules.Assistant;
using Microsoft.Extensions.Options;

namespace Amiki.Tests.Assistant;

public class AssistantChatTests
{
    private static readonly DateTime Now = new(2026, 10, 21, 20, 30, 0);

    // ---------- What she's told ----------

    [Fact]
    public void She_is_always_told_the_diary_is_off_limits()
    {
        var prompt = AssistantPrompt.Build(new AssistantSettings(), Now, new AssistantTools(null!).For(new AssistantSettings()));

        Assert.Contains("can't see the user's diary", prompt);
        Assert.Contains("Wednesday, October 21, 2026", prompt);
    }

    [Fact]
    public void About_me_and_personality_go_into_her_instructions()
    {
        var settings = new AssistantSettings { AboutMe = "3rd-year CS student.", Personality = "A blunt coach." };

        var prompt = AssistantPrompt.Build(settings, Now, []);

        Assert.Contains("3rd-year CS student.", prompt);
        Assert.Contains("A blunt coach.", prompt);
        Assert.DoesNotContain(AssistantPrompt.DefaultPersonality, prompt);
        Assert.Contains("can't look at any of the user's data", prompt);
    }

    [Fact]
    public void Thinking_is_quick_by_default_and_must_be_a_known_level()
    {
        Assert.Equal(ThinkingLevels.Low, new AssistantSettings().Thinking);
        Assert.Null(new AssistantSettings { Thinking = ThinkingLevels.ModelDefault }.Validate());
        Assert.NotNull(new AssistantSettings { Thinking = "ultra" }.Validate());
    }

    // ---------- What she can look up ----------

    [Fact]
    public void Tools_follow_the_switches_and_there_is_never_a_diary_tool()
    {
        var tools = new AssistantTools(null!);

        var all = tools.For(new AssistantSettings()).Select(t => t.Area).Distinct().ToList();
        var noMoney = tools.For(new AssistantSettings { SeeMoney = false }).Select(t => t.Area).ToList();
        var nothing = tools.For(new AssistantSettings { SeeTasks = false, SeeIdeas = false, SeeMoney = false, SeeInbox = false, SeeWeeklyNotes = false });

        Assert.Equal(["tasks", "ideas", "money", "inbox", "weekly notes"], all);
        Assert.DoesNotContain("money", noMoney);
        Assert.Empty(nothing);
        Assert.DoesNotContain(tools.For(new AssistantSettings()), t => t.Name.Contains("diary"));
    }

    [Fact]
    public async Task A_tool_thats_switched_off_wont_run_even_if_asked()
    {
        var tools = new AssistantTools(null!);
        var allowed = tools.For(new AssistantSettings { SeeMoney = false });

        await Assert.ThrowsAsync<ToolException>(() =>
            tools.RunAsync(new ToolCall("account_balances", []), allowed, Now.Date, CancellationToken.None));
    }

    [Fact]
    public void Date_ranges_are_inclusive_and_default_to_this_month()
    {
        var (from, to) = AssistantTools.Range(new JsonObject { ["from"] = "2026-10-01", ["to"] = "2026-10-31" }, Now.Date);
        var (defaultFrom, defaultTo) = AssistantTools.Range([], Now.Date);

        Assert.Equal((new DateTime(2026, 10, 1), new DateTime(2026, 11, 1)), (from, to));
        Assert.Equal((new DateTime(2026, 10, 1), new DateTime(2026, 10, 22)), (defaultFrom, defaultTo));
        Assert.Throws<ToolException>(() => AssistantTools.Range(new JsonObject { ["from"] = "2026-10-10", ["to"] = "2026-10-01" }, Now.Date));
        Assert.Throws<ToolException>(() => AssistantTools.Range(new JsonObject { ["from"] = "last week" }, Now.Date));
    }

    [Fact]
    public void Spending_by_week_finds_the_most_expensive_week_and_what_drove_it()
    {
        AssistantTools.Flow Spent(DateTime day, decimal amount, string category, string note) => new(day, Amiki.Domain.Finance.TxKind.Expense, amount, category, "Cash", note);

        var result = AssistantTools.PerPeriod(
        [
            Spent(new DateTime(2026, 9, 14), 80, "Food", "Lunch"),                 // week of Sep 14
            Spent(new DateTime(2026, 9, 23), 3000, "School", "Tuition installment"), // week of Sep 21
            Spent(new DateTime(2026, 9, 25), 120, "Food", "Milk tea"),
            new(new DateTime(2026, 9, 26), Amiki.Domain.Finance.TxKind.Income, 1500, "Allowance", "Cash", "Baon"),
        ], byMonth: false);

        var top = result["most_spent"]!;
        Assert.Equal("2026-09-21 (Mon)", top["week_of"]!.GetValue<string>());
        Assert.Equal(3120m, top["spent"]!.GetValue<decimal>());
        Assert.Equal(1500m, top["received"]!.GetValue<decimal>());
        Assert.Equal("Tuition installment", top["biggest"]![0]!["note"]!.GetValue<string>());
        Assert.Equal(2, result["periods"]!.AsArray().Count);
    }

    // ---------- Talking to Gemini ----------

    private sealed class ScriptedGemini(params string[] replies) : HttpMessageHandler
    {
        private int _next;
        public List<JsonNode> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(JsonNode.Parse(await request.Content!.ReadAsStringAsync(ct))!);
            var body = replies[Math.Min(_next++, replies.Length - 1)];
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
        }
    }

    private static GeminiModel Model(ScriptedGemini fake) =>
        new(new HttpClient(fake), Options.Create(new GeminiOptions { ApiKey = "k", Model = "m" }), new UserContext());

    private const string CallsBalances = """{"candidates":[{"content":{"role":"model","parts":[{"functionCall":{"name":"account_balances","args":{}},"thoughtSignature":"sig"}]}}]}""";
    private const string SaysDone = """{"candidates":[{"content":{"role":"model","parts":[{"text":"You have ₱5,089 in Cash."}]}}]}""";

    private static readonly ToolDefinition Balances = new("account_balances", "balances", null, "money");

    [Fact]
    public async Task Runs_the_tool_she_asks_for_then_returns_her_answer()
    {
        var fake = new ScriptedGemini(CallsBalances, SaysDone);
        var called = new List<string>();

        var reply = await Model(fake).ChatAsync("rules", [new ChatLine(false, "hi"), new ChatLine(true, "hello!")], "how much cash?", [Balances],
            (call, _) => { called.Add(call.Name); return Task.FromResult<JsonNode>(new JsonObject { ["Cash"] = 5089 }); },
            ThinkingLevels.Low, CancellationToken.None);

        Assert.Equal("You have ₱5,089 in Cash.", reply);
        Assert.Equal(["account_balances"], called);

        var first = fake.Requests[0];
        Assert.Equal("account_balances", first["tools"]![0]!["functionDeclarations"]![0]!["name"]!.GetValue<string>());
        Assert.Equal("low", first["generationConfig"]!["thinkingConfig"]!["thinkingLevel"]!.GetValue<string>()); // quick replies by default
        Assert.Equal(["user", "model", "user"], first["contents"]!.AsArray().Select(c => c!["role"]!.GetValue<string>()));

        // Second request: her tool call sent back as-is (signature included), then the result.
        var contents = fake.Requests[1]["contents"]!.AsArray();
        Assert.Equal("sig", contents[3]!["parts"]![0]!["thoughtSignature"]!.GetValue<string>());
        Assert.Equal(5089, contents[4]!["parts"]![0]!["functionResponse"]!["response"]!["result"]!["Cash"]!.GetValue<int>());
    }

    [Fact]
    public async Task A_tool_error_goes_back_to_her_instead_of_failing_the_chat()
    {
        var fake = new ScriptedGemini(CallsBalances, SaysDone);

        await Model(fake).ChatAsync("rules", [], "cash?", [Balances], (_, _) => throw new ToolException("bad dates"), "low", CancellationToken.None);

        Assert.Equal("bad dates", fake.Requests[1]["contents"]![2]!["parts"]![0]!["functionResponse"]!["response"]!["result"]!["error"]!.GetValue<string>());
    }

    private const string DailyLimit = """
        {"error":{"code":429,"message":"quota","details":[
          {"@type":"type.googleapis.com/google.rpc.QuotaFailure","violations":[{"quotaId":"GenerateRequestsPerDayPerProjectPerModel-FreeTier","quotaValue":"20"}]},
          {"@type":"type.googleapis.com/google.rpc.RetryInfo","retryDelay":"57821s"}]}}
        """;

    [Fact]
    public void Limits_say_which_one_and_when_it_lifts()
    {
        Assert.Equal("The free daily limit for gemini-3.8-flash (20 requests) is used up; it resets in about 16 hours.",
            GeminiModel.LimitMessage(DailyLimit, "gemini-3.8-flash"));
        Assert.Equal("Gemini's free per-minute limit for m was reached; try again in about 40 seconds.",
            GeminiModel.LimitMessage("""{"error":{"details":[{"violations":[{"quotaId":"GenerateRequestsPerMinute"}]},{"retryDelay":"39.2s"}]}}""", "m"));
        Assert.Equal("Gemini's free-tier limit was reached. Try again later.", GeminiModel.LimitMessage("not json", "m"));
    }

    [Fact]
    public async Task When_the_chat_model_is_out_of_quota_the_light_model_answers()
    {
        var fake = new LimitedThenOk();
        var model = new GeminiModel(new HttpClient(fake), Options.Create(new GeminiOptions { ApiKey = "k", Model = "big", LightModel = "small" }), new UserContext());

        var reply = await model.ChatAsync("rules", [], "hi", [], (_, _) => throw new ToolException("none"), "low", CancellationToken.None);

        Assert.Equal("You have ₱5,089 in Cash.", reply);
        Assert.Equal(["big", "small"], fake.Models);
        Assert.DoesNotContain("thinkingConfig", fake.LastBody!); // the chat model's thinking setting isn't sent to the backup
        Assert.StartsWith("answered by small: The free daily limit", model.LastNote);
    }

    private sealed class LimitedThenOk : HttpMessageHandler
    {
        public List<string> Models { get; } = [];
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Models.Add(request.RequestUri!.AbsolutePath.Split('/')[^1].Split(':')[0]);
            LastBody = await request.Content!.ReadAsStringAsync(ct);
            return Models.Count == 1
                ? new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent(DailyLimit) }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(SaysDone) };
        }
    }

    [Fact]
    public async Task A_busy_moment_is_retried_before_giving_up()
    {
        GeminiModel.RetryDelays = [TimeSpan.Zero, TimeSpan.Zero];
        var busy = new BusyThenOk(failures: 2);

        var reply = await new GeminiModel(new HttpClient(busy), Options.Create(new GeminiOptions { ApiKey = "k", Model = "m" }), new UserContext())
            .ChatAsync("rules", [], "hi", [], (_, _) => throw new ToolException("none"), "low", CancellationToken.None);

        Assert.Equal("You have ₱5,089 in Cash.", reply);
        Assert.Equal(3, busy.Calls);
    }

    private sealed class BusyThenOk(int failures) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(++Calls <= failures
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("""{"error":{"message":"high demand"}}""") }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(SaysDone) });
    }

    [Fact]
    public async Task Stops_if_she_keeps_looking_things_up()
    {
        var fake = new ScriptedGemini(CallsBalances);

        await Assert.ThrowsAsync<LanguageModelException>(() => Model(fake).ChatAsync("rules", [], "cash?", [Balances],
            (_, _) => Task.FromResult<JsonNode>(new JsonObject()), "low", CancellationToken.None));
        Assert.Equal(GeminiModel.MaxToolRounds + 1, fake.Requests.Count);
    }
}
