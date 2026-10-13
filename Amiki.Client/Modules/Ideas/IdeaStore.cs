using Amiki.Data;
using Amiki.Modules.Tasks;

namespace Amiki.Modules.Ideas;

/// <summary>
/// Your ideas. Also keeps each idea's status in step with the tasks made from it (ticking off the
/// last open one marks the idea done; reopening one puts it back in progress), and moves ideas that
/// have been done for a while into the archive.
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

    /// <summary>Ideas on the board (not archived).</summary>
    public IEnumerable<Idea> Active => _items.Where(i => !i.IsArchived);

    public IEnumerable<Idea> Archived => _items.Where(i => i.IsArchived).OrderByDescending(i => i.ArchivedAt);

    public IEnumerable<(string Path, object Data)> Snapshot() => [(Path, _items)];

    public IEnumerable<string> Tags => Active.SelectMany(i => i.Tags).Distinct().Order();

    public Idea? Get(Guid id) => _items.FirstOrDefault(i => i.Id == id);

    public async Task LoadAsync(CancellationToken ct)
    {
        _items = await _api.GetAllAsync<Idea>(Path, ct);
        ArchiveFinished();
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
        Upsert(current.WithStatus(status, DateTime.Now));
    }

    public void Archive(Idea idea)
    {
        if (Get(idea.Id) is { IsArchived: false } current) Upsert(current.Archived(DateTime.Now));
    }

    public void Restore(Idea idea)
    {
        if (Get(idea.Id) is { IsArchived: true } current) Upsert(current.Restored(DateTime.Now));
    }

    public void Remove(Idea idea)
    {
        if (_items.RemoveAll(x => x.Id == idea.Id) == 0) return;
        _sync.Delete(Path, idea.Id);
        Changed?.Invoke();
    }

    // Runs after every load, on whichever device is open; saving an archive is idempotent, so two
    // devices doing it at once is harmless.
    private void ArchiveFinished()
    {
        var now = DateTime.Now;
        for (var i = 0; i < _items.Count; i++)
        {
            if (!_items[i].ShouldAutoArchive(now)) continue;
            _items[i] = _items[i].Archived(now);
            _sync.Put(Path, _items[i].Id, _items[i]);
        }
    }

    private void OnTaskToggled(TaskItem task)
    {
        if (task.IdeaId is not { } ideaId || Get(ideaId) is not { } idea) return;
        if (Idea.StatusFromTasks(_tasks.ForIdea(ideaId)) is { } status) SetStatus(idea, status);
    }

    public void Dispose() => _tasks.Toggled -= OnTaskToggled;
}
