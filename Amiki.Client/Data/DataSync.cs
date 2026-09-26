using Microsoft.JSInterop;

namespace Amiki.Data;

/// <summary>
/// Gets every module's data on screen and keeps it fresh.
///
/// Opening: if this device has a saved copy, it's shown immediately (so the installed app opens
/// instantly, offline or while a sleeping server wakes up), then refreshed from the server in the
/// background. With no saved copy, it waits for the server.
///
/// Staying fresh: reloads when you come back to the tab and every 30 seconds, which is what gets
/// your phone captures onto the dashboard at home. A reload is skipped while your own changes are
/// still waiting to be sent, so the server's older copy never overwrites something you just did.
///
/// The saved copy is rewritten shortly after any change, including ones made offline, so reopening
/// the app offline still shows them.
/// </summary>
public sealed class DataSync : IAsyncDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    private readonly IReadOnlyList<IRemoteStore> _stores;
    private readonly SyncQueue _queue;
    private readonly Api _api;
    private readonly IJSRuntime _js;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private Task? _initial;
    private bool _restored;
    private PeriodicTimer? _timer;
    private DotNetObjectReference<DataSync>? _self;
    private CancellationTokenSource? _saveDebounce;

    public DataSync(IEnumerable<IRemoteStore> stores, SyncQueue queue, Api api, IJSRuntime js)
    {
        _stores = stores.ToList();
        _queue = queue;
        _api = api;
        _js = js;
        foreach (var store in _stores) store.Changed += ScheduleSave;
    }

    /// <summary>Set while the screen shows the device's saved copy because the server couldn't be reached yet.</summary>
    public DateTime? ShowingSavedCopyFrom { get; private set; }

    public event Action? StatusChanged;

    public Task EnsureLoadedAsync() => _initial ??= StartAsync();

    /// <summary>After a failed first load, lets the next <see cref="EnsureLoadedAsync"/> try again.</summary>
    public void Reset()
    {
        if (_initial is { IsCompleted: true, IsCompletedSuccessfully: false }) _initial = null;
    }

    private async Task StartAsync()
    {
        // Changes saved offline last time go out first; once they land, reload once so they show.
        // (Only then: reloading after every save would swap out the objects an Undo still holds.)
        if (!_restored)
        {
            _restored = true;
            if (_queue.RestoreSaved() > 0)
            {
                void OnDrained()
                {
                    _queue.Drained -= OnDrained;
                    _ = RefreshQuietlyAsync();
                }
                _queue.Drained += OnDrained;
            }
        }

        if (_api.CachedAt is { } savedAt && await TryLoadSavedCopyAsync())
        {
            ShowingSavedCopyFrom = savedAt;
            StatusChanged?.Invoke();
            _ = RefreshQuietlyAsync();
        }
        else
        {
            await RefreshAsync(force: true);
        }

        _self = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("amiki.onTabVisible", _self, nameof(OnTabVisible));
        _ = PollAsync();
    }

    private async Task<bool> TryLoadSavedCopyAsync()
    {
        _api.CacheOnly = true;
        try
        {
            await Task.WhenAll(_stores.Select(s => s.LoadAsync(CancellationToken.None)));
            return true;
        }
        catch
        {
            return false; // incomplete copy (e.g. a module added since): load from the server instead
        }
        finally
        {
            _api.CacheOnly = false;
        }
    }

    /// <summary>Called from JavaScript when the tab becomes visible again.</summary>
    [JSInvokable]
    public Task OnTabVisible() => RefreshQuietlyAsync();

    private async Task RefreshQuietlyAsync()
    {
        try { await RefreshAsync(); }
        catch { /* offline: the timer tries again */ }
    }

    private async Task PollAsync()
    {
        _timer = new PeriodicTimer(Interval);
        while (await _timer.WaitForNextTickAsync()) await RefreshQuietlyAsync();
    }

    // One refresh at a time: overlapping ones could finish out of order and let an older copy of
    // the data overwrite a newer one (e.g. the startup load landing after the post-sync reload).
    private async Task RefreshAsync(bool force = false)
    {
        await _gate.WaitAsync();
        try
        {
            if (!force && _queue.Pending > 0) return;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            await Task.WhenAll(_stores.Select(s => s.LoadAsync(cts.Token)));
            _api.MarkCacheFresh();
            if (ShowingSavedCopyFrom is not null)
            {
                ShowingSavedCopyFrom = null;
                StatusChanged?.Invoke();
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    // Many changes arrive in bursts (a load touches every store); save the copy once they settle.
    private void ScheduleSave()
    {
        _saveDebounce?.Cancel();
        var cts = _saveDebounce = new CancellationTokenSource();
        _ = Task.Delay(500, cts.Token).ContinueWith(t =>
        {
            if (t.IsCanceled || _api.CacheOnly) return;
            foreach (var store in _stores)
                foreach (var (path, data) in store.Snapshot())
                    _api.SaveCache(path, data);
        }, TaskScheduler.Default);
    }

    public ValueTask DisposeAsync()
    {
        _timer?.Dispose();
        _self?.Dispose();
        return ValueTask.CompletedTask;
    }
}
