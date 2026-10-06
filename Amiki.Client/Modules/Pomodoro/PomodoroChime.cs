using Microsoft.JSInterop;

namespace Amiki.Modules.Pomodoro;

/// <summary>Plays a short sound when a focus round or break runs out (if the chime is on).</summary>
public sealed class PomodoroChime : IDisposable
{
    private readonly IJSRuntime _js;
    private readonly PomodoroTimer _timer;

    public PomodoroChime(IJSRuntime js, PomodoroTimer timer)
    {
        _js = js;
        _timer = timer;
        _timer.Finished += OnFinished;
    }

    private async void OnFinished(PomodoroPhase finished)
    {
        if (!_timer.Settings.Chime) return;
        // Rising notes back to work, falling notes into a break.
        try { await _js.InvokeVoidAsync("amiki.chime", finished == PomodoroPhase.Focus ? "down" : "up"); }
        catch (JSException) { /* no sound available; the timer carries on */ }
    }

    public void Dispose() => _timer.Finished -= OnFinished;
}
