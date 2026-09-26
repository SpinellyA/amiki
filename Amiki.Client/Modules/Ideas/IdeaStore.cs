using Amiki.Data;

namespace Amiki.Modules.Ideas;

public sealed class IdeaStore(Api api, SyncQueue sync) : IRemoteStore
{
    private const string Path = "ideas";
    private List<Idea> _items = [];

    public event Action? Changed;

    public IReadOnlyList<Idea> All => _items;

    public IEnumerable<(string Path, object Data)> Snapshot() => [(Path, _items)];

    public IEnumerable<string> Tags => _items.SelectMany(i => i.Tags).Distinct().Order();

    public async Task LoadAsync(CancellationToken ct)
    {
        _items = await api.GetAllAsync<Idea>(Path, ct);
        Changed?.Invoke();
    }

    public void Upsert(Idea idea)
    {
        var i = _items.FindIndex(x => x.Id == idea.Id);
        if (i >= 0) _items[i] = idea;
        else _items.Add(idea);
        sync.Put(Path, idea.Id, idea);
        Changed?.Invoke();
    }

    public void Remove(Idea idea)
    {
        if (_items.RemoveAll(x => x.Id == idea.Id) == 0) return;
        sync.Delete(Path, idea.Id);
        Changed?.Invoke();
    }
}
