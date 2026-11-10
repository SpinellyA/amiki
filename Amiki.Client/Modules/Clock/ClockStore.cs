using Amiki.Data;

namespace Amiki.Modules.Clock;

/// <summary>Your time blocks on this device, kept in sync with the server.</summary>
public sealed class ClockStore(Api api, SyncQueue sync) : IRemoteStore
{
    private const string Path = "time-blocks";

    private List<TimeBlock> _blocks = [];

    public event Action? Changed;

    public IReadOnlyList<TimeBlock> All => _blocks;

    public IEnumerable<(string Path, object Data)> Snapshot() => [(Path, _blocks)];

    public async Task LoadAsync(CancellationToken ct)
    {
        _blocks = await api.GetAllAsync<TimeBlock>(Path, ct);
        Changed?.Invoke();
    }

    public DayPlan PlanFor(DateTime day) => new(day, _blocks);

    public void Save(TimeBlock block)
    {
        var i = _blocks.FindIndex(b => b.Id == block.Id);
        if (i >= 0) _blocks[i] = block;
        else _blocks.Add(block);
        sync.Put(Path, block.Id, block);
        Changed?.Invoke();
    }

    public void Remove(TimeBlock block)
    {
        if (_blocks.RemoveAll(b => b.Id == block.Id) == 0) return;
        sync.Delete(Path, block.Id);
        Changed?.Invoke();
    }
}
