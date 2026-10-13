using Amiki.Data;

namespace Amiki.Modules.Diary;

/// <summary>Your diary: one page per day, saved as you write.</summary>
public sealed class DiaryStore(Api api, SyncQueue sync) : IRemoteStore
{
    private const string Path = "diary";
    private List<DiaryEntry> _items = [];

    public event Action? Changed;

    public IEnumerable<(string Path, object Data)> Snapshot() => [(Path, _items)];

    /// <summary>Days with something on them, newest first.</summary>
    public IEnumerable<DiaryEntry> Written => _items.Where(e => !e.IsEmpty).OrderByDescending(e => e.Day);

    public int Streak => DiaryEntry.Streak(Written.Select(e => e.Day), DateTime.Today);

    /// <summary>The day's page, or a blank one (saved once you write or pick a mood).</summary>
    public DiaryEntry For(DateTime day) =>
        _items.FirstOrDefault(e => e.Day == day.Date) ?? DiaryEntry.For(day);

    public async Task LoadAsync(CancellationToken ct)
    {
        _items = await api.GetAllAsync<DiaryEntry>(Path, ct);
        Changed?.Invoke();
    }

    public void Save(DiaryEntry entry)
    {
        entry.UpdatedAt = DateTime.Now;
        var i = _items.FindIndex(e => e.Id == entry.Id);
        if (i >= 0) _items[i] = entry;
        else _items.Add(entry);
        sync.Put(Path, entry.Id, entry);
        Changed?.Invoke();
    }
}
