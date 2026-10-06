using Amiki.Data;

namespace Amiki.Modules.Tasks;

/// <summary>
/// Everything outside the module talks to this class. Changes apply to the local list at once
/// (instant UI) and are queued to the API; <see cref="LoadAsync"/> refreshes from the server.
/// </summary>
public sealed class TaskStore(Api api, SyncQueue sync) : IRemoteStore
{
    private const string Path = "tasks";
    private List<TaskItem> _items = [];

    public event Action? Changed;

    /// <summary>Raised when you tick a task off or back on (other modules react, e.g. an idea finishing).</summary>
    public event Action<TaskItem>? Toggled;

    public IReadOnlyList<TaskItem> All => _items;

    public IEnumerable<(string Path, object Data)> Snapshot() => [(Path, _items)];

    public IEnumerable<string> Projects =>
        _items.Select(t => t.Project).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).Order();

    public async Task LoadAsync(CancellationToken ct)
    {
        _items = await api.GetAllAsync<TaskItem>(Path, ct);
        Changed?.Invoke();
    }

    public void Upsert(TaskItem item)
    {
        var i = _items.FindIndex(t => t.Id == item.Id);
        if (i >= 0) _items[i] = item;
        else _items.Add(item);
        sync.Put(Path, item.Id, item);
        Changed?.Invoke();
    }

    public void Toggle(TaskItem item)
    {
        // A refresh may have replaced the object the caller holds (e.g. an Undo); act on the current one.
        item = _items.Find(t => t.Id == item.Id) ?? item;
        item.CompletedAt = item.IsDone ? null : DateTime.Now;
        sync.Put(Path, item.Id, item);
        Changed?.Invoke();
        Toggled?.Invoke(item);
    }

    public IReadOnlyList<TaskItem> ForIdea(Guid ideaId) => _items.Where(t => t.IdeaId == ideaId).ToList();

    public void Delete(TaskItem item)
    {
        if (_items.RemoveAll(t => t.Id == item.Id) == 0) return;
        sync.Delete(Path, item.Id);
        Changed?.Invoke();
    }
}
