namespace Amiki.Domain.Finance;

/// <summary>
/// Spending so far against a budget, and where it's heading: "at this rate" means spending the
/// rest of the period the way you have so far (days counted up to and including today).
/// </summary>
public readonly record struct BudgetPace(decimal Limit, decimal Spent, MoneyPeriod Period, DateTime Today)
{
    public int DaysTotal => (Period.End - Period.Start).Days;

    /// <summary>Days of the period gone by, today included.</summary>
    public int DaysGone =>
        Today.Date < Period.Start ? 0
        : Today.Date >= Period.End ? DaysTotal
        : (Today.Date - Period.Start).Days + 1;

    /// <summary>Days left to spend in, today included (today isn't over yet).</summary>
    public int DaysLeft => Today.Date >= Period.End ? 0 : DaysTotal - DaysGone + (DaysGone > 0 ? 1 : 0);

    public bool IsFinished => Today.Date >= Period.End;

    public decimal Left => Limit - Spent;

    /// <summary>What spending evenly would have used by now.</summary>
    public decimal ExpectedByNow => Math.Round(Limit * DaysGone / DaysTotal, 2);

    /// <summary>What you'll have spent by the end, at this rate.</summary>
    public decimal Projected => DaysGone == 0 ? Spent : Math.Round(Spent / DaysGone * DaysTotal, 2);

    /// <summary>What you can spend a day from now on and still stay within it.</summary>
    public decimal PerDayLeft => DaysLeft == 0 ? 0 : Math.Max(0, Math.Floor(Left / DaysLeft));

    /// <summary>
    /// How much more you can spend today and still be at or under the even pace by tonight (the
    /// marker on the budget bar). Days you spent less carry over; when you're ahead of the pace,
    /// it's zero until the pace catches up. Never more than what's left of the budget.
    /// </summary>
    public decimal SafeToday =>
        DaysGone == 0 || IsFinished ? 0 : Math.Max(0, Math.Floor(Math.Min(ExpectedByNow - Spent, Left)));

    /// <summary>
    /// When today's safe amount is zero: in how many days the even pace passes what you've spent
    /// (if you spend nothing more), so there's something safe to spend again. Zero when it's already
    /// safe today, or when that won't happen before the period ends.
    /// </summary>
    public int DaysUntilSafe
    {
        get
        {
            if (SafeToday > 0 || Limit <= 0 || DaysGone == 0 || IsFinished) return 0;
            var firstSafeDay = (int)Math.Floor(Spent * DaysTotal / Limit) + 1; // 1 = the period's first day
            // Less than a peso under the pace rounds down to nothing safe today, so it's tomorrow at the soonest.
            return firstSafeDay <= DaysTotal ? Math.Max(1, firstSafeDay - DaysGone) : 0;
        }
    }

    /// <summary>How much of the budget is used, 0 to 1 (more than 1 when over).</summary>
    public double Used => Limit <= 0 ? 0 : (double)(Spent / Limit);

    /// <summary>How far through the period you are, 0 to 1.</summary>
    public double TimeGone => (double)DaysGone / DaysTotal;

    public BudgetStatus Status =>
        Spent > Limit ? BudgetStatus.Over
        : DaysGone == 0 ? BudgetStatus.Upcoming
        : !IsFinished && Projected > Limit ? BudgetStatus.AtRisk
        : BudgetStatus.OnTrack;
}
