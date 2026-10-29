using Amiki.Data;

namespace Amiki.Modules.Wishlist;

/// <summary>Your wishlist on this device, kept in sync with the server.</summary>
public sealed class WishlistStore(Api api, SyncQueue sync) : IRemoteStore
{
    private const string Path = "wishlist";

    private List<WishItem> _items = [];

    public event Action? Changed;

    public IReadOnlyList<WishItem> All => _items;

    public IEnumerable<WishItem> InStatus(WishStatus status) =>
        _items.Where(i => i.Status == status).OrderBy(i => i.IsOpen ? i.Price : 0).ThenByDescending(i => i.BoughtAt ?? i.CreatedAt);

    public IEnumerable<(string Path, object Data)> Snapshot() => [(Path, _items)];

    public WishItem? Get(Guid id) => _items.FirstOrDefault(i => i.Id == id);

    public async Task LoadAsync(CancellationToken ct)
    {
        _items = await api.GetAllAsync<WishItem>(Path, ct);
        Changed?.Invoke();
    }

    public void Upsert(WishItem item)
    {
        var i = _items.FindIndex(x => x.Id == item.Id);
        if (i >= 0) _items[i] = item;
        else _items.Add(item);
        sync.Put(Path, item.Id, item);
        Changed?.Invoke();
    }

    public void Remove(WishItem item)
    {
        if (_items.RemoveAll(x => x.Id == item.Id) == 0) return;
        sync.Delete(Path, item.Id);
        Changed?.Invoke();
    }
}
