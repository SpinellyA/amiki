namespace Amiki.Modules.Clock;

/// <summary>Ticks twice a minute so the clock's "now" hand and "what's on" text keep up.</summary>
public sealed class MinuteTicker : IDisposable
{
    private readonly Timer _timer;

    public MinuteTicker() => _timer = new Timer(_ => Ticked?.Invoke(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

    public event Action? Ticked;

    public void Dispose() => _timer.Dispose();
}
