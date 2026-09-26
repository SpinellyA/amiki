using Amiki.Data;

namespace Amiki.Modules.Inbox;

public sealed class InboxStore(Api api, SyncQueue sync) : IRemoteStore
{
    private const string Path = "inbox";
    private List<InboxItem> _items = [];

    public event Action? Changed;

    public IReadOnlyList<InboxItem> All => _items;

    public IEnumerable<(string Path, object Data)> Snapshot() => [(Path, _items)];

    public IEnumerable<InboxItem> Newest => _items.OrderByDescending(i => i.CapturedAt);

    public async Task LoadAsync(CancellationToken ct)
    {
        _items = await api.GetAllAsync<InboxItem>(Path, ct);
        Changed?.Invoke();
    }

    public void Capture(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var item = new InboxItem { Text = text.Trim() };
        _items.Add(item);
        sync.Put(Path, item.Id, item);
        Changed?.Invoke();
    }

    public void Remove(InboxItem item)
    {
        if (_items.RemoveAll(i => i.Id == item.Id) == 0) return;
        sync.Delete(Path, item.Id);
        Changed?.Invoke();
    }

    public void Restore(InboxItem item)
    {
        if (_items.Any(i => i.Id == item.Id)) return;
        _items.Add(item);
        sync.Put(Path, item.Id, item);
        Changed?.Invoke();
    }
}
