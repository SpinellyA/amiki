namespace Amiki.Modules.Pomodoro;

/// <summary>
/// The Pomodoro timer: focus, short break, focus, … and a long break after every few rounds.
///
/// One instance for the whole app, so it keeps running while you use other pages. Time left is
/// worked out from when the current part ends, not by counting ticks, so it stays right even when
/// the browser slows timers down in a background tab.
/// </summary>
public sealed class PomodoroTimer : IDisposable
{
    private static readonly TimeSpan TickEvery = TimeSpan.FromMilliseconds(250);

    private readonly TimeProvider _clock;
    private ITimer? _ticker;
    private DateTimeOffset? _endsAt;   // set while running
    private TimeSpan _left;            // used while paused
    private long _shownSecond = -1;

    public PomodoroTimer(TimeProvider clock)
    {
        _clock = clock;
        Length = _left = Settings.LengthOf(Phase);
    }

    /// <summary>Raised when anything changes, and once a second while running.</summary>
    public event Action? Changed;

    /// <summary>Raised when a part runs out by itself (not when skipped), with the part that ended.</summary>
    public event Action<PomodoroPhase>? Finished;

    public PomodoroSettings Settings { get; private set; } = new();
    public PomodoroPhase Phase { get; private set; } = PomodoroPhase.Focus;

    /// <summary>How long the current part lasts, including minutes you added or took off.</summary>
    public TimeSpan Length { get; private set; }

    /// <summary>Focus rounds finished in the current set (back to 0 after the long break).</summary>
    public int RoundsDone { get; private set; }

    public int FocusSessionsToday => _countedOn == Today ? _focusToday : 0;
    private int _focusToday;
    private DateTime _countedOn;

    public bool IsRunning => _endsAt is not null;

    /// <summary>Started (running or paused partway): there's something to reset.</summary>
    public bool InProgress => IsRunning || _left != Length;

    public TimeSpan Remaining => _endsAt is { } end
        ? TimeSpan.FromTicks(Math.Max(0, (end - _clock.GetUtcNow()).Ticks))
        : _left;

    /// <summary>0 at the start of the part, 1 when it's over.</summary>
    public double Progress => Length <= TimeSpan.Zero ? 1 : Math.Clamp(1 - Remaining / Length, 0, 1);

    /// <summary>The part that comes after this one if it runs out now.</summary>
    public PomodoroPhase Next => Phase switch
    {
        PomodoroPhase.Focus => RoundsDone + 1 >= Settings.RoundsBeforeLongBreak ? PomodoroPhase.LongBreak : PomodoroPhase.ShortBreak,
        _ => PomodoroPhase.Focus,
    };

    private DateTime Today => _clock.GetLocalNow().Date;

    public void Start()
    {
        if (IsRunning) return;
        _endsAt = _clock.GetUtcNow() + _left; // with no time left, the next tick finishes the part
        _ticker ??= _clock.CreateTimer(_ => Tick(), null, TickEvery, TickEvery);
        Notify();
    }

    public void Pause()
    {
        if (!IsRunning) return;
        _left = Remaining;
        _endsAt = null;
        StopTicker();
        Notify();
    }

    public void Toggle()
    {
        if (IsRunning) Pause();
        else Start();
    }

    /// <summary>Back to the start of the current part, stopped.</summary>
    public void Reset()
    {
        _endsAt = null;
        StopTicker();
        Length = _left = Settings.LengthOf(Phase);
        Notify();
    }

    /// <summary>Moves on to the next part without counting this one as done.</summary>
    public void Skip()
    {
        if (Phase == PomodoroPhase.LongBreak) RoundsDone = 0;
        SwitchTo(Next);
    }

    /// <summary>Jumps straight to a part (stopped, full length). The round count is kept.</summary>
    public void SwitchTo(PomodoroPhase phase)
    {
        Phase = phase;
        Reset();
    }

    /// <summary>Adds (or with a negative number, takes off) minutes from the current part. Never below zero.</summary>
    public void AddMinutes(int minutes)
    {
        var change = TimeSpan.FromMinutes(minutes);
        if (Remaining + change < TimeSpan.Zero) change = -Remaining;
        Length += change;
        if (_endsAt is { } end) _endsAt = end + change;
        else _left += change;
        Notify();
        Tick();
    }

    /// <summary>
    /// Uses new settings. A part that hasn't started yet takes the new length right away; one that's
    /// underway keeps going and the new length applies from the next part.
    /// </summary>
    public void Apply(PomodoroSettings settings)
    {
        var wasUntouched = !InProgress;
        Settings = settings.Clamped();
        if (wasUntouched) Length = _left = Settings.LengthOf(Phase);
        Notify();
    }

    /// <summary>Puts back today's focus count saved on this device.</summary>
    public void RestoreCount(DateTime day, int count)
    {
        _countedOn = day.Date;
        _focusToday = count;
        Notify();
    }

    /// <summary>Checks whether time's up. Runs on its own while the timer runs; public so tests can drive it.</summary>
    public void Tick()
    {
        if (!IsRunning) return;
        if (Remaining > TimeSpan.Zero)
        {
            // Only re-draw when the seconds shown would change.
            var second = (long)Math.Ceiling(Remaining.TotalSeconds);
            if (second != _shownSecond) Notify();
            return;
        }

        var finished = Phase;
        if (finished == PomodoroPhase.Focus)
        {
            if (_countedOn != Today) (_countedOn, _focusToday) = (Today, 0);
            _focusToday++;
        }
        var next = Next;
        if (finished == PomodoroPhase.Focus) RoundsDone++;
        if (finished == PomodoroPhase.LongBreak) RoundsDone = 0;

        SwitchTo(next);
        Finished?.Invoke(finished);
        if (Settings.AutoStartNext) Start();
    }

    private void StopTicker()
    {
        _ticker?.Dispose();
        _ticker = null;
    }

    private void Notify()
    {
        _shownSecond = (long)Math.Ceiling(Remaining.TotalSeconds);
        Changed?.Invoke();
    }

    public void Dispose() => StopTicker();
}
