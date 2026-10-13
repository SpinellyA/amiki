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
    private const string HistoryKey = "amiki.pomodoro.history";
    private const int HistoryDays = 90;

    private sealed record DayCount(DateTime Day, int Count);

    private bool _loaded;
    private Dictionary<string, int> _history = [];

    /// <summary>Loads saved settings into the timer once, and from then on saves the count as rounds finish.</summary>
    public void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        if (Read<PomodoroSettings>(SettingsKey) is { } settings) timer.Apply(settings);
        if (Read<DayCount>(CountKey) is { } count) timer.RestoreCount(count.Day, count.Count);
        _history = Read<Dictionary<string, int>>(HistoryKey) ?? [];
        // Rounds done today before the history existed still count.
        if (timer.FocusSessionsToday > 0) _history.TryAdd(Key(DateTime.Today), timer.FocusSessionsToday);
        timer.Finished += OnFinished;
    }

    /// <summary>Focus rounds finished on this device on that day (kept for the last 90 days).</summary>
    public int FocusRoundsOn(DateTime day)
    {
        EnsureLoaded();
        return _history.GetValueOrDefault(Key(day));
    }

    private void OnFinished(PomodoroPhase finished)
    {
        Write(CountKey, new DayCount(DateTime.Today, timer.FocusSessionsToday));
        if (finished != PomodoroPhase.Focus) return;

        _history[Key(DateTime.Today)] = timer.FocusSessionsToday;
        var oldest = Key(DateTime.Today.AddDays(-HistoryDays));
        foreach (var old in _history.Keys.Where(k => string.CompareOrdinal(k, oldest) < 0).ToList()) _history.Remove(old);
        Write(HistoryKey, _history);
    }

    private static string Key(DateTime day) => day.ToString("yyyy-MM-dd");

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
