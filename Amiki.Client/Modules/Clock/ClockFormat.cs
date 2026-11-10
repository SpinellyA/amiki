namespace Amiki.Modules.Clock;

/// <summary>How the clock writes times and lengths: "2:00 PM", "1h 30m".</summary>
public static class ClockFormat
{
    public static string Time(int minute) =>
        minute >= TimeBlock.MinutesInDay ? "midnight" : DateTime.MinValue.AddMinutes(minute).ToString("h:mm tt");

    public static string Range(TimeBlock block) => $"{Time(block.StartMinute)} – {Time(block.EndMinute)}";

    public static string Length(int minutes) => (minutes / 60, minutes % 60) switch
    {
        (0, var m) => $"{m}m",
        (var h, 0) => $"{h}h",
        (var h, var m) => $"{h}h {m}m",
    };

    public static int MinuteOf(DateTime moment) => moment.Hour * 60 + moment.Minute;
}
