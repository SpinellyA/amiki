using Amiki.Data;
using Amiki.Modules.Tasks;

namespace Amiki.Modules.Ideas;

/// <summary>
/// Your ideas. Also keeps each idea's status in step with the tasks made from it: ticking off the
/// last open one marks the idea done, and reopening one puts it back in progress.
/// </summary>
public sealed class IdeaStore : IRemoteStore, IDisposable
{
    private const string Path = "ideas";

    private readonly Api _api;
    private readonly SyncQueue _sync;
    private readonly TaskStore _tasks;
    private List<Idea> _items = [];

    public IdeaStore(Api api, SyncQueue sync, TaskStore tasks)
    {
        _api = api;
        _sync = sync;
        _tasks = tasks;
        _tasks.Toggled += OnTaskToggled;
    }

    public event Action? Changed;

    public IReadOnlyList<Idea> All => _items;

    public IEnumerable<(string Path, object Data)> Snapshot() => [(Path, _items)];

    public IEnumerable<string> Tags => _items.SelectMany(i => i.Tags).Distinct().Order();

    public Idea? Get(Guid id) => _items.FirstOrDefault(i => i.Id == id);

    public async Task LoadAsync(CancellationToken ct)
    {
        _items = await _api.GetAllAsync<Idea>(Path, ct);
        Changed?.Invoke();
    }

    public void Upsert(Idea idea)
    {
        var i = _items.FindIndex(x => x.Id == idea.Id);
        if (i >= 0) _items[i] = idea;
        else _items.Add(idea);
        _sync.Put(Path, idea.Id, idea);
        Changed?.Invoke();
    }

    /// <summary>Moves an idea to another column. Works on the current copy, in case a refresh replaced the caller's.</summary>
    public void SetStatus(Idea idea, IdeaStatus status)
    {
        if (Get(idea.Id) is not { } current || current.Status == status) return;
        var moved = current.Clone();
        moved.Status = status;
        Upsert(moved);
    }

    public void Remove(Idea idea)
    {
        if (_items.RemoveAll(x => x.Id == idea.Id) == 0) return;
        _sync.Delete(Path, idea.Id);
        Changed?.Invoke();
    }

    private void OnTaskToggled(TaskItem task)
    {
        if (task.IdeaId is not { } ideaId || Get(ideaId) is not { } idea) return;
        if (Idea.StatusFromTasks(_tasks.ForIdea(ideaId)) is { } status) SetStatus(idea, status);
    }

    public void Dispose() => _tasks.Toggled -= OnTaskToggled;
}
