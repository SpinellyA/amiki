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
