namespace Amiki.Modules.Pomodoro;

/// <summary>How long each part lasts, in minutes, and what happens when one ends.</summary>
/// <param name="RoundsBeforeLongBreak">Focus rounds in a set; the break after the last one is the long one.</param>
/// <param name="AutoStartNext">Start the next part by itself when one ends, instead of waiting for you.</param>
/// <param name="Chime">Play a short sound when time's up.</param>
public sealed record PomodoroSettings(
    int FocusMinutes = 25,
    int ShortBreakMinutes = 5,
    int LongBreakMinutes = 15,
    int RoundsBeforeLongBreak = 4,
    bool AutoStartNext = false,
    bool Chime = true)
{
    public const int MaxMinutes = 180;
    public const int MaxRounds = 12;

    public TimeSpan LengthOf(PomodoroPhase phase) => TimeSpan.FromMinutes(phase switch
    {
        PomodoroPhase.ShortBreak => ShortBreakMinutes,
        PomodoroPhase.LongBreak => LongBreakMinutes,
        _ => FocusMinutes,
    });

    /// <summary>The same settings with every number pulled into a usable range (e.g. a 0-minute focus becomes 1).</summary>
    public PomodoroSettings Clamped() => this with
    {
        FocusMinutes = Math.Clamp(FocusMinutes, 1, MaxMinutes),
        ShortBreakMinutes = Math.Clamp(ShortBreakMinutes, 1, MaxMinutes),
        LongBreakMinutes = Math.Clamp(LongBreakMinutes, 1, MaxMinutes),
        RoundsBeforeLongBreak = Math.Clamp(RoundsBeforeLongBreak, 1, MaxRounds),
    };
}
