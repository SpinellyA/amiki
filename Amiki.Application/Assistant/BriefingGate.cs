using System.Collections.Concurrent;

namespace Amiki.Application.Assistant;

/// <summary>
/// Who may ask the model for today's note right now. One per app (a singleton), per person:
///   - one note written at a time: when the phone and PC open the dashboard together, the second
///     waits and gets the first one's note instead of asking the model again;
///   - after hitting the free tier's limit, no asking again on every dashboard load for a while.
/// </summary>
public sealed class BriefingGate(TimeProvider time)
{
    public static readonly TimeSpan PauseAfterLimit = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _writing = new();
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _quietUntil = new();

    public bool IsPaused(Guid person) => time.GetUtcNow() < _quietUntil.GetValueOrDefault(person);

    public void PauseAfterRateLimit(Guid person) => _quietUntil[person] = time.GetUtcNow() + PauseAfterLimit;

    /// <summary>Waits for this person's turn to write. Dispose the result to let the next one in.</summary>
    public async Task<IDisposable> WaitToWriteAsync(Guid person, CancellationToken ct)
    {
        var gate = _writing.GetOrAdd(person, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        return new Turn(gate);
    }

    private sealed class Turn(SemaphoreSlim gate) : IDisposable
    {
        public void Dispose() => gate.Release();
    }
}
