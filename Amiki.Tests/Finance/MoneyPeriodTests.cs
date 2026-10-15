using Amiki.Modules.Finance;

namespace Amiki.Tests.Finance;

public class MoneyPeriodTests
{
    private static readonly DateTime Thursday = new(2026, 10, 15);

    [Fact]
    public void A_month_runs_from_the_first_to_the_next_first()
    {
        var month = MoneyPeriod.Containing(PeriodLength.Month, Thursday);

        Assert.Equal(new DateTime(2026, 10, 1), month.Start);
        Assert.Equal(new DateTime(2026, 11, 1), month.End);
        Assert.Equal(31, month.Days.Count());
        Assert.Equal("October 2026", month.Label);
    }

    [Fact]
    public void A_week_runs_Monday_to_Sunday()
    {
        var week = MoneyPeriod.Containing(PeriodLength.Week, Thursday);

        Assert.Equal(new DateTime(2026, 10, 12), week.Start);
        Assert.Equal(new DateTime(2026, 10, 19), week.End);
        Assert.Equal(7, week.Days.Count());
        Assert.True(week.Contains(new DateTime(2026, 10, 18, 23, 0, 0)));
        Assert.False(week.Contains(week.End));
    }

    [Fact]
    public void Stepping_moves_by_the_periods_own_length()
    {
        var week = MoneyPeriod.Containing(PeriodLength.Week, Thursday);
        var month = MoneyPeriod.Containing(PeriodLength.Month, Thursday);

        Assert.Equal(new DateTime(2026, 10, 5), week.Previous.Start);
        Assert.Equal(new DateTime(2026, 10, 19), week.Next.Start);
        Assert.Equal(new DateTime(2026, 9, 1), month.Previous.Start);
    }

    [Fact]
    public void Switching_length_lands_where_the_old_period_started()
    {
        var pastWeek = MoneyPeriod.Containing(PeriodLength.Week, new DateTime(2026, 8, 3)); // Mon Aug 3
        var pastMonth = MoneyPeriod.Containing(PeriodLength.Month, new DateTime(2026, 7, 20));

        Assert.Equal(new DateTime(2026, 8, 1), pastWeek.As(PeriodLength.Month).Start);
        Assert.Equal(new DateTime(2026, 6, 29), pastMonth.As(PeriodLength.Week).Start); // the week with July 1
    }

    [Fact]
    public void Switching_from_the_current_period_stays_current()
    {
        Assert.True(MoneyPeriod.Current(PeriodLength.Month).As(PeriodLength.Week).IsCurrent);
        Assert.True(MoneyPeriod.Current(PeriodLength.Week).As(PeriodLength.Month).IsCurrent);
    }

    [Fact]
    public void Names_read_naturally_in_a_sentence()
    {
        Assert.Equal("this week", MoneyPeriod.Current(PeriodLength.Week).Name);
        Assert.Equal("the week of Sep 14", MoneyPeriod.Containing(PeriodLength.Week, new DateTime(2026, 9, 16)).Name);
        Assert.Equal("September", MoneyPeriod.Containing(PeriodLength.Month, new DateTime(2026, 9, 16)).Name);
    }
}
