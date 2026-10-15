using Amiki.Components;
using Amiki.Data;

namespace Amiki.Modules.Review;

/// <summary>Your weekly reflections, one per week. Saved as you type (the sync queue merges keystrokes).</summary>
public sealed class WeeklyNoteStore(Api api, SyncQueue sync) : IRemoteStore
{
    private const string Path = "weekly-notes";
    private List<WeeklyNote> _items = [];

    public event Action? Changed;

    public IEnumerable<(string Path, object Data)> Snapshot() => [(Path, _items)];

    /// <summary>Weeks you wrote something for, newest first.</summary>
    public IEnumerable<WeeklyNote> Written => _items.Where(n => !n.IsEmpty).OrderByDescending(n => n.WeekStart);

    /// <summary>The week's note, or a blank one (not saved until you type).</summary>
    public WeeklyNote For(WeekRange week) =>
        _items.FirstOrDefault(n => n.WeekStart == week.Start) ?? WeeklyNote.For(week.Start);

    public async Task LoadAsync(CancellationToken ct)
    {
        _items = await api.GetAllAsync<WeeklyNote>(Path, ct);
        Changed?.Invoke();
    }

    public void Save(WeeklyNote note)
    {
        note.UpdatedAt = DateTime.Now;
        var i = _items.FindIndex(n => n.Id == note.Id);
        if (i >= 0) _items[i] = note;
        else _items.Add(note);
        sync.Put(Path, note.Id, note);
        Changed?.Invoke();
    }
}
