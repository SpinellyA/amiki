using Amiki.Modules.Pomodoro;

namespace Amiki.Tests.Pomodoro;

public class PomodoroTimerTests
{
    private readonly FakeClock _clock = new();
    private readonly PomodoroTimer _timer;

    public PomodoroTimerTests() => _timer = new PomodoroTimer(_clock);

    private void Run(TimeSpan time)
    {
        _clock.Advance(time);
        _timer.Tick();
    }

    [Fact]
    public void Starts_on_a_full_focus_round_stopped()
    {
        Assert.Equal(PomodoroPhase.Focus, _timer.Phase);
        Assert.Equal(TimeSpan.FromMinutes(25), _timer.Remaining);
        Assert.False(_timer.IsRunning);
        Assert.False(_timer.InProgress);
    }

    [Fact]
    public void Counts_down_from_the_clock_while_running()
    {
        _timer.Start();
        Run(TimeSpan.FromMinutes(10));

        Assert.Equal(TimeSpan.FromMinutes(15), _timer.Remaining);
        Assert.Equal(0.4, _timer.Progress, precision: 3);
    }

    [Fact]
    public void Pausing_stops_the_countdown()
    {
        _timer.Start();
        Run(TimeSpan.FromMinutes(5));
        _timer.Pause();
        Run(TimeSpan.FromMinutes(30));

        Assert.Equal(TimeSpan.FromMinutes(20), _timer.Remaining);
        Assert.True(_timer.InProgress);
    }

    [Fact]
    public void A_finished_focus_round_counts_and_moves_to_a_short_break()
    {
        PomodoroPhase? finished = null;
        _timer.Finished += phase => finished = phase;

        _timer.Start();
        Run(TimeSpan.FromMinutes(25));

        Assert.Equal(PomodoroPhase.Focus, finished);
        Assert.Equal(PomodoroPhase.ShortBreak, _timer.Phase);
        Assert.Equal(1, _timer.RoundsDone);
        Assert.Equal(1, _timer.FocusSessionsToday);
        Assert.False(_timer.IsRunning); // waits for you unless auto-start is on
    }

    [Fact]
    public void The_last_round_of_a_set_is_followed_by_the_long_break_then_a_new_set()
    {
        _timer.Apply(new PomodoroSettings(FocusMinutes: 1, ShortBreakMinutes: 1, LongBreakMinutes: 2, RoundsBeforeLongBreak: 2));

        _timer.Start(); Run(TimeSpan.FromMinutes(1)); // focus 1
        _timer.Start(); Run(TimeSpan.FromMinutes(1)); // short break
        _timer.Start(); Run(TimeSpan.FromMinutes(1)); // focus 2
        Assert.Equal(PomodoroPhase.LongBreak, _timer.Phase);

        _timer.Start(); Run(TimeSpan.FromMinutes(2));
        Assert.Equal(PomodoroPhase.Focus, _timer.Phase);
        Assert.Equal(0, _timer.RoundsDone);
        Assert.Equal(2, _timer.FocusSessionsToday);
    }

    [Fact]
    public void Auto_start_carries_straight_on_into_the_next_part()
    {
        _timer.Apply(new PomodoroSettings(AutoStartNext: true));
        _timer.Start();
        Run(TimeSpan.FromMinutes(25));

        Assert.Equal(PomodoroPhase.ShortBreak, _timer.Phase);
        Assert.True(_timer.IsRunning);
    }

    [Fact]
    public void Skipping_moves_on_without_counting_the_round()
    {
        _timer.Start();
        _timer.Skip();

        Assert.Equal(PomodoroPhase.ShortBreak, _timer.Phase);
        Assert.Equal(0, _timer.RoundsDone);
        Assert.Equal(0, _timer.FocusSessionsToday);
        Assert.False(_timer.IsRunning);
    }

    [Fact]
    public void Adding_minutes_extends_the_current_part()
    {
        _timer.Start();
        Run(TimeSpan.FromMinutes(20));
        _timer.AddMinutes(5);

        Assert.Equal(TimeSpan.FromMinutes(10), _timer.Remaining);
        Assert.Equal(TimeSpan.FromMinutes(30), _timer.Length);
    }

    [Fact]
    public void Taking_off_more_minutes_than_are_left_stops_at_zero_and_finishes()
    {
        _timer.Start();
        _timer.AddMinutes(-60);

        Assert.Equal(PomodoroPhase.ShortBreak, _timer.Phase);
        Assert.Equal(1, _timer.RoundsDone);
    }

    [Fact]
    public void New_settings_apply_now_to_an_untouched_part_but_wait_for_the_next_one_otherwise()
    {
        _timer.Apply(new PomodoroSettings(FocusMinutes: 50));
        Assert.Equal(TimeSpan.FromMinutes(50), _timer.Remaining);

        _timer.Start();
        Run(TimeSpan.FromMinutes(10));
        _timer.Apply(new PomodoroSettings(FocusMinutes: 15));
        Assert.Equal(TimeSpan.FromMinutes(40), _timer.Remaining);
    }

    [Fact]
    public void Settings_are_pulled_into_a_usable_range()
    {
        _timer.Apply(new PomodoroSettings(FocusMinutes: 0, RoundsBeforeLongBreak: 99));

        Assert.Equal(1, _timer.Settings.FocusMinutes);
        Assert.Equal(PomodoroSettings.MaxRounds, _timer.Settings.RoundsBeforeLongBreak);
    }

    [Fact]
    public void Yesterdays_count_does_not_carry_over()
    {
        _timer.RestoreCount(_clock.GetLocalNow().Date.AddDays(-1), 6);

        Assert.Equal(0, _timer.FocusSessionsToday);
    }

    [Theory]
    [InlineData(1500, "25:00")]
    [InlineData(59.2, "1:00")]
    [InlineData(0.4, "0:01")]
    [InlineData(0, "0:00")]
    [InlineData(3900, "1:05:00")]
    public void Clock_shows_minutes_and_seconds_rounded_up(double seconds, string expected)
    {
        Assert.Equal(expected, PomodoroFormat.Clock(TimeSpan.FromSeconds(seconds)));
    }
}
