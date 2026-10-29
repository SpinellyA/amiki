using Amiki.Data;

namespace Amiki.Modules.Wishlist;

/// <summary>Your wishlist and the money set aside for it, on this device, kept in sync with the server.</summary>
public sealed class WishlistStore(Api api, SyncQueue sync) : IRemoteStore
{
    private const string Path = "wishlist";
    private const string FundPath = "wishlist-fund";

    private List<WishItem> _items = [];

    public event Action? Changed;

    public IReadOnlyList<WishItem> All => _items;

    /// <summary>Wishlist money. Zero until you set some aside.</summary>
    public WishlistFund Fund { get; private set; } = new();

    /// <summary>Whether wishlist money was ever set (so the page can invite you to).</summary>
    public bool HasFund { get; private set; }

    public IEnumerable<WishItem> InStatus(WishStatus status) =>
        _items.Where(i => i.Status == status).OrderBy(i => i.IsOpen ? i.Price : 0).ThenByDescending(i => i.BoughtAt ?? i.CreatedAt);

    public IEnumerable<(string Path, object Data)> Snapshot() =>
        [(Path, _items), (FundPath, HasFund ? new List<WishlistFund> { Fund } : [])];

    public WishItem? Get(Guid id) => _items.FirstOrDefault(i => i.Id == id);

    public async Task LoadAsync(CancellationToken ct)
    {
        var items = api.GetAllAsync<WishItem>(Path, ct);
        var funds = api.GetAllAsync<WishlistFund>(FundPath, ct);
        _items = await items;
        var fund = (await funds).FirstOrDefault();
        (Fund, HasFund) = (fund ?? new WishlistFund(), fund is not null);
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

    public void SaveFund(WishlistFund fund)
    {
        (Fund, HasFund) = (fund, true);
        sync.Put(FundPath, fund.Id, fund);
        Changed?.Invoke();
    }
}
