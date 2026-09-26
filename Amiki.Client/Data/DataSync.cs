using Microsoft.JSInterop;

namespace Amiki.Data;

/// <summary>
/// Loads every module's data at startup, then keeps it fresh: whenever you come back to the
/// tab (open the laptop, switch to the dashboard) and every 30 seconds while it's open. That's
/// what gets what you captured on your phone at school onto the second monitor at home.
///
/// A refresh is skipped while your own changes are still waiting to be sent, so the server's
/// older copy never overwrites something you just did.
/// </summary>
public sealed class DataSync(IEnumerable<IRemoteStore> stores, SyncQueue queue, IJSRuntime js) : IAsyncDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    private Task? _initial;
    private bool _restored;
    private PeriodicTimer? _timer;
    private DotNetObjectReference<DataSync>? _self;

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
            if (queue.RestoreSaved() > 0)
            {
                void OnDrained()
                {
                    queue.Drained -= OnDrained;
                    _ = RefreshQuietlyAsync();
                }
                queue.Drained += OnDrained;
            }
        }
        await RefreshAsync(force: true);
        _self = DotNetObjectReference.Create(this);
        await js.InvokeVoidAsync("amiki.onTabVisible", _self, nameof(OnTabVisible));
        _ = PollAsync();
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
        while (await _timer.WaitForNextTickAsync())
        {
            try { await RefreshAsync(); }
            catch { /* offline: the next tick tries again */ }
        }
    }

    // One refresh at a time: overlapping ones could finish out of order and let an older copy of
    // the data overwrite a newer one (e.g. the startup load landing after the post-sync reload).
    private readonly SemaphoreSlim _gate = new(1, 1);

    private async Task RefreshAsync(bool force = false)
    {
        await _gate.WaitAsync();
        try
        {
            if (!force && queue.Pending > 0) return;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            await Task.WhenAll(stores.Select(s => s.LoadAsync(cts.Token)));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _timer?.Dispose();
        _self?.Dispose();
        await Task.CompletedTask;
    }
}
