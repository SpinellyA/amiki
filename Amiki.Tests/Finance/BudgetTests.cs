namespace Amiki.Tests.Finance;

public class BudgetTests
{
    private static readonly MoneyPeriod November = new(PeriodLength.Month, new DateTime(2026, 11, 1));
    private static readonly MoneyPeriod December = November.Next;
    private static readonly MoneyPeriod January = December.Next;

    // ---------- Which budget applies ----------

    [Fact]
    public void A_repeating_budget_carries_on_until_another_takes_over()
    {
        var book = new BudgetBook([Budget.For(November, 8000, repeats: true), Budget.For(January, 9000, repeats: true)]);

        Assert.Equal(8000, book.For(November)!.Amount);
        Assert.Equal(8000, book.For(December)!.Amount); // carried over
        Assert.False(book.HasOwn(December));
        Assert.Equal(9000, book.For(January)!.Amount);
        Assert.Null(book.For(November.Previous)); // nothing before the first one
    }

    [Fact]
    public void A_one_off_budget_only_covers_its_own_period()
    {
        var book = new BudgetBook([Budget.For(November, 8000, repeats: true), Budget.For(December, 15000, repeats: false)]);

        Assert.Equal(15000, book.For(December)!.Amount); // holiday month
        Assert.Equal(8000, book.For(January)!.Amount);   // back to the usual
    }

    [Fact]
    public void Turning_it_off_stops_it_from_then_on()
    {
        var book = new BudgetBook([Budget.For(November, 8000, repeats: true), Budget.For(December, 0, repeats: true)]);

        Assert.NotNull(book.For(November));
        Assert.Null(book.For(December));
        Assert.Null(book.For(January));
    }

    [Fact]
    public void Weeks_and_months_are_separate()
    {
        var week = new MoneyPeriod(PeriodLength.Week, new DateTime(2026, 11, 2));
        var book = new BudgetBook([Budget.For(November, 8000, repeats: true)]);

        Assert.Null(book.For(week));
        Assert.NotEqual(Budget.IdFor(week), Budget.IdFor(November));
    }

    [Fact]
    public void A_budget_has_to_start_its_period_and_carry_its_own_id()
    {
        Assert.Null(Budget.For(November, 8000, true).Validate());
        Assert.NotNull(new Budget { Id = Budget.IdFor(November), Length = PeriodLength.Month, Start = new DateTime(2026, 11, 5), Amount = 1 }.Validate());
        Assert.NotNull(new Budget { Id = Guid.NewGuid(), Length = PeriodLength.Month, Start = November.Start, Amount = 1 }.Validate());
        Assert.NotNull(Budget.For(November, -1, true).Validate());
    }

    // ---------- Pace ----------

    [Fact]
    public void Halfway_through_and_halfway_spent_is_on_track()
    {
        var pace = new BudgetPace(Limit: 3000, Spent: 1400, November, Today: new DateTime(2026, 11, 15));

        Assert.Equal(BudgetStatus.OnTrack, pace.Status);
        Assert.Equal((15, 16), (pace.DaysGone, pace.DaysLeft)); // the 15th counts as gone, and as still to spend in
        Assert.Equal(2800, pace.Projected);
        Assert.Equal(100, pace.PerDayLeft); // 1,600 left over 16 days
    }

    [Fact]
    public void Spending_fast_is_flagged_before_the_money_runs_out()
    {
        var pace = new BudgetPace(Limit: 3000, Spent: 2000, November, Today: new DateTime(2026, 11, 10));

        Assert.Equal(BudgetStatus.AtRisk, pace.Status);
        Assert.Equal(6000, pace.Projected);
        Assert.Equal(1000, pace.Left);
    }

    [Fact]
    public void Spending_more_than_the_budget_is_over()
    {
        var pace = new BudgetPace(Limit: 3000, Spent: 3500, November, Today: new DateTime(2026, 11, 20));

        Assert.Equal(BudgetStatus.Over, pace.Status);
        Assert.Equal(0, pace.PerDayLeft);
    }

    // ---------- Safe to spend today ----------

    // November: 30 days, a ₱3,000 budget is ₱100 a day, so the even pace is ₱1,000 by the end of Nov 10.
    private static BudgetPace OnTheTenth(decimal spent) => new(3000, spent, November, new DateTime(2026, 11, 10));

    [Fact]
    public void Whatever_you_didnt_spend_so_far_is_safe_to_spend_today()
    {
        Assert.Equal(300, OnTheTenth(700).SafeToday); // ₱300 under the pace
        Assert.Equal(1000, OnTheTenth(0).SafeToday);  // a quiet start carries over
    }

    [Fact]
    public void Ahead_of_the_pace_nothing_is_safe_until_it_catches_up()
    {
        Assert.Equal((0m, 1), (OnTheTenth(1000).SafeToday, OnTheTenth(1000).DaysUntilSafe)); // exactly on it: tomorrow
        Assert.Equal((0m, 6), (OnTheTenth(1500).SafeToday, OnTheTenth(1500).DaysUntilSafe)); // the pace passes ₱1,500 on the 16th
        Assert.Equal(0, OnTheTenth(3000).DaysUntilSafe); // not again this month
    }

    [Fact]
    public void Safe_today_never_goes_past_whats_left_and_only_counts_while_the_period_runs()
    {
        var lastDay = new BudgetPace(3000, 2900, November, new DateTime(2026, 11, 30));
        Assert.Equal(100, lastDay.SafeToday);

        Assert.Equal(0, new BudgetPace(3000, 0, December, new DateTime(2026, 11, 20)).SafeToday); // not started
        Assert.Equal(0, new BudgetPace(3000, 0, November, new DateTime(2026, 12, 2)).SafeToday);  // over
    }

    [Fact]
    public void A_future_period_is_upcoming_and_a_finished_one_just_within_or_over()
    {
        Assert.Equal(BudgetStatus.Upcoming, new BudgetPace(3000, 0, December, new DateTime(2026, 11, 20)).Status);

        var finished = new BudgetPace(3000, 2900, November, new DateTime(2026, 12, 3));
        Assert.Equal((BudgetStatus.OnTrack, 0, true), (finished.Status, finished.DaysLeft, finished.IsFinished));
    }
}
