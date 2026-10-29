namespace Amiki.Domain.Wishlist;

/// <summary>
/// The wishlist against the money set aside for it, with your full <see cref="Balance"/> alongside.
///
/// Set aside = the pool (<see cref="Fund"/>) plus what's put toward open items. That's only ever a
/// label on your balance, so if spending elsewhere leaves less in your accounts than you've set
/// aside, only what's really there counts (<see cref="Pool"/>).
/// </summary>
public sealed class WishlistSummary(IEnumerable<WishItem> items, decimal fund, decimal balance)
{
    private readonly IReadOnlyList<WishItem> _items = [.. items];

    /// <summary>Wishlist money not put toward anything, as recorded.</summary>
    public decimal Fund { get; } = fund;

    /// <summary>Your whole balance, shown but not spent from.</summary>
    public decimal Balance { get; } = balance;

    /// <summary>Money put toward open items.</summary>
    public decimal SavedInItems => _items.Where(i => i.IsOpen).Sum(i => i.Saved);

    /// <summary>Everything set aside for the wishlist: the pool plus what's put toward items.</summary>
    public decimal SetAside => Fund + SavedInItems;

    /// <summary>More is set aside than your accounts hold right now.</summary>
    public bool Overcommitted => SetAside > Math.Max(0, Balance);

    /// <summary>The pool you can really count on: never more than what your balance still covers.</summary>
    public decimal Pool => Math.Clamp(Math.Max(0, Balance) - SavedInItems, 0, Fund);

    public decimal SavingFor => Total(WishStatus.Saving);
    public decimal Wanted => Total(WishStatus.Want);

    /// <summary>The pool after finishing everything you're saving for.</summary>
    public Affordability AfterSaving => new(Still(WishStatus.Saving), Pool);

    /// <summary>The pool after finishing everything still open (saving for and wanted).</summary>
    public Affordability AfterEverything => new(Still(WishStatus.Saving) + Still(WishStatus.Want), Pool);

    public int OpenCount => _items.Count(i => i.IsOpen);

    /// <summary>The things you're saving for, in your order.</summary>
    public IReadOnlyList<WishItem> SavingInOrder =>
        [.. _items.Where(i => i.Status == WishStatus.Saving).OrderBy(i => i.Rank).ThenBy(i => i.CreatedAt)];

    /// <summary>How many of your saving-for items, top first, you could finish with the pool right now.</summary>
    public int TopAffordable
    {
        get
        {
            var (count, left) = (0, Pool);
            foreach (var item in SavingInOrder)
            {
                if (item.Remaining > left) break;
                (count, left) = (count + 1, left - item.Remaining);
            }
            return count;
        }
    }

    /// <summary>The open item closest to done (most of its price put toward it), for the dashboard.</summary>
    public WishItem? Closest =>
        _items.Where(i => i.Status == WishStatus.Saving).OrderByDescending(i => i.Progress).ThenBy(i => i.Rank).FirstOrDefault();

    private decimal Total(WishStatus status) => _items.Where(i => i.Status == status).Sum(i => i.Price);

    private decimal Still(WishStatus status) => _items.Where(i => i.Status == status).Sum(i => i.Remaining);
}
