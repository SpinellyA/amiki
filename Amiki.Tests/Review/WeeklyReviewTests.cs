using Amiki.Modules.Finance;
using Amiki.Modules.Ideas;
using Amiki.Modules.Inbox;
using Amiki.Modules.Review;
using Amiki.Modules.Tasks;

namespace Amiki.Tests.Review;

public class WeeklyReviewTests
{
    // Monday Oct 12 – Sunday Oct 18, 2026.
    private static readonly WeekRange Week = new(new DateTime(2026, 10, 12));
    private static readonly DateTime Thursday = new(2026, 10, 15);

    [Theory]
    [InlineData("2026-10-12")] // Monday
    [InlineData("2026-10-15")] // Thursday
    [InlineData("2026-10-18")] // Sunday
    public void Any_day_belongs_to_the_week_starting_that_Monday(string day)
    {
        Assert.Equal(new DateTime(2026, 10, 12), WeekRange.Of(DateTime.Parse(day)).Start);
    }

    [Fact]
    public void A_week_includes_late_Sunday_but_not_the_next_Monday()
    {
        Assert.True(Week.Contains(new DateTime(2026, 10, 18, 23, 59, 0)));
        Assert.False(Week.Contains(new DateTime(2026, 10, 19)));
    }

    [Fact]
    public void A_weeks_note_always_gets_the_same_id()
    {
        var a = WeeklyNote.For(Week.Start);
        var b = WeeklyNote.For(Week.Start);

        Assert.Equal(a.Id, b.Id);
        Assert.NotEqual(a.Id, WeeklyNote.For(Week.Next.Start).Id);
        Assert.Null(a.Validate());
    }

    [Fact]
    public void A_note_must_start_on_a_Monday_and_match_its_id()
    {
        Assert.NotNull(new WeeklyNote { Id = WeeklyNote.IdFor(Thursday), WeekStart = Thursday }.Validate());
        Assert.NotNull(new WeeklyNote { Id = Guid.NewGuid(), WeekStart = Week.Start }.Validate());
    }

    [Fact]
    public void Summary_counts_the_weeks_tasks()
    {
        TaskItem[] tasks =
        [
            new() { Title = "done this week", CompletedAt = Thursday.AddHours(10) },
            new() { Title = "done last week", CompletedAt = Week.Start.AddDays(-2) },
            new() { Title = "overdue", Due = Thursday.AddDays(-1) },
            new() { Title = "due next week", Due = Week.Next.Start.AddDays(2) },
            new() { Title = "due later", Due = Week.Next.End.AddDays(1) },
        ];

        var summary = Build(tasks: tasks);

        Assert.Equal(["done this week"], summary.TasksDone.Select(t => t.Title));
        Assert.Equal(["overdue"], summary.Overdue.Select(t => t.Title));
        Assert.Equal(["due next week"], summary.DueNextWeek.Select(t => t.Title));
    }

    [Fact]
    public void Summary_flags_parked_ideas_untouched_for_a_month()
    {
        Idea[] ideas =
        [
            new() { Text = "old parked", CreatedAt = Week.Start.AddDays(-40) },
            new() { Text = "new parked", CreatedAt = Week.Start.AddDays(-3) },
            new() { Text = "old but started", CreatedAt = Week.Start.AddDays(-40), Status = IdeaStatus.InProgress },
            new() { Text = "finished this week", Status = IdeaStatus.Done, DoneAt = Thursday, CreatedAt = Thursday.AddDays(-50) },
        ];

        var summary = Build(ideas: ideas);

        Assert.Equal(["old parked"], summary.StaleParked.Select(i => i.Text));
        Assert.Equal(["finished this week"], summary.IdeasFinished.Select(i => i.Text));
    }

    [Fact]
    public void Summary_compares_spending_with_the_week_before()
    {
        Transaction Spend(string category, decimal amount) => new() { Kind = TxKind.Expense, Category = category, Amount = amount, Date = Thursday };

        var summary = Build(
            thisWeek: [Spend("Food", 500), Spend("Food", 200), Spend("Transport", 100), new() { Kind = TxKind.Income, Category = "Allowance", Amount = 1500, Date = Thursday }],
            lastWeek: [Spend("Food", 300)]);

        Assert.Equal(800, summary.Spent);
        Assert.Equal(1500, summary.Received);
        Assert.Equal(300, summary.SpentLastWeek);
        var food = summary.TopSpending[0];
        Assert.Equal(("Food", 700m, 300m), (food.Name, food.ThisWeek, food.LastWeek));
    }

    private static WeeklySummary Build(
        IEnumerable<TaskItem>? tasks = null,
        IEnumerable<Idea>? ideas = null,
        IEnumerable<Transaction>? thisWeek = null,
        IEnumerable<Transaction>? lastWeek = null) =>
        WeeklySummary.Build(Week, Thursday, Array.Empty<InboxItem>(), tasks ?? [], ideas ?? [], thisWeek ?? [], lastWeek ?? []);
}
