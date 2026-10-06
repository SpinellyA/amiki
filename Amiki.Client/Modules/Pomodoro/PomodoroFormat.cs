namespace Amiki.Modules.Pomodoro;

public static class PomodoroFormat
{
    /// <summary>"24:59", or "1:05:00" past an hour. Rounds up, so it reads 0:01 until the very end.</summary>
    public static string Clock(TimeSpan left)
    {
        var seconds = (long)Math.Ceiling(left.TotalSeconds);
        var t = TimeSpan.FromSeconds(seconds);
        return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes}:{t.Seconds:00}";
    }

    public static string Name(PomodoroPhase phase) => phase switch
    {
        PomodoroPhase.ShortBreak => "Short break",
        PomodoroPhase.LongBreak => "Long break",
        _ => "Focus",
    };

    public static string CssClass(PomodoroPhase phase) => phase == PomodoroPhase.Focus ? "focus" : "rest";
}
