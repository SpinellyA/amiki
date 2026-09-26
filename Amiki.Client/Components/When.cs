namespace Amiki.Components;

/// <summary>Compact timestamps for lists: clock time today, weekday this week, date otherwise.</summary>
public static class When
{
    public static string Short(DateTime t)
    {
        var days = (DateTime.Today - t.Date).Days;
        return days switch
        {
            0 => t.ToString("h:mm tt"),
            1 => $"Yesterday {t:h:mm tt}",
            < 7 => t.ToString("ddd h:mm tt"),
            _ => t.ToString("MMM d"),
        };
    }

    public static string Day(DateTime date) => (DateTime.Today - date.Date).Days switch
    {
        0 => "Today",
        1 => "Yesterday",
        < 7 => date.ToString("dddd"),
        _ => date.ToString("dddd, MMM d"),
    };
}
