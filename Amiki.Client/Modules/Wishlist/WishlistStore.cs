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

    /// <summary>Saving for: in your order. Want: cheapest first. Bought and Skipped: newest first.</summary>
    public IEnumerable<WishItem> InStatus(WishStatus status)
    {
        var items = _items.Where(i => i.Status == status);
        return status switch
        {
            WishStatus.Saving => items.OrderBy(i => i.Rank).ThenBy(i => i.CreatedAt),
            WishStatus.Want => items.OrderBy(i => i.Price),
            _ => items.OrderByDescending(i => i.BoughtAt ?? i.CreatedAt),
        };
    }

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

    /// <summary>Saves a move of money between wishlist money and an item: both sides, together.</summary>
    public void Apply(WishlistChange change)
    {
        if (change.Fund.Amount != Fund.Amount || !HasFund) SaveFund(change.Fund);
        Upsert(change.Item);
    }

    /// <summary>Saves several items' new order at once (only the ones that actually moved).</summary>
    public void Reorder(IReadOnlyList<WishItem> inOrder)
    {
        for (var rank = 0; rank < inOrder.Count; rank++)
        {
            if (Get(inOrder[rank].Id) is not { } current || current.Rank == rank) continue;
            var moved = current.Clone();
            moved.Rank = rank;
            Upsert(moved);
        }
    }
}
