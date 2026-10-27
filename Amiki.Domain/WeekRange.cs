namespace Amiki.Domain;

/// <summary>
/// A Monday-to-Sunday week. <see cref="End"/> is the next Monday (not included). Used by the
/// weekly review, Money's week view and Amiki's nudges. On the server, use <see cref="Of"/> with
/// the server's clock, never <see cref="Current"/> (that's the device's today).
/// </summary>
public readonly record struct WeekRange(DateTime Start)
{
    public DateTime End => Start.AddDays(7);
    public DateTime LastDay => Start.AddDays(6);

    public static WeekRange Of(DateTime day)
    {
        var date = day.Date;
        var sinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return new WeekRange(date.AddDays(-sinceMonday));
    }

    public static WeekRange Current => Of(DateTime.Today);

    public WeekRange Previous => new(Start.AddDays(-7));
    public WeekRange Next => new(Start.AddDays(7));
    public bool IsCurrent => this == Current;

    public bool Contains(DateTime moment) => moment >= Start && moment < End;

    public IEnumerable<DateTime> Days
    {
        get
        {
            var start = Start;
            return Enumerable.Range(0, 7).Select(i => start.AddDays(i));
        }
    }

    /// <summary>"Oct 6 – 12", or "Dec 29 – Jan 4" across a month, with the year when it isn't this year.</summary>
    public string Label
    {
        get
        {
            var year = LastDay.Year != DateTime.Today.Year ? $", {LastDay.Year}" : "";
            return Start.Month == LastDay.Month
                ? $"{Start:MMM d} – {LastDay:%d}{year}"
                : $"{Start:MMM d} – {LastDay:MMM d}{year}";
        }
    }
}
