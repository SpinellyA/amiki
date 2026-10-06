using System.Text.Json;
using Microsoft.JSInterop;

namespace Amiki.Modules.Pomodoro;

/// <summary>
/// Keeps your timer settings and today's focus count on this device (the browser's storage), so a
/// reload doesn't reset them. Not synced: each device has its own timer.
/// </summary>
public sealed class PomodoroPrefs(IJSRuntime js, PomodoroTimer timer)
{
    private const string SettingsKey = "amiki.pomodoro.settings";
    private const string CountKey = "amiki.pomodoro.today";

    private sealed record DayCount(DateTime Day, int Count);

    private bool _loaded;

    /// <summary>Loads saved settings into the timer once, and from then on saves the count as rounds finish.</summary>
    public void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        if (Read<PomodoroSettings>(SettingsKey) is { } settings) timer.Apply(settings);
        if (Read<DayCount>(CountKey) is { } count) timer.RestoreCount(count.Day, count.Count);
        timer.Finished += _ => Write(CountKey, new DayCount(DateTime.Today, timer.FocusSessionsToday));
    }

    public void Save(PomodoroSettings settings)
    {
        timer.Apply(settings);
        Write(SettingsKey, timer.Settings);
    }

    // Storage can be missing or full (private windows, some embedded browsers): the timer still
    // works, it just forgets on reload.
    private T? Read<T>(string key) where T : class
    {
        try
        {
            var json = ((IJSInProcessRuntime)js).Invoke<string?>("localStorage.getItem", key);
            return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return null;
        }
    }

    private void Write<T>(string key, T value)
    {
        try { ((IJSInProcessRuntime)js).InvokeVoid("localStorage.setItem", key, JsonSerializer.Serialize(value)); }
        catch { /* see Read */ }
    }
}
