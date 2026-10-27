using Amiki.Application.Assistant;
using Amiki.Infrastructure.Assistant;
using Amiki.Modules.Assistant;

namespace Amiki.Tests.Assistant;

public class NudgeTests
{
    // Wednesday Oct 21, 2026; its week starts Monday Oct 19.
    private static readonly DateTime Wednesday = new(2026, 10, 21);

    private static NudgeInput Input(
        DateTime? today = null,
        IReadOnlyList<(string, DateTime?)>? tasks = null,
        IReadOnlyList<DateTime>? ideas = null,
        IReadOnlyList<(string, DateTime?)>? accounts = null,
        IReadOnlyList<(DateTime, string, decimal)>? spending = null,
        int inbox = 0,
        IReadOnlyList<DateTime>? reviewed = null,
        bool seeAll = true) =>
        new(today ?? Wednesday, tasks ?? [], ideas ?? [], accounts ?? [], spending ?? [], inbox, reviewed ?? [],
            seeAll, seeAll, seeAll, seeAll, seeAll);

    private static string[] Kinds(NudgeInput input) => NudgeRules.From(input).Select(n => n.Kind).ToArray();

    [Fact]
    public void A_quiet_day_has_no_nudges()
    {
        Assert.Empty(NudgeRules.From(Input()));
    }

    [Fact]
    public void Overdue_and_due_today_tasks_come_first()
    {
        var nudges = NudgeRules.From(Input(tasks: [("Lab 3", Wednesday.AddDays(-3)), ("Quiz", Wednesday), ("Later", Wednesday.AddDays(4)), ("Someday", null)]));

        Assert.Equal(["overdue", "due-today"], nudges.Select(n => n.Kind));
        Assert.Equal("1 task overdue (oldest: “Lab 3”)", nudges[0].Text);
        Assert.Equal("Due today: “Quiz”", nudges[1].Text);
    }

    [Fact]
    public void Sunday_asks_for_this_weeks_review_and_Monday_for_last_weeks()
    {
        var sunday = new DateTime(2026, 10, 25);
        var monday = new DateTime(2026, 10, 26);

        Assert.Equal(["review"], Kinds(Input(today: sunday)));
        Assert.Empty(Kinds(Input(today: sunday, reviewed: [new DateTime(2026, 10, 19)])));
        Assert.Equal(["review"], Kinds(Input(today: monday)));
        Assert.Empty(Kinds(Input(today: monday, reviewed: [new DateTime(2026, 10, 19)])));
        Assert.Empty(Kinds(Input(today: Wednesday)));
    }

    [Fact]
    public void Accounts_not_checked_in_two_weeks_are_named_together()
    {
        var nudges = NudgeRules.From(Input(accounts: [("Landbank", Wednesday.AddDays(-16)), ("Cash", null), ("GCash", Wednesday.AddDays(-2))]));

        Assert.Equal("Cash and Landbank haven't been checked against the real balance in 2+ weeks.", nudges.Single().Text);
    }

    [Fact]
    public void Spending_well_above_the_usual_week_is_flagged()
    {
        // Food: about ₱600 a week for four weeks, then ₱1,400 so far this week. Transport: normal.
        var spending = Enumerable.Range(1, 4)
            .SelectMany(w => new[] { (Wednesday.AddDays(-7 * w), "Food", 600m), (Wednesday.AddDays(-7 * w), "Transport", 300m) })
            .Append((Wednesday, "Food", 1400m))
            .Append((Wednesday, "Transport", 320m))
            .ToList();

        var nudge = NudgeRules.From(Input(spending: spending)).Single();

        Assert.Equal(("spending", "Food: ₱1,400 this week, usually about ₱600."), (nudge.Kind, nudge.Text));
    }

    [Fact]
    public void Small_bumps_in_spending_are_not_worth_a_nudge()
    {
        var spending = new[] { (Wednesday.AddDays(-7), "Load", 100m), (Wednesday, "Load", 250m) };

        Assert.Empty(NudgeRules.From(Input(spending: spending)));
    }

    [Fact]
    public void Stale_ideas_and_a_full_inbox_only_count_past_a_threshold()
    {
        var old = Wednesday.AddDays(-40);

        Assert.Empty(Kinds(Input(ideas: [old, old], inbox: 4)));
        Assert.Equal(["ideas", "inbox"], Kinds(Input(ideas: [old, old, old], inbox: 5)));
    }

    [Fact]
    public void Areas_she_cant_see_never_produce_nudges()
    {
        var input = Input(tasks: [("Lab 3", Wednesday.AddDays(-3))], inbox: 9, accounts: [("Cash", null)], seeAll: false);

        Assert.Empty(NudgeRules.From(input));
        Assert.DoesNotContain("Open tasks", BriefingService.Facts(input, []));
        Assert.DoesNotContain("Spent", BriefingService.Facts(input, []));
    }

    [Fact]
    public void Her_daily_note_is_told_to_stick_to_the_facts()
    {
        var instructions = BriefingService.Instructions(new AssistantSettings { AboutMe = "Sem ends Dec 12." }, Wednesday.AddHours(8));

        Assert.Contains("don't invent", instructions);
        Assert.Contains("Sem ends Dec 12.", instructions);
        Assert.Contains("Wednesday, October 21, 8:00 AM", instructions);
    }

    [Fact]
    public void Daily_notes_have_one_id_per_day()
    {
        Assert.Equal(DailyBriefing.IdFor(Wednesday), DailyBriefing.For(Wednesday.AddHours(9), "Hi", Wednesday).Id);
        Assert.Null(DailyBriefing.For(Wednesday, "Hi", Wednesday).Validate());
    }

    [Fact]
    public async Task Remembering_is_a_suggestion_too()
    {
        var proposals = new AssistantProposals(null!);

        Assert.Contains(proposals.For(new AssistantSettings { SeeTasks = false, SeeMoney = false, SeeIdeas = false, SeeInbox = false }), t => t.Name == "propose_remember");
        Assert.Empty(proposals.For(new AssistantSettings { CanPropose = false }));

        var p = await proposals.ProposeAsync(new ToolCall("propose_remember", new() { ["fact"] = "Sem ends Dec 12." }), Wednesday, CancellationToken.None);
        Assert.Equal("Remember: “Sem ends Dec 12.”", p.Describe());
    }
}
