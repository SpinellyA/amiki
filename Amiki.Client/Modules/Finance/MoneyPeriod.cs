using Amiki.Components;

namespace Amiki.Modules.Finance;

/// <summary>
/// The stretch of time the Money page shows: a calendar month, or a Monday-to-Sunday week.
/// <see cref="End"/> is the first day after it (not included).
/// </summary>
public readonly record struct MoneyPeriod(PeriodLength Length, DateTime Start)
{
    public static MoneyPeriod Containing(PeriodLength length, DateTime day) => length == PeriodLength.Week
        ? new(length, WeekRange.Of(day).Start)
        : new(length, new DateTime(day.Year, day.Month, 1));

    public static MoneyPeriod Current(PeriodLength length) => Containing(length, DateTime.Today);

    public DateTime End => Length == PeriodLength.Week ? Start.AddDays(7) : Start.AddMonths(1);

    public MoneyPeriod Previous => Length == PeriodLength.Week ? this with { Start = Start.AddDays(-7) } : this with { Start = Start.AddMonths(-1) };
    public MoneyPeriod Next => Length == PeriodLength.Week ? this with { Start = Start.AddDays(7) } : this with { Start = Start.AddMonths(1) };

    public bool IsCurrent => this == Current(Length);

    public bool Contains(DateTime moment) => moment >= Start && moment < End;

    public IEnumerable<DateTime> Days
    {
        get
        {
            var start = Start;
            return Enumerable.Range(0, (End - Start).Days).Select(i => start.AddDays(i));
        }
    }

    /// <summary>
    /// The same stretch of time at the other length. The current week becomes the current month
    /// (and back); otherwise you land on the period the old one started in.
    /// </summary>
    public MoneyPeriod As(PeriodLength length) =>
        length == Length ? this
        : IsCurrent ? Current(length)
        : Containing(length, Start);

    /// <summary>"October 2026", or "Oct 12 – 18" for a week.</summary>
    public string Label => Length == PeriodLength.Week ? new WeekRange(Start).Label : Start.ToString("MMMM yyyy");

    /// <summary>For sentences: "October", "this week", "the week of Oct 5".</summary>
    public string Name =>
        Length == PeriodLength.Month ? Start.ToString("MMMM")
        : IsCurrent ? "this week"
        : $"the week of {Start:MMM d}";
}
